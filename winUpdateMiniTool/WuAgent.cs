using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Windows.Threading;
using sergiye.Common;
using winUpdateMiniTool.Common;
using WUApiLib;

namespace winUpdateMiniTool;

internal class WuAgent {
  public enum AgentOperation {
    None = 0,
    CheckingUpdates,
    PreparingCheck,
    DownloadingUpdates,
    InstallingUpdates,
    PreparingUpdates,
    RemovingUpdates,
    CancelingOperation
  }

  public enum RetCodes {
    InProgress = 2,
    Success = 1,
    Undefined = 0,
    AccessError = -1,
    Busy = -2,
    DownloadFailed = -3,
    InstallFailed = -4,
    NoUpdated = -5,
    InternalError = -6,
    FileNotFound = -7,
    Aborted = -99
  }

  private static WuAgent mInstance;
  public static readonly string MsUpdGuid = "7971f918-a847-4430-9279-4a52d1efe18d"; // Microsoft Update
  // public static string WinUpdUid = "9482f4b4-e343-43b6-b170-9a65bc822c77"; // Windows Update
  // public static string WsUsUid = "3da21691-e39d-4da6-8a4b-b43877bcb1b7"; // Windows Server Update Service
  // public static string DCatGuid = "8b24b027-1dee-babb-9a95-3517dfb9c552"; // DCat Fighting Prod - Windows Insider Program
  // public static string WinStorGuid = "117cab2d-82b1-4b5a-a08c-4d62dbee7782 "; // Windows Store
  // public static string WinStorDCat2Guid = "855e8a7c-ecb4-4ca3-b045-1dfa50104289"; // Windows Store (DCat Prod) - Insider Updates for Store Apps

  private readonly Dispatcher mDispatcher;
  private const string MMyOfflineSvc = "Offline Sync Service";
  private readonly UpdateDownloader mUpdateDownloader;
  private readonly UpdateInstaller mUpdateInstaller;
  private readonly UpdateServiceManager mUpdateServiceManager;
  private readonly UpdateSession mUpdateSession;
  public readonly string DlPath;
  public readonly List<MsUpdate> MHiddenUpdates = [];
  public readonly List<MsUpdate> MInstalledUpdates = [];
  public readonly List<MsUpdate> MPendingUpdates = [];
  public readonly StringCollection MServiceList = new();
  public readonly List<MsUpdate> MUpdateHistory = [];
  private UpdateCallback mCallback;
  private AgentOperation mCurOperation = AgentOperation.None;
  private WUApiLib.UpdateDownloader mDownloader;
  private IDownloadJob mDownloadJob;
  private IInstallationJob mInstalationJob;
  // Tracks whether the in-flight WUA installation job is an install or an uninstall, independent of
  // mCurOperation, which CancelOperations() overwrites with CancelingOperation before the job completes.
  private AgentOperation mInstalationOperation = AgentOperation.None;
  private AgentOperation mManualInstallOperation = AgentOperation.None;
  private IUpdateInstaller mInstaller;
  private bool mIsValid;
  private IUpdateService mOfflineService;
  private bool restoreLists;
  private ISearchJob mSearchJob;
  private IUpdateSearcher mUpdateSearcher;

  public WuAgent() {
    mInstance = this;
    mDispatcher = Dispatcher.CurrentDispatcher;
    mUpdateDownloader = new UpdateDownloader();
    mUpdateDownloader.Finished += DownloadsFinished;
    mUpdateDownloader.Progress += DownloadProgress;
    mUpdateInstaller = new UpdateInstaller();
    mUpdateInstaller.Finished += InstallFinished;
    mUpdateInstaller.Progress += InstallProgress;

    DlPath = Program.WrkPath + @"\Updates";

    WindowsUpdateAgentInfo info = new();
    var currentVersion = $"{info.GetInfo("ApiMajorVersion").ToString().Trim()}.{info.GetInfo("ApiMinorVersion").ToString().Trim()} ({info.GetInfo("ProductVersionString").ToString().Trim()})";
    AppLog.Line("Windows Update Agent Version: {0}", currentVersion);

    mUpdateSession = new UpdateSession {
      ClientApplicationID = Updater.ApplicationTitle
    };
    //mUpdateSession.UserLocale = 1033; // always show strings in englisch

    mUpdateServiceManager = new UpdateServiceManager();

    restoreLists = MiscFunc.ParseInt(Program.IniReadValue("Options", "LoadLists", "0")) != 0;
    if (restoreLists)
      LoadUpdates();
  }

  /// <summary>
  ///     Whether the update lists are saved to updates.ini and restored on the next start.
  /// </summary>
  public bool RestoreLists {
    get => restoreLists;
    set {
      if (restoreLists == value)
        return;
      restoreLists = value;
      Program.IniWriteValue("Options", "LoadLists", value ? "1" : "0");
      if (value)
        StoreUpdates();
      else
        FileOps.DeleteFile(DlPath + @"\updates.ini");
    }
  }

  public static WuAgent GetInstance() {
    return mInstance;
  }

  public bool Init() {
    if (!LoadServices(true))
      return false;

    mUpdateSearcher = mUpdateSession.CreateUpdateSearcher();

    UpdateHistory();
    return true;
  }

