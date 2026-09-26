using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;
using sergiye.Common;
using TaskScheduler;
using winUpdateMiniTool.Common;
using IExecAction = TaskScheduler.IExecAction;

namespace winUpdateMiniTool;

internal static class Program {
  private const string MF_APP_TASK_NAME = "wumtNoUAC";
  private static string[] args;
  private static bool mConsole;
  private static string appPath;
  public static string WrkPath;
  private static WuAgent agent;

  private static string GetIniPath() {
    return Path.Combine(WrkPath, Path.ChangeExtension(Path.GetFileName(Updater.CurrentFileLocation), ".ini"));
  }

  public static string GetToolsPath() {
    return Path.Combine(appPath, "Tools");
  }

  [STAThread]
  private static void Main(string[] mainArgs) {
    args = mainArgs;

    mConsole = WinConsole.Initialize(TestArg("-console"));

    if (TestArg("-help") || TestArg("/?")) {
      ShowHelp();
      return;
    }

    if (TestArg("-dbg_wait"))
      MessageBox.Show("Waiting for debugger. (Press OK once attached.)", Updater.ApplicationTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);

    Console.WriteLine(@"Starting...");

    WrkPath = appPath = Path.GetDirectoryName(Updater.CurrentFileLocation);

    AppLog.Line("{0}, Version v{1}", Updater.ApplicationTitle, Updater.CurrentVersion);
    AppLog.Line("This tool is open source under the GNU General Public License, Version 3.\r\n");

    if (!OSHelper.IsCompatible(false, out var errorMessage, out var fixAction)) {
      if (fixAction != null) {
        if (MessageBox.Show(errorMessage, Updater.ApplicationName, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes) {
          fixAction?.Invoke();
        }
      }
      else {
        MessageBox.Show(errorMessage, Updater.ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
      }
      Environment.Exit(0);
    }

    if (WinApiHelper.CheckRunningInstances(true, true)) {
      // fallback
      MessageBox.Show($"{Updater.ApplicationName} is already running.", Updater.ApplicationName,
        MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
      return;
    }

    if (!OSHelper.IsAdministrator() && !OSHelper.IsDebugging()) {
      Console.WriteLine(@"Trying to get admin privileges...");

      if (SkipUacRun()) {
        Application.Exit();
        return;
      }

      if (!OSHelper.IsRunningAsUwp()) {
        Console.WriteLine(@"Trying to start with 'runas'...");
        // Restart program and run as admin
        var exeName = Process.GetCurrentProcess().MainModule?.FileName;
        var arguments = string.Join(" ", mainArgs.Select(EscapeArg));
        ProcessStartInfo startInfo = new(exeName, arguments) {
          UseShellExecute = true,
          Verb = "runas"
        };
        try {
          Process.Start(startInfo);
          Application.Exit();
          return;
        }
        catch {
          AppLog.Line("Administrator privileges are required to install updates.");
        }
      }
    }

    if (!FileOps.TestWrite(GetIniPath())) {
      Console.WriteLine(@"Cannot write to the default working directory.");

      var downloadFolder = KnownFolders.GetPath(KnownFolder.Downloads) ??
                           Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads";

      WrkPath = downloadFolder + @"\winUpdateMiniTool";
      try {
        if (!Directory.Exists(WrkPath))
          Directory.CreateDirectory(WrkPath);
      }
      catch {
        MessageBox.Show($"Cannot write to working directory: {WrkPath}", Updater.ApplicationTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
      }
    }

    AppLog.Line("Working Directory: {0}", WrkPath);
    agent = new WuAgent();
    ExecOnStart();
    agent.Init();
    Application.EnableVisualStyles();
    Application.SetCompatibleTextRenderingDefault(false);
    Application.Run(new MainForm());
    agent.UnInit();
    ExecOnClose();
  }

  /// <summary>
  ///     Executes commands on application start.
  /// </summary>
  private static void ExecOnStart() {
    var toolsIni = GetToolsPath() + @"\Tools.ini";

    if (MiscFunc.ParseInt(IniReadValue("OnStart", "EnableWuAuServ", "0", toolsIni)) != 0)
      agent.EnableWuAuServ();

    var onStart = IniReadValue("OnStart", "Exec", "", toolsIni);
    if (onStart.Length > 0)
      DoExec(PrepExec(onStart, MiscFunc.ParseInt(IniReadValue("OnStart", "Silent", "1", toolsIni)) != 0), true);
  }

  /// <summary>
  ///     Executes commands on application close.
  /// </summary>
  private static void ExecOnClose() {
    var toolsIni = GetToolsPath() + @"\Tools.ini";

    var onClose = IniReadValue("OnClose", "Exec", "", toolsIni);
    if (onClose.Length > 0)
      DoExec(PrepExec(onClose, MiscFunc.ParseInt(IniReadValue("OnClose", "Silent", "1", toolsIni)) != 0), true);

    if (MiscFunc.ParseInt(IniReadValue("OnClose", "DisableWuAuServ", "0", toolsIni)) != 0)
      agent.EnableWuAuServ(false);

    // Note: With the UAC bypass the onclose parameter can be used for a local privilege escalation exploit
    if (TestArg("-NoUAC")) return;
    for (var i = 0; i < args.Length; i++)
      if (args[i].Equals("-onclose", StringComparison.CurrentCultureIgnoreCase) && i + 1 < args.Length)
        DoExec(PrepExec(args[++i]));
  }

  /// <summary>
  ///     Quotes and escapes a single argument for a Win32 command line, so an argument containing
  ///     embedded quotes or backslashes round-trips correctly through re-parsing by the child process.
  /// </summary>
  /// <param name="arg">The raw argument value.</param>
  /// <returns>The escaped, quoted-if-needed argument.</returns>
  private static string EscapeArg(string arg) {
    if (arg.Length > 0 && arg.IndexOfAny([' ', '\t', '\n', '\v', '"']) < 0)
      return arg;

    var sb = new StringBuilder();
    sb.Append('"');
    for (var i = 0; i < arg.Length;) {
      var backslashCount = 0;
      while (i < arg.Length && arg[i] == '\\') {
        backslashCount++;
        i++;
      }

      if (i == arg.Length) {
        sb.Append('\\', backslashCount * 2);
        break;
      }

      if (arg[i] == '"') {
        sb.Append('\\', backslashCount * 2 + 1);
        sb.Append('"');
      }
      else {
        sb.Append('\\', backslashCount);
        sb.Append(arg[i]);
      }

      i++;
    }

    sb.Append('"');
    return sb.ToString();
  }

  /// <summary>
  ///     Prepares a ProcessStartInfo object for executing a command.
  /// </summary>
  /// <param name="command">The command to execute.</param>
  /// <param name="silent">Whether to execute the command silently.</param>
  /// <returns>A ProcessStartInfo object.</returns>
  public static ProcessStartInfo PrepExec(string command, bool silent = true) {
    // -onclose """cm d.exe"" /c ping 10.70.0.1" -test
    int pos;
    if (command.Length > 0 && command.Substring(0, 1) == "\"") {
      command = command.Remove(0, 1).Trim();
      pos = command.IndexOf("\"", StringComparison.Ordinal);
    }
    else {
      pos = command.IndexOf(" ", StringComparison.Ordinal);
    }

    string exec;
    var arguments = "";
    if (pos != -1) {
      exec = command.Substring(0, pos);
      arguments = command.Substring(pos + 1).Trim();
    }
    else {
      exec = command;
    }

    ProcessStartInfo startInfo = new() {
      FileName = exec,
      Arguments = arguments
    };
    if (!silent) return startInfo;
    startInfo.RedirectStandardOutput = true;
    startInfo.RedirectStandardError = true;
    startInfo.UseShellExecute = false;
    startInfo.CreateNoWindow = true;

    return startInfo;
  }

  /// <summary>
  ///     Executes a command using the specified ProcessStartInfo.
  /// </summary>
  /// <param name="startInfo">The ProcessStartInfo object.</param>
  /// <param name="wait">Whether to wait for the process to exit.</param>
  /// <returns>True if the command executed successfully, false otherwise.</returns>
  public static bool DoExec(ProcessStartInfo startInfo, bool wait = false) {
    try {
      Process proc = new();
      proc.StartInfo = startInfo;
      proc.EnableRaisingEvents = true;
      proc.Start();
      // Redirected pipes must be drained, otherwise a chatty child blocks once the pipe buffer fills up.
      if (startInfo.RedirectStandardOutput)
        proc.BeginOutputReadLine();
      if (startInfo.RedirectStandardError)
        proc.BeginErrorReadLine();
      if (wait)
        proc.WaitForExit();
    }
    catch {
      return false;
    }

    return true;
  }

  [DllImport("kernel32", CharSet = CharSet.Unicode)]
  [return: MarshalAs(UnmanagedType.Bool)]
  private static extern bool WritePrivateProfileString(string section, string key, string val, string filePath);

  /// <summary>
  ///     Writes a value to the INI file.
  /// </summary>
  /// <param name="section">The section in the INI file.</param>
  /// <param name="key">The key in the INI file.</param>
  /// <param name="value">The value to write.</param>
  /// <param name="iniPath">The path to the INI file.</param>
  public static void IniWriteValue(string section, string key, string value, string iniPath = null) {
    WritePrivateProfileString(section, key, value, iniPath ?? GetIniPath());
  }

  [DllImport("kernel32", CharSet = CharSet.Unicode)]
  private static extern int GetPrivateProfileString(string section, string key, string def, [In][Out] char[] retVal,
      int size, string filePath);

  /// <summary>
  ///     Reads a value from the INI file.
  /// </summary>
  /// <param name="section">The section in the INI file.</param>
  /// <param name="key">The key in the INI file.</param>
  /// <param name="default">The default value if the key is not found.</param>
  /// <param name="iniPath">The path to the INI file.</param>
  /// <returns>The value read from the INI file.</returns>
  public static string IniReadValue(string section, string key, string @default = "", string iniPath = null) {
    var path = iniPath ?? GetIniPath();
    var chars = new char[8192];
    int size;
    // A return value of buffer size - 1 means the value was truncated.
    while ((size = GetPrivateProfileString(section, key, @default, chars, chars.Length, path)) == chars.Length - 1)
      chars = new char[chars.Length * 2];
    return new string(chars, 0, size);
  }

  /// <summary>
  ///     Enumerates the sections in the INI file.
  /// </summary>
  /// <param name="iniPath">The path to the INI file.</param>
  /// <returns>An array of section names.</returns>
  public static string[] IniEnumSections(string iniPath = null) {
    var path = iniPath ?? GetIniPath();
    var chars = new char[8192];
    int size;
    // A return value of buffer size - 2 means the section list was truncated.
    while ((size = GetPrivateProfileString(null, null, null, chars, chars.Length, path)) == chars.Length - 2)
      chars = new char[chars.Length * 2];
    return new string(chars, 0, size).Split(['\0'], StringSplitOptions.RemoveEmptyEntries);
  }

  /// <summary>
  ///     Tests if a command line argument is present.
  /// </summary>
  /// <param name="name">The name of the argument.</param>
  /// <returns>True if the argument is present, false otherwise.</returns>
  public static bool TestArg(string name) {
    foreach (var t in args)
      if (t.Equals(name, StringComparison.CurrentCultureIgnoreCase))
        return true;

    return false;
  }

  /// <summary>
  ///     Gets the value of a command line argument.
  /// </summary>
  /// <param name="name">The name of the argument.</param>
  /// <returns>The value of the argument.</returns>
  public static string GetArg(string name) {
    for (var i = 0; i < args.Length; i++)
      if (args[i].Equals(name, StringComparison.CurrentCultureIgnoreCase)) {
        if (i + 1 >= args.Length)
          return "";
        var temp = args[i + 1];
        if (temp.Length > 0 && temp[0] != '-')
          return temp;
        return "";
      }

    return null;
  }

  /// <summary>
  ///     Enables or disables auto-start for the application.
  /// </summary>
  /// <param name="enable">True to enable auto-start, false to disable.</param>
  public static void AutoStart(bool enable) {
    var subKey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
    if (enable) {
      var value = $"\"{Updater.CurrentFileLocation}\" -tray";
      subKey.SetValue("winUpdateMiniTool", value);
    }
    else {
      subKey.DeleteValue("winUpdateMiniTool", false);
    }
  }

  /// <summary>
  ///     Checks if auto-start is enabled for the application.
  /// </summary>
  /// <returns>True if auto-start is enabled, false otherwise.</returns>
  public static bool IsAutoStart() {
    var subKey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false);
    return subKey?.GetValue("winUpdateMiniTool") != null;
  }

  /// <summary>
  ///     Checks if the UAC skip task is enabled.
  /// </summary>
  /// <returns>True if the UAC skip task is enabled, false otherwise.</returns>
  public static bool IsSkipUacRun() {
    try {
      TaskScheduler.TaskScheduler service = new();
      service.Connect();
      var folder = service.GetFolder(@"\"); // root
      var task = folder.GetTask(MF_APP_TASK_NAME);
      return task != null;
    }
    catch (Exception e) {
      Console.WriteLine(e.Message);
    }

    return false;
  }

  /// <summary>
  ///     Enables or disables the UAC skip task.
  /// </summary>
  /// <param name="isEnable">True to enable the UAC skip task, false to disable.</param>
  /// <returns>True if the operation succeeded, false otherwise.</returns>
  public static bool SkipUacEnable(bool isEnable) {
    try {
      TaskScheduler.TaskScheduler service = new();
      service.Connect();
      var folder = service.GetFolder(@"\"); // root
      if (isEnable) {
        var exePath = Updater.CurrentFileLocation;
        var task = service.NewTask(0);
        task.RegistrationInfo.Author = "winUpdateMiniTool";
        task.Principal.RunLevel = _TASK_RUNLEVEL.TASK_RUNLEVEL_HIGHEST;
        task.Settings.AllowHardTerminate = false;
        task.Settings.StartWhenAvailable = false;
        task.Settings.DisallowStartIfOnBatteries = false;
        task.Settings.StopIfGoingOnBatteries = false;
        task.Settings.MultipleInstances = _TASK_INSTANCES_POLICY.TASK_INSTANCES_PARALLEL;
        task.Settings.ExecutionTimeLimit = "PT0S";
        var action = (IExecAction)task.Actions.Create(_TASK_ACTION_TYPE.TASK_ACTION_EXEC);
        action.Path = exePath;
        action.WorkingDirectory = appPath;
        action.Arguments = "-NoUAC $(Arg0)";

        var registeredTask = folder.RegisterTaskDefinition(MF_APP_TASK_NAME, task,
            (int)_TASK_CREATION.TASK_CREATE_OR_UPDATE, null, null,
            _TASK_LOGON_TYPE.TASK_LOGON_INTERACTIVE_TOKEN);

        if (registeredTask == null)
          return false;

        // Note: if we run as UWP we need to adjust the file permissions for this workaround to work
        if (OSHelper.IsRunningAsUwp()) {
          if (!FileOps.TakeOwn(exePath))
            return false;

          FileInfo fi = new(exePath);
          var ac = fi.GetAccessControl();
          ac.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(FileOps.MF_SID_WORLS),
              FileSystemRights.ReadAndExecute, AccessControlType.Allow));
          fi.SetAccessControl(ac);
        }
      }
      else {
        folder.DeleteTask(MF_APP_TASK_NAME, 0);
      }
    }
    catch (Exception err) {
      AppLog.Line("Enable SkipUAC Error {0}", err.ToString());
      return false;
    }

    return true;
  }

  /// <summary>
  ///     Checks whether non-elevated processes can modify the application directory, which would let them
  ///     replace the executable or Tools\Tools.ini and get code run elevated through the UAC skip task.
  /// </summary>
  /// <returns>True if the directory grants write access to the current user or to broad user groups.</returns>
  public static bool IsAppDirWritableByUsers() {
    try {
      string[] userSids = [
        "S-1-1-0", // Everyone
        "S-1-5-4", // Interactive
        "S-1-5-11", // Authenticated Users
        "S-1-5-32-545", // Users
        WindowsIdentity.GetCurrent().User?.Value
      ];
      var toolsPath = GetToolsPath();
      return GrantsWrite(new DirectoryInfo(appPath).GetAccessControl(), userSids) ||
             GrantsWrite(new FileInfo(Updater.CurrentFileLocation).GetAccessControl(), userSids) ||
             Directory.Exists(toolsPath) && GrantsWrite(new DirectoryInfo(toolsPath).GetAccessControl(), userSids);
    }
    catch (Exception err) {
      AppLog.Line("Failed to check application directory permissions: {0}", err.Message);
      return true;
    }
  }

  private static bool GrantsWrite(FileSystemSecurity security, string[] sids) {
    const FileSystemRights writeRights = FileSystemRights.WriteData | FileSystemRights.AppendData |
                                         FileSystemRights.Delete | FileSystemRights.ChangePermissions |
                                         FileSystemRights.TakeOwnership;
    FileSystemRights allowed = 0, denied = 0;
    foreach (FileSystemAccessRule rule in security.GetAccessRules(true, true, typeof(SecurityIdentifier))) {
      // Inherit-only entries apply to children, not to the object itself.
      if ((rule.PropagationFlags & PropagationFlags.InheritOnly) != 0 || !sids.Contains(rule.IdentityReference.Value))
        continue;
      if (rule.AccessControlType == AccessControlType.Deny)
        denied |= rule.FileSystemRights;
      else
        allowed |= rule.FileSystemRights;
    }

    return (allowed & ~denied & writeRights) != 0;
  }

  /// <summary>
  ///     Runs the UAC skip task.
  /// </summary>
  /// <returns>True if the task was started successfully, false otherwise.</returns>
  private static bool SkipUacRun() {
    try {
      TaskScheduler.TaskScheduler service = new();
      service.Connect();
      var folder = service.GetFolder(@"\");
      var task = folder.GetTask(MF_APP_TASK_NAME);
      AppLog.Line("Trying to SkipUAC ...");
      var action = (IExecAction)task.Definition.Actions[1];
      if (action.Path.Equals(Updater.CurrentFileLocation, StringComparison.OrdinalIgnoreCase)) {
        var arguments = string.Join(" ", args.Select(EscapeArg));
        var runningTask = task.RunEx(arguments, (int)_TASK_RUN_FLAGS.TASK_RUN_NO_FLAGS, 0, null);

        for (var i = 0; i < 5; i++) {
          Thread.Sleep(250);
          runningTask.Refresh();
          var state = runningTask.State;
          if (state is _TASK_STATE.TASK_STATE_RUNNING or _TASK_STATE.TASK_STATE_READY)
            return true;
          if (state == _TASK_STATE.TASK_STATE_DISABLED)
            break;
        }
      }
    }
    catch (Exception err) {
      AppLog.Line("SkipUAC Error {0}", err.ToString());
    }

    return false;
  }

  /// <summary>
  ///     Shows the help message.
  /// </summary>
  private static void ShowHelp() {
    var message = "Available command line options\r\n";
    string[] help =
    [
        "-tray\t\tStart in Tray",
            "-onclose [cmd]\tExecute commands when closing",
            "-update\t\tSearch for updates on start",
            "-console\t\tShow console (for debugging)",
            "-help\t\tShow this help message"
    ];
    if (!mConsole) {
      MessageBox.Show(message + string.Join("\r\n", help), Updater.ApplicationTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
    else {
      Console.WriteLine(message);
      foreach (var t in help)
        Console.WriteLine(@" " + t);
    }
  }
}
