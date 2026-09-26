using System;
using System.Globalization;
using System.Security.AccessControl;
using System.Security.Principal;
using System.ServiceProcess;
using Microsoft.Win32;
using sergiye.Common;
using winUpdateMiniTool.Common;

namespace winUpdateMiniTool;

/// <summary>
///     Abstract class for managing Group Policy Objects (GPO) related to Windows Update settings.
/// </summary>
internal abstract class Gpo {
  /// <summary>
  ///     Enumeration for Automatic Update options.
  /// </summary>
  public enum AuOptions {
    Default = 0, // Automatic
    Disabled = 1,
    Notification = 2,
    Download = 3,
    Scheduled = 4,
    ManagedByAdmin = 5
  }

  /// <summary>
  ///     Enumeration for the level of respect for GPO settings.
  /// </summary>
  public enum Respect {
    Unknown = 0,
    Full, // Win 7, 8, 10, 11 Ent/Edu/Svr
    Partial, // Win 10, 11 Pro
    None // Win 10, 11 Home
  }

  private const string ExplorerPolicies = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer";
  private const string MWuGpo = @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate";

  /// <summary>
  ///     Configures Automatic Update settings.
  /// </summary>
  /// <param name="option">The Automatic Update option to set.</param>
  /// <param name="day">The scheduled install day (optional).</param>
  /// <param name="time">The scheduled install time (optional).</param>
  public static void ConfigAu(AuOptions option, int day = -1, int time = -1) {
    try {
      using var subKey = Registry.LocalMachine.CreateSubKey(MWuGpo + @"\AU", true);
      subKey.SetValue("NoAutoUpdate", option == AuOptions.Disabled ? 1 : 0);

      if (option == AuOptions.Default)
        subKey.DeleteValue("AUOptions", false);
      else
        subKey.SetValue("AUOptions", (int)option);

      if (option == AuOptions.Scheduled) {
        if (day != -1) subKey.SetValue("ScheduledInstallDay", day);
        if (time != -1) subKey.SetValue("ScheduledInstallTime", time);
      }
      else {
        subKey.DeleteValue("ScheduledInstallDay", false);
        subKey.DeleteValue("ScheduledInstallTime", false);
      }
    }
    catch (Exception e) {
      AppLog.Line("Error configuring Automatic Update settings: {0}", e.Message);
    }
  }

  /// <summary>
  ///     Retrieves the current Automatic Update settings.
  /// </summary>
  /// <param name="day">The scheduled install day.</param>
  /// <param name="time">The scheduled install time.</param>
  /// <returns>The current Automatic Update option.</returns>
  public static AuOptions GetAu(out int day, out int time) {
    var option = AuOptions.Default;
    try {
      using var subKey = Registry.LocalMachine.OpenSubKey(MWuGpo + @"\AU", false);
      var valueNo = subKey?.GetValue("NoAutoUpdate") as int?;
      if (valueNo is null or 0) {
        var valueAu = subKey?.GetValue("AUOptions");
        option = valueAu switch {
          2 => AuOptions.Notification,
          3 => AuOptions.Download,
          4 => AuOptions.Scheduled,
          5 => AuOptions.ManagedByAdmin,
          _ => AuOptions.Default
        };
      }
      else {
        option = AuOptions.Disabled;
      }

      day = subKey?.GetValue("ScheduledInstallDay") as int? ?? 0;
      time = subKey?.GetValue("ScheduledInstallTime") as int? ?? 0;
    }
    catch {
      day = 0;
      time = 0;
    }

    return option;
  }