  public void UnInit() {
    ClearOffline();

    mUpdateSearcher = null;
  }

  public bool IsActive() {
    return mUpdateSearcher != null;
  }

  public bool IsBusy() {
    return mCurOperation != AgentOperation.None;
  }

  private bool LoadServices(bool cleanUp = false) {
    try {
      Console.WriteLine(@"Update Services:");
      MServiceList.Clear();
      foreach (IUpdateService service in mUpdateServiceManager.Services) {
        if (service.Name == MMyOfflineSvc) {
          if (cleanUp) TryRemoveService(service.ServiceID);
          continue;
        }

        Console.WriteLine($@"{service.Name}: {service.ServiceID}");
        //AppLog.Line($"{service.Name}: {service.ServiceID}");
        MServiceList.Add(service.Name);
      }

      return true;
    }
    catch (Exception err) {
      if ((uint)err.HResult != 0x80070422) LogError(err);
      return false;
    }
  }

  private void TryRemoveService(string serviceId) {
    try {
      mUpdateServiceManager.RemoveService(serviceId);
    }
    catch (Exception e) {
      AppLog.Line("Error removing update service {0}: {1}", serviceId, e.Message);
    }
  }

  private static void LogError(Exception error) {
    var errCode = (uint)error.HResult;
    AppLog.Line("Error 0x{0}: {1}", errCode.ToString("X").PadLeft(8, '0'), UpdateErrors.GetErrorStr(errCode));
  }

  /// <returns>True if the service was added or removed.</returns>
  public bool EnableService(string guid, bool enable = true) {
    try {
      if (enable)
        AddService(guid);
      else
        RemoveService(guid);
      return true;
    }
    catch (Exception err) {
      AppLog.Line("Failed to {0} update service {1}", enable ? "register" : "remove", guid);
      LogError(err);
      return false;
    }
    finally {
      LoadServices();
    }
  }

  private void AddService(string id) {
    mUpdateServiceManager.AddService2(id,
        (int)(tagAddServiceFlag.asfAllowOnlineRegistration | tagAddServiceFlag.asfAllowPendingRegistration |
              tagAddServiceFlag.asfRegisterServiceWithAU), "");
  }

  private void RemoveService(string id) {
    mUpdateServiceManager.RemoveService(id);
  }

  public bool TestService(string id) {
    try {
      return mUpdateServiceManager.Services.Cast<IUpdateService>().Any(service => service.ServiceID.Equals(id));
    }
    catch (Exception err) {
      LogError(err);
      return false;
    }
  }

  public string GetServiceName(string id, bool bAdd = false) {
    try {
      foreach (var service in mUpdateServiceManager.Services.Cast<IUpdateService>().Where(service => service.ServiceID.Equals(id)))
        return service.Name;
    }
    catch (Exception err) {
      LogError(err);
      return null;
    }

    if (bAdd == false || !EnableService(id))
      return null;
    return GetServiceName(id);
  }

  public void UpdateHistory() {
    MUpdateHistory.Clear();
    try {
      var count = mUpdateSearcher.GetTotalHistoryCount();
      if (count == 0) // sanity check
        return;
      foreach (var update in mUpdateSearcher.QueryHistory(0, count).Cast<IUpdateHistoryEntry2>().Where(update => update.Title != null)) {
        MUpdateHistory.Add(new MsUpdate(update));
      }
    }
    catch (Exception err) {
      AppLog.Line("Failed to read the update history");
      LogError(err);
    }
  }

  private RetCodes SetupOffline() {
    try {
      if (mOfflineService == null) {
        AppLog.Line("Setting up 'Offline Sync Service'");
        mOfflineService =
            mUpdateServiceManager.AddScanPackageService(MMyOfflineSvc, DlPath + @"\wsusscn2.cab");
      }

      mUpdateSearcher.ServerSelection = ServerSelection.ssOthers;
      mUpdateSearcher.ServiceID = mOfflineService.ServiceID;
    }
    catch (Exception err) {
      AppLog.Line(err.Message);
      return err switch {
        FileNotFoundException => RetCodes.FileNotFound,
        UnauthorizedAccessException => RetCodes.AccessError,
        _ => RetCodes.InternalError
      };
    }

    return RetCodes.Success;
  }

  public bool IsValid() {
    return mIsValid;
  }

  private RetCodes ClearOffline() {
    if (mOfflineService != null) {
      // note: if we keep references to updates referring to and removed service we may get a crash
      foreach (var update in MUpdateHistory)
        update.Invalidate();
      foreach (var update in MPendingUpdates)
        update.Invalidate();
      foreach (var update in MInstalledUpdates)
        update.Invalidate();
      foreach (var update in MHiddenUpdates)
        update.Invalidate();
      mIsValid = false;

      OnUpdatesChanged();

      try {
        mUpdateServiceManager.RemoveService(mOfflineService.ServiceID);
        mOfflineService = null;
      }
      catch (Exception err) {
        AppLog.Line(err.Message);
        return RetCodes.InternalError;
      }
    }

    return RetCodes.Success;
  }

