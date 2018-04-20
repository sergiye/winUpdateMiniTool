using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using winUpdateMiniTool.Common;
using winUpdateMiniTool.Properties;
using WUApiLib;

namespace winUpdateMiniTool;

public partial class WuMgr : Form {
  private const int WmSyscommand = 0x112;

  public const int MF_BITMAP = 0x00000004;
  public const int MF_CHECKED = 0x00000008;
  public const int MF_DISABLED = 0x00000002;
  public const int MF_ENABLED = 0x00000000;
  public const int MF_GRAYED = 0x00000001;
  public const int MF_MENUBARBREAK = 0x00000020;
  public const int MF_MENUBREAK = 0x00000040;
  public const int MF_OWNERDRAW = 0x00000100;
  private const int MfPopup = 0x00000010;
  private const int MfSeparator = 0x00000800;
  public const int MF_STRING = 0x00000000;
  public const int MF_UNCHECKED = 0x00000000;
  private const int MfByposition = 0x400;

  public const int MF_BYCOMMAND = 0x000;

  //public const Int32 MF_REMOVE = 0x1000;
  private const int MymenuAbout = 1000;
  private static Timer mTimer;
  private readonly WuAgent agent;
  private readonly int idleDelay;
  private readonly Gpo.Respect mGpoRespect = Gpo.Respect.Unknown;
  private readonly float mSearchBoxHeight;
  private readonly MenuItem mToolsMenu;
  private readonly float mWinVersion;
  private bool allowshowdisplay = true;
  private AutoUpdateOptions autoUpdate = AutoUpdateOptions.No;
  private bool bUpdateList;
  private bool checkChecks;
  private UpdateLists currentList = UpdateLists.UpdateHistory;
  private bool doUpdte;
  private bool ignoreChecks;
  private DateTime lastBaloon = DateTime.MinValue;
  private DateTime lastCheck = DateTime.MaxValue;
  private string mSearchFilter;
  private bool mSuspendUpdate;
  private bool resultShown;
  private bool suspendChange;
  private MenuItem wuauMenu;