  /// <summary>
  ///     Configures driver Automatic Update settings.
  /// </summary>
  /// <param name="option">The option to set for driver updates.</param>
  public static void ConfigDriverAu(int option) {
    try {
      using var subKey = Registry.LocalMachine.CreateSubKey(MWuGpo, true);
      switch (option) {
        case 0: // CheckState.Unchecked:
          subKey.SetValue("ExcludeWUDriversInQualityUpdate", 1);
          break;
        case 2: // CheckState.Indeterminate:
          subKey.DeleteValue("ExcludeWUDriversInQualityUpdate", false);
          break;
        case 1: // CheckState.Checked:
          subKey.SetValue("ExcludeWUDriversInQualityUpdate", 0);
          break;
      }
    }
    catch (Exception e) {
      AppLog.Line("Error configuring driver Automatic Update settings: {0}", e.Message);
    }
  }

  /// <summary>
  ///     Retrieves the current driver Automatic Update settings.
  /// </summary>
  /// <returns>The current driver Automatic Update option.</returns>
  public static int GetDriverAu() {
    try {
      using var subKey = Registry.LocalMachine.OpenSubKey(MWuGpo, false);
      var valueDrv = subKey?.GetValue("ExcludeWUDriversInQualityUpdate");

      if (valueDrv == null)
        return 2; // CheckState.Indeterminate;
      if ((int)valueDrv == 1)
        return 0; // CheckState.Unchecked;
                  //if ((int)value_drv == 0)
      return 1; // CheckState.Checked
    }
    catch (Exception e) {
      AppLog.Line("Error reading driver Automatic Update settings: {0}", e.Message);
    }

    return 2;
  }

  /// <summary>
  ///     Hides or shows the Windows Update settings page.
  /// </summary>
  /// <param name="hide">True to hide the page, false to show it.</param>
  public static void HideUpdatePage(bool hide = true) {
    try {
      using var subKey =
          Registry.LocalMachine.CreateSubKey(ExplorerPolicies,
              true);
      if (hide)
        subKey.SetValue("SettingsPageVisibility", "hide:windowsupdate");
      else
        subKey.DeleteValue("SettingsPageVisibility", false);
    }
    catch (Exception e) {
      AppLog.Line("Error configuring the Windows Update settings page visibility: {0}", e.Message);
    }
  }

  /// <summary>
  ///     Checks if the Windows Update settings page is hidden.
  /// </summary>
  /// <returns>True if the page is hidden, false otherwise.</returns>
  public static bool IsUpdatePageHidden() {
    try {
      using var subKey =
          Registry.LocalMachine.OpenSubKey(ExplorerPolicies);
      var value = subKey?.GetValue("SettingsPageVisibility", "")?.ToString();
      return value != null && value.Contains("hide:windowsupdate");
    }
    catch (Exception e) {
      AppLog.Line("Error reading the Windows Update settings page visibility: {0}", e.Message);
    }

    return false;
  }

  /// <summary>
  ///     Blocks or unblocks connections to Microsoft Update servers.
  /// </summary>
  /// <param name="block">True to block, false to unblock.</param>
  public static void BlockMs(bool block = true) {
    try {
      if (block) {
        using var subKey = Registry.LocalMachine.CreateSubKey(MWuGpo, true);
        subKey.SetValue("DoNotConnectToWindowsUpdateInternetLocations", 1);
        subKey.SetValue("WUServer", "\" \"");
        subKey.SetValue("WUStatusServer", "\" \"");
        subKey.SetValue("UpdateServiceUrlAlternate", "\" \"");

        using var subKey2 = Registry.LocalMachine.CreateSubKey(MWuGpo + @"\AU", true);
        subKey2.SetValue("UseWUServer", 1);
      }
      else {
        using var subKey = Registry.LocalMachine.CreateSubKey(MWuGpo, true);
        subKey.DeleteValue("DoNotConnectToWindowsUpdateInternetLocations", false);
        subKey.DeleteValue("WUServer", false);
        subKey.DeleteValue("WUStatusServer", false);
        subKey.DeleteValue("UpdateServiceUrlAlternate", false);

        using var subKey2 = Registry.LocalMachine.CreateSubKey(MWuGpo + @"\AU", true);
        subKey2.DeleteValue("UseWUServer", false);
      }
    }
    catch (Exception e) {
      AppLog.Line("Error configuring Microsoft Update server blocking: {0}", e.Message);
    }
  }