  private void SetOnline(string serviceName) {
    foreach (var service in mUpdateServiceManager.Services
               .Cast<IUpdateService>()
               .Where(service => service.Name.Equals(serviceName, StringComparison.CurrentCultureIgnoreCase))) {
      mUpdateSearcher.ServerSelection = ServerSelection.ssDefault;
      mUpdateSearcher.ServiceID = service.ServiceID;
      //mUpdateSearcher.Online = true;
    }
  }

  public AgentOperation CurOperation() {
    return mCurOperation;
  }

  public RetCodes SearchForUpdates(string source, bool includePotentiallySupersededUpdates = false) {
    if (mCallback != null)
      return RetCodes.Busy;

    mUpdateSearcher.IncludePotentiallySupersededUpdates = includePotentiallySupersededUpdates;

    SetOnline(source);

    return EndSyncStart(SearchForUpdates());
  }

  public RetCodes SearchForUpdates(bool download, bool includePotentiallySupersededUpdates = false) {
    if (mCallback != null || mUpdateDownloader.IsBusy())
      return RetCodes.Busy;

    mUpdateSearcher.IncludePotentiallySupersededUpdates = includePotentiallySupersededUpdates;

    if (download) {
      mCurOperation = AgentOperation.PreparingCheck;
      OnProgress(-1, 0, 0, 0);

      AppLog.Line("Downloading wsusscn2.cab...");

      List<UpdateDownloader.Task> downloads = [];
      downloads.Add(new  UpdateDownloader.Task {
        Url = Program.IniReadValue("Options", "OfflineCab", "https://go.microsoft.com/fwlink/p/?LinkID=74689"),
        Path = DlPath,
        FileName = "wsusscn2.cab"
      });
      if (!mUpdateDownloader.Download(downloads))
        OnFinished(RetCodes.DownloadFailed);
      return RetCodes.InProgress;
    }

    var ret = SetupOffline();
    return ret < 0 ? ret : EndSyncStart(SearchForUpdates());
  }

  // A failed start is reported only through the return value; the caller either returns it to the UI
  // or raises Finished itself, so raising Finished here would report the error twice.
  private RetCodes OnWuError(Exception err) {
    var access = err.GetType() == typeof(UnauthorizedAccessException);
    var ret = access ? RetCodes.AccessError : RetCodes.InternalError;

    mCallback = null;
    AppLog.Line(err.Message);
    return ret;
  }

  private RetCodes EndSyncStart(RetCodes ret) {
    if (ret != RetCodes.InProgress)
      mCurOperation = AgentOperation.None;
    return ret;
  }

  private RetCodes SearchForUpdates() {
    mCurOperation = AgentOperation.CheckingUpdates;
    OnProgress(-1, 0, 0, 0);

    mCallback = new UpdateCallback(this);

    AppLog.Line("Searching for updates");
    //for the above search criteria refer to
    // http://msdn.microsoft.com/en-us/library/windows/desktop/aa386526(v=VS.85).aspx
    try {
      //string query = "(IsInstalled = 0 and IsHidden = 0) or (IsInstalled = 1 and IsHidden = 0) or (IsHidden = 1)";
      //string query = "(IsInstalled = 0 and IsHidden = 0) or (IsInstalled = 1 and IsHidden = 0) or (IsHidden = 1) or (IsInstalled = 0 and IsHidden = 0 and DeploymentAction='OptionalInstallation') or (IsInstalled = 1 and IsHidden = 0 and DeploymentAction='OptionalInstallation') or (IsHidden = 1 and DeploymentAction='OptionalInstallation')";
      var query = OSHelper.IsWindows7OrLower
          ? "(IsInstalled = 0 and IsHidden = 0) or (IsInstalled = 1 and IsHidden = 0) or (IsHidden = 1)"
          : "(IsInstalled = 0 and IsHidden = 0 and DeploymentAction=*) or (IsInstalled = 1 and IsHidden = 0 and DeploymentAction=*) or (IsHidden = 1 and DeploymentAction=*)";
      mSearchJob = mUpdateSearcher.BeginSearch(query, mCallback, null);
    }
    catch (Exception err) {
      return OnWuError(err);
    }

    return RetCodes.InProgress;
  }

  public void CancelOperations() {
    if (IsBusy())
      mCurOperation = AgentOperation.CancelingOperation;

    // Note: at any given time only one (or none) of the 3 conditions can be true
    if (mCallback != null) {
      mSearchJob?.RequestAbort();
      mDownloadJob?.RequestAbort();
      mInstalationJob?.RequestAbort();
    }
    else if (mUpdateDownloader.IsBusy()) {
      mUpdateDownloader.CancelOperations();
    }
    else if (mUpdateInstaller.IsBusy()) {
      mUpdateInstaller.CancelOperations();
    }
  }

