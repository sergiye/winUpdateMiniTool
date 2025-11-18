using System.Windows.Forms;

using winUpdateMiniTool.Common;

namespace winUpdateMiniTool {
  partial class MainForm {
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing) {
      if (disposing && (components != null)) {
        components.Dispose();
      }
      base.Dispose(disposing);
    }

    #region Vom Windows Form-Designer generierter Code

    private void InitializeComponent() {
      this.components = new System.ComponentModel.Container();
      this.toolTip = new System.Windows.Forms.ToolTip(this.components);
      this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
      this.panelList = new System.Windows.Forms.Panel();
      this.tableLayoutPanel7 = new System.Windows.Forms.TableLayoutPanel();
      this.lblSupport = new System.Windows.Forms.LinkLabel();
      this.chkGrupe = new System.Windows.Forms.CheckBox();
      this.chkAll = new System.Windows.Forms.CheckBox();
      this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
      this.btnSearchOff = new System.Windows.Forms.Button();
      this.txtFilter = new System.Windows.Forms.TextBox();
      this.lblSearch = new System.Windows.Forms.Label();
      this.panOperations = new System.Windows.Forms.Panel();
      this.panControls = new System.Windows.Forms.FlowLayoutPanel();
      this.btnSearch = new System.Windows.Forms.Button();
      this.btnDownload = new System.Windows.Forms.Button();
      this.btnInstall = new System.Windows.Forms.Button();
      this.btnUnInstall = new System.Windows.Forms.Button();
      this.btnHide = new System.Windows.Forms.Button();
      this.btnGetLink = new System.Windows.Forms.Button();
      this.btnHidden = new System.Windows.Forms.CheckBox();
      this.btnInstalled = new System.Windows.Forms.CheckBox();
      this.btnWinUpd = new System.Windows.Forms.CheckBox();
      this.btnHistory = new System.Windows.Forms.CheckBox();
      this.logSplitter = new System.Windows.Forms.Splitter();
      this.logBox = new System.Windows.Forms.RichTextBox();
      this.panStatus = new System.Windows.Forms.Panel();
      this.lblStatus = new System.Windows.Forms.Label();
      this.progTotal = new System.Windows.Forms.ProgressBar();
      this.btnCancel = new System.Windows.Forms.Button();
      this.panelLeft = new System.Windows.Forms.Panel();
      this.gbStartup = new System.Windows.Forms.GroupBox();
      this.dlAutoCheck = new System.Windows.Forms.ComboBox();
      this.gbxAutoUpdate = new System.Windows.Forms.GroupBox();
      this.label1 = new System.Windows.Forms.Label();
      this.chkBlockMS = new System.Windows.Forms.CheckBox();
      this.chkDrivers = new System.Windows.Forms.CheckBox();
      this.dlShTime = new System.Windows.Forms.ComboBox();
      this.chkStore = new System.Windows.Forms.CheckBox();
      this.dlShDay = new System.Windows.Forms.ComboBox();
      this.chkHideWU = new System.Windows.Forms.CheckBox();
      this.radDisable = new System.Windows.Forms.RadioButton();
      this.chkDisableAU = new System.Windows.Forms.CheckBox();
      this.radNotify = new System.Windows.Forms.RadioButton();
      this.radDefault = new System.Windows.Forms.RadioButton();
      this.radDownload = new System.Windows.Forms.RadioButton();
      this.radSchedule = new System.Windows.Forms.RadioButton();
      this.gbxOptions = new System.Windows.Forms.GroupBox();
      this.dlSource = new System.Windows.Forms.ComboBox();
      this.chkDownload = new System.Windows.Forms.CheckBox();
      this.chkOffline = new System.Windows.Forms.CheckBox();
      this.chkManual = new System.Windows.Forms.CheckBox();
      this.chkMsUpd = new System.Windows.Forms.CheckBox();
      this.chkOld = new System.Windows.Forms.CheckBox();
      this.mainMenu = new System.Windows.Forms.MenuStrip();
      this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
      this.cleanToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
      this.optimizeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
      this.restoreDefaultsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
      this.toolStripMenuItem4 = new System.Windows.Forms.ToolStripSeparator();
      this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripSeparator();
      this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
      this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
      this.toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
      this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
      this.chkAutoRun = new System.Windows.Forms.ToolStripMenuItem();
      this.chkAutoUpdateApp = new System.Windows.Forms.ToolStripMenuItem();
      this.chkNoUAC = new System.Windows.Forms.ToolStripMenuItem();
      this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
      this.themeMenuItem = new System.Windows.Forms.ToolStripMenuItem();
      this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
      this.siteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
      this.checkForNewVersionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
      this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
      this.updateView = new sergiye.Common.ListViewExtended();
      this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
      this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
      this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
      this.columnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
      this.columnHeader5 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
      this.columnHeader6 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
      this.panUpdates = new System.Windows.Forms.Panel();
      this.selectUIFontToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
      this.panelList.SuspendLayout();
      this.tableLayoutPanel7.SuspendLayout();
      this.tableLayoutPanel3.SuspendLayout();
      this.panOperations.SuspendLayout();
      this.panControls.SuspendLayout();
      this.panStatus.SuspendLayout();
      this.panelLeft.SuspendLayout();
      this.gbStartup.SuspendLayout();
      this.gbxAutoUpdate.SuspendLayout();
      this.gbxOptions.SuspendLayout();
      this.mainMenu.SuspendLayout();
      this.panUpdates.SuspendLayout();
      this.SuspendLayout();
      // 
      // notifyIcon
      // 
      this.notifyIcon.Text = "notifyIcon1";
      this.notifyIcon.BalloonTipClicked += new System.EventHandler(this.notifyIcon_BalloonTipClicked);
      this.notifyIcon.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.notifyIcon1_MouseDoubleClick);
      // 
      // panelList
      // 
      this.panelList.Controls.Add(this.panUpdates);
      this.panelList.Controls.Add(this.logSplitter);
      this.panelList.Controls.Add(this.logBox);
      this.panelList.Controls.Add(this.panStatus);
      this.panelList.Dock = System.Windows.Forms.DockStyle.Fill;
      this.panelList.Location = new System.Drawing.Point(183, 24);
      this.panelList.Margin = new System.Windows.Forms.Padding(0);
      this.panelList.Name = "panelList";
      this.panelList.Size = new System.Drawing.Size(717, 419);
      this.panelList.TabIndex = 1;
      // 
      // tableLayoutPanel7
      // 
      this.tableLayoutPanel7.ColumnCount = 4;
      this.tableLayoutPanel7.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
      this.tableLayoutPanel7.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
      this.tableLayoutPanel7.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
      this.tableLayoutPanel7.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
      this.tableLayoutPanel7.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
      this.tableLayoutPanel7.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
      this.tableLayoutPanel7.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
      this.tableLayoutPanel7.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
      this.tableLayoutPanel7.Controls.Add(this.lblSupport, 3, 0);
      this.tableLayoutPanel7.Controls.Add(this.chkGrupe, 1, 0);
      this.tableLayoutPanel7.Controls.Add(this.chkAll, 0, 0);
      this.tableLayoutPanel7.Dock = System.Windows.Forms.DockStyle.Top;
      this.tableLayoutPanel7.Location = new System.Drawing.Point(0, 30);
      this.tableLayoutPanel7.Margin = new System.Windows.Forms.Padding(0);
      this.tableLayoutPanel7.Name = "tableLayoutPanel7";
      this.tableLayoutPanel7.RowCount = 2;
      this.tableLayoutPanel7.RowStyles.Add(new System.Windows.Forms.RowStyle());
      this.tableLayoutPanel7.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 13F));
      this.tableLayoutPanel7.Size = new System.Drawing.Size(717, 21);
      this.tableLayoutPanel7.TabIndex = 5;
      // 
      // lblSupport
      // 
      this.lblSupport.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
      this.lblSupport.AutoSize = true;
      this.lblSupport.Location = new System.Drawing.Point(645, 5);
      this.lblSupport.Name = "lblSupport";
      this.lblSupport.Size = new System.Drawing.Size(69, 13);
      this.lblSupport.TabIndex = 0;
      this.lblSupport.TabStop = true;
      this.lblSupport.Text = "Support URL";
      this.lblSupport.Visible = false;
      this.lblSupport.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lblSupport_LinkClicked);
      // 
      // chkGrupe
      // 
      this.chkGrupe.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.chkGrupe.AutoSize = true;
      this.chkGrupe.Location = new System.Drawing.Point(79, 3);
      this.chkGrupe.Name = "chkGrupe";
      this.chkGrupe.Size = new System.Drawing.Size(98, 17);
      this.chkGrupe.TabIndex = 1;
      this.chkGrupe.Text = "Group Updates";
      this.chkGrupe.UseVisualStyleBackColor = true;
      this.chkGrupe.CheckedChanged += new System.EventHandler(this.chkGrupe_CheckedChanged);
      // 
      // chkAll
      // 
      this.chkAll.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.chkAll.AutoSize = true;
      this.chkAll.Location = new System.Drawing.Point(3, 3);
      this.chkAll.Name = "chkAll";
      this.chkAll.Size = new System.Drawing.Size(70, 17);
      this.chkAll.TabIndex = 2;
      this.chkAll.Text = "Select All";
      this.chkAll.UseVisualStyleBackColor = true;
      this.chkAll.CheckedChanged += new System.EventHandler(this.chkAll_CheckedChanged);
      // 
      // tableLayoutPanel3
      // 
      this.tableLayoutPanel3.ColumnCount = 3;
      this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 100F));
      this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
      this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 25F));
      this.tableLayoutPanel3.Controls.Add(this.btnSearchOff, 2, 0);
      this.tableLayoutPanel3.Controls.Add(this.txtFilter, 1, 0);
      this.tableLayoutPanel3.Controls.Add(this.lblSearch, 0, 0);
      this.tableLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Bottom;
      this.tableLayoutPanel3.Location = new System.Drawing.Point(0, 276);
      this.tableLayoutPanel3.Margin = new System.Windows.Forms.Padding(0);
      this.tableLayoutPanel3.Name = "tableLayoutPanel3";
      this.tableLayoutPanel3.RowCount = 1;
      this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
      this.tableLayoutPanel3.Size = new System.Drawing.Size(717, 25);
      this.tableLayoutPanel3.TabIndex = 6;
      // 
      // btnSearchOff
      // 
      this.btnSearchOff.Location = new System.Drawing.Point(695, 3);
      this.btnSearchOff.Name = "btnSearchOff";
      this.btnSearchOff.Size = new System.Drawing.Size(19, 18);
      this.btnSearchOff.TabIndex = 0;
      this.btnSearchOff.Text = "X";
      this.btnSearchOff.UseVisualStyleBackColor = true;
      this.btnSearchOff.Click += new System.EventHandler(this.btnSearchOff_Click);
      // 
      // txtFilter
      // 
      this.txtFilter.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.txtFilter.Location = new System.Drawing.Point(103, 3);
      this.txtFilter.Name = "txtFilter";
      this.txtFilter.Size = new System.Drawing.Size(586, 20);
      this.txtFilter.TabIndex = 1;
      this.txtFilter.TextChanged += new System.EventHandler(this.txtFilter_TextChanged);
      // 
      // lblSearch
      // 
      this.lblSearch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
      this.lblSearch.AutoSize = true;
      this.lblSearch.Location = new System.Drawing.Point(3, 6);
      this.lblSearch.Name = "lblSearch";
      this.lblSearch.Size = new System.Drawing.Size(94, 13);
      this.lblSearch.TabIndex = 2;
      this.lblSearch.Text = "Search Filter:";
      // 
      // panOperations
      // 
      this.panOperations.Controls.Add(this.panControls);
      this.panOperations.Controls.Add(this.btnHidden);
      this.panOperations.Controls.Add(this.btnInstalled);
      this.panOperations.Controls.Add(this.btnWinUpd);
      this.panOperations.Controls.Add(this.btnHistory);
      this.panOperations.Dock = System.Windows.Forms.DockStyle.Top;
      this.panOperations.Location = new System.Drawing.Point(0, 0);
      this.panOperations.Margin = new System.Windows.Forms.Padding(2);
      this.panOperations.Name = "panOperations";
      this.panOperations.Size = new System.Drawing.Size(717, 30);
      this.panOperations.TabIndex = 7;
      // 
      // panControls
      // 
      this.panControls.Controls.Add(this.btnSearch);
      this.panControls.Controls.Add(this.btnDownload);
      this.panControls.Controls.Add(this.btnInstall);
      this.panControls.Controls.Add(this.btnUnInstall);
      this.panControls.Controls.Add(this.btnHide);
      this.panControls.Controls.Add(this.btnGetLink);
      this.panControls.Dock = System.Windows.Forms.DockStyle.Right;
      this.panControls.Location = new System.Drawing.Point(537, 0);
      this.panControls.Margin = new System.Windows.Forms.Padding(2);
      this.panControls.Name = "panControls";
      this.panControls.Size = new System.Drawing.Size(180, 30);
      this.panControls.TabIndex = 4;
      // 
      // btnSearch
      // 
      this.btnSearch.Location = new System.Drawing.Point(0, 0);
      this.btnSearch.Margin = new System.Windows.Forms.Padding(0);
      this.btnSearch.Name = "btnSearch";
      this.btnSearch.Size = new System.Drawing.Size(30, 30);
      this.btnSearch.TabIndex = 0;
      this.btnSearch.UseVisualStyleBackColor = true;
      this.btnSearch.Click += new System.EventHandler(this.btnSearch_Click);
      // 
      // btnDownload
      // 
      this.btnDownload.Location = new System.Drawing.Point(30, 0);
      this.btnDownload.Margin = new System.Windows.Forms.Padding(0);
      this.btnDownload.Name = "btnDownload";
      this.btnDownload.Size = new System.Drawing.Size(30, 30);
      this.btnDownload.TabIndex = 1;
      this.btnDownload.UseVisualStyleBackColor = true;
      this.btnDownload.Click += new System.EventHandler(this.btnDownload_Click);
      // 
      // btnInstall
      // 
      this.btnInstall.Location = new System.Drawing.Point(60, 0);
      this.btnInstall.Margin = new System.Windows.Forms.Padding(0);
      this.btnInstall.Name = "btnInstall";
      this.btnInstall.Size = new System.Drawing.Size(30, 30);
      this.btnInstall.TabIndex = 2;
      this.btnInstall.UseVisualStyleBackColor = true;
      this.btnInstall.Click += new System.EventHandler(this.btnInstall_Click);
      // 
      // btnUnInstall
      // 
      this.btnUnInstall.Location = new System.Drawing.Point(90, 0);
      this.btnUnInstall.Margin = new System.Windows.Forms.Padding(0);
      this.btnUnInstall.Name = "btnUnInstall";
      this.btnUnInstall.Size = new System.Drawing.Size(30, 30);
      this.btnUnInstall.TabIndex = 3;
      this.btnUnInstall.UseVisualStyleBackColor = true;
      this.btnUnInstall.Click += new System.EventHandler(this.btnUnInstall_Click);
      // 
      // btnHide
      // 
      this.btnHide.Location = new System.Drawing.Point(120, 0);
      this.btnHide.Margin = new System.Windows.Forms.Padding(0);
      this.btnHide.Name = "btnHide";
      this.btnHide.Size = new System.Drawing.Size(30, 30);
      this.btnHide.TabIndex = 4;
      this.btnHide.UseVisualStyleBackColor = true;
      this.btnHide.Click += new System.EventHandler(this.btnHide_Click);
      // 
      // btnGetLink
      // 
      this.btnGetLink.Location = new System.Drawing.Point(150, 0);
      this.btnGetLink.Margin = new System.Windows.Forms.Padding(0);
      this.btnGetLink.Name = "btnGetLink";
      this.btnGetLink.Size = new System.Drawing.Size(30, 30);
      this.btnGetLink.TabIndex = 5;
      this.btnGetLink.UseVisualStyleBackColor = true;
      this.btnGetLink.Click += new System.EventHandler(this.btnGetLink_Click);
      // 
      // btnHidden
      // 
      this.btnHidden.Appearance = System.Windows.Forms.Appearance.Button;
      this.btnHidden.Dock = System.Windows.Forms.DockStyle.Left;
      this.btnHidden.Location = new System.Drawing.Point(357, 0);
      this.btnHidden.Name = "btnHidden";
      this.btnHidden.Size = new System.Drawing.Size(119, 30);
      this.btnHidden.TabIndex = 7;
      this.btnHidden.Text = "Hidden Updates";
      this.btnHidden.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
      this.btnHidden.CheckedChanged += new System.EventHandler(this.btnHidden_CheckedChanged);
      // 
      // btnInstalled
      // 
      this.btnInstalled.Appearance = System.Windows.Forms.Appearance.Button;
      this.btnInstalled.Dock = System.Windows.Forms.DockStyle.Left;
      this.btnInstalled.Location = new System.Drawing.Point(238, 0);
      this.btnInstalled.Name = "btnInstalled";
      this.btnInstalled.Size = new System.Drawing.Size(119, 30);
      this.btnInstalled.TabIndex = 8;
      this.btnInstalled.Text = "Installed Updates";
      this.btnInstalled.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
      this.btnInstalled.CheckedChanged += new System.EventHandler(this.btnInstalled_CheckedChanged);
      // 
      // btnWinUpd
      // 
      this.btnWinUpd.Appearance = System.Windows.Forms.Appearance.Button;
      this.btnWinUpd.Dock = System.Windows.Forms.DockStyle.Left;
      this.btnWinUpd.Location = new System.Drawing.Point(119, 0);
      this.btnWinUpd.Name = "btnWinUpd";
      this.btnWinUpd.Size = new System.Drawing.Size(119, 30);
      this.btnWinUpd.TabIndex = 0;
      this.btnWinUpd.Text = "Windows Updates";
      this.btnWinUpd.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
      this.btnWinUpd.CheckedChanged += new System.EventHandler(this.btnWinUpd_CheckedChanged);
      // 
      // btnHistory
      // 
      this.btnHistory.Appearance = System.Windows.Forms.Appearance.Button;
      this.btnHistory.Dock = System.Windows.Forms.DockStyle.Left;
      this.btnHistory.Location = new System.Drawing.Point(0, 0);
      this.btnHistory.Name = "btnHistory";
      this.btnHistory.Size = new System.Drawing.Size(119, 30);
      this.btnHistory.TabIndex = 6;
      this.btnHistory.Text = "Update History";
      this.btnHistory.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
      this.btnHistory.CheckedChanged += new System.EventHandler(this.btnHistory_CheckedChanged);
      // 
      // splitter1
      // 
      this.logSplitter.Dock = System.Windows.Forms.DockStyle.Bottom;
      this.logSplitter.Location = new System.Drawing.Point(0, 301);
      this.logSplitter.Name = "splitter1";
      this.logSplitter.Size = new System.Drawing.Size(717, 3);
      this.logSplitter.TabIndex = 9;
      this.logSplitter.TabStop = false;
      // 
      // logBox
      // 
      this.logBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
      this.logBox.Dock = System.Windows.Forms.DockStyle.Bottom;
      this.logBox.Location = new System.Drawing.Point(0, 304);
      this.logBox.Name = "logBox";
      this.logBox.ReadOnly = true;
      this.logBox.Size = new System.Drawing.Size(717, 92);
      this.logBox.TabIndex = 4;
      this.logBox.Text = "";
      // 
      // panStatus
      // 
      this.panStatus.Controls.Add(this.lblStatus);
      this.panStatus.Controls.Add(this.progTotal);
      this.panStatus.Controls.Add(this.btnCancel);
      this.panStatus.Dock = System.Windows.Forms.DockStyle.Bottom;
      this.panStatus.Location = new System.Drawing.Point(0, 396);
      this.panStatus.Margin = new System.Windows.Forms.Padding(2);
      this.panStatus.Name = "panStatus";
      this.panStatus.Size = new System.Drawing.Size(717, 23);
      this.panStatus.TabIndex = 8;
      // 
      // lblStatus
      // 
      this.lblStatus.Dock = System.Windows.Forms.DockStyle.Fill;
      this.lblStatus.Location = new System.Drawing.Point(0, 0);
      this.lblStatus.Name = "lblStatus";
      this.lblStatus.Size = new System.Drawing.Size(537, 23);
      this.lblStatus.TabIndex = 9;
      this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
      // 
      // progTotal
      // 
      this.progTotal.Dock = System.Windows.Forms.DockStyle.Right;
      this.progTotal.Location = new System.Drawing.Point(537, 0);
      this.progTotal.Name = "progTotal";
      this.progTotal.Size = new System.Drawing.Size(151, 23);
      this.progTotal.TabIndex = 1;
      // 
      // btnCancel
      // 
      this.btnCancel.Dock = System.Windows.Forms.DockStyle.Right;
      this.btnCancel.Location = new System.Drawing.Point(688, 0);
      this.btnCancel.Margin = new System.Windows.Forms.Padding(0);
      this.btnCancel.Name = "btnCancel";
      this.btnCancel.Size = new System.Drawing.Size(29, 23);
      this.btnCancel.TabIndex = 0;
      this.btnCancel.UseVisualStyleBackColor = true;
      this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
      // 
      // panelLeft
      // 
      this.panelLeft.Controls.Add(this.gbStartup);
      this.panelLeft.Controls.Add(this.gbxAutoUpdate);
      this.panelLeft.Controls.Add(this.gbxOptions);
      this.panelLeft.Dock = System.Windows.Forms.DockStyle.Left;
      this.panelLeft.Location = new System.Drawing.Point(0, 24);
      this.panelLeft.Margin = new System.Windows.Forms.Padding(0);
      this.panelLeft.Name = "panelLeft";
      this.panelLeft.Size = new System.Drawing.Size(183, 419);
      this.panelLeft.TabIndex = 0;
      // 
      // gbStartup
      // 
      this.gbStartup.Controls.Add(this.dlAutoCheck);
      this.gbStartup.Dock = System.Windows.Forms.DockStyle.Top;
      this.gbStartup.Location = new System.Drawing.Point(0, 349);
      this.gbStartup.Name = "gbStartup";
      this.gbStartup.Size = new System.Drawing.Size(183, 48);
      this.gbStartup.TabIndex = 8;
      this.gbStartup.TabStop = false;
      this.gbStartup.Text = "Background tasks";
      // 
      // dlAutoCheck
      // 
      this.dlAutoCheck.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this.dlAutoCheck.Enabled = false;
      this.dlAutoCheck.FormattingEnabled = true;
      this.dlAutoCheck.Items.AddRange(new object[] {
            "No auto search for updates",
            "Search for updates every day",
            "Search for updates once a week",
            "Search for updates every month"});
      this.dlAutoCheck.Location = new System.Drawing.Point(6, 19);
      this.dlAutoCheck.Name = "dlAutoCheck";
      this.dlAutoCheck.Size = new System.Drawing.Size(163, 21);
      this.dlAutoCheck.TabIndex = 2;
      this.dlAutoCheck.SelectedIndexChanged += new System.EventHandler(this.dlAutoCheck_SelectedIndexChanged);
      // 
      // gbxAutoUpdate
      // 
      this.gbxAutoUpdate.Controls.Add(this.label1);
      this.gbxAutoUpdate.Controls.Add(this.chkBlockMS);
      this.gbxAutoUpdate.Controls.Add(this.chkDrivers);
      this.gbxAutoUpdate.Controls.Add(this.dlShTime);
      this.gbxAutoUpdate.Controls.Add(this.chkStore);
      this.gbxAutoUpdate.Controls.Add(this.dlShDay);
      this.gbxAutoUpdate.Controls.Add(this.chkHideWU);
      this.gbxAutoUpdate.Controls.Add(this.radDisable);
      this.gbxAutoUpdate.Controls.Add(this.chkDisableAU);
      this.gbxAutoUpdate.Controls.Add(this.radNotify);
      this.gbxAutoUpdate.Controls.Add(this.radDefault);
      this.gbxAutoUpdate.Controls.Add(this.radDownload);
      this.gbxAutoUpdate.Controls.Add(this.radSchedule);
      this.gbxAutoUpdate.Dock = System.Windows.Forms.DockStyle.Top;
      this.gbxAutoUpdate.Location = new System.Drawing.Point(0, 127);
      this.gbxAutoUpdate.Margin = new System.Windows.Forms.Padding(2);
      this.gbxAutoUpdate.Name = "gbxAutoUpdate";
      this.gbxAutoUpdate.Padding = new System.Windows.Forms.Padding(2);
      this.gbxAutoUpdate.Size = new System.Drawing.Size(183, 222);
      this.gbxAutoUpdate.TabIndex = 11;
      this.gbxAutoUpdate.TabStop = false;
      this.gbxAutoUpdate.Text = "Auto Update";
      // 
      // label1
      // 
      this.label1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
      this.label1.Location = new System.Drawing.Point(3, 162);
      this.label1.Name = "label1";
      this.label1.Size = new System.Drawing.Size(174, 2);
      this.label1.TabIndex = 22;
      // 
      // chkBlockMS
      // 
      this.chkBlockMS.AutoSize = true;
      this.chkBlockMS.Location = new System.Drawing.Point(7, 18);
      this.chkBlockMS.Name = "chkBlockMS";
      this.chkBlockMS.Size = new System.Drawing.Size(164, 17);
      this.chkBlockMS.TabIndex = 4;
      this.chkBlockMS.Text = "Block Access to WU Servers";
      this.chkBlockMS.UseVisualStyleBackColor = true;
      this.chkBlockMS.CheckedChanged += new System.EventHandler(this.chkBlockMS_CheckedChanged);
      // 
      // chkDrivers
      // 
      this.chkDrivers.AutoSize = true;
      this.chkDrivers.Location = new System.Drawing.Point(7, 200);
      this.chkDrivers.Name = "chkDrivers";
      this.chkDrivers.Size = new System.Drawing.Size(97, 17);
      this.chkDrivers.TabIndex = 7;
      this.chkDrivers.Text = "Include Drivers";
      this.chkDrivers.ThreeState = true;
      this.chkDrivers.UseVisualStyleBackColor = true;
      this.chkDrivers.CheckStateChanged += new System.EventHandler(this.chkDrivers_CheckStateChanged);
      // 
      // dlShTime
      // 
      this.dlShTime.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this.dlShTime.Enabled = false;
      this.dlShTime.FormattingEnabled = true;
      this.dlShTime.Items.AddRange(new object[] {
            "00:00",
            "01:00",
            "02:00",
            "03:00",
            "04:00",
            "05:00",
            "06:00",
            "07:00",
            "08:00",
            "09:00",
            "10:00",
            "11:00",
            "12:00",
            "13:00",
            "14:00",
            "15:00",
            "16:00",
            "17:00",
            "18:00",
            "19:00",
            "20:00",
            "21:00",
            "22:00",
            "23:00"});
      this.dlShTime.Location = new System.Drawing.Point(117, 122);
      this.dlShTime.Name = "dlShTime";
      this.dlShTime.Size = new System.Drawing.Size(55, 21);
      this.dlShTime.TabIndex = 6;
      this.dlShTime.SelectedIndexChanged += new System.EventHandler(this.dlShTime_SelectedIndexChanged);
      // 
      // chkStore
      // 
      this.chkStore.AutoSize = true;
      this.chkStore.Location = new System.Drawing.Point(7, 184);
      this.chkStore.Name = "chkStore";
      this.chkStore.Size = new System.Drawing.Size(152, 17);
      this.chkStore.TabIndex = 21;
      this.chkStore.Text = "Disable Store Auto Update";
      this.chkStore.UseVisualStyleBackColor = true;
      this.chkStore.CheckedChanged += new System.EventHandler(this.chkStore_CheckedChanged);
      // 
      // dlShDay
      // 
      this.dlShDay.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this.dlShDay.Enabled = false;
      this.dlShDay.FormattingEnabled = true;
      this.dlShDay.Items.AddRange(new object[] {
            "Daily",
            "Sunday",
            "Monday",
            "Tuesday",
            "Wednesday",
            "Thursday",
            "Friday",
            "Saturday"});
      this.dlShDay.Location = new System.Drawing.Point(21, 122);
      this.dlShDay.Name = "dlShDay";
      this.dlShDay.Size = new System.Drawing.Size(90, 21);
      this.dlShDay.TabIndex = 5;
      this.dlShDay.SelectedIndexChanged += new System.EventHandler(this.dlShDay_SelectedIndexChanged);
      // 
      // chkHideWU
      // 
      this.chkHideWU.AutoSize = true;
      this.chkHideWU.Location = new System.Drawing.Point(7, 168);
      this.chkHideWU.Name = "chkHideWU";
      this.chkHideWU.Size = new System.Drawing.Size(139, 17);
      this.chkHideWU.TabIndex = 1;
      this.chkHideWU.Text = "Hide WU Settings Page";
      this.chkHideWU.UseVisualStyleBackColor = true;
      this.chkHideWU.CheckedChanged += new System.EventHandler(this.chkHideWU_CheckedChanged);
      // 
      // radDisable
      // 
      this.radDisable.AutoSize = true;
      this.radDisable.Location = new System.Drawing.Point(7, 36);
      this.radDisable.Name = "radDisable";
      this.radDisable.Size = new System.Drawing.Size(148, 17);
      this.radDisable.TabIndex = 15;
      this.radDisable.TabStop = true;
      this.radDisable.Text = "Disable Automatic Update";
      this.radDisable.UseVisualStyleBackColor = true;
      this.radDisable.CheckedChanged += new System.EventHandler(this.radGPO_CheckedChanged);
      // 
      // chkDisableAU
      // 
      this.chkDisableAU.Location = new System.Drawing.Point(19, 51);
      this.chkDisableAU.Name = "chkDisableAU";
      this.chkDisableAU.Size = new System.Drawing.Size(155, 17);
      this.chkDisableAU.TabIndex = 20;
      this.chkDisableAU.Text = "Disable Update Facilitators";
      this.chkDisableAU.UseVisualStyleBackColor = true;
      this.chkDisableAU.CheckedChanged += new System.EventHandler(this.chkDisableAU_CheckedChanged);
      // 
      // radNotify
      // 
      this.radNotify.AutoSize = true;
      this.radNotify.Location = new System.Drawing.Point(7, 69);
      this.radNotify.Name = "radNotify";
      this.radNotify.Size = new System.Drawing.Size(102, 17);
      this.radNotify.TabIndex = 16;
      this.radNotify.TabStop = true;
      this.radNotify.Text = "Notification Only";
      this.radNotify.UseVisualStyleBackColor = true;
      this.radNotify.CheckedChanged += new System.EventHandler(this.radGPO_CheckedChanged);
      // 
      // radDefault
      // 
      this.radDefault.AutoSize = true;
      this.radDefault.Location = new System.Drawing.Point(7, 144);
      this.radDefault.Name = "radDefault";
      this.radDefault.Size = new System.Drawing.Size(151, 17);
      this.radDefault.TabIndex = 19;
      this.radDefault.TabStop = true;
      this.radDefault.Text = "Automatic Update (default)";
      this.radDefault.UseVisualStyleBackColor = true;
      this.radDefault.CheckedChanged += new System.EventHandler(this.radGPO_CheckedChanged);
      // 
      // radDownload
      // 
      this.radDownload.AutoSize = true;
      this.radDownload.Location = new System.Drawing.Point(7, 86);
      this.radDownload.Name = "radDownload";
      this.radDownload.Size = new System.Drawing.Size(97, 17);
      this.radDownload.TabIndex = 17;
      this.radDownload.TabStop = true;
      this.radDownload.Text = "Download Only";
      this.radDownload.UseVisualStyleBackColor = true;
      this.radDownload.CheckedChanged += new System.EventHandler(this.radGPO_CheckedChanged);
      // 
      // radSchedule
      // 
      this.radSchedule.AutoSize = true;
      this.radSchedule.Location = new System.Drawing.Point(7, 105);
      this.radSchedule.Name = "radSchedule";
      this.radSchedule.Size = new System.Drawing.Size(132, 17);
      this.radSchedule.TabIndex = 18;
      this.radSchedule.TabStop = true;
      this.radSchedule.Text = "Scheduled & Installation";
      this.radSchedule.UseVisualStyleBackColor = true;
      this.radSchedule.CheckedChanged += new System.EventHandler(this.radGPO_CheckedChanged);
      // 
      // gbxOptions
      // 
      this.gbxOptions.Controls.Add(this.dlSource);
      this.gbxOptions.Controls.Add(this.chkDownload);
      this.gbxOptions.Controls.Add(this.chkOffline);
      this.gbxOptions.Controls.Add(this.chkManual);
      this.gbxOptions.Controls.Add(this.chkMsUpd);
      this.gbxOptions.Controls.Add(this.chkOld);
      this.gbxOptions.Dock = System.Windows.Forms.DockStyle.Top;
      this.gbxOptions.Location = new System.Drawing.Point(0, 0);
      this.gbxOptions.Margin = new System.Windows.Forms.Padding(2);
      this.gbxOptions.Name = "gbxOptions";
      this.gbxOptions.Padding = new System.Windows.Forms.Padding(2);
      this.gbxOptions.Size = new System.Drawing.Size(183, 127);
      this.gbxOptions.TabIndex = 10;
      this.gbxOptions.TabStop = false;
      this.gbxOptions.Text = "Options";
      // 
      // dlSource
      // 
      this.dlSource.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this.dlSource.Enabled = false;
      this.dlSource.FormattingEnabled = true;
      this.dlSource.Location = new System.Drawing.Point(7, 18);
      this.dlSource.Name = "dlSource";
      this.dlSource.Size = new System.Drawing.Size(164, 21);
      this.dlSource.TabIndex = 0;
      this.dlSource.SelectedIndexChanged += new System.EventHandler(this.dlSource_SelectedIndexChanged);
      // 
      // chkDownload
      // 
      this.chkDownload.AutoSize = true;
      this.chkDownload.Checked = true;
      this.chkDownload.CheckState = System.Windows.Forms.CheckState.Checked;
      this.chkDownload.Location = new System.Drawing.Point(7, 57);
      this.chkDownload.Name = "chkDownload";
      this.chkDownload.Size = new System.Drawing.Size(145, 17);
      this.chkDownload.TabIndex = 3;
      this.chkDownload.Text = "Download wsusscn2.cab";
      this.chkDownload.UseVisualStyleBackColor = true;
      this.chkDownload.CheckedChanged += new System.EventHandler(this.chkDownload_CheckedChanged);
      // 
      // chkOffline
      // 
      this.chkOffline.AutoSize = true;
      this.chkOffline.Checked = true;
      this.chkOffline.CheckState = System.Windows.Forms.CheckState.Checked;
      this.chkOffline.Location = new System.Drawing.Point(7, 42);
      this.chkOffline.Name = "chkOffline";
      this.chkOffline.Size = new System.Drawing.Size(86, 17);
      this.chkOffline.TabIndex = 1;
      this.chkOffline.Text = "Offline Mode";
      this.chkOffline.UseVisualStyleBackColor = true;
      this.chkOffline.CheckedChanged += new System.EventHandler(this.chkOffline_CheckedChanged);
      // 
      // chkManual
      // 
      this.chkManual.AutoSize = true;
      this.chkManual.Location = new System.Drawing.Point(7, 73);
      this.chkManual.Name = "chkManual";
      this.chkManual.Size = new System.Drawing.Size(148, 17);
      this.chkManual.TabIndex = 0;
      this.chkManual.Text = "\'Manual\' Download/Install";
      this.chkManual.UseVisualStyleBackColor = true;
      this.chkManual.CheckedChanged += new System.EventHandler(this.chkManual_CheckedChanged);
      // 
      // chkMsUpd
      // 
      this.chkMsUpd.AutoSize = true;
      this.chkMsUpd.Location = new System.Drawing.Point(7, 105);
      this.chkMsUpd.Name = "chkMsUpd";
      this.chkMsUpd.Size = new System.Drawing.Size(149, 17);
      this.chkMsUpd.TabIndex = 0;
      this.chkMsUpd.Text = "Register Microsoft Update";
      this.chkMsUpd.UseVisualStyleBackColor = true;
      this.chkMsUpd.CheckedChanged += new System.EventHandler(this.chkMsUpd_CheckedChanged);
      // 
      // chkOld
      // 
      this.chkOld.AutoSize = true;
      this.chkOld.Location = new System.Drawing.Point(7, 89);
      this.chkOld.Name = "chkOld";
      this.chkOld.Size = new System.Drawing.Size(119, 17);
      this.chkOld.TabIndex = 2;
      this.chkOld.Text = "Include superseded";
      this.chkOld.UseVisualStyleBackColor = true;
      this.chkOld.CheckedChanged += new System.EventHandler(this.chkOld_CheckedChanged);
      // 
      // mainMenu
      // 
      this.mainMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.optionsToolStripMenuItem,
            this.helpToolStripMenuItem});
      this.mainMenu.Location = new System.Drawing.Point(0, 0);
      this.mainMenu.Name = "mainMenu";
      this.mainMenu.Size = new System.Drawing.Size(900, 24);
      this.mainMenu.TabIndex = 9;
      this.mainMenu.Text = "menuStrip1";
      // 
      // fileToolStripMenuItem
      // 
      this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.cleanToolStripMenuItem,
            this.optimizeToolStripMenuItem,
            this.toolStripMenuItem3,
            this.restoreDefaultsToolStripMenuItem,
            this.toolStripMenuItem4,
            this.exitToolStripMenuItem});
      this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
      this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
      this.fileToolStripMenuItem.Text = "File";
      // 
      // cleanToolStripMenuItem
      // 
      this.cleanToolStripMenuItem.Name = "cleanToolStripMenuItem";
      this.cleanToolStripMenuItem.Size = new System.Drawing.Size(179, 22);
      this.cleanToolStripMenuItem.Text = "Clean cache";
      this.cleanToolStripMenuItem.Click += new System.EventHandler(this.menuClean_Click);
      // 
      // optimizeToolStripMenuItem
      // 
      this.optimizeToolStripMenuItem.Name = "optimizeToolStripMenuItem";
      this.optimizeToolStripMenuItem.Size = new System.Drawing.Size(179, 22);
      this.optimizeToolStripMenuItem.Text = "Optimize kernel size";
      this.optimizeToolStripMenuItem.Click += new System.EventHandler(this.menuOptimize_Click);
      // 
      // toolStripMenuItem3
      // 
      this.toolStripMenuItem3.Name = "toolStripMenuItem3";
      this.toolStripMenuItem3.Size = new System.Drawing.Size(176, 6);
      // 
      // restoreDefaultsToolStripMenuItem
      // 
      this.restoreDefaultsToolStripMenuItem.Name = "restoreDefaultsToolStripMenuItem";
      this.restoreDefaultsToolStripMenuItem.Size = new System.Drawing.Size(179, 22);
      this.restoreDefaultsToolStripMenuItem.Text = "Restore default settings";
      this.restoreDefaultsToolStripMenuItem.Click += new System.EventHandler(this.restoreDefaults_Click);
      // 
      // toolStripMenuItem4
      // 
      this.toolStripMenuItem4.Name = "toolStripMenuItem4";
      this.toolStripMenuItem4.Size = new System.Drawing.Size(176, 6);
      // 
      // exitToolStripMenuItem
      // 
      this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
      this.exitToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.W)));
      this.exitToolStripMenuItem.Size = new System.Drawing.Size(179, 22);
      this.exitToolStripMenuItem.Text = "Exit";
      this.exitToolStripMenuItem.Click += new System.EventHandler(this.menuExit_Click);
      // 
      // optionsToolStripMenuItem
      // 
      this.optionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolsToolStripMenuItem,
            this.toolStripMenuItem1,
            this.chkAutoRun,
            this.chkAutoUpdateApp,
            this.chkNoUAC,
            this.toolStripMenuItem2,
            this.themeMenuItem,
            this.selectUIFontToolStripMenuItem});
      this.optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
      this.optionsToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
      this.optionsToolStripMenuItem.Text = "Options";
      // 
      // toolsToolStripMenuItem
      // 
      this.toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
      this.toolsToolStripMenuItem.Size = new System.Drawing.Size(222, 22);
      this.toolsToolStripMenuItem.Text = "Tools";
      // 
      // toolStripMenuItem1
      // 
      this.toolStripMenuItem1.Name = "toolStripMenuItem1";
      this.toolStripMenuItem1.Size = new System.Drawing.Size(219, 6);
      // 
      // chkAutoRun
      // 
      this.chkAutoRun.Name = "chkAutoRun";
      this.chkAutoRun.Size = new System.Drawing.Size(222, 22);
      this.chkAutoRun.Text = "Run in background";
      this.chkAutoRun.Click += new System.EventHandler(this.chkAutoRun_CheckedChanged);
      // 
      // chkAutoUpdateApp
      // 
      this.chkAutoUpdateApp.Name = "chkAutoUpdateApp";
      this.chkAutoUpdateApp.Size = new System.Drawing.Size(222, 22);
      this.chkAutoUpdateApp.Text = "Auto-update application";
      this.chkAutoUpdateApp.Click += new System.EventHandler(this.chkAutoUpdateApp_Click);
      // 
      // chkNoUAC
      // 
      this.chkNoUAC.Name = "chkNoUAC";
      this.chkNoUAC.Size = new System.Drawing.Size(222, 22);
      this.chkNoUAC.Text = "Always run as Administrator";
      this.chkNoUAC.Click += new System.EventHandler(this.chkNoUAC_CheckedChanged);
      // 
      // toolStripMenuItem2
      // 
      this.toolStripMenuItem2.Name = "toolStripMenuItem2";
      this.toolStripMenuItem2.Size = new System.Drawing.Size(219, 6);
      // 
      // themeMenuItem
      // 
      this.themeMenuItem.Name = "themeMenuItem";
      this.themeMenuItem.Size = new System.Drawing.Size(222, 22);
      this.themeMenuItem.Text = "Themes";
      // 
      // helpToolStripMenuItem
      // 
      this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.siteToolStripMenuItem,
            this.checkForNewVersionToolStripMenuItem,
            this.aboutToolStripMenuItem});
      this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
      this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
      this.helpToolStripMenuItem.Text = "Help";
      // 
      // siteToolStripMenuItem
      // 
      this.siteToolStripMenuItem.Name = "siteToolStripMenuItem";
      this.siteToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.F1)));
      this.siteToolStripMenuItem.Size = new System.Drawing.Size(233, 22);
      this.siteToolStripMenuItem.Text = "Site";
      this.siteToolStripMenuItem.Click += new System.EventHandler(this.siteToolStripMenuItem_Click);
      // 
      // checkForNewVersionToolStripMenuItem
      // 
      this.checkForNewVersionToolStripMenuItem.Name = "checkForNewVersionToolStripMenuItem";
      this.checkForNewVersionToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.U)));
      this.checkForNewVersionToolStripMenuItem.Size = new System.Drawing.Size(233, 22);
      this.checkForNewVersionToolStripMenuItem.Text = "Check for new version";
      this.checkForNewVersionToolStripMenuItem.Click += new System.EventHandler(this.checkForNewVersionToolStripMenuItem_Click);
      // 
      // aboutToolStripMenuItem
      // 
      this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
      this.aboutToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F1;
      this.aboutToolStripMenuItem.Size = new System.Drawing.Size(233, 22);
      this.aboutToolStripMenuItem.Text = "About";
      this.aboutToolStripMenuItem.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);
      // 
      // updateView
      // 
      this.updateView.CheckBoxes = true;
      this.updateView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2,
            this.columnHeader3,
            this.columnHeader4,
            this.columnHeader5,
            this.columnHeader6});
      this.updateView.Dock = System.Windows.Forms.DockStyle.Fill;
      this.updateView.FullRowSelect = true;
      this.updateView.GroupHeadingBackColor = System.Drawing.Color.Gray;
      this.updateView.GroupHeadingForeColor = System.Drawing.Color.Black;
      this.updateView.HideSelection = false;
      this.updateView.Location = new System.Drawing.Point(0, 51);
      this.updateView.Name = "updateView";
      this.updateView.SeparatorColor = System.Drawing.Color.Black;
      this.updateView.ShowItemToolTips = true;
      this.updateView.Size = new System.Drawing.Size(717, 250);
      this.updateView.TabIndex = 2;
      this.updateView.UseCompatibleStateImageBehavior = false;
      this.updateView.View = System.Windows.Forms.View.Details;
      this.updateView.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.updateView_ColumnClick);
      this.updateView.ColumnWidthChanged += new System.Windows.Forms.ColumnWidthChangedEventHandler(this.UpdateView_ColumnWidthChanged);
      this.updateView.ItemChecked += new System.Windows.Forms.ItemCheckedEventHandler(this.updateView_ItemChecked);
      this.updateView.SelectedIndexChanged += new System.EventHandler(this.updateView_SelectedIndexChanged);
      this.updateView.SizeChanged += new System.EventHandler(this.updateView_SizeChanged);
      // 
      // columnHeader1
      // 
      this.columnHeader1.Text = "Title";
      this.columnHeader1.Width = 250;
      // 
      // columnHeader2
      // 
      this.columnHeader2.Text = "Category";
      this.columnHeader2.Width = 100;
      // 
      // columnHeader3
      // 
      this.columnHeader3.Text = "KB Article";
      this.columnHeader3.Width = 120;
      // 
      // columnHeader4
      // 
      this.columnHeader4.Text = "Date";
      // 
      // columnHeader5
      // 
      this.columnHeader5.Text = "Size";
      this.columnHeader5.Width = 70;
      // 
      // columnHeader6
      // 
      this.columnHeader6.Text = "State";
      this.columnHeader6.Width = 110;
      // 
      // panUpdates
      // 
      this.panUpdates.Controls.Add(this.tableLayoutPanel3);
      this.panUpdates.Controls.Add(this.updateView);
      this.panUpdates.Controls.Add(this.tableLayoutPanel7);
      this.panUpdates.Controls.Add(this.panOperations);
      this.panUpdates.Dock = System.Windows.Forms.DockStyle.Fill;
      this.panUpdates.Location = new System.Drawing.Point(0, 0);
      this.panUpdates.Name = "panUpdates";
      this.panUpdates.Size = new System.Drawing.Size(717, 301);
      this.panUpdates.TabIndex = 10;
      // 
      // selectUIFontToolStripMenuItem
      // 
      this.selectUIFontToolStripMenuItem.Name = "selectUIFontToolStripMenuItem";
      this.selectUIFontToolStripMenuItem.Size = new System.Drawing.Size(222, 22);
      this.selectUIFontToolStripMenuItem.Text = "Select UI font";
      this.selectUIFontToolStripMenuItem.Click += new System.EventHandler(this.selectUIFontToolStripMenuItem_Click);
      // 
      // MainForm
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(900, 443);
      this.Controls.Add(this.panelList);
      this.Controls.Add(this.panelLeft);
      this.Controls.Add(this.mainMenu);
      this.MainMenuStrip = this.mainMenu;
      this.MinimumSize = new System.Drawing.Size(916, 482);
      this.Name = "MainForm";
      this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
      this.Text = "Windows Update Mini Tool";
      this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainFormClosing);
      this.panelList.ResumeLayout(false);
      this.tableLayoutPanel7.ResumeLayout(false);
      this.tableLayoutPanel7.PerformLayout();
      this.tableLayoutPanel3.ResumeLayout(false);
      this.tableLayoutPanel3.PerformLayout();
      this.panOperations.ResumeLayout(false);
      this.panControls.ResumeLayout(false);
      this.panStatus.ResumeLayout(false);
      this.panelLeft.ResumeLayout(false);
      this.gbStartup.ResumeLayout(false);
      this.gbxAutoUpdate.ResumeLayout(false);
      this.gbxAutoUpdate.PerformLayout();
      this.gbxOptions.ResumeLayout(false);
      this.gbxOptions.PerformLayout();
      this.mainMenu.ResumeLayout(false);
      this.mainMenu.PerformLayout();
      this.panUpdates.ResumeLayout(false);
      this.ResumeLayout(false);
      this.PerformLayout();

    }

    #endregion
    private System.Windows.Forms.ToolTip toolTip;
    private System.Windows.Forms.NotifyIcon notifyIcon;
    private System.Windows.Forms.Panel panelList;
    private System.Windows.Forms.Panel panelLeft;
    private System.Windows.Forms.FlowLayoutPanel panControls;
    private System.Windows.Forms.Button btnSearch;
    private System.Windows.Forms.Button btnDownload;
    private System.Windows.Forms.Button btnInstall;
    private System.Windows.Forms.Button btnUnInstall;
    private System.Windows.Forms.Button btnHide;
    private System.Windows.Forms.Button btnGetLink;
    private System.Windows.Forms.Button btnCancel;
    private System.Windows.Forms.ProgressBar progTotal;
    private System.Windows.Forms.CheckBox btnHistory;
    private System.Windows.Forms.CheckBox btnHidden;
    private System.Windows.Forms.CheckBox btnInstalled;
    private System.Windows.Forms.CheckBox btnWinUpd;
    private System.Windows.Forms.Label lblStatus;
    private System.Windows.Forms.CheckBox chkBlockMS;
    private System.Windows.Forms.CheckBox chkDrivers;
    private System.Windows.Forms.ComboBox dlShTime;
    private System.Windows.Forms.ComboBox dlShDay;
    private System.Windows.Forms.CheckBox chkMsUpd;
    private System.Windows.Forms.CheckBox chkOld;
    private System.Windows.Forms.ComboBox dlSource;
    private System.Windows.Forms.CheckBox chkOffline;
    private System.Windows.Forms.CheckBox chkDownload;
    private System.Windows.Forms.CheckBox chkManual;
    private System.Windows.Forms.RichTextBox logBox;
    private System.Windows.Forms.TableLayoutPanel tableLayoutPanel7;
    private System.Windows.Forms.LinkLabel lblSupport;
    private System.Windows.Forms.CheckBox chkHideWU;
    private sergiye.Common.ListViewExtended updateView;
    private System.Windows.Forms.ColumnHeader columnHeader1;
    private System.Windows.Forms.ColumnHeader columnHeader2;
    private System.Windows.Forms.ColumnHeader columnHeader3;
    private System.Windows.Forms.ColumnHeader columnHeader4;
    private System.Windows.Forms.ColumnHeader columnHeader5;
    private System.Windows.Forms.ColumnHeader columnHeader6;
    private System.Windows.Forms.ComboBox dlAutoCheck;
    private System.Windows.Forms.CheckBox chkStore;
    private System.Windows.Forms.CheckBox chkDisableAU;
    private System.Windows.Forms.RadioButton radDefault;
    private System.Windows.Forms.RadioButton radSchedule;
    private System.Windows.Forms.RadioButton radDownload;
    private System.Windows.Forms.RadioButton radNotify;
    private System.Windows.Forms.RadioButton radDisable;
    private System.Windows.Forms.GroupBox gbStartup;
    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
    private System.Windows.Forms.Button btnSearchOff;
    private System.Windows.Forms.TextBox txtFilter;
    private System.Windows.Forms.Label lblSearch;
    private System.Windows.Forms.CheckBox chkGrupe;
    private System.Windows.Forms.CheckBox chkAll;
    private Panel panOperations;
    private GroupBox gbxOptions;
    private GroupBox gbxAutoUpdate;
    private Panel panStatus;
    private MenuStrip mainMenu;
    private ToolStripMenuItem fileToolStripMenuItem;
    private ToolStripMenuItem toolsToolStripMenuItem;
    private ToolStripSeparator toolStripMenuItem1;
    private ToolStripMenuItem exitToolStripMenuItem;
    private ToolStripMenuItem optionsToolStripMenuItem;
    private ToolStripMenuItem helpToolStripMenuItem;
    private ToolStripMenuItem siteToolStripMenuItem;
    private ToolStripMenuItem checkForNewVersionToolStripMenuItem;
    private ToolStripMenuItem aboutToolStripMenuItem;
    private ToolStripMenuItem themeMenuItem;
    private ToolStripMenuItem chkAutoRun;
    private ToolStripMenuItem chkAutoUpdateApp;
    private ToolStripMenuItem chkNoUAC;
    private ToolStripSeparator toolStripMenuItem2;
    private ToolStripMenuItem cleanToolStripMenuItem;
    private ToolStripMenuItem optimizeToolStripMenuItem;
    private ToolStripSeparator toolStripMenuItem3;
    private ToolStripMenuItem restoreDefaultsToolStripMenuItem;
    private ToolStripSeparator toolStripMenuItem4;
    private Splitter logSplitter;
    private Panel panUpdates;
    private ToolStripMenuItem selectUIFontToolStripMenuItem;
  }
}