  /// <summary>
  ///     Retrieves the current block status for Microsoft Update servers.
  /// </summary>
  /// <returns>The current block status.</returns>
  public static int GetBlockMs() {
    try {
      using var subKey = Registry.LocalMachine.OpenSubKey(MWuGpo, false);

      var valueBlock =
          subKey?.GetValue("DoNotConnectToWindowsUpdateInternetLocations");

      using var subKey2 = Registry.LocalMachine.OpenSubKey(MWuGpo + @"\AU", false);
      var valueWsus = subKey2?.GetValue("UseWUServer");

      if (valueBlock as int? == 1 && valueWsus as int? == 1)
        return 1; // CheckState.Checked;
      // BlockMs(false) deletes the values, so a missing value also means not blocked.
      if (valueBlock as int? is null or 0 && valueWsus as int? is null or 0)
        return 0; // CheckState.Unchecked;
      return 2; // CheckState.Indeterminate;
    }
    catch (Exception e) {
      AppLog.Line("Error reading Microsoft Update server blocking status: {0}", e.Message);
    }

    return 2;
  }

  /// <summary>
  ///     Configures the Windows Store Automatic Update settings.
  /// </summary>
  /// <param name="disable">True to disable, false to enable.</param>
  public static void SetStoreAu(bool disable) {
    try {
      using var subKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\WindowsStore", true);
      if (disable)
        subKey.SetValue("AutoDownload", 2);
      else
        subKey.DeleteValue("AutoDownload", false);
    }
    catch (Exception e) {
      AppLog.Line("Error configuring Windows Store Automatic Update settings: {0}", e.Message);
    }
  }

  /// <summary>
  ///     Retrieves the current Windows Store Automatic Update settings.
  /// </summary>
  /// <returns>True if disabled, false otherwise.</returns>
  public static bool GetStoreAu() {
    try {
      using var subKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\WindowsStore", false);
      var valueBlock = subKey?.GetValue("AutoDownload");
      return valueBlock != null && (int)valueBlock == 2;
    }
    catch (Exception e) {
      AppLog.Line("Error reading Windows Store Automatic Update settings: {0}", e.Message);
    }

    return false;
  }

  /// <summary>
  ///     Disables or enables Automatic Updates.
  /// </summary>
  /// <param name="disable">True to disable, false to enable.</param>
  public static void DisableAu(bool disable) {
    try {
      if (disable) {
        ConfigSvc("UsoSvc", ServiceStartMode.Disabled); // Update Orchestrator Service
        ConfigSvc("WaaSMedicSvc", ServiceStartMode.Disabled); // Windows Update Medic Service
      }
      else {
        ConfigSvc("UsoSvc", ServiceStartMode.Automatic); // Update Orchestrator Service
        ConfigSvc("WaaSMedicSvc", ServiceStartMode.Manual); // Windows Update Medic Service
      }
    }
    catch (Exception e) {
      AppLog.Line("Error disabling/enabling Automatic Updates: {0}", e.Message);
    }
  }

  /// <summary>
  ///     Configures the start mode of a service.
  /// </summary>
  /// <param name="name">The name of the service.</param>
  /// <param name="mode">The start mode to set.</param>
  private static void ConfigSvc(string name, ServiceStartMode mode) {
    using ServiceController svc = new(name);
    try {
      if (mode == ServiceStartMode.Disabled && svc.Status == ServiceControllerStatus.Running) svc.Stop();
    }
    catch (Exception e) {
      AppLog.Line("Error stopping service {0}: {1}", name, e.Message);
    }

    using var subKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + name,
        RegistryKeyPermissionCheck.ReadWriteSubTree,
        RegistryRights.ReadPermissions | RegistryRights.SetValue | RegistryRights.ChangePermissions | RegistryRights.TakeOwnership);
    if (subKey == null) {
      AppLog.Line("Service {0} does not exist", name);
      return;
    }

    subKey.SetValue("Start", (int)mode);