  public RetCodes DownloadUpdatesManually(List<MsUpdate> updates, bool install = false) {
    if (mUpdateDownloader.IsBusy())
      return RetCodes.Busy;

    mCurOperation = install ? AgentOperation.PreparingUpdates : AgentOperation.DownloadingUpdates;
    OnProgress(-1, 0, 0, 0);

    List<UpdateDownloader.Task> downloads = [];
    foreach (var update in updates) {
      if (update.Downloads.Count == 0) {
        AppLog.Line("Error: No download URLs found for update {0}", update.Title);
        continue;
      }

      foreach (var url in update.Downloads) {
        UpdateDownloader.Task download = new() {
          Url = UpdateDownloader.ToHttps(url),
          Path = DlPath + @"\" + update.DownloadFolder,
          FileName = UpdateDownloader.GetSafeFileName(url, "Download_" + downloads.Count),
          UpdateKey = update.Key
        };
        downloads.Add(download);
      }
    }

    if (!mUpdateDownloader.Download(downloads, updates))
      OnFinished(RetCodes.DownloadFailed);

    return RetCodes.InProgress;
  }

  private RetCodes InstallUpdatesManually(List<MsUpdate> updates, MultiValueDictionary<string, string> allFiles) {
    if (mUpdateInstaller.IsBusy())
      return RetCodes.Busy;

    mCurOperation = AgentOperation.InstallingUpdates;
    mManualInstallOperation = mCurOperation;
    OnProgress(-1, 0, 0, 0);

    if (!mUpdateInstaller.Install(updates, allFiles))
      return RetCodes.InstallFailed;

    return RetCodes.InProgress;
  }


  public RetCodes UnInstallUpdatesManually(List<MsUpdate> updates) {
    if (mUpdateInstaller.IsBusy())
      return RetCodes.Busy;

    List<MsUpdate> filteredUpdates = [];
    foreach (var update in updates) {
      if ((update.Attributes & (int)MsUpdate.UpdateAttr.Uninstallable) == 0) {
        AppLog.Line("Update cannot be uninstalled: {0}", update.Title);
        continue;
      }

      filteredUpdates.Add(update);
    }

    if (filteredUpdates.Count == 0) {
      AppLog.Line("No updates selected or eligible for uninstallation");
      return RetCodes.NoUpdated;
    }

    mCurOperation = AgentOperation.RemovingUpdates;
    mManualInstallOperation = mCurOperation;
    OnProgress(-1, 0, 0, 0);

    if (!mUpdateInstaller.UnInstall(filteredUpdates))
      OnFinished(RetCodes.InstallFailed);

    return RetCodes.InProgress;
  }

