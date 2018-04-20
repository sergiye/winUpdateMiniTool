using System;
using WUApiLib;
using StringCollection = System.Collections.Specialized.StringCollection;

namespace winUpdateMiniTool;

/// <summary>
///     Represents a Microsoft Update with various attributes and states.
/// </summary>
public class MsUpdate {
  /// <summary>
  ///     Enumeration for update attributes.
  /// </summary>
  public enum UpdateAttr {
    None = 0x0000,
    Beta = 0x0001,
    Downloaded = 0x0002,
    Hidden = 0x0004,
    Installed = 0x0008,
    Mandatory = 0x0010,
    Uninstallable = 0x0020,
    Exclusive = 0x0040,
    Reboot = 0x0080,
    AutoSelect = 0x0100
  }

  /// <summary>
  ///     Enumeration for update states.
  /// </summary>
  public enum UpdateState {
    None = 0,
    Pending,
    Installed,
    Hidden,
    History
  }

  /// <summary>
  ///     The application ID associated with the update.
  /// </summary>
  public readonly string ApplicationId = "";

  /// <summary>
  ///     Collection of download URLs for the update.
  /// </summary>
  public readonly StringCollection Downloads = new();

  private IUpdate entry;

  /// <summary>
  ///     Attributes of the update.
  /// </summary>
  public int Attributes;

  /// <summary>
  ///     Category of the update.
  /// </summary>
  public string Category = "";

  /// <summary>
  ///     Date of the update.
  /// </summary>
  public DateTime Date = DateTime.MinValue;

  /// <summary>
  ///     Description of the update.
  /// </summary>
  public string Description = "";

  /// <summary>
  ///     HRESULT code of the update.
  /// </summary>
  public int HResult;

  /// <summary>
  ///     KB article ID of the update.
  /// </summary>
  public string Kb = "";

  /// <summary>
  ///     Result code of the update.
  /// </summary>
  public int ResultCode;

  /// <summary>
  ///     Size of the update.
  /// </summary>
  public decimal Size;

  /// <summary>
  ///     State of the update.
  /// </summary>
  public UpdateState State = UpdateState.None;

  /// <summary>
  ///     Support URL for the update.
  /// </summary>
  public string SupportUrl = "";

  /// <summary>
  ///     Title of the update.
  /// </summary>
  public string Title = "";

  /// <summary>
  ///     UUID of the update.
  /// </summary>
  public string Uuid = "";

  /// <summary>
  ///     Initializes a new instance of the <see cref="MsUpdate" /> class.
  /// </summary>
  public MsUpdate() {
  }

  /// <summary>
  ///     Initializes a new instance of the <see cref="MsUpdate" /> class with the specified update and state.
  /// </summary>
  /// <param name="update">The update object.</param>
  /// <param name="state">The state of the update.</param>
  public MsUpdate(IUpdate update, UpdateState state) {
    entry = update;

    try {
      Uuid = update.Identity.UpdateID;

      Title = update.Title;
      Category = GetCategory(update.Categories);
      Description = update.Description;
      Size = update.MaxDownloadSize;
      Date = update.LastDeploymentChangeTime;
      Kb = GetKb(update);
      SupportUrl = update.SupportUrl;

      AddUpdates();

      State = state;

      Attributes |= update.IsBeta ? (int)UpdateAttr.Beta : 0;
      Attributes |= update.IsDownloaded ? (int)UpdateAttr.Downloaded : 0;
      Attributes |= update.IsHidden ? (int)UpdateAttr.Hidden : 0;
      Attributes |= update.IsInstalled ? (int)UpdateAttr.Installed : 0;
      Attributes |= update.IsMandatory ? (int)UpdateAttr.Mandatory : 0;
      Attributes |= update.IsUninstallable ? (int)UpdateAttr.Uninstallable : 0;
      Attributes |= update.AutoSelectOnWebSites ? (int)UpdateAttr.AutoSelect : 0;

      if (update.InstallationBehavior.Impact == InstallationImpact.iiRequiresExclusiveHandling)
        Attributes |= (int)UpdateAttr.Exclusive;

      switch (update.InstallationBehavior.RebootBehavior) {
        case InstallationRebootBehavior.irbAlwaysRequiresReboot:
          Attributes |= (int)UpdateAttr.Reboot;
          break;
        case InstallationRebootBehavior.irbCanRequestReboot:
        case InstallationRebootBehavior.irbNeverReboots:
          break;
      }
    }
    catch (Exception e) {
      Console.WriteLine(e.Message);
    }
  }

  /// <summary>
  ///     Initializes a new instance of the <see cref="MsUpdate" /> class with the specified update history entry.
  /// </summary>
  /// <param name="update">The update history entry object.</param>
  public MsUpdate(IUpdateHistoryEntry2 update) {
    try {
      Uuid = update.UpdateIdentity.UpdateID;

      Title = update.Title;
      Category = GetCategory(update.Categories);
      Description = update.Description;
      Date = update.Date;
      SupportUrl = update.SupportUrl;
      ApplicationId = update.ClientApplicationID;

      State = UpdateState.History;

      ResultCode = (int)update.ResultCode;
      HResult = update.HResult;
    }
    catch (Exception e) {
      Console.WriteLine(e.Message);
    }
  }

  /// <summary>
  ///     Adds the download URLs of the update to the Downloads collection.
  /// </summary>
  private void AddUpdates() {
    AddUpdates(entry.DownloadContents);
    if (Downloads.Count != 0) return;
    foreach (IUpdate5 bundle in entry.BundledUpdates)
      AddUpdates(bundle.DownloadContents);
  }

  /// <summary>
  ///     Adds the download URLs from the specified content collection to the Downloads collection.
  /// </summary>
  /// <param name="content">The content collection.</param>
  private void AddUpdates(IUpdateDownloadContentCollection content) {
    foreach (IUpdateDownloadContent2 udc in content) {
      if (udc.IsDeltaCompressedContent)
        continue;
      if (string.IsNullOrEmpty(udc.DownloadUrl))
        continue; // sanity check
      Downloads.Add(udc.DownloadUrl);
    }
  }

  /// <summary>
  ///     Retrieves the KB article ID from the specified update.
  /// </summary>
  /// <param name="update">The update object.</param>
  /// <returns>The KB article ID.</returns>
  private static string GetKb(IUpdate update) {
    return update.KBArticleIDs.Count > 0 ? "KB" + update.KBArticleIDs[0] : "KBUnknown";
  }

  /// <summary>
  ///     Retrieves the category from the specified category collection.
  /// </summary>
  /// <param name="cats">The category collection.</param>
  /// <returns>The category string.</returns>
  private static string GetCategory(ICategoryCollection cats) {
    var classification = "";
    var product = "";
    foreach (ICategory cat in cats)
      switch (cat.Type) {
        case "UpdateClassification":
          classification = cat.Name;
          break;
        case "Product":
          product = cat.Name;
          break;
        default:
          continue;
      }

    return product.Length == 0 ? classification : product + "; " + classification;
  }

  /// <summary>
  ///     Invalidates the update entry.
  /// </summary>
  public void Invalidate() {
    entry = null;
  }

  /// <summary>
  ///     Retrieves the update object.
  /// </summary>
  /// <returns>The update object.</returns>
  public IUpdate GetUpdate() {
    /*if (Entry == null)
    {
        WuAgent agen = WuAgent.GetInstance();
        if (agen.IsActive())
            Entry = agen.FindUpdate(UUID);
    }*/
    return entry;
  }
}
