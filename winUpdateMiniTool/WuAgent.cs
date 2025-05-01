using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Windows.Threading;
using sergiye.Common;
using winUpdateMiniTool.Common;
using WUApiLib;
using StringCollection = System.Collections.Specialized.StringCollection;

//this is required to use the Interfaces given by microsoft. 
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
  private IUpdateInstaller mInstaller;
  private bool mIsValid;
  private IUpdateService mOfflineService;
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

    if (MiscFunc.ParseInt(Program.IniReadValue("Options", "LoadLists", "0")) != 0)
      LoadUpdates();
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
      Console.WriteLine(e.Message);
    }
  }


  private void LogError(Exception error) {
    var errCode = (uint)error.HResult;
    AppLog.Line("Error 0x{0}: {1}", errCode.ToString("X").PadLeft(8, '0'), UpdateErrors.GetErrorStr(errCode));
  }

  public void EnableService(string guid, bool enable = true) {
    if (enable)
      AddService(guid);
    else
      RemoveService(guid);
    LoadServices();
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
    return mUpdateServiceManager.Services.Cast<IUpdateService>().Any(service => service.ServiceID.Equals(id));
  }

  public string GetServiceName(string id, bool bAdd = false) {
    foreach (var service in mUpdateServiceManager.Services.Cast<IUpdateService>().Where(service => service.ServiceID.Equals(id)))
      return service.Name;
    if (bAdd == false)
      return null;
    AddService(id);
    LoadServices();
    return GetServiceName(id);
  }

  public void UpdateHistory() {
    MUpdateHistory.Clear();
    var count = mUpdateSearcher.GetTotalHistoryCount();
    if (count == 0) // sanity check
      return;
    foreach (var update in mUpdateSearcher.QueryHistory(0, count).Cast<IUpdateHistoryEntry2>().Where(update => update.Title != null)) {
      MUpdateHistory.Add(new MsUpdate(update));
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
    foreach (IUpdateService service in mUpdateServiceManager.Services)
      if (service.Name.Equals(serviceName, StringComparison.CurrentCultureIgnoreCase)) {
        mUpdateSearcher.ServerSelection = ServerSelection.ssDefault;
        mUpdateSearcher.ServiceID = service.ServiceID;
        //mUpdateSearcher.Online = true;
      }
  }

  public AgentOperation CurOperation() {
    return mCurOperation;
  }

  public RetCodes SearchForUpdates(string source = "", bool includePotentiallySupersededUpdates = false) {
    if (mCallback != null)
      return RetCodes.Busy;

    mUpdateSearcher.IncludePotentiallySupersededUpdates = includePotentiallySupersededUpdates;

    SetOnline(source);

    return SearchForUpdates();
  }

  public RetCodes SearchForUpdates(bool download, bool includePotentiallySupersededUpdates = false) {
    if (mCallback != null)
      return RetCodes.Busy;

    mUpdateSearcher.IncludePotentiallySupersededUpdates = includePotentiallySupersededUpdates;

    if (download) {
      mCurOperation = AgentOperation.PreparingCheck;
      OnProgress(-1, 0, 0, 0);

      AppLog.Line("downloading wsusscn2.cab");

      List<UpdateDownloader.Task> downloads = [];
      downloads.Add(new  UpdateDownloader.Task() {
        Url = Program.IniReadValue("Options", "OfflineCab", "https://go.microsoft.com/fwlink/p/?LinkID=74689"),
        Path = DlPath,
        FileName = "wsusscn2.cab"
      });
      if (!mUpdateDownloader.Download(downloads))
        OnFinished(RetCodes.DownloadFailed);
      return RetCodes.InProgress;
    }

    var ret = SetupOffline();
    return ret < 0 ? ret : SearchForUpdates();
  }

  private RetCodes OnWuError(Exception err) {
    var access = err.GetType() == typeof(UnauthorizedAccessException);
    var ret = access ? RetCodes.AccessError : RetCodes.InternalError;

    mCallback = null;
    AppLog.Line(err.Message);
    OnFinished(ret);
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
      var query = MiscFunc.IsWindows7OrLower
          ? "(IsInstalled = 0 and IsHidden = 0) or (IsInstalled = 1 and IsHidden = 0) or (IsHidden = 1)"
          : "(IsInstalled = 0 and IsHidden = 0 and DeploymentAction=*) or (IsInstalled = 1 and IsHidden = 0 and DeploymentAction=*) or (IsHidden = 1 and DeploymentAction=*)";
      mSearchJob = mUpdateSearcher.BeginSearch(query, mCallback, null);
    }
    catch (Exception err) {
      return OnWuError(err);
    }

    return RetCodes.InProgress;
  }

  public IUpdate FindUpdate(string uuid) {
    if (mUpdateSearcher == null)
      return null;
    try {
      // Note: this is slow!
      var result = mUpdateSearcher.Search("UpdateID = '" + uuid + "'");
      if (result.Updates.Count > 0)
        return result.Updates[0];
    }
    catch (Exception err) {
      AppLog.Line(err.Message);
    }

    return null;
  }

  public void CancelOperations() {
    if (IsBusy())
      mCurOperation = AgentOperation.CancelingOperation;

    // Note: at any given time only one (or none) of the 3 conditions can be true
    if (mCallback != null) {
      if (mSearchJob != null)
        mSearchJob.RequestAbort();

      if (mDownloadJob != null)
        mDownloadJob.RequestAbort();

      if (mInstalationJob != null)
        mInstalationJob.RequestAbort();
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
        AppLog.Line("Error: No Download Url's found for update {0}", update.Title);
        continue;
      }

      foreach (var url in update.Downloads) {
        UpdateDownloader.Task download = new() {
          Url = url,
          Path = DlPath + @"\" + update.Kb,
          Kb = update.Kb
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
    OnProgress(-1, 0, 0, 0);

    if (!mUpdateInstaller.Install(updates, allFiles)) {
      OnFinished(RetCodes.InstallFailed);
      return RetCodes.InstallFailed;
    }

    return RetCodes.InProgress;
  }


  public RetCodes UnInstallUpdatesManually(List<MsUpdate> updates) {
    if (mUpdateInstaller.IsBusy())
      return RetCodes.Busy;

    List<MsUpdate> filteredUpdates = [];
    foreach (var update in updates) {
      if ((update.Attributes & (int)MsUpdate.UpdateAttr.Uninstallable) == 0) {
        AppLog.Line("Update can not be uninstalled: {0}", update.Title);
        continue;
      }

      filteredUpdates.Add(update);
    }

    if (filteredUpdates.Count == 0) {
      AppLog.Line("No updates selected or eligible for uninstallation");
      return RetCodes.NoUpdated;
    }

    mCurOperation = AgentOperation.RemovingUpdates;
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
      foreach (var task in args.Downloads) {
        if (task is { Failed: true, FileName: not null })
          continue;
        allFiles.Add(task.Kb, task.Path + @"\" + task.FileName);
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
    if (args.Success) {
      AppLog.Line("Updates (Un)Installed successfully");

      foreach (var update in args.Updates)
        switch (mCurOperation) {
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
    }
    else {
      AppLog.Line("Updates failed to (Un)Install");
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
      return OnWuError(err);
    }

    return RetCodes.InProgress;
  }

  private RetCodes InstallUpdates(List<MsUpdate> updates) {
    if (mCallback != null)
      return RetCodes.Busy;

    mInstaller ??= mUpdateSession.CreateUpdateInstaller();
    mInstaller.Updates = new UpdateCollection();

    foreach (var update in updates.Select(u => u.GetUpdate()).Where(u => u != null))
      mInstaller.Updates.Add(update);

    if (mInstaller.Updates.Count == 0) {
      AppLog.Line("No updates selected for installation");
      return RetCodes.NoUpdated;
    }

    mCurOperation = AgentOperation.InstallingUpdates;
    OnProgress(-1, 0, 0, 0);

    mCallback = new UpdateCallback(this);

    AppLog.Line("Installing Updates... This may take several minutes.");
    try {
      mInstalationJob = mInstaller.BeginInstall(mCallback, mCallback, updates);
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
              AppLog.Line("Update can not be uninstalled: {0}", update.Title);
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

  private bool RemoveFrom(List<MsUpdate> updates, MsUpdate update) {
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

        OnUpdatesChanged();
      }
      catch (Exception e) {
        Console.WriteLine(e.Message);
      } // Hide update may throw an exception, if the user has hidden the update manually while the search was in progress.
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

    var ret = RetCodes.Undefined;
    if (searchResults.ResultCode == OperationResultCode.orcSucceeded ||
        searchResults.ResultCode == OperationResultCode.orcSucceededWithErrors)
      ret = RetCodes.Success;
    else if (searchResults.ResultCode == OperationResultCode.orcAborted)
      ret = RetCodes.Aborted;
    else if (searchResults.ResultCode == OperationResultCode.orcFailed)
      ret = RetCodes.InternalError;
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

    if (mCurOperation == AgentOperation.PreparingUpdates) {
      var ret = InstallUpdates(updates);
      if (ret <= 0)
        OnFinished(ret);
    }
    else {
      AppLog.Line("Updates downloaded to %windir%\\SoftwareDistribution\\Download");

      var ret = downloadResults.ResultCode switch {
        OperationResultCode.orcSucceeded or OperationResultCode.orcSucceededWithErrors => RetCodes.Success,
        OperationResultCode.orcAborted => RetCodes.Aborted,
        OperationResultCode.orcFailed => RetCodes.InternalError,
        _ => RetCodes.Undefined
      };
      OnFinished(ret);
    }
  }

  private void OnInstalationCompleted(IInstallationJob installationJob, List<MsUpdate> updates) {
    if (installationJob != mInstalationJob)
      return;
    mInstalationJob = null;
    mCallback = null;

    IInstallationResult installationResults = null;
    try {
      if (mCurOperation == AgentOperation.InstallingUpdates)
        installationResults = mInstaller.EndInstall(installationJob);
      else if (mCurOperation == AgentOperation.RemovingUpdates)
        installationResults = mInstaller.EndUninstall(installationJob);
    }
    catch (Exception err) {
      AppLog.Line("(Un)Installing updates failed");
      LogError(err);
      OnFinished(RetCodes.InternalError);
      return;
    }

    if (installationResults!.ResultCode == OperationResultCode.orcSucceeded) {
      AppLog.Line("Updates (Un)Installed succesfully");

      foreach (var update in updates)
        if (mCurOperation == AgentOperation.InstallingUpdates) {
          if (RemoveFrom(MPendingUpdates, update)) {
            update.Attributes |= (int)MsUpdate.UpdateAttr.Installed;
            MInstalledUpdates.Add(update);
          }
        }
        else if (mCurOperation == AgentOperation.RemovingUpdates) {
          if (RemoveFrom(MInstalledUpdates, update)) {
            update.Attributes &= ~(int)MsUpdate.UpdateAttr.Installed;
            MPendingUpdates.Add(update);
          }
        }

      if (installationResults.RebootRequired)
        AppLog.Line("Reboot is required for one or more updates");
    }
    else {
      AppLog.Line("Updates failed to (Un)Install");
    }

    OnUpdatesChanged();

    var ret = RetCodes.Undefined;
    switch (installationResults.ResultCode) {
      case OperationResultCode.orcSucceeded:
      case OperationResultCode.orcSucceededWithErrors:
        ret = RetCodes.Success;
        break;
      case OperationResultCode.orcAborted:
        ret = RetCodes.Aborted;
        break;
      case OperationResultCode.orcFailed:
        ret = RetCodes.InternalError;
        break;
    }

    OnFinished(ret, installationResults.RebootRequired);
  }

  public void EnableWuAuServ(bool enable = true) {
    ServiceController svc = new("wuauserv"); // Windows Update Service
    try {
      if (enable) {
        if (svc.Status != ServiceControllerStatus.Running) {
          ServiceHelper.ChangeStartMode(svc, ServiceStartMode.Manual);
          svc.Start();
        }
      }
      else {
        if (svc.Status == ServiceControllerStatus.Running)
          svc.Stop();
        ServiceHelper.ChangeStartMode(svc, ServiceStartMode.Disabled);
      }
    }
    catch (Exception err) {
      AppLog.Line("Error: " + err.Message);
    }

    svc.Close();
  }

  public bool TestWuAuServ() {
    ServiceController svc = new("wuauserv");
    var ret = svc.Status == ServiceControllerStatus.Running;
    svc.Close();
    return ret;
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
    var iniPath = DlPath + @"\updates.ini";
    FileOps.DeleteFile(iniPath);

    StoreUpdates(MUpdateHistory);
    StoreUpdates(MPendingUpdates);
    StoreUpdates(MInstalledUpdates);
    StoreUpdates(MHiddenUpdates);

    UpdatesChanged?.Invoke(this, new UpdatesArgs(found));
  }

  private void StoreUpdates(List<MsUpdate> updates) {
    var iniPath = DlPath + @"\updates.ini";
    foreach (var update in updates) {
      if (update.Kb.Length == 0) // sanity check
        continue;

      Program.IniWriteValue(update.Kb, "UUID", update.Uuid, iniPath);
      Program.IniWriteValue(update.Kb, "Title", update.Title, iniPath);
      Program.IniWriteValue(update.Kb, "Info", update.Description, iniPath);
      Program.IniWriteValue(update.Kb, "Category", update.Category, iniPath);
      Program.IniWriteValue(update.Kb, "Date",
          update.Date.ToString(CultureInfo.CurrentUICulture.DateTimeFormat.ShortDatePattern), iniPath);
      Program.IniWriteValue(update.Kb, "Size", update.Size.ToString(CultureInfo.InvariantCulture), iniPath);
      Program.IniWriteValue(update.Kb, "SupportUrl", update.SupportUrl, iniPath);
      Program.IniWriteValue(update.Kb, "Downloads", string.Join("|", update.Downloads.Cast<string>().ToArray()),
          iniPath);
      Program.IniWriteValue(update.Kb, "State", ((int)update.State).ToString(), iniPath);
      Program.IniWriteValue(update.Kb, "Attributes", update.Attributes.ToString(), iniPath);
      Program.IniWriteValue(update.Kb, "ResultCode", update.ResultCode.ToString(), iniPath);
      Program.IniWriteValue(update.Kb, "HResult", update.HResult.ToString(), iniPath);
    }
  }

  private void LoadUpdates() {
    var iniPath = DlPath + @"\updates.ini";
    foreach (var kb in Program.IniEnumSections(iniPath)) {
      if (kb.Length == 0)
        continue;

      MsUpdate update = new() {
        Kb = kb
      };
      update.Uuid = Program.IniReadValue(update.Kb, "UUID", "", iniPath);
      update.Title = Program.IniReadValue(update.Kb, "Title", "", iniPath);
      update.Description = Program.IniReadValue(update.Kb, "Info", "", iniPath);
      update.Category = Program.IniReadValue(update.Kb, "Category", "", iniPath);

      try {
        update.Date = DateTime.Parse(Program.IniReadValue(update.Kb, "Date", "", iniPath));
      }
      catch (Exception e) {
        Console.WriteLine(e.Message);
      }

      update.Size = MiscFunc.ParseInt(Program.IniReadValue(update.Kb, "Size", "0", iniPath));
      update.SupportUrl = Program.IniReadValue(update.Kb, "SupportUrl", "", iniPath);
      update.Downloads.AddRange(Program.IniReadValue(update.Kb, "Downloads", "", iniPath).Split('|'));
      update.State =
          (MsUpdate.UpdateState)MiscFunc.ParseInt(Program.IniReadValue(update.Kb, "State", "0", iniPath));
      update.Attributes = MiscFunc.ParseInt(Program.IniReadValue(update.Kb, "Attributes", "0", iniPath));
      update.ResultCode = MiscFunc.ParseInt(Program.IniReadValue(update.Kb, "ResultCode", "0", iniPath));
      update.HResult = MiscFunc.ParseInt(Program.IniReadValue(update.Kb, "HResult", "0", iniPath));

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
