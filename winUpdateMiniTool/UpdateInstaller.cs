using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Windows.Threading;
using winUpdateMiniTool.Common;

namespace winUpdateMiniTool;

/// <summary>
///     Handles the installation and uninstallation of updates.
/// </summary>
internal class UpdateInstaller {
  private readonly Dispatcher mDispatcher = Dispatcher.CurrentDispatcher;
  private bool canceled;
  private bool doInstall = true;
  private int errorCount;
  private MultiValueDictionary<string, string> mAllFiles;
  private int mCurrentTask;
  private Thread mThread;
  private List<MsUpdate> mUpdates;
  private bool rebootRequired;

  /// <summary>
  ///     Resets the internal state of the installer.
  /// </summary>
  private void Reset() {
    errorCount = 0;
    rebootRequired = false;
    canceled = false;
    mCurrentTask = 0;
  }

  /// <summary>
  ///     Initiates the installation of updates.
  /// </summary>
  /// <param name="updates">List of updates to install.</param>
  /// <param name="allFiles">Dictionary of all files associated with the updates.</param>
  /// <returns>True if the installation process started successfully.</returns>
  public bool Install(List<MsUpdate> updates, MultiValueDictionary<string, string> allFiles) {
    Reset();
    mUpdates = updates;
    mAllFiles = allFiles;
    doInstall = true;

    NextUpdate();
    return true;
  }

  /// <summary>
  ///     Initiates the uninstallation of updates.
  /// </summary>
  /// <param name="updates">List of updates to uninstall.</param>
  /// <returns>True if the uninstallation process started successfully.</returns>
  public bool UnInstall(List<MsUpdate> updates) {
    Reset();
    mUpdates = updates;
    doInstall = false;

    NextUpdate();
    return true;
  }

  /// <summary>
  ///     Checks if the installer is currently busy.
  /// </summary>
  /// <returns>True if the installer is busy, otherwise false.</returns>
  public bool IsBusy() {
    return mUpdates != null;
  }

  /// <summary>
  ///     Cancels the ongoing operations.
  /// </summary>
  public void CancelOperations() {
    canceled = true;
  }

  /// <summary>
  ///     Proceeds to the next update in the list.
  /// </summary>
  private void NextUpdate() {
    if (!canceled && mUpdates.Count > mCurrentTask) {
      var percent = 0; // Note: there does not seem to be an easy way to get this value
      if (mUpdates is { Count: > 0 })
        Progress?.Invoke(
            this,
            new WuAgent.ProgressArgs(
                mUpdates.Count,
                (100 * mCurrentTask + percent) / mUpdates.Count,
                mCurrentTask + 1,
                percent,
                mUpdates[mCurrentTask].Title
            )
        );
      else
        Progress?.Invoke(this, new WuAgent.ProgressArgs(0, 0, 0, percent, string.Empty));

      if (doInstall) {
        var files = mAllFiles.GetValues(mUpdates[mCurrentTask].Kb);

        mThread = new Thread(RunInstall);
        mThread.Start(files);
      }
      else {
        var kb = mUpdates[mCurrentTask].Kb;

        mThread = new Thread(RunUnInstall);
        mThread.Start(kb);
      }

      return;
    }

    FinishedEventArgs args =
        new(errorCount, rebootRequired) {
          //args.AllFiles = mAllFiles;
          Updates = mUpdates
        };
    mAllFiles = null;
    mUpdates = null;
    Finished?.Invoke(this, args);
  }

  /// <summary>
  ///     Handles the completion of an update task.
  /// </summary>
  /// <param name="success">Indicates if the task was successful.</param>
  /// <param name="reboot">Indicates if a reboot is required.</param>
  private void OnFinished(bool success, bool reboot) {
    if (!success)
      errorCount++;
    if (reboot)
      rebootRequired = true;

    mThread.Join();
    mThread = null;

    mCurrentTask++;
    NextUpdate();
  }