    try {
      var system = new SecurityIdentifier(FileOps.MF_SID_SYSTEM);
      var ac = subKey.GetAccessControl();
      var hasSystemAllow = false;
      var rules = ac.GetAccessRules(true, true, typeof(SecurityIdentifier)); // get as SID not string
      foreach (RegistryAccessRule rule in rules) {
        if (!rule.IdentityReference.Equals(system))
          continue;
        // Only the deny entry added on disable is removed; the original allow entries stay intact.
        if (rule.AccessControlType == AccessControlType.Deny)
          ac.RemoveAccessRule(rule);
        else
          hasSystemAllow = true;
      }

      if (mode == ServiceStartMode.Disabled)
        ac.AddAccessRule(new RegistryAccessRule(system, RegistryRights.FullControl, AccessControlType.Deny));
      else if (!hasSystemAllow) // restore access removed by earlier versions of this tool
        ac.AddAccessRule(new RegistryAccessRule(system, RegistryRights.FullControl,
            InheritanceFlags.ContainerInherit, PropagationFlags.None, AccessControlType.Allow));
      subKey.SetAccessControl(ac);
    }
    catch (Exception e) {
      AppLog.Line("Error updating permissions for service {0}: {1}", name, e.Message);
    }
  }

  /// <summary>
  ///     Checks if Automatic Updates are disabled.
  /// </summary>
  /// <returns>True if disabled, false otherwise.</returns>
  public static bool GetDisableAu() {
    return IsSvcDisabled("UsoSvc") && IsSvcDisabled("WaaSMedicSvc");
  }

  /// <summary>
  ///     Checks if a service is disabled.
  /// </summary>
  /// <param name="name">The name of the service.</param>
  /// <returns>True if disabled, false otherwise.</returns>
  private static bool IsSvcDisabled(string name) {
    try {
      using var subKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + name, false);
      return subKey == null || MiscFunc.ParseInt(subKey.GetValue("Start", "-1").ToString()) ==
          (int)ServiceStartMode.Disabled;
    }
    catch (Exception e) {
      AppLog.Line("Error reading service {0} state: {1}", name, e.Message);
    }

    return false;
  }

  /// <summary>
  ///     Retrieves the level of respect for GPO settings.
  /// </summary>
  /// <returns>The level of respect.</returns>
  public static Respect GetRespect() {
    try {
      using var subKey =
          Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", false);
      if (subKey == null)
        return Respect.Unknown;
      var name = subKey.GetValue("ProductName", "").ToString();
      var type = subKey.GetValue("InstallationType", "").ToString();

      if (GetWinVersion() < 10.0f || type.Equals("Server", StringComparison.CurrentCultureIgnoreCase) ||
          name.Contains("Education") || name.Contains("Enterprise"))
        return Respect.Full;

      if (type.Equals("Client", StringComparison.CurrentCultureIgnoreCase)) {
        if (name.Contains("Pro"))
          return Respect.Partial;
        if (name.Contains("Home"))
          return Respect.None;
      }
    }
    catch (Exception e) {
      AppLog.Line("Error determining GPO respect level: {0}", e.Message);
    }

    return Respect.Unknown;
  }

  /// <summary>
  ///     Retrieves the Windows version.
  /// </summary>
  /// <returns>The Windows version as a float.</returns>
  public static float GetWinVersion() {
    try {
      using var subKey =
          Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", false);
      if (subKey == null)
        return 0.0f;
      var version = subKey.GetValue("CurrentVersion", "0").ToString();
      var versionNum = float.Parse(version, CultureInfo.InvariantCulture.NumberFormat);

      if (versionNum >= 6.3) {
        var build = MiscFunc.ParseInt(subKey.GetValue("CurrentBuildNumber", "0").ToString());
        if (build >= 10000)
          return 10.0f;
      }

      return versionNum;
    }
    catch (Exception e) {
      AppLog.Line("Error determining Windows version: {0}", e.Message);
    }

    return 0.0f;
  }
}