  public WuMgr() {
    InitializeComponent();

    Icon = Icon.ExtractAssociatedIcon(typeof(WuMgr).Assembly.Location);
    notifyIcon.Icon = Icon;
    notifyIcon.Text = Program.APP_TITLE;

    if (Program.TestArg("-tray")) {
      allowshowdisplay = false;
      notifyIcon.Visible = true;
    }

    if (!MiscFunc.IsRunningAsUwp())
      Text = $"{Program.APP_TITLE} v{Program.MVersion}";

    Localize();

    btnSearch.Image = new Bitmap(Resources.icons8_available_updates_32, new Size(25, 25));
    btnInstall.Image = new Bitmap(Resources.icons8_software_installer_32, new Size(25, 25));
    btnDownload.Image = new Bitmap(Resources.icons8_downloading_updates_32, new Size(25, 25));
    btnUnInstall.Image = new Bitmap(Resources.icons8_trash_32, new Size(25, 25));
    btnHide.Image = new Bitmap(Resources.icons8_hide_32, new Size(25, 25));
    btnGetLink.Image = new Bitmap(Resources.icons8_link_32, new Size(25, 25));
    btnCancel.Image = new Bitmap(Resources.icons8_cancel_32, new Size(25, 25));

    AppLog.Logger += LineLogger;

    foreach (var line in AppLog.GetLog())
      logBox.AppendText(line + Environment.NewLine);
    logBox.ScrollToCaret();


    agent = WuAgent.GetInstance();
    agent.Progress += OnProgress;
    agent.UpdatesChanged += OnUpdates;
    agent.Finished += OnFinished;

    if (!agent.IsActive())
      if (MessageBox.Show("Windows Update Service is not available, try to start it?", Program.APP_TITLE, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) {
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

    int day, time;
    switch (Gpo.GetAu(out day, out time)) {
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
    }

    chkAutoRun.Checked = Program.IsAutoStart();
    if (MiscFunc.IsRunningAsUwp() && chkAutoRun.CheckState == CheckState.Checked)
      chkAutoRun.Enabled = false;
    idleDelay = MiscFunc.ParseInt(GetConfig("IdleDelay", "20"));
    chkNoUAC.Checked = Program.IsSkipUacRun();
    chkNoUAC.Enabled = MiscFunc.IsAdministrator();
    chkNoUAC.Visible = chkNoUAC.Enabled || chkNoUAC.Checked || !MiscFunc.IsRunningAsUwp();


    chkOffline.Checked = MiscFunc.ParseInt(GetConfig("Offline", "0")) != 0;
    chkDownload.Checked = MiscFunc.ParseInt(GetConfig("Download", "1")) != 0;
    chkManual.Checked = MiscFunc.ParseInt(GetConfig("Manual", "0")) != 0;
    if (!MiscFunc.IsAdministrator()) {
      if (MiscFunc.IsRunningAsUwp()) {
        chkOffline.Enabled = false;
        chkOffline.Checked = false;

        chkManual.Enabled = false;
        chkManual.Checked = true;
      }

      chkMsUpd.Enabled = false;
    }

    chkMsUpd.Checked = agent.IsActive() && agent.TestService(WuAgent.MsUpdGuid);

    // Note: when running in the UWP sandbox we cant write the real registry even as admins
    if (!MiscFunc.IsAdministrator() || MiscFunc.IsRunningAsUwp())
      foreach (Control ctl in tabAU.Controls)
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

    mSearchBoxHeight = panelList.RowStyles[2].Height;
    panelList.RowStyles[2].Height = 0;

    chkGrupe.Checked = MiscFunc.ParseInt(GetConfig("GroupUpdates", "1")) != 0;
    updateView.ShowGroups = chkGrupe.Checked;

    mSuspendUpdate = false;


    if (Program.TestArg("-provisioned"))
      tabs.Enabled = false;


    mToolsMenu = new MenuItem();
    mToolsMenu.Text = "&Tools";

    BuildToolsMenu();

    notifyIcon.ContextMenu = new ContextMenu();

    MenuItem menuExit = new();
    menuExit.Text = "E&xit";
    menuExit.Click += menuExit_Click;

    notifyIcon.ContextMenu.MenuItems.AddRange([mToolsMenu, new MenuItem("-"), menuExit]);


    var menuHandle = GetSystemMenu(Handle, false); // Note: to restore default set true
    InsertMenu(menuHandle, 5, MfByposition | MfSeparator, 0, string.Empty); // <-- Add a menu seperator
    InsertMenu(menuHandle, 6, MfByposition | MfPopup, (int)mToolsMenu.Handle, mToolsMenu.Text);
    // InsertMenu(MenuHandle, 7, MF_BYPOSITION, MYMENU_ABOUT, menuAbout.Text);


    UpdateCounts();
    SwitchList(UpdateLists.UpdateHistory);

    doUpdte = Program.TestArg("-update");

    mTimer = new Timer();
    mTimer.Interval = 250; // 4 times per second
    mTimer.Tick += OnTimedEvent;
    mTimer.Enabled = true;

    Program.Ipc.PipeMessage += PipesMessageHandler;
    Program.Ipc.Listen();
  }

  [DllImport("user32.dll")]
  private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

  [DllImport("user32.dll")]
  private static extern bool InsertMenu(IntPtr hMenu, int wPosition, int wFlags, int wIdNewItem, string lpNewItem);

  [DllImport("user32.dll")]
  private static extern bool RemoveMenu(IntPtr hMenu, uint uPosition, uint uFlags);

  private void LineLogger(object sender, AppLog.LogEventArgs args) {
    logBox.AppendText(args.Message + Environment.NewLine);
    logBox.ScrollToCaret();
  }

  protected override void SetVisibleCore(bool value) {
    base.SetVisibleCore(allowshowdisplay ? value : allowshowdisplay);
  }

  private void PipesMessageHandler(PipeIpc.PipeServer pipe, string data) {
    if (data.Equals("show", StringComparison.CurrentCultureIgnoreCase)) {
      notifyIcon_BalloonTipClicked(null, null);
      pipe.Send("ok");
    }
    else {
      pipe.Send("unknown");
    }
  }

  private void OnTimedEvent(object source, EventArgs e) {
    var updateNow = false;
    if (notifyIcon.Visible) {
      var daysDue = GetAutoUpdateDue();
      if (daysDue != 0 && !agent.IsBusy()) {
        // ensure we only start a check when user is not doing anything
        var idleTime = MiscFunc.GetIdleTime();
        if (idleDelay * 60 < idleTime) {
          AppLog.Line("Starting automatic search for updates.");
          updateNow = true;
        }
        else if (daysDue > GetGraceDays()) {
          if (lastBaloon < DateTime.Now.AddHours(-4)) {
            lastBaloon = DateTime.Now;
            notifyIcon.ShowBalloonTip(int.MaxValue, "Please Check For Updates",
              $"{Program.APP_TITLE} couldn't check for updates for {daysDue} days, please check for updates manually and resolve possible issues", ToolTipIcon.Warning);
          }
        }
      }

      if (agent.MPendingUpdates.Count > 0)
        if (lastBaloon < DateTime.Now.AddHours(-4)) {
          lastBaloon = DateTime.Now;
          notifyIcon.ShowBalloonTip(int.MaxValue, "New Updates found",
              string.Format("{0} has found {1} new updates, please review the updates and install them", Program.APP_TITLE,
                  string.Join(Environment.NewLine, agent.MPendingUpdates.Select(x => $"- {x.Title}"))),
              ToolTipIcon.Info);
        }
    }

    if ((doUpdte || updateNow && !resultShown) && agent.IsActive()) {
      doUpdte = false;
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

  private void WuMgr_Load(object sender, EventArgs e) {
    Width = 900;
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
    switch (autoUpdate) {
      case AutoUpdateOptions.EveryMonth: return 15;
      default: return 3;
    }
  }

  private void WuMgr_FormClosing(object sender, FormClosingEventArgs e) {
    if (notifyIcon.Visible && allowshowdisplay) {
      e.Cancel = true;
      allowshowdisplay = false;
      Hide();
      return;
    }

    agent.Progress -= OnProgress;
    agent.UpdatesChanged -= OnUpdates;
    agent.Finished -= OnFinished;
  }

  private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e) {
    if (allowshowdisplay) {
      allowshowdisplay = false;
      Hide();
    }
    else {
      allowshowdisplay = true;
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
    updateView.ForeColor = updateView.CheckBoxes && !agent.IsValid() ? Color.Gray : Color.Black;

    switch (currentList) {
      case UpdateLists.PendingUpdates:
        LoadList(agent.MPendingUpdates);
        break;
      case UpdateLists.InstaledUpdates:
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
    for (var i = 0; i < list.Count; i++) {
      var update = list[i];
      var state = "";
      switch (update.State) {
        case MsUpdate.UpdateState.History:
          switch ((OperationResultCode)update.ResultCode) {
            case OperationResultCode.orcNotStarted:
              state = "Not Started";
              break;
            case OperationResultCode.orcInProgress:
              state = "In Progress";
              break;
            case OperationResultCode.orcSucceeded:
              state = "Succeeded";
              break;
            case OperationResultCode.orcSucceededWithErrors:
              state = "Succeeded with Errors";
              break;
            case OperationResultCode.orcFailed:
              state = "Failed";
              break;
            case OperationResultCode.orcAborted:
              state = "Aborted";
              break;
          }

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


      string[] strings =
      [
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

      ListViewItem item = new(strings);
      item.SubItems[3].Tag = update.Date;
      item.SubItems[4].Tag = update.Size;


      item.Tag = update;

      if (currentList == UpdateLists.PendingUpdates) {
        if (MiscFunc.ParseInt(Program.IniReadValue(update.Kb, "BlackList", "0", iniPath)) != 0)
          item.Font = new Font(item.Font.FontFamily, item.Font.Size, FontStyle.Strikeout);
        else if (MiscFunc.ParseInt(Program.IniReadValue(update.Kb, "Select", "0", iniPath)) != 0)
          item.Checked = true;
      }
      else if (currentList == UpdateLists.InstaledUpdates) {
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

  public List<MsUpdate> GetUpdates() {
    List<MsUpdate> updates = [];
    foreach (ListViewItem item in updateView.CheckedItems)
      updates.Add((MsUpdate)item.Tag);
    return updates;
  }

  private void SwitchList(UpdateLists list) {
    if (suspendChange)
      return;

    suspendChange = true;
    btnWinUpd.CheckState = list == UpdateLists.PendingUpdates ? CheckState.Checked : CheckState.Unchecked;
    btnInstalled.CheckState = list == UpdateLists.InstaledUpdates ? CheckState.Checked : CheckState.Unchecked;
    btnHidden.CheckState = list == UpdateLists.HiddenUpdates ? CheckState.Checked : CheckState.Unchecked;
    btnHistory.CheckState = list == UpdateLists.UpdateHistory ? CheckState.Checked : CheckState.Unchecked;
    suspendChange = false;

    currentList = list;

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
    btnCancel.Visible = busy;
    progTotal.Visible = busy;
    lblStatus.Visible = busy;

    var isValid = agent.IsValid();
    var isValid2 = isValid || chkManual.Checked;

    var admin = MiscFunc.IsAdministrator() || !MiscFunc.IsRunningAsUwp();

    var enable = agent.IsActive() && !busy;
    btnSearch.Enabled = enable;
    btnDownload.Enabled = isChecked && enable && isValid2 && currentList == UpdateLists.PendingUpdates;
    btnInstall.Enabled = isChecked && admin && enable && isValid2 && currentList == UpdateLists.PendingUpdates;
    btnUnInstall.Enabled = isChecked && admin && enable && currentList == UpdateLists.InstaledUpdates;
    btnHide.Enabled = isChecked && enable && isValid &&
                      (currentList == UpdateLists.PendingUpdates || currentList == UpdateLists.HiddenUpdates);
    btnGetLink.Enabled = isChecked && currentList != UpdateLists.UpdateHistory;
  }

  private void BuildToolsMenu() {
    wuauMenu = new MenuItem();
    wuauMenu.Text = "Windows Update Service";
    wuauMenu.Checked = agent.TestWuAuServ();
    wuauMenu.Click += menuWuAu_Click;
    mToolsMenu.MenuItems.Add(wuauMenu);
    mToolsMenu.MenuItems.Add(new MenuItem("-"));

    if (Directory.Exists(Program.GetToolsPath())) {
      foreach (var subDir in Directory.GetDirectories(Program.GetToolsPath())) {
        var iniName = Path.GetFileName(subDir);
        var iniPath = subDir + @"\" + iniName + ".ini";

        MenuItem toolMenu = new();
        toolMenu.Text = Program.IniReadValue("Root", "Name", iniName, iniPath);

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

            MenuItem subMenu = new();
            subMenu.Text = name;

            var entryExec = Program.IniReadValue("Entry" + i, "Exec", "", iniPath);
            var entrySilent = MiscFunc.ParseInt(Program.IniReadValue("Entry" + i, "Silent", "0", iniPath)) != 0;
            subMenu.Click += delegate (object sender, EventArgs e) {
              menuExec_Click(sender, e, entryExec, subDir, entrySilent);
            };

            toolMenu.MenuItems.Add(subMenu);
          }
        }

        mToolsMenu.MenuItems.Add(toolMenu);
      }

      mToolsMenu.MenuItems.Add(new MenuItem("-"));
    }

    mToolsMenu.MenuItems.Add(new MenuItem("&Refresh", menuRefresh_Click));
  }

  private void menuExec_Click(object sender, EventArgs e, string exec, string dir, bool silent = false) {
    var startInfo = Program.PrepExec(exec, silent);
    startInfo.WorkingDirectory = dir;
    if (!Program.DoExec(startInfo))
      MessageBox.Show("Failed to start tool", Program.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Warning);
  }

  private void menuExit_Click(object sender, EventArgs e) {
    Application.Exit();
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
    var menuHandle = GetSystemMenu(Handle, false); // Note: to restore default set true
    RemoveMenu(menuHandle, 6, MfByposition);
    mToolsMenu.MenuItems.Clear();
    BuildToolsMenu();
    InsertMenu(menuHandle, 6, MfByposition | MfPopup, (int)mToolsMenu.Handle, "&Tools");
  }

  private void btnWinUpd_CheckedChanged(object sender, EventArgs e) {
    SwitchList(UpdateLists.PendingUpdates);
  }

  private void btnInstalled_CheckedChanged(object sender, EventArgs e) {
    SwitchList(UpdateLists.InstaledUpdates);
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
    var ret = WuAgent.RetCodes.Undefined;
    if (chkOffline.Checked)
      ret = agent.SearchForUpdates(chkDownload.Checked, chkOld.Checked);
    else
      ret = agent.SearchForUpdates(dlSource.Text, chkOld.Checked);
    ShowResult(WuAgent.AgentOperation.CheckingUpdates, ret);
  }

  private void btnDownload_Click(object sender, EventArgs e) {
    if (!chkManual.Checked && !MiscFunc.IsAdministrator()) {
      MessageBox.Show("Administrator privileges are required in order to download updates using windows update services. Use 'Manual' download instead.", Program. APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Information);
      return;
    }

    if (!agent.IsActive() || agent.IsBusy())
      return;
    var ret = WuAgent.RetCodes.Undefined;
    if (chkManual.Checked)
      ret = agent.DownloadUpdatesManually(GetUpdates());
    else
      ret = agent.DownloadUpdates(GetUpdates());
    ShowResult(WuAgent.AgentOperation.DownloadingUpdates, ret);
  }

  private void btnInstall_Click(object sender, EventArgs e) {
    if (!MiscFunc.IsAdministrator()) {
      MessageBox.Show("Administrator privileges are required in order to install updates.", Program.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Information);
      return;
    }

    if (!agent.IsActive() || agent.IsBusy())
      return;
    var ret = WuAgent.RetCodes.Undefined;
    if (chkManual.Checked)
      ret = agent.DownloadUpdatesManually(GetUpdates(), true);
    else
      ret = agent.DownloadUpdates(GetUpdates(), true);
    ShowResult(WuAgent.AgentOperation.InstallingUpdates, ret);
  }

  private void btnUnInstall_Click(object sender, EventArgs e) {
    if (!MiscFunc.IsAdministrator()) {
      MessageBox.Show("Administrator privileges are required in order to remove updates.", Program.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Information);
      return;
    }

    if (!agent.IsActive() || agent.IsBusy())
      return;
    var ret = WuAgent.RetCodes.Undefined;
    ret = agent.UnInstallUpdatesManually(GetUpdates());
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
        doUpdte = true;
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
        MessageBox.Show($"Updates downloaded to {agent.DlPath}, ready to be installed by the user.", Program.APP_TITLE, MessageBoxButtons.OK,
            MessageBoxIcon.Information);
        return;
      }

      if (ret == WuAgent.RetCodes.DownloadFailed) {
        MessageBox.Show($"Updates downloaded to {agent.DlPath}, some updates failed to download.", Program.APP_TITLE, MessageBoxButtons.OK,
            MessageBoxIcon.Exclamation);
        return;
      }
    }

    if (op == WuAgent.AgentOperation.InstallingUpdates && reboot) {
      if (ret == WuAgent.RetCodes.Success) {
        MessageBox.Show("Updates successfully installed, however, a reboot is required.", Program.APP_TITLE, MessageBoxButtons.OK,
            MessageBoxIcon.Information);
        return;
      }

      if (ret == WuAgent.RetCodes.DownloadFailed) {
        MessageBox.Show("Installation of some Updates has failed, also a reboot is required.", Program.APP_TITLE, MessageBoxButtons.OK,
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
    MessageBox.Show($"{action} failed: {status}.", Program.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
          MessageBox.Show("For the new configuration to fully take effect a reboot is required.", Program.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Information);
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
          switch (MessageBox.Show("Your version of Windows does not respect the standard GPO's, to keep automatic Windows updates blocked, update facilitation services must be disabled.", Program.APP_TITLE, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning)) {
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
    notifyIcon.Visible = dlAutoCheck.Enabled = chkAutoRun.Checked;
    autoUpdate = chkAutoRun.Checked ? (AutoUpdateOptions)dlAutoCheck.SelectedIndex : AutoUpdateOptions.No;
    if (mSuspendUpdate)
      return;
    if (chkAutoRun.CheckState == CheckState.Indeterminate)
      return;
    if (MiscFunc.IsRunningAsUwp()) {
      if (chkAutoRun.CheckState == CheckState.Checked) {
        mSuspendUpdate = true;
        chkAutoRun.CheckState = CheckState.Indeterminate;
        mSuspendUpdate = false;
      }

      return;
    }

    Program.AutoStart(chkAutoRun.Checked);
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
    if (updateView.SelectedItems.Count == 1) {
      var update = (MsUpdate)updateView.SelectedItems[0].Tag;
      if (update.Kb != null && update.Kb.Length > 2) {
        lblSupport.Links[0].LinkData = "https://support.microsoft.com/help/" + update.Kb.Substring(2);
        lblSupport.Links[0].Visited = false;
        lblSupport.Visible = true;
        toolTip.SetToolTip(lblSupport, lblSupport.Links[0].LinkData.ToString());
      }
    }
  }

  private void lblSupport_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) {
    var target = e.Link.LinkData as string;
    Process.Start(target);
  }


  public string GetConfig(string name, string def = "") {
    return Program.IniReadValue("Options", name, def);
    //var subKey = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Xanatos\Windows Update Manager", true);
    //return subKey.GetValue(name, def).ToString();
  }

  public void SetConfig(string name, string value) {
    if (mSuspendUpdate)
      return;
    Program.IniWriteValue("Options", name, value);
    //var subKey = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Xanatos\Windows Update Manager", true);
    //subKey.SetValue(name, value);
  }

  [DllImport("User32.dll")]
  public static extern int SetForegroundWindow(int hWnd);

  private void notifyIcon_BalloonTipClicked(object sender, EventArgs e) {
    if (!allowshowdisplay) {
      allowshowdisplay = true;
      Show();
    }

    if (WindowState == FormWindowState.Minimized)
      WindowState = FormWindowState.Normal;
    SetForegroundWindow(Handle.ToInt32());
  }

  private void updateView_ColumnClick(object sender, ColumnClickEventArgs e) {
    if (updateView.ListViewItemSorter == null)
      updateView.ListViewItemSorter = new ListViewItemComparer();
    ((ListViewItemComparer)updateView.ListViewItemSorter).Update(e.Column);
    updateView.Sort();
  }


  private void Localize() {
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

    updateView.Columns[0].Text = "Title";
    updateView.Columns[1].Text = "Category";
    updateView.Columns[2].Text = "KB Article";
    updateView.Columns[3].Text = "Date";
    updateView.Columns[4].Text = "Size";
    updateView.Columns[5].Text = "State";

    chkGrupe.Text = "Group Updates";
    chkAll.Text = "Select All";

    lblSupport.Text = "Support Url";
    lblSearch.Text = "Search filter:";
    tabOptions.Text = "Options";

    chkOffline.Text = "Offline Mode";
    chkDownload.Text = "Download wsusscn2.cab";
    chkManual.Text = "'Manual' Download/Install";
    chkOld.Text = "Include superseded";
    chkMsUpd.Text = "Register Microsoft Update";

    gbStartup.Text = "Startup";
    chkAutoRun.Text = "Run in background";
    dlAutoCheck.Items.Clear();
    dlAutoCheck.Items.Add("No auto search for updates");
    dlAutoCheck.Items.Add("Search for updates every day");
    dlAutoCheck.Items.Add("Search for updates once a week");
    dlAutoCheck.Items.Add("Search for updates every month");
    chkNoUAC.Text = "Always run as Administrator";


    tabAU.Text = "Auto Update";

    chkBlockMS.Text = "Block Access to WU Servers";
    radDisable.Text = "Disable Automatic Update";
    chkDisableAU.Text = "Disable Update Facilitators";
    radNotify.Text = "Notification Only";
    radDownload.Text = "Download Only";
    radSchedule.Text = "Scheduled & Installation";
    radDefault.Text = "Automatic Update (default)";
    chkHideWU.Text = "Hide WU Settings Page";
    chkStore.Text =  "Disable Store Auto Update";
    chkDrivers.Text = "Include Drivers";
  }

  protected override bool ProcessCmdKey(ref Message msg, Keys keyData) {
    if (keyData == (Keys.Control | Keys.F)) {
      panelList.RowStyles[2].Height = mSearchBoxHeight;
      txtFilter.SelectAll();
      txtFilter.Focus();
      return true;
    }

    if (keyData == (Keys.Control | Keys.C)) {
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

    return base.ProcessCmdKey(ref msg, keyData);
  }

  private void btnSearchOff_Click(object sender, EventArgs e) {
    panelList.RowStyles[2].Height = 0;
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
    InstaledUpdates,
    HiddenUpdates,
    UpdateHistory
  }

  // Implements the manual sorting of items by columns.
  private class ListViewItemComparer : IComparer {
    private int col;
    private int inv;

    public ListViewItemComparer() {
      col = 0;
      inv = 1;
    }

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
      if (col == column)
        inv = -inv;
      else
        inv = 1;
      col = column;
    }
  }
}