  /// <summary>
  ///     Runs the installation process for the given files.
  /// </summary>
  /// <param name="parameters">List of files to install.</param>
  private void RunInstall(object parameters) {
    var files = (List<string>)parameters;

    var ok = true;
    var reboot = false;

    foreach (var curFile in files) {
      if (canceled)
        break;

      var file = curFile;

      AppLog.Line("Installing: {0}", file);

      try {
        var ext = Path.GetExtension(file);

        if (ext.Equals(".zip", StringComparison.CurrentCultureIgnoreCase)) {
          var path = Path.GetDirectoryName(file) + @"\files"; // + Path.GetFileNameWithoutExtension(File);

          if (!Directory.Exists(path)) // is it already unpacked?
            ZipFile.ExtractToDirectory(file, path);

          var supportedExtensions = "*.msu,*.msi,*.cab,*.exe";
          var foundFiles = Directory
              .GetFiles(path, "*.*", SearchOption.AllDirectories)
              .Where(s => supportedExtensions.Contains(Path.GetExtension(s).ToLower()));
          IEnumerable<string> enumerable = foundFiles as string[] ?? foundFiles.ToArray();
          if (!enumerable.Any())
            throw new FileNotFoundException("Expected file not found in zip");

          file = enumerable.First();
          ext = Path.GetExtension(file);
        }

        if (canceled)
          break;

        int exitCode;

        if (ext.Equals(".exe", StringComparison.CurrentCultureIgnoreCase))
          exitCode = InstallExe(file);
        else if (ext.Equals(".msi", StringComparison.CurrentCultureIgnoreCase))
          exitCode = InstallMsi(file);
        else if (ext.Equals(".msu", StringComparison.CurrentCultureIgnoreCase))
          exitCode = InstallMsu(file);
        else if (ext.Equals(".cab", StringComparison.CurrentCultureIgnoreCase))
          exitCode = InstallCab(file);
        else
          throw new FileFormatException("Unknown Update format: " + ext);

        if (exitCode == 3010) {
          reboot = true; // reboot required
        }
        else if (exitCode == 1641) {
          AppLog.Line("Error, reboot got initiated: {0}", file);
          reboot = true; // reboot initiated
          ok = false;
        }
        else if (exitCode != 1 && exitCode != 0) {
          ok = false; // some error
        }
      }
      catch (Exception e) {
        ok = false;
        Console.WriteLine(@"Error installing update: {0}", e.Message);
      }
    }

    mDispatcher.BeginInvoke(
        new Action(() => {
          OnFinished(ok, reboot);
        })
    );
  }

  /// <summary>
  ///     Installs an executable file.
  /// </summary>
  /// <param name="fileName">The name of the executable file.</param>
  /// <returns>The exit code of the installation process.</returns>
  private int InstallExe(string fileName) {
    ProcessStartInfo startInfo = new() { FileName = fileName };

    // ToDo: load from file or make it less complex
    var name = Path.GetFileNameWithoutExtension(fileName);
    string[] args = ["ndp", "OFV", "2553065"];
    startInfo.Arguments = args.Any(a =>
        name.StartsWith(a, StringComparison.CurrentCultureIgnoreCase)
    )
        ? "/q /norestart"
        : "/q /z";

    return ExecTask(startInfo);
  }

  /// <summary>
  ///     Installs an MSI file.
  /// </summary>
  /// <param name="fileName">The name of the MSI file.</param>
  /// <returns>The exit code of the installation process.</returns>
  private int InstallMsi(string fileName) {
    ProcessStartInfo startInfo =
        new() {
          FileName = @"%SystemRoot%\System32\msiexec.exe",
          Arguments = "/i \"" + fileName + "\" /qn /norestart"
        };

    return ExecTask(startInfo);
  }

  /// <summary>
  ///     Installs an MSU file.
  /// </summary>
  /// <param name="fileName">The name of the MSU file.</param>
  /// <returns>The exit code of the installation process.</returns>
  private int InstallMsu(string fileName) {
    ProcessStartInfo startInfo =
        new() {
          FileName = @"%SystemRoot%\System32\wusa.exe",
          Arguments = "\"" + fileName + "\" /quiet /norestart"
        };

    return ExecTask(startInfo);
  }

