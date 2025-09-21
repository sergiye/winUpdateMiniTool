using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using sergiye.Common;
using winUpdateMiniTool.Common;
using winUpdateMiniTool.Properties;
using WUApiLib;

namespace winUpdateMiniTool;

public partial class MainForm : Form {
  
  private static Timer mTimer;
  private readonly WuAgent agent;
  private readonly int idleDelay;
  private readonly Gpo.Respect mGpoRespect = Gpo.Respect.Unknown;
  private readonly float mWinVersion;
  private bool allowShowDisplay = true;
  private AutoUpdateOptions autoUpdate = AutoUpdateOptions.No;
  private bool bUpdateList;
  private bool checkChecks;
  private UpdateLists currentList = UpdateLists.UpdateHistory;
  private bool doUpdate;
  private bool ignoreChecks;
  private DateTime lastBalloon = DateTime.MinValue;
  private DateTime lastCheck = DateTime.MaxValue;
  private string mSearchFilter;
  private bool mSuspendUpdate;
  private bool resultShown;
  private bool suspendChange;
  private ToolStripMenuItem wuauMenu;

  public MainForm() {
    InitializeComponent();

    Icon = Icon.ExtractAssociatedIcon(typeof(MainForm).Assembly.Location);
    notifyIcon.Icon = Icon;
    notifyIcon.Text = Updater.ApplicationTitle;

    TryApplyUIFont();

    if (Program.TestArg("-tray")) {
      allowShowDisplay = false;
      notifyIcon.Visible = true;
    }

    if (!OperatingSystemHelper.IsRunningAsUwp())
      Text = Updater.ApplicationTitle;

    btnWinUpd.Text = string.Format("Windows Update ({0})", 0);
    btnInstalled.Text = string.Format("Installed Updates ({0})", 0);
    btnHidden.Text = string.Format("Hidden Updates ({0})", 0);
    btnHistory.Text = string.Format("Update History ({0})", 0);

    toolTip.SetToolTip(btnSearch, "Search");
    toolTip.SetToolTip(btnInstall, "Install");
    toolTip.SetToolTip(btnDownload, "Download");
    toolTip.SetToolTip(btnHide, "Hide");
    toolTip.SetToolTip(btnGetLink, "Get Links");
    toolTip.SetToolTip(btnUnInstall, "Uninstall");
    toolTip.SetToolTip(btnCancel, "Cancel");

    AppLog.Logger += LineLogger;

    foreach (var line in AppLog.GetLog())
      logBox.AppendText(line + Environment.NewLine);
    logBox.ScrollToCaret();

    agent = WuAgent.GetInstance();
    agent.Progress += OnProgress;
    agent.UpdatesChanged += OnUpdates;
    agent.Finished += OnFinished;

    if (!agent.IsActive())
      if (MessageBox.Show("Windows Update Service is not available, try to start it?", Updater.ApplicationTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) {
        agent.EnableWuAuServ();
        agent.Init();
      }

    mSuspendUpdate = true;
    chkDrivers.CheckState = (CheckState)Gpo.GetDriverAu();

    mGpoRespect = Gpo.GetRespect();
    mWinVersion = Gpo.GetWinVersion();

    if (mWinVersion < 10) // 8.1 or below
      chkHideWU.Enabled = false;
    chkHideWU.Checked = Gpo.IsUpdatePageHidden();

    if (mGpoRespect == Gpo.Respect.Partial || mGpoRespect == Gpo.Respect.None)
      radSchedule.Enabled = radDownload.Enabled = radNotify.Enabled = false;
    else if (mGpoRespect == Gpo.Respect.Unknown)
      AppLog.Line("Unrecognized Windows Edition, respect for GPO settings is unknown.");

    if (mGpoRespect == Gpo.Respect.None)
      chkBlockMS.Enabled = false;
    chkBlockMS.CheckState = (CheckState)Gpo.GetBlockMs();

    switch (Gpo.GetAu(out int day, out int time)) {
      case Gpo.AuOptions.Default:
        radDefault.Checked = true;
        break;
      case Gpo.AuOptions.Disabled:
        radDisable.Checked = true;
        break;
      case Gpo.AuOptions.Notification:
        radNotify.Checked = true;
        break;
      case Gpo.AuOptions.Download:
        radDownload.Checked = true;
        break;
      case Gpo.AuOptions.Scheduled:
        radSchedule.Checked = true;
        break;
    }

    try {
      dlShDay.SelectedIndex = day;
      dlShTime.SelectedIndex = time;
    }
    catch {
      // ignored
    }

    if (mWinVersion >= 10) // 10 or abive
      chkDisableAU.Checked = Gpo.GetDisableAu();

    if (mWinVersion < 6.2) // win 7 or below
      chkStore.Enabled = false;
    chkStore.Checked = Gpo.GetStoreAu();

    try {
      dlAutoCheck.SelectedIndex = MiscFunc.ParseInt(GetConfig("AutoUpdate", "0"));
    }
    catch {
      // ignored
    }

    if (Program.IsAutoStart())
      chkAutoRun_CheckedChanged(null, EventArgs.Empty);
    if (OperatingSystemHelper.IsRunningAsUwp() && chkAutoRun.CheckState == CheckState.Checked)
      chkAutoRun.Enabled = false;
    idleDelay = MiscFunc.ParseInt(GetConfig("IdleDelay", "20"));
    if (Program.IsSkipUacRun())
      chkNoUAC_CheckedChanged(null, EventArgs.Empty);
    chkNoUAC.Enabled = OperatingSystemHelper.IsAdministrator();
    chkNoUAC.Visible = chkNoUAC.Enabled || chkNoUAC.Checked || !OperatingSystemHelper.IsRunningAsUwp();

    chkOffline.Checked = MiscFunc.ParseInt(GetConfig("Offline", "0")) != 0;
    chkDownload.Checked = MiscFunc.ParseInt(GetConfig("Download", "1")) != 0;
    chkManual.Checked = MiscFunc.ParseInt(GetConfig("Manual", "0")) != 0;
    if (!OperatingSystemHelper.IsAdministrator()) {
      if (OperatingSystemHelper.IsRunningAsUwp()) {
        chkOffline.Enabled = false;
        chkOffline.Checked = false;

        chkManual.Enabled = false;
        chkManual.Checked = true;
      }

      chkMsUpd.Enabled = false;
    }

    chkMsUpd.Checked = agent.IsActive() && agent.TestService(WuAgent.MsUpdGuid);

    // Note: when running in the UWP sandbox we cant write the real registry even as admins
    if (!OperatingSystemHelper.IsAdministrator() || OperatingSystemHelper.IsRunningAsUwp())
      foreach (Control ctl in gbxAutoUpdate.Controls)
        ctl.Enabled = false;

    chkOld.Checked = MiscFunc.ParseInt(GetConfig("IncludeOld", "0")) != 0;
    var source = GetConfig("Source", "Windows Update");

    var online = Program.GetArg("-online");
    if (online != null) {
      chkOffline.Checked = false;
      if (online.Length > 0)
        source = agent.GetServiceName(online, true);
    }

    var offline = Program.GetArg("-offline");
    if (offline != null) {
      chkOffline.Checked = true;
      if (offline.Equals("download", StringComparison.CurrentCultureIgnoreCase))
        chkDownload.Checked = true;
      else if (offline.Equals("no_download", StringComparison.CurrentCultureIgnoreCase))
        chkDownload.Checked = false;
    }

    if (Program.TestArg("-manual"))
      chkManual.Checked = true;

    try {
      lastCheck = DateTime.Parse(GetConfig("LastCheck"));
      AppLog.Line("Last Checked for updates: {0}",
          lastCheck.ToString(CultureInfo.CurrentUICulture.DateTimeFormat.ShortDatePattern));
    }
    catch {
      lastCheck = DateTime.Now;
    }

    LoadProviders(source);

    chkGrupe.Checked = MiscFunc.ParseInt(GetConfig("GroupUpdates", "1")) != 0;
    updateView.ShowGroups = chkGrupe.Checked;

    mSuspendUpdate = false;

    if (Program.TestArg("-provisioned"))
      gbxAutoUpdate.Enabled = false;

    notifyIcon.ContextMenuStrip = new ContextMenuStrip();
    notifyIcon.ContextMenuStrip.Items.Add(new ToolStripMenuItem("Show/Hide", null, notifyIcon1_MouseDoubleClick));
    notifyIcon.ContextMenuStrip.Items.Add("-");
    notifyIcon.ContextMenuStrip.Items.Add(new ToolStripMenuItem("Site", null, siteToolStripMenuItem_Click));
    notifyIcon.ContextMenuStrip.Items.Add(new ToolStripMenuItem("Check for new version", null, checkForNewVersionToolStripMenuItem_Click));
    notifyIcon.ContextMenuStrip.Items.Add(new ToolStripMenuItem("About", null, aboutToolStripMenuItem_Click));
    notifyIcon.ContextMenuStrip.Items.Add("-");
    notifyIcon.ContextMenuStrip.Items.Add(new ToolStripMenuItem("Exit", null, menuExit_Click));

    BuildToolsMenu();

    UpdateCounts();
    SwitchList(UpdateLists.UpdateHistory);

    doUpdate = Program.TestArg("-update");

    mTimer = new Timer();
    mTimer.Interval = 250; // 4 times per second
    mTimer.Tick += OnTimedEvent;
    mTimer.Enabled = true;

    Updater.Subscribe(
      (message, isError) => { MessageBox.Show(message, Updater.ApplicationTitle, MessageBoxButtons.OK, isError ? MessageBoxIcon.Warning : MessageBoxIcon.Information); },
      (message) => { return MessageBox.Show(message, Updater.ApplicationTitle, MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK; },
      () => { menuExit_Click(null, EventArgs.Empty); },
      MiscFunc.ParseInt(GetConfig("AutoUpdate", "0")) != 0
    );
    chkAutoUpdateApp.Checked = Updater.AutoUpdate;
    InitializeTheme();
  }

  #region themes

  private void OnThemeCurrentChanged() {

    btnSearch.Image = Theme.Current.GetBitmapFromImage(Resources.icons8_available_updates_32, new Size(25, 25));
    btnInstall.Image = Theme.Current.GetBitmapFromImage(Resources.icons8_software_installer_32, new Size(25, 25));
    btnDownload.Image = Theme.Current.GetBitmapFromImage(Resources.icons8_downloading_updates_32, new Size(25, 25));
    btnUnInstall.Image = Theme.Current.GetBitmapFromImage(Resources.icons8_trash_32, new Size(25, 25));
    btnHide.Image = Theme.Current.GetBitmapFromImage(Resources.icons8_hide_32, new Size(25, 25));
    btnGetLink.Image = Theme.Current.GetBitmapFromImage(Resources.icons8_link_32, new Size(25, 25));
    btnCancel.Image = Theme.Current.GetBitmapFromImage(Resources.icons8_cancel_32, new Size(25, 25));

    Program.IniWriteValue("Root", "Theme", Theme.IsAutoThemeEnabled ? "auto" : Theme.Current.Id);
  }

  private void InitializeTheme() {

    mainMenu.Renderer = new ThemedToolStripRenderer();
    notifyIcon.ContextMenuStrip.Renderer = new ThemedToolStripRenderer();

    themeMenuItem.DropDownItems.Clear();
    var currentItem = CustomTheme.FillThemesMenu((title, theme, onClick) => {
      if (theme == null && onClick == null) {
        themeMenuItem.DropDownItems.Add(title);
        return null;
      }
      var item = new ToolStripRadioButtonMenuItem(title, null, onClick);
      themeMenuItem.DropDownItems.Add(item);
      return item;
    }, 
    OnThemeCurrentChanged, 
    Program.IniReadValue("Root", "Theme", ""), "winUpdateMiniTool.themes");
    currentItem?.PerformClick();
    Theme.Current.Apply(this);
  }

  #endregion

  protected override void WndProc(ref Message m) {
    if (m.Msg == WinApiHelper.WM_SHOWME) {
      notifyIcon_BalloonTipClicked(null, null);
      Visible = true;
      WindowState = FormWindowState.Normal;
      Activate();
      BringToFront();
      WinApiHelper.SetForegroundWindow(this.Handle);
    }
    else {
      base.WndProc(ref m);
    }
  }
  
  private void siteToolStripMenuItem_Click(object sender, EventArgs e) {
    Updater.VisitAppSite();
  }

  private void checkForNewVersionToolStripMenuItem_Click(object sender, EventArgs e) {
    Updater.CheckForUpdates(Updater.CheckUpdatesMode.AllMessages);
  }

  private void aboutToolStripMenuItem_Click(object sender, EventArgs e) {
    Updater.ShowAbout();
  }

  private void LineLogger(object sender, AppLog.LogEventArgs args) {
    if (InvokeRequired) {
      BeginInvoke(new EventHandler<AppLog.LogEventArgs>(LineLogger), sender, args);
      return;
    }

    Console.WriteLine(@"LOG: " + args.Message);
    logBox.AppendText(args.Message + Environment.NewLine);
    logBox.ScrollToCaret();
  }

  protected override void SetVisibleCore(bool value) {
    base.SetVisibleCore(allowShowDisplay ? value : allowShowDisplay);
  }

  private void OnTimedEvent(object source, EventArgs e) {
    var updateNow = false;
    if (notifyIcon.Visible) {
      var daysDue = GetAutoUpdateDue();
      if (daysDue != 0 && !agent.IsBusy()) {
        // ensure we only start a check when user is not doing anything
        var idleTime = OperatingSystemHelper.GetIdleTime();
        if (idleDelay * 60 < idleTime) {
          AppLog.Line("Starting automatic search for updates.");
          updateNow = true;
        }
        else if (daysDue > GetGraceDays()) {
          if (lastBalloon < DateTime.Now.AddHours(-4)) {
            lastBalloon = DateTime.Now;
            notifyIcon.ShowBalloonTip(int.MaxValue, "Please Check For Updates",
              $"{Updater.ApplicationTitle} couldn't check for updates for {daysDue} days, please check for updates manually and resolve possible issues", ToolTipIcon.Warning);
          }
        }
      }

      if (agent.MPendingUpdates.Count > 0)
        if (lastBalloon < DateTime.Now.AddHours(-4)) {
          lastBalloon = DateTime.Now;
          notifyIcon.ShowBalloonTip(int.MaxValue, "New Updates found",
              string.Format("{0} has found {1} new updates, please review the updates and install them", Updater.ApplicationTitle,
                  string.Join(Environment.NewLine, agent.MPendingUpdates.Select(x => $"- {x.Title}"))),
              ToolTipIcon.Info);
        }
    }

    if ((doUpdate || updateNow && !resultShown) && agent.IsActive()) {
      doUpdate = false;
      if (chkOffline.Checked)
        agent.SearchForUpdates(chkDownload.Checked, chkOld.Checked);
      else
        agent.SearchForUpdates(dlSource.Text, chkOld.Checked);
    }

    if (bUpdateList) {
      bUpdateList = false;
      LoadList();
    }

    if (checkChecks)
      UpdateState();
  }

  private int GetAutoUpdateDue() {
    try {
      var nextUpdate = DateTime.MaxValue;
      switch (autoUpdate) {
        case AutoUpdateOptions.EveryDay:
          nextUpdate = lastCheck.AddDays(1);
          break;
        case AutoUpdateOptions.EveryWeek:
          nextUpdate = lastCheck.AddDays(7);
          break;
        case AutoUpdateOptions.EveryMonth:
          nextUpdate = lastCheck.AddMonths(1);
          break;
      }

      if (nextUpdate >= DateTime.Now)
        return 0;
      return (int)Math.Ceiling((DateTime.Now - nextUpdate).TotalDays);
    }
    catch {
      lastCheck = DateTime.Now;
      return 0;
    }
  }

  private int GetGraceDays() {
    return autoUpdate switch {
      AutoUpdateOptions.EveryMonth => 15,
      _ => 3,
    };
  }

  private void MainFormClosing(object sender, FormClosingEventArgs e) {
    if (notifyIcon.Visible && allowShowDisplay) {
      e.Cancel = true;
      allowShowDisplay = false;
      Hide();
      return;
    }

    agent.Progress -= OnProgress;
    agent.UpdatesChanged -= OnUpdates;
    agent.Finished -= OnFinished;
  }

  private void notifyIcon1_MouseDoubleClick(object sender, EventArgs e) {
    if (allowShowDisplay) {
      allowShowDisplay = false;
      Hide();
    }
    else {
      allowShowDisplay = true;
      Show();
    }
  }

  private void LoadProviders(string source = null) {
    dlSource.Items.Clear();
    for (var i = 0; i < agent.MServiceList.Count; i++) {
      var service = agent.MServiceList[i];
      dlSource.Items.Add(service);

      if (source != null && service.Equals(source, StringComparison.CurrentCultureIgnoreCase))
        dlSource.SelectedIndex = i;
    }
  }

  private void UpdateCounts() {
    btnWinUpd.Text = string.Format("Windows Update ({0})", agent.MPendingUpdates.Count);
    btnInstalled.Text = string.Format("Installed Updates ({0})", agent.MInstalledUpdates.Count);
    btnHidden.Text = string.Format("Hidden Updates ({0})", agent.MHiddenUpdates.Count);
    btnHistory.Text = string.Format("Update History ({0})", agent.MUpdateHistory.Count);
  }

  private void LoadList() {
    ignoreChecks = true;
    updateView.CheckBoxes = currentList != UpdateLists.UpdateHistory;
    ignoreChecks = false;
    updateView.ForeColor = updateView.CheckBoxes && !agent.IsValid() ? Theme.Current.LineColor : Theme.Current.ForegroundColor;

    switch (currentList) {
      case UpdateLists.PendingUpdates:
        LoadList(agent.MPendingUpdates);
        break;
      case UpdateLists.InstalledUpdates:
        LoadList(agent.MInstalledUpdates);
        break;
      case UpdateLists.HiddenUpdates:
        LoadList(agent.MHiddenUpdates);
        break;
      case UpdateLists.UpdateHistory:
        LoadList(agent.MUpdateHistory);
        break;
    }
  }

  private void LoadList(List<MsUpdate> list) {
    var iniPath = Program.WrkPath + @"\Updates.ini";

    updateView.Items.Clear();
    List<ListViewItem> items = [];
    foreach (var update in list) {
      var state = "";
      switch (update.State) {
        case MsUpdate.UpdateState.History:
          state = (OperationResultCode) update.ResultCode switch {
            OperationResultCode.orcNotStarted => "Not Started",
            OperationResultCode.orcInProgress => "In Progress",
            OperationResultCode.orcSucceeded => "Succeeded",
            OperationResultCode.orcSucceededWithErrors => "Succeeded with Errors",
            OperationResultCode.orcFailed => "Failed",
            OperationResultCode.orcAborted => "Aborted",
            _ => state
          };

          if (update.HResult != 0)
            state += " (0x" + string.Format("{0:X8}", update.HResult) + ")";
          break;

        default:
          if ((update.Attributes & (int)MsUpdate.UpdateAttr.Beta) != 0)
            state = "Beta ";

          if ((update.Attributes & (int)MsUpdate.UpdateAttr.Installed) != 0) {
            state += "Installed";
            if ((update.Attributes & (int)MsUpdate.UpdateAttr.Uninstallable) != 0)
              state += " Removable";
          }
          else if ((update.Attributes & (int)MsUpdate.UpdateAttr.Hidden) != 0) {
            state += "Hidden";
            if ((update.Attributes & (int)MsUpdate.UpdateAttr.Downloaded) != 0)
              state += " Downloaded";
          }
          else {
            if ((update.Attributes & (int)MsUpdate.UpdateAttr.Downloaded) != 0)
              state += "Downloaded";
            else
              state += "Pending";
            if ((update.Attributes & (int)MsUpdate.UpdateAttr.AutoSelect) != 0)
              state += " (!)";
            if ((update.Attributes & (int)MsUpdate.UpdateAttr.Mandatory) != 0)
              state += " Mandatory";
          }

          if ((update.Attributes & (int)MsUpdate.UpdateAttr.Exclusive) != 0)
            state += ", Exclusive";

          if ((update.Attributes & (int)MsUpdate.UpdateAttr.Reboot) != 0)
            state += ", Needs Reboot";
          break;
      }

      string[] strings = [
        update.Title,
        update.Category,
        currentList == UpdateLists.UpdateHistory ? update.ApplicationId : update.Kb,
        update.Date.ToString(CultureInfo.CurrentUICulture.DateTimeFormat.ShortDatePattern),
        FileOps.FormatSize(update.Size),
        state
      ];

      if (mSearchFilter != null) {
        var match = false;
        foreach (var str in strings)
          if (str.IndexOf(mSearchFilter, StringComparison.CurrentCultureIgnoreCase) != -1) {
            match = true;
            break;
          }

        if (!match)
          continue;
      }

      var item = new ListViewItem(strings);
      item.SubItems[3].Tag = update.Date;
      item.SubItems[4].Tag = update.Size;
      item.Tag = update;

      if (currentList == UpdateLists.PendingUpdates) {
        if (MiscFunc.ParseInt(Program.IniReadValue(update.Kb, "BlackList", "0", iniPath)) != 0)
          item.Font = new Font(item.Font.FontFamily, item.Font.Size, FontStyle.Strikeout);
        else if (MiscFunc.ParseInt(Program.IniReadValue(update.Kb, "Select", "0", iniPath)) != 0)
          item.Checked = true;
      }
      else if (currentList == UpdateLists.InstalledUpdates) {
        if (MiscFunc.ParseInt(Program.IniReadValue(update.Kb, "Remove", "0", iniPath)) != 0)
          item.Checked = true;
      }

      var colorStr = Program.IniReadValue(update.Kb, "Color", "", iniPath);
      if (colorStr.Length > 0) {
        var color = MiscFunc.ParseColor(colorStr);
        if (color != null)
          item.BackColor = (Color)color;
      }

      var lvg = updateView.Groups[update.Category];
      if (lvg == null) {
        lvg = updateView.Groups.Add(update.Category, update.Category);
        ListViewExtended.SetGrpState(lvg, ListViewGroupState.Collapsible);
      }

      item.Group = lvg;
      items.Add(item);
    }

    updateView.Items.AddRange(items.ToArray());

    // Note: this has caused issues in the past
    //updateView.SetGroupState(ListViewGroupState.Collapsible);
  }

  private List<MsUpdate> GetUpdates() {
    List<MsUpdate> updates = [];
    foreach (ListViewItem item in updateView.CheckedItems)
      updates.Add((MsUpdate)item.Tag);
    return updates;
  }

  private void SwitchList(UpdateLists list) {
    if (suspendChange)
      return;
    suspendChange = true;
    currentList = list;

    btnWinUpd.Checked = currentList == UpdateLists.PendingUpdates;
    btnInstalled.Checked = currentList == UpdateLists.InstalledUpdates;
    btnHidden.Checked = currentList == UpdateLists.HiddenUpdates;
    btnHistory.Checked = currentList == UpdateLists.UpdateHistory;

    suspendChange = false;

    updateView.Columns[2].Text = currentList == UpdateLists.UpdateHistory
        ? "Application ID"
        : "KB Article";

    LoadList();

    UpdateState();

    lblSupport.Visible = false;
  }

  private void UpdateState() {
    checkChecks = false;

    var isChecked = updateView.CheckedItems.Count > 0;

    var busy = agent.IsBusy();
    SetControlsState(!busy);

    var isValid = agent.IsValid();
    var isValid2 = isValid || chkManual.Checked;

    var admin = OperatingSystemHelper.IsAdministrator() || !OperatingSystemHelper.IsRunningAsUwp();

    var enable = agent.IsActive() && !busy;
    btnSearch.Enabled = enable;
    btnDownload.Enabled = isChecked && enable && isValid2 && currentList == UpdateLists.PendingUpdates;
    btnInstall.Enabled = isChecked && admin && enable && isValid2 && currentList == UpdateLists.PendingUpdates;
    btnUnInstall.Enabled = isChecked && admin && enable && currentList == UpdateLists.InstalledUpdates;
    btnHide.Enabled = isChecked && enable && isValid &&
                      (currentList == UpdateLists.PendingUpdates || currentList == UpdateLists.HiddenUpdates);
    btnGetLink.Enabled = isChecked && currentList != UpdateLists.UpdateHistory;
  }

  private void BuildToolsMenu() {
    toolsToolStripMenuItem.DropDownItems.Clear();
    wuauMenu = new ToolStripMenuItem("Windows Update Service", null, menuWuAu_Click);
    wuauMenu.Checked = agent.TestWuAuServ();
    toolsToolStripMenuItem.DropDownItems.Add(wuauMenu);
    toolsToolStripMenuItem.DropDownItems.Add("-");

    if (Directory.Exists(Program.GetToolsPath())) {
      foreach (var subDir in Directory.GetDirectories(Program.GetToolsPath())) {
        var iniName = Path.GetFileName(subDir);
        var iniPath = subDir + @"\" + iniName + ".ini";

        var toolMenu = new ToolStripMenuItem(Program.IniReadValue("Root", "Name", iniName, iniPath));
        var exec = Program.IniReadValue("Root", "Exec", "", iniPath);
        var silent = MiscFunc.ParseInt(Program.IniReadValue("Root", "Silent", "0", iniPath)) != 0;
        if (exec.Length > 0) {
          toolMenu.Click += delegate (object sender, EventArgs e) {
            menuExec_Click(sender, e, exec, subDir, silent);
          };
        }
        else {
          var count = MiscFunc.ParseInt(Program.IniReadValue("Root", "Entries", "", iniPath), 99);
          for (var i = 1; i <= count; i++) {
            var name = Program.IniReadValue("Entry" + i, "Name", "", iniPath);
            if (name.Length == 0) {
              if (count != 99)
                continue;
              break;
            }

            var subMenu = new ToolStripMenuItem(name);
            var entryExec = Program.IniReadValue("Entry" + i, "Exec", "", iniPath);
            var entrySilent = MiscFunc.ParseInt(Program.IniReadValue("Entry" + i, "Silent", "0", iniPath)) != 0;
            subMenu.Click += delegate (object sender, EventArgs e) {
              menuExec_Click(sender, e, entryExec, subDir, entrySilent);
            };

            toolMenu.DropDownItems.Add(subMenu);
          }
        }

        toolsToolStripMenuItem.DropDownItems.Add(toolMenu);
      }

      toolsToolStripMenuItem.DropDownItems.Add("-");
    }

    toolsToolStripMenuItem.DropDownItems.Add(new ToolStripMenuItem("&Refresh", null, menuRefresh_Click));
  }

  private void menuExec_Click(object sender, EventArgs e, string exec, string dir, bool silent = false) {
    var startInfo = Program.PrepExec(exec, silent);
    startInfo.WorkingDirectory = dir;
    if (!Program.DoExec(startInfo))
      MessageBox.Show("Failed to start tool", Updater.ApplicationTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
  }

  private void menuExit_Click(object sender, EventArgs e) {
    if (InvokeRequired) {
      Invoke(new EventHandler(menuExit_Click), sender, e);
      return;
    }
    FormClosing -= MainFormClosing;
    Visible = false;
    notifyIcon.Visible = false;
    notifyIcon.Dispose();
    mTimer.Enabled = false;
    mTimer.Dispose();
    Application.Exit();
  }

  private void menuClean_Click(object sender, EventArgs e) {
    SetControlsState(false, "Cleaning Windows Update cache...");
    Task.Run(CleanCache);
  }

  private Task CleanCache() {
    const string CachePath = "c:\\Windows\\SoftwareDistribution\\Download";
    long freedBytes = 0;
    if (Directory.Exists(CachePath)) {
      try {
        foreach (string file in Directory.GetFiles(CachePath, "*", SearchOption.AllDirectories)) {
          long fileSize = new FileInfo(file).Length;
          if (FileOps.DeleteFile(file))
            freedBytes += fileSize;
        }
        foreach (string dir in Directory.GetDirectories(CachePath, "*", SearchOption.TopDirectoryOnly)) {
          FileOps.SafeDeleteFolder(dir);
        }
        FileOps.SafeDeleteFolder(CachePath);
      }
      catch (Exception ex) {
        LineLogger(null, new AppLog.LogEventArgs($"Error cleaning updates cache: {ex.Message}"));
      }
    }
    SetControlsState(true);
    LineLogger(null, new AppLog.LogEventArgs($"Windows Update cache cleaned, freed {FileOps.FormatSize(freedBytes)}"));
    return Task.CompletedTask;
  }

  private void menuOptimize_Click(object sender, EventArgs e) {
    SetControlsState(false, "Windows kernel optimization...");
    Task.Run(OptimizeKernel);
  }

  private async Task OptimizeKernel() {
    try {
      var script = @"@echo off
DISM.exe /Online /Set-ReservedStorageState /State:Disabled
Dism.exe /online /cleanup-image /StartComponentCleanup
compact.exe /CompactOS:always";
      var result = await ProcessManager.ExecuteScript(script, Environment.SystemDirectory, onOutput: message => {
        LineLogger(null, new AppLog.LogEventArgs(message));
      });
    }
    catch (Exception ex) {
      LineLogger(null, new AppLog.LogEventArgs($"Error optimizing kernel: {ex.Message}"));
    }
    LineLogger(null, new AppLog.LogEventArgs($"Windows kernel optimization finished."));
    SetControlsState(true);
  }

  private void restoreDefaults_Click(object sender, EventArgs e) {

    chkOffline.Checked = false;
    chkDownload.Checked = true;
    chkManual.Checked = false;
    chkOld.Checked = false;
    chkMsUpd.Checked = false;
    chkBlockMS.Checked = false;
    
    chkDisableAU.Checked = false;
    radDefault.Checked = true;
    
    chkHideWU.Checked = false;
    chkStore.Checked = false;
    chkDrivers.Checked = true;
    
    dlAutoCheck.SelectedIndex = 0;

    MessageBox.Show("Default settings restored.", Updater.ApplicationTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
  }

  private void SetControlsState(bool enabled, string status = null) {
    if (InvokeRequired) {
      BeginInvoke(new Action(() => { SetControlsState(enabled, status); }));
      return;
    }

    if (!enabled) {
      progTotal.Style = ProgressBarStyle.Marquee;
      progTotal.MarqueeAnimationSpeed = 30;
    }
    if (!string.IsNullOrEmpty(status))
      lblStatus.Text = status;
    panStatus.Visible = !enabled;
    panControls.Enabled = enabled;
    panOperations.Enabled = enabled;
    cleanToolStripMenuItem.Enabled = enabled;
    optimizeToolStripMenuItem.Enabled = enabled;
    restoreDefaultsToolStripMenuItem.Enabled = enabled;
  }

  private void menuWuAu_Click(object sender, EventArgs e) {
    wuauMenu.Checked = !wuauMenu.Checked;
    if (wuauMenu.Checked) {
      agent.EnableWuAuServ();
      agent.Init();
    }
    else {
      agent.UnInit();
      agent.EnableWuAuServ(false);
    }

    UpdateState();
  }

  private void menuRefresh_Click(object sender, EventArgs e) {
    BuildToolsMenu();
  }

  private void btnWinUpd_CheckedChanged(object sender, EventArgs e) {
    SwitchList(UpdateLists.PendingUpdates);
  }

  private void btnInstalled_CheckedChanged(object sender, EventArgs e) {
    SwitchList(UpdateLists.InstalledUpdates);
  }

  private void btnHidden_CheckedChanged(object sender, EventArgs e) {
    SwitchList(UpdateLists.HiddenUpdates);
  }

  private void btnHistory_CheckedChanged(object sender, EventArgs e) {
    if (agent.IsActive())
      agent.UpdateHistory();
    SwitchList(UpdateLists.UpdateHistory);
  }

  private void btnSearch_Click(object sender, EventArgs e) {
    if (!agent.IsActive() || agent.IsBusy())
      return;
    var ret = chkOffline.Checked 
      ? agent.SearchForUpdates(chkDownload.Checked, chkOld.Checked) 
      : agent.SearchForUpdates(dlSource.Text, chkOld.Checked);
    ShowResult(WuAgent.AgentOperation.CheckingUpdates, ret);
  }

  private void btnDownload_Click(object sender, EventArgs e) {
    if (!chkManual.Checked && !OperatingSystemHelper.IsAdministrator()) {
      MessageBox.Show("Administrator privileges are required in order to download updates using windows update services. Use 'Manual' download instead.", Updater.ApplicationTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
      return;
    }

    if (!agent.IsActive() || agent.IsBusy())
      return;
    var ret = chkManual.Checked 
      ? agent.DownloadUpdatesManually(GetUpdates()) 
      : agent.DownloadUpdates(GetUpdates());
    ShowResult(WuAgent.AgentOperation.DownloadingUpdates, ret);
  }

  private void btnInstall_Click(object sender, EventArgs e) {
    if (!OperatingSystemHelper.IsAdministrator()) {
      MessageBox.Show("Administrator privileges are required in order to install updates.", Updater.ApplicationTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
      return;
    }

    if (!agent.IsActive() || agent.IsBusy())
      return;
    var ret = chkManual.Checked 
      ? agent.DownloadUpdatesManually(GetUpdates(), true) 
      : agent.DownloadUpdates(GetUpdates(), true);
    ShowResult(WuAgent.AgentOperation.InstallingUpdates, ret);
  }

  private void btnUnInstall_Click(object sender, EventArgs e) {
    if (!OperatingSystemHelper.IsAdministrator()) {
      MessageBox.Show("Administrator privileges are required in order to remove updates.", Updater.ApplicationTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
      return;
    }

    if (!agent.IsActive() || agent.IsBusy())
      return;
    var ret = agent.UnInstallUpdatesManually(GetUpdates());
    ShowResult(WuAgent.AgentOperation.RemovingUpdates, ret);
  }

  private void btnHide_Click(object sender, EventArgs e) {
    if (!agent.IsActive() || agent.IsBusy())
      return;
    switch (currentList) {
      case UpdateLists.PendingUpdates:
        agent.HideUpdates(GetUpdates(), true);
        break;
      case UpdateLists.HiddenUpdates:
        agent.HideUpdates(GetUpdates(), false);
        break;
    }
  }

  private void btnGetLink_Click(object sender, EventArgs e) {
    var links = "";
    foreach (var update in GetUpdates()) {
      links += update.Title + "\r\n";
      foreach (var url in update.Downloads)
        links += url + "\r\n";
      links += "\r\n";
    }

    if (links.Length != 0) {
      Clipboard.SetText(links);
      AppLog.Line("Update Download Links copyed to clipboard");
    }
    else {
      AppLog.Line("No updates selected");
    }
  }

  private void btnCancel_Click(object sender, EventArgs e) {
    agent.CancelOperations();
  }

  private string GetOpStr(WuAgent.AgentOperation op) {
    switch (op) {
      case WuAgent.AgentOperation.CheckingUpdates: return "Checking for Updates";
      case WuAgent.AgentOperation.PreparingCheck: return "Preparing Check";
      case WuAgent.AgentOperation.PreparingUpdates:
      case WuAgent.AgentOperation.DownloadingUpdates: return "Downloading Updates";
      case WuAgent.AgentOperation.InstallingUpdates: return "Installing Updates";
      case WuAgent.AgentOperation.RemovingUpdates: return "Removing Updates";
      case WuAgent.AgentOperation.CancelingOperation: return "Cancelling Operation";
    }

    return "Unknown Operation";
  }

  private void OnProgress(object sender, WuAgent.ProgressArgs args) {
    var status = GetOpStr(agent.CurOperation());

    if (args.TotalCount == -1) {
      progTotal.Style = ProgressBarStyle.Marquee;
      progTotal.MarqueeAnimationSpeed = 30;
      status += "...";
    }
    else {
      progTotal.Style = ProgressBarStyle.Continuous;
      progTotal.MarqueeAnimationSpeed = 0;

      if (args.TotalPercent >= 0 && args.TotalPercent <= 100)
        progTotal.Value = args.TotalPercent;

      if (args.TotalCount > 1)
        status += " " + args.CurrentIndex + "/" + args.TotalCount + " ";

      //if (args.UpdatePercent != 0)
      //    Status += " " + args.UpdatePercent + "%";
    }

    lblStatus.Text = status;
    toolTip.SetToolTip(lblStatus, args.Info);

    UpdateState();
  }

  private void OnUpdates(object sender, WuAgent.UpdatesArgs args) {
    UpdateCounts();
    if (args.Found) // if (agent.CurOperation() == WuAgent.AgentOperation.CheckingUpdates)
    {
      lastCheck = DateTime.Now;
      SetConfig("LastCheck", lastCheck.ToString());
      SwitchList(UpdateLists.PendingUpdates);
    }
    else {
      LoadList();

      if (MiscFunc.ParseInt(Program.IniReadValue("Options", "Refresh", "0")) == 1 &&
          (agent.CurOperation() == WuAgent.AgentOperation.InstallingUpdates ||
           agent.CurOperation() == WuAgent.AgentOperation.RemovingUpdates))
        doUpdate = true;
    }
  }

  private void OnFinished(object sender, WuAgent.FinishedArgs args) {
    UpdateState();
    lblStatus.Text = "";
    toolTip.SetToolTip(lblStatus, "");

    ShowResult(args.Op, args.Ret, args.RebootNeeded);
  }

  private void ShowResult(WuAgent.AgentOperation op, WuAgent.RetCodes ret, bool reboot = false) {
    if (op == WuAgent.AgentOperation.DownloadingUpdates && chkManual.Checked) {
      if (ret == WuAgent.RetCodes.Success) {
        MessageBox.Show($"Updates downloaded to {agent.DlPath}, ready to be installed by the user.", Updater.ApplicationTitle, MessageBoxButtons.OK,
            MessageBoxIcon.Information);
        return;
      }

      if (ret == WuAgent.RetCodes.DownloadFailed) {
        MessageBox.Show($"Updates downloaded to {agent.DlPath}, some updates failed to download.", Updater.ApplicationTitle, MessageBoxButtons.OK,
            MessageBoxIcon.Exclamation);
        return;
      }
    }

    if (op == WuAgent.AgentOperation.InstallingUpdates && reboot) {
      if (ret == WuAgent.RetCodes.Success) {
        MessageBox.Show("Updates successfully installed, however, a reboot is required.", Updater.ApplicationTitle, MessageBoxButtons.OK,
            MessageBoxIcon.Information);
        return;
      }

      if (ret == WuAgent.RetCodes.DownloadFailed) {
        MessageBox.Show("Installation of some Updates has failed, also a reboot is required.", Updater.ApplicationTitle, MessageBoxButtons.OK,
            MessageBoxIcon.Exclamation);
        return;
      }
    }

    var status = "";
    switch (ret) {
      case WuAgent.RetCodes.Success:
      case WuAgent.RetCodes.Aborted:
      case WuAgent.RetCodes.InProgress: return;
      case WuAgent.RetCodes.AccessError:
        status = "Required privileges are not available";
        break;
      case WuAgent.RetCodes.Busy:
        status = "Another operation is already in progress";
        break;
      case WuAgent.RetCodes.DownloadFailed:
        status = "Download failed";
        break;
      case WuAgent.RetCodes.InstallFailed:
        status = "Installation failed";
        break;
      case WuAgent.RetCodes.NoUpdated:
        status = "No selected updates or no updates eligible for the operation";
        break;
      case WuAgent.RetCodes.InternalError:
        status = "Internal error";
        break;
      case WuAgent.RetCodes.FileNotFound:
        status = "Required file(s) could not be found";
        break;
    }

    var action = GetOpStr(op);

    resultShown = true;
    MessageBox.Show($"{action} failed: {status}.", Updater.ApplicationTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
    resultShown = false;
  }

  private void dlSource_SelectedIndexChanged(object sender, EventArgs e) {
    SetConfig("Source", dlSource.Text);
  }

  private void chkOffline_CheckedChanged(object sender, EventArgs e) {
    dlSource.Enabled = !chkOffline.Checked;
    chkDownload.Enabled = chkOffline.Checked;

    SetConfig("Offline", chkOffline.Checked ? "1" : "0");
  }

  private void chkDownload_CheckedChanged(object sender, EventArgs e) {
    SetConfig("Download", chkDownload.Checked ? "1" : "0");
  }

  private void chkOld_CheckedChanged(object sender, EventArgs e) {
    SetConfig("IncludeOld", chkOld.Checked ? "1" : "0");
  }

  private void chkDrivers_CheckStateChanged(object sender, EventArgs e) {
    if (mSuspendUpdate)
      return;
    Gpo.ConfigDriverAu((int)chkDrivers.CheckState);
  }

  private void dlShDay_SelectedIndexChanged(object sender, EventArgs e) {
    if (mSuspendUpdate)
      return;
    Gpo.ConfigAu(Gpo.AuOptions.Scheduled, dlShDay.SelectedIndex, dlShTime.SelectedIndex);
  }

  private void dlShTime_SelectedIndexChanged(object sender, EventArgs e) {
    if (mSuspendUpdate)
      return;
    Gpo.ConfigAu(Gpo.AuOptions.Scheduled, dlShDay.SelectedIndex, dlShTime.SelectedIndex);
  }

  private void radGPO_CheckedChanged(object sender, EventArgs e) {
    dlShDay.Enabled = dlShTime.Enabled = radSchedule.Checked;

    if (radDisable.Checked)
      switch (mGpoRespect) {
        case Gpo.Respect.Partial:
          if (chkBlockMS.Checked) {
            chkDisableAU.Enabled = true;
            break;
          }

          goto case Gpo.Respect.None;
        case Gpo.Respect.None:
          chkDisableAU.Enabled = false;
          chkDisableAU.Checked = true;
          break;
        case Gpo.Respect.Full: // we can do whatever we want
          chkDisableAU.Enabled = mWinVersion >= 10;
          break;
      }
    else
      chkDisableAU.Enabled = false;

    if (mSuspendUpdate)
      return;

    if (radDisable.Checked) {
      if (chkDisableAU.Checked) {
        var test = Gpo.GetDisableAu();
        Gpo.DisableAu(true);
        if (!test)
          MessageBox.Show("For the new configuration to fully take effect a reboot is required.", Updater.ApplicationTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
      }

      Gpo.ConfigAu(Gpo.AuOptions.Disabled);
    }
    else {
      chkDisableAU.Checked = false; // Note: this triggers chkDisableAU_CheckedChanged

      if (radNotify.Checked)
        Gpo.ConfigAu(Gpo.AuOptions.Notification);
      else if (radDownload.Checked)
        Gpo.ConfigAu(Gpo.AuOptions.Download);
      else if (radSchedule.Checked)
        Gpo.ConfigAu(Gpo.AuOptions.Scheduled, dlShDay.SelectedIndex, dlShTime.SelectedIndex);
      else //if (radDefault.Checked)
        Gpo.ConfigAu(Gpo.AuOptions.Default);
    }
  }

  private void chkBlockMS_CheckedChanged(object sender, EventArgs e) {
    if (mSuspendUpdate)
      return;

    if (radDisable.Checked && mGpoRespect == Gpo.Respect.Partial) {
      if (chkBlockMS.Checked) {
        chkDisableAU.Enabled = true;
      }
      else {
        if (!chkDisableAU.Checked)
          switch (MessageBox.Show("Your version of Windows does not respect the standard GPO's, to keep automatic Windows updates blocked, update facilitation services must be disabled.", Updater.ApplicationTitle, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning)) {
            case DialogResult.Yes:
              chkDisableAU.Checked = true; // Note: this triggers chkDisableAU_CheckedChanged
              break;
            case DialogResult.No:
              radDefault.Checked = true;
              break;
            case DialogResult.Cancel:
              mSuspendUpdate = true;
              chkBlockMS.Checked = true;
              mSuspendUpdate = false;
              return;
          }

        chkDisableAU.Enabled = false;
      }
    }

    Gpo.BlockMs(chkBlockMS.Checked);
  }

  private void chkDisableAU_CheckedChanged(object sender, EventArgs e) {
    if (chkDisableAU.Checked) {
      chkHideWU.Checked = true;
      chkHideWU.Enabled = false;
    }
    else {
      //chkHideWU.Checked = false;
      chkHideWU.Enabled = true;
    }

    if (mSuspendUpdate)
      return;
    var test = Gpo.GetDisableAu();
    Gpo.DisableAu(chkDisableAU.Checked);
    if (test != chkDisableAU.Checked)
      MessageBox.Show("For the new configuration to fully take effect a reboot is required.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
  }

  private void chkAutoRun_CheckedChanged(object sender, EventArgs e) {
    chkAutoRun.Checked = !chkAutoRun.Checked;
    notifyIcon.Visible = dlAutoCheck.Enabled = chkAutoRun.Checked;
    autoUpdate = chkAutoRun.Checked ? (AutoUpdateOptions)dlAutoCheck.SelectedIndex : AutoUpdateOptions.No;
    if (mSuspendUpdate)
      return;
    if (chkAutoRun.CheckState == CheckState.Indeterminate)
      return;
    if (OperatingSystemHelper.IsRunningAsUwp()) {
      if (chkAutoRun.CheckState == CheckState.Checked) {
        mSuspendUpdate = true;
        chkAutoRun.CheckState = CheckState.Indeterminate;
        mSuspendUpdate = false;
      }

      return;
    }

    Program.AutoStart(chkAutoRun.Checked);
  }

  private void chkAutoUpdateApp_Click(object sender, EventArgs e) {
    Updater.AutoUpdate = !Updater.AutoUpdate;
    chkAutoUpdateApp.Checked = Updater.AutoUpdate;
    SetConfig("AutoUpdate", Updater.AutoUpdate ? "1" : "0");
  }

  private void dlAutoCheck_SelectedIndexChanged(object sender, EventArgs e) {
    if (mSuspendUpdate)
      return;
    SetConfig("AutoUpdate", dlAutoCheck.SelectedIndex.ToString());
    autoUpdate = (AutoUpdateOptions)dlAutoCheck.SelectedIndex;
  }

  private void chkNoUAC_CheckedChanged(object sender, EventArgs e) {
    if (mSuspendUpdate)
      return;
    chkNoUAC.Checked = !chkNoUAC.Checked;
    Program.SkipUacEnable(chkNoUAC.Checked);
  }

  private void chkMsUpd_CheckedChanged(object sender, EventArgs e) {
    if (mSuspendUpdate)
      return;
    var source = dlSource.Text;
    agent.EnableService(WuAgent.MsUpdGuid, chkMsUpd.Checked);
    LoadProviders(source);
  }

  private void chkManual_CheckedChanged(object sender, EventArgs e) {
    UpdateState();
    SetConfig("Manual", chkManual.Checked ? "1" : "0");
  }

  private void chkHideWU_CheckedChanged(object sender, EventArgs e) {
    if (mSuspendUpdate)
      return;
    Gpo.HideUpdatePage(chkHideWU.Checked);
  }

  private void chkStore_CheckedChanged(object sender, EventArgs e) {
    if (mSuspendUpdate)
      return;
    Gpo.SetStoreAu(chkStore.Checked);
  }

  private void updateView_SelectedIndexChanged(object sender, EventArgs e) {
    lblSupport.Visible = false;
    if (updateView.SelectedItems.Count != 1) return;
    var update = (MsUpdate)updateView.SelectedItems[0].Tag;
    if (update.Kb == null || update.Kb.Length <= 2) return;
    lblSupport.Links[0].LinkData = "https://support.microsoft.com/help/" + update.Kb.Substring(2);
    lblSupport.Links[0].Visited = false;
    lblSupport.Visible = true;
    toolTip.SetToolTip(lblSupport, lblSupport.Links[0].LinkData.ToString());
  }

  private void lblSupport_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) {
    if (e.Link.LinkData is string target) Process.Start(target);
  }

  private string GetConfig(string name, string def = "") {
    return Program.IniReadValue("Options", name, def);
  }

  private void SetConfig(string name, string value) {
    if (mSuspendUpdate)
      return;
    Program.IniWriteValue("Options", name, value);
  }

  private void notifyIcon_BalloonTipClicked(object sender, EventArgs e) {
    if (!allowShowDisplay) {
      allowShowDisplay = true;
      Show();
    }

    if (WindowState == FormWindowState.Minimized)
      WindowState = FormWindowState.Normal;
    WinApiHelper.SetForegroundWindow(Handle);
  }

  private void updateView_ColumnClick(object sender, ColumnClickEventArgs e) {
    updateView.ListViewItemSorter ??= new ListViewItemComparer();
    ((ListViewItemComparer)updateView.ListViewItemSorter).Update(e.Column);
    updateView.Sort();
  }

  protected override bool ProcessCmdKey(ref Message msg, Keys keyData) {
    switch (keyData) {
      case Keys.Control | Keys.F:
        txtFilter.SelectAll();
        txtFilter.Focus();
        return true;
      case Keys.Control | Keys.C: {
        var info = "";
        foreach (ListViewItem item in updateView.SelectedItems) {
          if (info.Length != 0)
            info += "\r\n";
          info += item.Text;
          for (var i = 1; i < item.SubItems.Count; i++)
            info += "; " + item.SubItems[i].Text;
        }

        if (info.Length != 0)
          Clipboard.SetText(info);
        return true;
      }
      default:
        return base.ProcessCmdKey(ref msg, keyData);
    }
  }

  private void btnSearchOff_Click(object sender, EventArgs e) {
    mSearchFilter = null;
    LoadList();
  }

  private void txtFilter_TextChanged(object sender, EventArgs e) {
    mSearchFilter = txtFilter.Text;
    bUpdateList = true;
  }

  private void chkGrupe_CheckedChanged(object sender, EventArgs e) {
    if (mSuspendUpdate)
      return;

    updateView.ShowGroups = chkGrupe.Checked;
    SetConfig("GroupUpdates", chkGrupe.Checked ? "1" : "0");
  }

  private void chkAll_CheckedChanged(object sender, EventArgs e) {
    if (ignoreChecks)
      return;

    ignoreChecks = true;

    foreach (ListViewItem item in updateView.Items)
      item.Checked = chkAll.Checked;

    ignoreChecks = false;

    checkChecks = true;
  }

  private void updateView_ItemChecked(object sender, ItemCheckedEventArgs e) {
    if (ignoreChecks)
      return;

    ignoreChecks = true;

    if (updateView.CheckedItems.Count == 0)
      chkAll.CheckState = CheckState.Unchecked;
    else if (updateView.CheckedItems.Count == updateView.Items.Count)
      chkAll.CheckState = CheckState.Checked;
    else
      chkAll.CheckState = CheckState.Indeterminate;

    ignoreChecks = false;

    checkChecks = true;
  }

  private enum AutoUpdateOptions {
    No = 0,
    EveryDay,
    EveryWeek,
    EveryMonth
  }

  private enum UpdateLists {
    PendingUpdates,
    InstalledUpdates,
    HiddenUpdates,
    UpdateHistory
  }

  // Implements the manual sorting of items by columns.
  private class ListViewItemComparer : IComparer {
    private int col = 0;
    private int inv = 1;

    public int Compare(object x, object y) {
      if (col == 3) // date
        return ((DateTime)((ListViewItem)y).SubItems[col].Tag).CompareTo(
            (DateTime)((ListViewItem)x).SubItems[col].Tag) * inv;
      if (col == 4) // size
        return ((decimal)((ListViewItem)y).SubItems[col].Tag).CompareTo(
            (decimal)((ListViewItem)x).SubItems[col].Tag) * inv;
      return string.Compare(((ListViewItem)x).SubItems[col].Text, ((ListViewItem)y).SubItems[col].Text) * inv;
    }

    public void Update(int column) {
      inv = col == column ? -inv : 1;
      col = column;
    }
  }

  private void updateView_SizeChanged(object sender, EventArgs e) {
    var otherColumnsWidth = 0;
    for (var i = 1; i < updateView.Columns.Count; i++)
      otherColumnsWidth += updateView.Columns[i].Width;
    updateView.Columns[0].Width = updateView.Width - otherColumnsWidth - 30;
  }

  private void UpdateView_ColumnWidthChanged(object sender, ColumnWidthChangedEventArgs e) {
    if (e.ColumnIndex == 0)
      return;
    updateView_SizeChanged(sender, EventArgs.Empty);
  }

  private void selectUIFontToolStripMenuItem_Click(object sender, EventArgs e) {
    using (var fontDialog = new FontDialog()) {
      fontDialog.ShowColor = false;
      fontDialog.Font = Font;
      if (fontDialog.ShowDialog() != DialogResult.OK)
        return;
      if (fontDialog.Font == Font)
        return;
      Font = fontDialog.Font;
      SetConfig("UIFont", $"{Font.Name};{Font.Size};{(int)Font.Style}");
    }
  }

  private void TryApplyUIFont() {
    try {
      var serialized = GetConfig("UIFont", null);
      if (string.IsNullOrWhiteSpace(serialized))
        return;
      string[] parts = serialized.Split(';');
      string name = parts[0];
      float size = float.Parse(parts[1], CultureInfo.InvariantCulture);
      FontStyle style = (FontStyle)int.Parse(parts[2]);
      Font = new Font(name, size, style);
    }
    catch (Exception ex) {
      AppLog.Line($"Error restoring saved UI font: {ex.Message}");
    }
  }
}