  private void DownloadsFinished(object sender, UpdateDownloader.FinishedEventArgs args) // "manuall" mode
  {
    if (mCurOperation == AgentOperation.CancelingOperation) {
      OnFinished(RetCodes.Aborted);
      return;
    }

    if (mCurOperation == AgentOperation.PreparingCheck) {
      AppLog.Line("wsusscn2.cab downloaded");

      var ret = ClearOffline();
      if (ret == RetCodes.Success)
        ret = SetupOffline();
      if (ret == RetCodes.Success)
        ret = SearchForUpdates();
      if (ret <= 0)
        OnFinished(ret);
    }
    else {
      MultiValueDictionary<string, string> allFiles = new();
      foreach (var task in args.Downloads.Where(task => !task.Failed)) {
        allFiles.Add(task.UpdateKey, task.Path + @"\" + task.FileName);
      }

      // TODO
      /*string INIPath = dlPath + @"\updates.ini";
      foreach (string KB in AllFiles.Keys)
      {
          string Files = "";
          foreach (string FileName in AllFiles.GetValues(KB))
          {
              if (Files.Length > 0)
                  Files += "|";
              Files += FileName;
          }
          Program.IniWriteValue(KB, "Files", Files, INIPath);
      }*/

      AppLog.Line("Downloaded {0} out of {1} to {2}", allFiles.GetCount(), args.Downloads.Count, DlPath);

      if (mCurOperation == AgentOperation.PreparingUpdates) {
        var ret = InstallUpdatesManually(args.Updates, allFiles);
        if (ret <= 0)
          OnFinished(ret);
      }
      else {
        var ret = allFiles.GetCount() == args.Downloads.Count ? RetCodes.Success : RetCodes.DownloadFailed;
        if (mCurOperation == AgentOperation.CancelingOperation)
          ret = RetCodes.Aborted;
        OnFinished(ret);
      }
    }
  }

  private void DownloadProgress(object sender, ProgressArgs args) {
    OnProgress(args.TotalCount, args.TotalPercent, args.CurrentIndex, args.CurrentPercent, args.Info);
  }

  private void InstallFinished(object sender, UpdateInstaller.FinishedEventArgs args) // "manual" mode
  {
    AppLog.Line(args.Success ? "Updates (Un)Installed successfully" : "Updates failed to (Un)Install");

    // mCurOperation is overwritten on cancel, so the operation kind is tracked separately.
    var operation = mManualInstallOperation;
    mManualInstallOperation = AgentOperation.None;
    foreach (var update in args.Succeeded)
      switch (operation) {
        case AgentOperation.InstallingUpdates: {
            if (RemoveFrom(MPendingUpdates, update)) {
              update.Attributes |= (int)MsUpdate.UpdateAttr.Installed;
              MInstalledUpdates.Add(update);
            }

            break;
          }
        case AgentOperation.RemovingUpdates: {
            if (RemoveFrom(MInstalledUpdates, update)) {
              update.Attributes &= ~(int)MsUpdate.UpdateAttr.Installed;
              MPendingUpdates.Add(update);
            }

            break;
          }
      }

    if (args.Reboot)
      AppLog.Line("Reboot is required for one or more updates");

    OnUpdatesChanged();

    var ret = args.Success ? RetCodes.Success : RetCodes.InstallFailed;
    if (mCurOperation == AgentOperation.CancelingOperation)
      ret = RetCodes.Aborted;
    OnFinished(ret, args.Reboot);
  }

  private void InstallProgress(object sender, ProgressArgs args) {
    OnProgress(args.TotalCount, args.TotalPercent, args.CurrentIndex, args.CurrentPercent, args.Info);
  }

  public RetCodes DownloadUpdates(List<MsUpdate> updates, bool install = false) {
    if (mCallback != null)
      return RetCodes.Busy;

    mDownloader ??= mUpdateSession.CreateUpdateDownloader();
    mDownloader.Updates = new UpdateCollection();

    foreach (var update in updates.Select(u => u.GetUpdate()).Where(u => u != null)) {
      if (!update.EulaAccepted) update.AcceptEula();
      mDownloader.Updates.Add(update);
    }

    if (mDownloader.Updates.Count == 0) {
      AppLog.Line("No updates selected for download");
      return RetCodes.NoUpdated;
    }

    mCurOperation = install ? AgentOperation.PreparingUpdates : AgentOperation.DownloadingUpdates;
    OnProgress(-1, 0, 0, 0);

    mCallback = new UpdateCallback(this);

    AppLog.Line("Downloading Updates... This may take several minutes.");
    try {
      mDownloadJob = mDownloader.BeginDownload(mCallback, mCallback, updates);
    }
    catch (Exception err) {
      return EndSyncStart(OnWuError(err));
    }

    return RetCodes.InProgress;
  }

  private RetCodes InstallUpdates(List<MsUpdate> updates) {
    if (mCallback != null)
      return RetCodes.Busy;

    mInstaller ??= mUpdateSession.CreateUpdateInstaller();
    mInstaller.Updates = new UpdateCollection();

    // Keeps the same order as mInstaller.Updates, so per-update results can be matched by index.
    List<MsUpdate> added = [];
    foreach (var update in updates) {
      var entry = update.GetUpdate();
      if (entry == null)
        continue;
      mInstaller.Updates.Add(entry);
      added.Add(update);
    }

    if (mInstaller.Updates.Count == 0) {
      AppLog.Line("No updates selected for installation");
      return RetCodes.NoUpdated;
    }

    mCurOperation = AgentOperation.InstallingUpdates;
    mInstalationOperation = mCurOperation;
    OnProgress(-1, 0, 0, 0);

    mCallback = new UpdateCallback(this);

    AppLog.Line("Installing Updates... This may take several minutes.");
    try {
      mInstalationJob = mInstaller.BeginInstall(mCallback, mCallback, added);
    }
    catch (Exception err) {
      return OnWuError(err);
    }

    return RetCodes.InProgress;
  }

  // Note: this works _only_ for updates installed from WSUS
  /*public RetCodes UnInstallUpdates(List<MsUpdate> Updates)
  {
      if (mCallback != null)
          return RetCodes.Busy;

      if (mInstaller == null)
          mInstaller = mUpdateSession.CreateUpdateInstaller() as IUpdateInstaller;

      mInstaller.Updates = new UpdateCollection();
      foreach (MsUpdate Update in Updates)
      {
          IUpdate update = Update.GetUpdate();
          if (update == null)
              continue;

          if (!update.IsUninstallable)
          {
              AppLog.Line("Update cannot be uninstalled: {0}", update.Title);
              continue;
          }
          mInstaller.Updates.Add(update);
      }
      if (mInstaller.Updates.Count == 0)
      {
          AppLog.Line("No updates selected or eligible for uninstallation");
          return RetCodes.NoUpdated;
      }

      mCurOperation = AgentOperation.RemovingUpdates;
      OnProgress(-1, 0, 0, 0);

      mCallback = new UpdateCallback(this);

      AppLog.Line("Removing Updates... This may take several minutes.");
      try
      {
          mInstalationJob = mInstaller.BeginUninstall(mCallback, mCallback, Updates);
      }
      catch (Exception err)
      {
          return OnWuError(err);
      }
      return RetCodes.InProgress;
  }*/

  private static bool RemoveFrom(List<MsUpdate> updates, MsUpdate update) {
    for (var i = 0; i < updates.Count; i++)
      if (updates[i] == update) {
        updates.RemoveAt(i);
        return true;
      }

    return false;
  }

  public void HideUpdates(List<MsUpdate> updates, bool hide) {
    foreach (var upd in updates)
      try {
        var update = upd.GetUpdate();
        if (update == null)
          continue;
        update.IsHidden = hide;

        if (hide) {
          upd.Attributes |= (int)MsUpdate.UpdateAttr.Hidden;
          MHiddenUpdates.Add(upd);
          RemoveFrom(MPendingUpdates, upd);
        }
        else {
          upd.Attributes &= ~(int)MsUpdate.UpdateAttr.Hidden;
          MPendingUpdates.Add(upd);
          RemoveFrom(MHiddenUpdates, upd);
        }
      }
      catch (Exception e) {
        AppLog.Line("Error hiding/unhiding update {0}: {1}", upd.Title, e.Message);
      } // Hide update may throw an exception, if the user has hidden the update manually while the search was in progress.

    OnUpdatesChanged();
  }

  private void OnUpdatesFound(ISearchJob searchJob) {
    if (searchJob != mSearchJob)
      return;
    mSearchJob = null;
    mCallback = null;

    ISearchResult searchResults;
    try {
      searchResults = mUpdateSearcher.EndSearch(searchJob);
    }
    catch (Exception err) {
      AppLog.Line("Search for updates failed");
      LogError(err);
      OnFinished(RetCodes.InternalError);
      return;
    }

    var ret = searchResults.ResultCode switch {
      OperationResultCode.orcSucceeded or OperationResultCode.orcSucceededWithErrors => RetCodes.Success,
      OperationResultCode.orcAborted => RetCodes.Aborted,
      OperationResultCode.orcFailed => RetCodes.InternalError,
      _ => RetCodes.Undefined
    };

    // Partial results of an aborted or failed search must neither replace the lists nor count as a completed check.
    if (ret != RetCodes.Success) {
      AppLog.Line("Search for updates did not complete");
      OnFinished(ret);
      return;
    }

    MPendingUpdates.Clear();
    MInstalledUpdates.Clear();
    MHiddenUpdates.Clear();
    mIsValid = true;

    foreach (IUpdate update in searchResults.Updates) {
      if (update.IsHidden)
        MHiddenUpdates.Add(new MsUpdate(update, MsUpdate.UpdateState.Hidden));
      else if (update.IsInstalled)
        MInstalledUpdates.Add(new MsUpdate(update, MsUpdate.UpdateState.Installed));
      else
        MPendingUpdates.Add(new MsUpdate(update, MsUpdate.UpdateState.Pending));
      Console.WriteLine(update.Title);
    }

    AppLog.Line("Found {0} pending updates.", MPendingUpdates.Count);

    OnUpdatesChanged(true);

    OnFinished(ret);
  }

  private void OnUpdatesDownloaded(IDownloadJob downloadJob, List<MsUpdate> updates) {
    if (downloadJob != mDownloadJob)
      return;
    mDownloadJob = null;
    mCallback = null;

    IDownloadResult downloadResults;
    try {
      downloadResults = mDownloader.EndDownload(downloadJob);
    }
    catch (Exception err) {
      AppLog.Line("Downloading updates failed");
      LogError(err);
      OnFinished(RetCodes.InternalError);
      return;
    }

    OnUpdatesChanged();

    var ret = downloadResults.ResultCode switch {
      OperationResultCode.orcSucceeded or OperationResultCode.orcSucceededWithErrors => RetCodes.Success,
      OperationResultCode.orcAborted => RetCodes.Aborted,
      OperationResultCode.orcFailed => RetCodes.DownloadFailed,
      _ => RetCodes.Undefined
    };

    if (mCurOperation == AgentOperation.PreparingUpdates && ret == RetCodes.Success) {
      ret = InstallUpdates(updates);
      if (ret <= 0)
        OnFinished(ret);
    }
    else {
      if (ret == RetCodes.Success)
        AppLog.Line("Updates downloaded to %windir%\\SoftwareDistribution\\Download");
      else
        AppLog.Line("Downloading updates failed");
      OnFinished(ret);
    }
  }

  private void OnInstalationCompleted(IInstallationJob installationJob, List<MsUpdate> updates) {
    if (installationJob != mInstalationJob)
      return;
    mInstalationJob = null;
    mCallback = null;

    var wasUninstall = mInstalationOperation == AgentOperation.RemovingUpdates;
    mInstalationOperation = AgentOperation.None;

    IInstallationResult installationResults;
    try {
      installationResults = wasUninstall
          ? mInstaller.EndUninstall(installationJob)
          : mInstaller.EndInstall(installationJob);
    }
    catch (Exception err) {
      AppLog.Line("(Un)Installing updates failed");
      LogError(err);
      OnFinished(RetCodes.InternalError);
      return;
    }

    if (installationResults.ResultCode is OperationResultCode.orcSucceeded or OperationResultCode.orcSucceededWithErrors) {
      AppLog.Line(installationResults.ResultCode == OperationResultCode.orcSucceeded
          ? "Updates (Un)Installed successfully"
          : "Updates (Un)Installed with errors");

      for (var i = 0; i < updates.Count; i++) {
        var update = updates[i];
        if (GetUpdateResultCode(installationResults, i) is not (OperationResultCode.orcSucceeded or OperationResultCode.orcSucceededWithErrors)) {
          AppLog.Line("Failed to (un)install: {0}", update.Title);
          continue;
        }

        if (!wasUninstall) {
          if (RemoveFrom(MPendingUpdates, update)) {
            update.Attributes |= (int)MsUpdate.UpdateAttr.Installed;
            MInstalledUpdates.Add(update);
          }
        }
        else {
          if (RemoveFrom(MInstalledUpdates, update)) {
            update.Attributes &= ~(int)MsUpdate.UpdateAttr.Installed;
            MPendingUpdates.Add(update);
          }
        }
      }

      if (installationResults.RebootRequired)
        AppLog.Line("Reboot is required for one or more updates");
    }
    else {
      AppLog.Line("Updates failed to (Un)Install");
    }

    OnUpdatesChanged();

    var ret = installationResults.ResultCode switch {
      OperationResultCode.orcSucceeded or OperationResultCode.orcSucceededWithErrors => RetCodes.Success,
      OperationResultCode.orcAborted => RetCodes.Aborted,
      OperationResultCode.orcFailed => RetCodes.InternalError,
      _ => RetCodes.Undefined
    };

    OnFinished(ret, installationResults.RebootRequired);
  }

  private static OperationResultCode GetUpdateResultCode(IInstallationResult results, int index) {
    try {
      return results.GetUpdateResult(index).ResultCode;
    }
    catch (Exception err) {
      AppLog.Line("Failed to read the installation result of update #{0}: {1}", index + 1, err.Message);
      return OperationResultCode.orcFailed;
    }
  }

  public void EnableWuAuServ(bool enable = true) {
    try {
      if (enable) {
        if (!WinServiceHelper.IsServiceRunning("wuauserv")) {
          WinServiceHelper.ChangeStartMode("wuauserv", ServiceStartMode.Manual);
          WinServiceHelper.StartService("wuauserv");
        }
      }
      else {
        if (WinServiceHelper.IsServiceRunning("wuauserv"))
          WinServiceHelper.StopService("wuauserv");
        WinServiceHelper.ChangeStartMode("wuauserv", ServiceStartMode.Disabled);
      }
    }
    catch (Exception err) {
      AppLog.Line("Error: " + err.Message);
    }
  }

  public bool TestWuAuServ() {

    return WinServiceHelper.IsServiceRunning("wuauserv");
  }

  public event EventHandler<ProgressArgs> Progress;

  private void OnProgress(int totalUpdates, int totalPercent, int currentIndex, int updatePercent, string info = "") {
    Progress?.Invoke(this, new ProgressArgs(totalUpdates, totalPercent, currentIndex, updatePercent, info));
  }

  public event EventHandler<FinishedArgs> Finished;

  private void OnFinished(RetCodes ret, bool needReboot = false) {
    FinishedArgs args = new(mCurOperation, ret, needReboot);

    mCurOperation = AgentOperation.None;

    Finished?.Invoke(this, args);
  }

  public event EventHandler<UpdatesArgs> UpdatesChanged;

  private void OnUpdatesChanged(bool found = false) {
    if (restoreLists)
      StoreUpdates();

    UpdatesChanged?.Invoke(this, new UpdatesArgs(found));
  }

  private void StoreUpdates() {
    var iniPath = DlPath + @"\updates.ini";
    StringBuilder ini = new();
    HashSet<string> stored = [];
    foreach (var update in MPendingUpdates.Concat(MInstalledUpdates).Concat(MHiddenUpdates)) {
      if (update.Key.Length == 0 || !stored.Add(update.Key)) // sanity check
        continue;

      ini.Append('[').Append(update.Key).Append("]\r\n");
      AppendIniValue(ini, "KB", update.Kb);
      AppendIniValue(ini, "UUID", update.Uuid);
      AppendIniValue(ini, "Title", update.Title);
      AppendIniValue(ini, "Info", update.Description);
      AppendIniValue(ini, "Category", update.Category);
      AppendIniValue(ini, "Date", update.Date.ToString("o", CultureInfo.InvariantCulture));
      AppendIniValue(ini, "Size", update.Size.ToString(CultureInfo.InvariantCulture));
      AppendIniValue(ini, "SupportUrl", update.SupportUrl);
      AppendIniValue(ini, "Downloads", string.Join("|", update.Downloads.Cast<string>().ToArray()));
      AppendIniValue(ini, "State", ((int)update.State).ToString());
      AppendIniValue(ini, "Attributes", update.Attributes.ToString());
      AppendIniValue(ini, "ResultCode", update.ResultCode.ToString());
      AppendIniValue(ini, "HResult", update.HResult.ToString());
    }

    try {
      Directory.CreateDirectory(DlPath);
      // UTF-16 with BOM makes the profile API read the file as Unicode, so localized titles survive.
      File.WriteAllText(iniPath, ini.ToString(), Encoding.Unicode);
    }
    catch (Exception err) {
      AppLog.Line("Failed to store the update list: {0}", err.Message);
    }
  }

  private static void AppendIniValue(StringBuilder ini, string key, string value) {
    ini.Append(key).Append('=').Append(EscapeIniValue(value)).Append("\r\n");
  }

  private static string EscapeIniValue(string value) {
    return (value ?? "").Replace("\\", "\\\\").Replace("\r", "").Replace("\n", "\\n");
  }

  private static string UnescapeIniValue(string value) {
    if (value.IndexOf('\\') < 0)
      return value;
    StringBuilder sb = new(value.Length);
    for (var i = 0; i < value.Length; i++) {
      if (value[i] == '\\' && i + 1 < value.Length) {
        i++;
        sb.Append(value[i] == 'n' ? '\n' : value[i]);
      }
      else {
        sb.Append(value[i]);
      }
    }

    return sb.ToString();
  }

  private void LoadUpdates() {
    var iniPath = DlPath + @"\updates.ini";
    foreach (var section in Program.IniEnumSections(iniPath)) {
      string Read(string key, string def = "") => UnescapeIniValue(Program.IniReadValue(section, key, def, iniPath));

      // Older files used the KB number as the section name and had no KB value.
      MsUpdate update = new() {
        Kb = Read("KB", section),
        Uuid = Read("UUID"),
        Title = Read("Title"),
        Description = Read("Info"),
        Category = Read("Category"),
        SupportUrl = Read("SupportUrl")
      };

      var date = Read("Date");
      if (DateTime.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedDate) ||
          DateTime.TryParse(date, out parsedDate))
        update.Date = parsedDate;
      else
        AppLog.Line("Error parsing stored date for update {0}: {1}", update.Kb, date);

      if (decimal.TryParse(Read("Size", "0"), NumberStyles.Number, CultureInfo.InvariantCulture, out var size))
        update.Size = size;
      update.Downloads.AddRange(Read("Downloads").Split(['|'], StringSplitOptions.RemoveEmptyEntries));
      update.State = (MsUpdate.UpdateState)MiscFunc.ParseInt(Read("State", "0"));
      update.Attributes = MiscFunc.ParseInt(Read("Attributes", "0"));
      update.ResultCode = MiscFunc.ParseInt(Read("ResultCode", "0"));
      update.HResult = MiscFunc.ParseInt(Read("HResult", "0"));

      switch (update.State) {
        case MsUpdate.UpdateState.Pending:
          MPendingUpdates.Add(update);
          break;
        case MsUpdate.UpdateState.Installed:
          MInstalledUpdates.Add(update);
          break;
        case MsUpdate.UpdateState.Hidden:
          MHiddenUpdates.Add(update);
          break;
        case MsUpdate.UpdateState.History:
          MUpdateHistory.Add(update);
          break;
      }
    }
  }