  /// <summary>
  ///     Checks if a CAB file is applicable.
  /// </summary>
  /// <param name="fileName">The name of the CAB file.</param>
  /// <returns>True if the CAB file is applicable, otherwise false.</returns>
  private bool CheckCab(string fileName) {
    try {
      Process proc = new();
      proc.StartInfo.FileName = Environment.ExpandEnvironmentVariables(
          @"%SystemRoot%\System32\Dism.exe"
      );
      proc.StartInfo.Arguments =
          "/Online /Get-PackageInfo /PackagePath:\"" + fileName + "\" /English";
      proc.StartInfo.RedirectStandardOutput = true;
      proc.StartInfo.RedirectStandardError = true;
      proc.StartInfo.UseShellExecute = false;
      proc.StartInfo.CreateNoWindow = true;
      proc.EnableRaisingEvents = true;
      proc.Start();
      proc.WaitForExit();
      while (!proc.StandardOutput.EndOfStream) {
        var line = proc.StandardOutput.ReadLine()?.Split(':');
        if (line != null && line.Length != 2)
          continue;

        if (
            line != null
            && !line[0]
                .Trim()
                .Equals("Applicable", StringComparison.CurrentCultureIgnoreCase)
        )
          continue;

        return line != null
               && line[1].Trim().Equals("Yes", StringComparison.CurrentCultureIgnoreCase);
      }
    }
    catch (Exception e) {
      Console.WriteLine(@"Dism error: {0}", e.Message);
    }

    return false;
  }

  /// <summary>
  ///     Installs a CAB file.
  /// </summary>
  /// <param name="fileName">The name of the CAB file.</param>
  /// <returns>The exit code of the installation process.</returns>
  private int InstallCab(string fileName) {
    if (!CheckCab(fileName) || canceled)
      return 0; // update not applicable or user canceled

    ProcessStartInfo startInfo =
        new() {
          FileName = @"%SystemRoot%\System32\Dism.exe",
          Arguments =
                "/Online /Quiet /NoRestart /Add-Package /PackagePath:\""
                + fileName
                + "\" /IgnoreCheck"
        };

    return ExecTask(startInfo);
  }

  /// <summary>
  ///     Executes a process with the given start information.
  /// </summary>
  /// <param name="startInfo">The start information for the process.</param>
  /// <param name="silent">Indicates if the process should run silently.</param>
  /// <returns>The exit code of the process.</returns>
  private int ExecTask(ProcessStartInfo startInfo, bool silent = true) {
    startInfo.FileName = Environment.ExpandEnvironmentVariables(startInfo.FileName);

    if (silent) {
      startInfo.RedirectStandardOutput = true;
      startInfo.RedirectStandardError = true;
      startInfo.UseShellExecute = false;
      startInfo.CreateNoWindow = true;
    }

    Process proc = new();
    proc.StartInfo = startInfo;
    proc.EnableRaisingEvents = true;
    proc.Start();
    proc.WaitForExit();

    return proc.ExitCode;
  }

  /// <summary>
  ///     Runs the uninstallation process for the given update.
  /// </summary>
  /// <param name="parameters">The KB number of the update to uninstall.</param>
  private void RunUnInstall(object parameters) {
    var kb = (string)parameters;

    AppLog.Line("Uninstalling: {0}", kb);

    var ok = true;
    var reboot = false;

    try {
      ProcessStartInfo startInfo =
          new() {
            FileName = @"%SystemRoot%\System32\wusa.exe",
            Arguments =
                  "/uninstall /kb:"
                  + kb.Substring(2)
                  + " /norestart" // /quiet
          };

      var exitCode = ExecTask(startInfo);

      if (exitCode == 3010 || exitCode == 1641) {
        reboot = true;
      }
      else if (exitCode != 1 && exitCode != 0) {
        AppLog.Line("Error, exit coded: {0}", exitCode);
        ok = false; // some error
      }
    }
    catch (Exception e) {
      ok = false;
      Console.WriteLine(@"Error removing update: {0}", e.Message);
    }

    mDispatcher.BeginInvoke(
        new Action(() => {
          OnFinished(ok, reboot);
        })
    );
  }

  /// <summary>
  ///     Event triggered when the installation or uninstallation process is finished.
  /// </summary>
  public event EventHandler<FinishedEventArgs> Finished;

  /// <summary>
  ///     Event triggered to report the progress of the installation or uninstallation process.
  /// </summary>
  public event EventHandler<WuAgent.ProgressArgs> Progress;

  /// <summary>
  ///     Arguments for the Finished event.
  /// </summary>
  public class FinishedEventArgs(int errorCount, bool reboot) : EventArgs {
    public readonly bool Reboot = reboot;
    public List<MsUpdate> Updates;

    //public MultiValueDictionary<string, string> AllFiles;
    public bool Success => errorCount == 0;
  }
}