  public class ProgressArgs(int totalCount, int totalPercent, int currentIndex, int currentPercent, string info)
      : EventArgs {
    public readonly int CurrentIndex = currentIndex;
    public readonly int CurrentPercent = currentPercent;
    public readonly string Info = info;
    public readonly int TotalCount = totalCount;
    public readonly int TotalPercent = totalPercent;
  }

  public class FinishedArgs(AgentOperation op, RetCodes ret, bool needReboot = false) : EventArgs {
    public readonly AgentOperation Op = op;
    public readonly bool RebootNeeded = needReboot;
    public readonly RetCodes Ret = ret;
  }

  public class UpdatesArgs(bool found) : EventArgs {
    public readonly bool Found = found;
  }

  private class UpdateCallback(WuAgent agent) : ISearchCompletedCallback, IDownloadProgressChangedCallback,
      IDownloadCompletedCallback, IInstallationProgressChangedCallback, IInstallationCompletedCallback {
    // Implementation of IDownloadCompletedCallback interface...
    public void Invoke(IDownloadJob downloadJob, IDownloadCompletedCallbackArgs callbackArgs) {
      // !!! warning this function is invoked from a different thread !!!
      agent.mDispatcher.Invoke(() => { agent.OnUpdatesDownloaded(downloadJob, downloadJob.AsyncState); });
    }

    // Implementation of IDownloadProgressChangedCallback interface...
    public void Invoke(IDownloadJob downloadJob, IDownloadProgressChangedCallbackArgs callbackArgs) {
      // !!! warning this function is invoced from a different thread !!!
      agent.mDispatcher.Invoke(() => {
        agent.OnProgress(downloadJob.Updates.Count, callbackArgs.Progress.PercentComplete,
            callbackArgs.Progress.CurrentUpdateIndex + 1,
            callbackArgs.Progress.CurrentUpdatePercentComplete,
            downloadJob.Updates[callbackArgs.Progress.CurrentUpdateIndex].Title);
      });
    }

    // Implementation of IInstallationCompletedCallback interface...
    public void Invoke(IInstallationJob installationJob, IInstallationCompletedCallbackArgs callbackArgs) {
      // !!! warning this function is invoced from a different thread !!!
      agent.mDispatcher.Invoke(() => {
        agent.OnInstalationCompleted(installationJob, installationJob.AsyncState);
      });
    }

    // Implementation of IInstallationProgressChangedCallback interface...
    public void Invoke(IInstallationJob installationJob, IInstallationProgressChangedCallbackArgs callbackArgs) {
      // !!! warning this function is invoced from a different thread !!!
      agent.mDispatcher.Invoke(() => {
        agent.OnProgress(installationJob.Updates.Count, callbackArgs.Progress.PercentComplete,
            callbackArgs.Progress.CurrentUpdateIndex + 1,
            callbackArgs.Progress.CurrentUpdatePercentComplete,
            installationJob.Updates[callbackArgs.Progress.CurrentUpdateIndex].Title);
      });
    }

    // Implementation of ISearchCompletedCallback interface...
    public void Invoke(ISearchJob searchJob, ISearchCompletedCallbackArgs e) {
      // !!! warning this function is invoced from a different thread !!!
      agent.mDispatcher.Invoke(() => { agent.OnUpdatesFound(searchJob); });
    }
  }
}
