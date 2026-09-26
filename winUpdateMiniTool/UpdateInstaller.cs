using sergiye.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Threading;

namespace winUpdateMiniTool;

/// <summary>
///     Handles the installation and uninstallation of updates.
/// </summary>
internal class UpdateInstaller {
  private const int CanceledExitCode = -1;
  private const int WuSAlreadyInstalled = 0x00240006;
  private const int WuSNotInstalled = 0x00240007;
  private const int WuENotApplicable = unchecked((int)0x80240017);
  private readonly Dispatcher mDispatcher = Dispatcher.CurrentDispatcher;
  private readonly object mProcessLock = new();
  private bool canceled;
  private bool doInstall = true;
  private int errorCount;
  private MultiValueDictionary<string, string> mAllFiles;
  private int mCurrentTask;
  private Process mCurProcess;
  private Thread mThread;
  private List<MsUpdate> mUpdates;
  private List<MsUpdate> mSucceeded = [];
  private bool rebootRequired;

  /// <summary>
  ///     Resets the internal state of the installer.
  /// </summary>
  private void Reset() {
    mSucceeded = [];
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
    lock (mProcessLock) {
      try {
        if (mCurProcess is { HasExited: false })
          mCurProcess.Kill();
      }
      catch (Exception e) {
        AppLog.Line("Error cancelling the running installer process: {0}", e.Message);
      }
    }
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
        var files = mAllFiles.GetValues(mUpdates[mCurrentTask].Key);

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
          Updates = mUpdates,
          Succeeded = mSucceeded
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
    if (success)
      mSucceeded.Add(mUpdates[mCurrentTask]);
    else
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

    if (files.Count == 0) {
      AppLog.Line("No downloaded files to install");
      ok = false;
    }

    List<string> installFiles = [];
    foreach (var file in files) {
      try {
        installFiles.AddRange(ExpandUpdateFile(file));
      }
      catch (Exception e) {
        ok = false;
        AppLog.Line("Error unpacking update {0}: {1}", file, e.Message);
      }
    }

    foreach (var file in installFiles) {
      if (canceled) {
        ok = false;
        break;
      }

      AppLog.Line("Installing: {0}", file);

      try {
        var ext = Path.GetExtension(file);

        if (!SignatureVerifier.IsMicrosoftSigned(file, out var signatureError))
          throw new InvalidDataException($"Refusing to install {Path.GetFileName(file)}: {signatureError}");

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
          throw new FileFormatException("Unknown update format: " + ext);

        if (exitCode == 3010) {
          reboot = true; // reboot required
        }
        else if (exitCode == 1641) {
          AppLog.Line("Error, reboot got initiated: {0}", file);
          reboot = true; // reboot initiated
          ok = false;
        }
        else if (exitCode == WuSAlreadyInstalled) {
          AppLog.Line("Already installed: {0}", file);
        }
        else if (exitCode == WuENotApplicable) {
          AppLog.Line("Update is not applicable to this system: {0}", file);
          ok = false;
        }
        // For msiexec, 1 is ERROR_INVALID_FUNCTION rather than a success code.
        else if (exitCode != 0 && (exitCode != 1 || ext.Equals(".msi", StringComparison.OrdinalIgnoreCase))) {
          AppLog.Line("Installer exit code 0x{0:X8}: {1}", exitCode, file);
          ok = false; // some error
        }
      }
      catch (Exception e) {
        ok = false;
        AppLog.Line("Error installing update: {0}", e.Message);
      }
    }

    mDispatcher.BeginInvoke(
        new Action(() => {
          OnFinished(ok, reboot);
        })
    );
  }

  /// <summary>
  ///     Returns the installable files for a downloaded file, unpacking it first if it is a zip archive.
  /// </summary>
  /// <param name="file">The downloaded file.</param>
  /// <returns>The files to install.</returns>
  private static List<string> ExpandUpdateFile(string file) {
    file = AddExtensionFromContent(file);
    if (!Path.GetExtension(file).Equals(".zip", StringComparison.OrdinalIgnoreCase))
      return [file];

    var path = Path.Combine(Path.GetDirectoryName(file)!, "files", Path.GetFileNameWithoutExtension(file));
    if (!Directory.Exists(path)) // is it already unpacked?
      ZipFile.ExtractToDirectory(file, path);

    string[] supportedExtensions = [".msu", ".msi", ".cab", ".exe"];
    var foundFiles = Directory
        .GetFiles(path, "*.*", SearchOption.AllDirectories)
        .Where(s => supportedExtensions.Contains(Path.GetExtension(s), StringComparer.OrdinalIgnoreCase))
        .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
        .ToList();
    if (foundFiles.Count == 0)
      throw new FileNotFoundException("No supported update file found in the zip archive");
    return foundFiles;
  }

  /// <summary>
  ///     Files from content delivery URLs are named after a GUID without an extension, so the installer
  ///     cannot tell their type. Detects the type from the content and renames the file accordingly.
  /// </summary>
  /// <param name="file">The downloaded file.</param>
  /// <returns>The path of the file with an extension, or the original path if the type is unknown.</returns>
  private static string AddExtensionFromContent(string file) {
    if (Path.GetExtension(file).Length > 0)
      return file;

    var ext = DetectFileType(file);
    if (ext == null) {
      AppLog.Line("Cannot determine the type of update file {0}", file);
      return file;
    }

    var target = file + ext;
    if (File.Exists(target))
      File.Delete(target);
    File.Move(file, target);
    return target;
  }

  private static string DetectFileType(string file) {
    using var reader = new BinaryReader(File.OpenRead(file));
    var header = reader.ReadBytes(4);
    if (header.Length >= 2 && header[0] == 'M' && header[1] == 'Z')
      return ".exe";
    if (header.Length == 4 && header[0] == 'P' && header[1] == 'K' && header[2] == 3 && header[3] == 4)
      return ".zip";
    if (header.Length == 4 && header[0] == 'M' && header[1] == 'S' && header[2] == 'C' && header[3] == 'F')
      return DetectCabinetKind(reader);
    return null;
  }

  // An .msu is a cabinet holding the package .cab and an .xml description, while a package .cab holds
  // update.mum; installing an .msu with DISM would report it as not applicable.
  private static string DetectCabinetKind(BinaryReader reader) {
    reader.BaseStream.Position = 16;
    var firstFileOffset = reader.ReadUInt32();
    reader.BaseStream.Position = 28;
    var fileCount = reader.ReadUInt16();

    reader.BaseStream.Position = firstFileOffset;
    bool hasCab = false, hasXml = false;
    for (var i = 0; i < fileCount && reader.BaseStream.Position < reader.BaseStream.Length; i++) {
      reader.BaseStream.Position += 16; // size, folder offset, folder index, date, time, attributes
      var name = new StringBuilder();
      for (int c; (c = reader.ReadByte()) != 0 && name.Length < 260;)
        name.Append((char)c);

      var fileName = name.ToString();
      if (fileName.EndsWith(".mum", StringComparison.OrdinalIgnoreCase))
        return ".cab";
      hasCab |= fileName.EndsWith(".cab", StringComparison.OrdinalIgnoreCase);
      hasXml |= fileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase);
    }

    return hasCab && hasXml ? ".msu" : null;
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
    using Process proc = new();
    try {
      proc.StartInfo.FileName = Environment.ExpandEnvironmentVariables(
          @"%SystemRoot%\System32\Dism.exe"
      );
      proc.StartInfo.Arguments =
          "/Online /Get-PackageInfo /PackagePath:\"" + fileName + "\" /English";
      proc.StartInfo.RedirectStandardOutput = true;
      proc.StartInfo.UseShellExecute = false;
      proc.StartInfo.CreateNoWindow = true;
      lock (mProcessLock) {
        if (canceled)
          return false;
        mCurProcess = proc;
        proc.Start();
      }

      // Read the whole output before waiting, otherwise DISM blocks on a full pipe.
      var output = proc.StandardOutput.ReadToEnd();
      proc.WaitForExit();
      foreach (var rawLine in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)) {
        var line = rawLine.Split(':');
        if (line.Length != 2 || !line[0].Trim().Equals("Applicable", StringComparison.OrdinalIgnoreCase))
          continue;

        return line[1].Trim().Equals("Yes", StringComparison.OrdinalIgnoreCase);
      }
    }
    catch (Exception e) {
      AppLog.Line("Dism error: {0}", e.Message);
    }
    finally {
      lock (mProcessLock) {
        if (mCurProcess == proc)
          mCurProcess = null;
      }
    }

    return false;
  }

  /// <summary>
  ///     Installs a CAB file.
  /// </summary>
  /// <param name="fileName">The name of the CAB file.</param>
  /// <returns>The exit code of the installation process.</returns>
  private int InstallCab(string fileName) {
    var applicable = CheckCab(fileName);
    if (canceled)
      return CanceledExitCode;
    if (!applicable)
      return 0; // update not applicable

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
  /// <returns>The exit code of the process.</returns>
  private int ExecTask(ProcessStartInfo startInfo) {
    startInfo.FileName = Environment.ExpandEnvironmentVariables(startInfo.FileName);
    startInfo.UseShellExecute = false;
    startInfo.CreateNoWindow = true;

    Process proc = new();
    proc.StartInfo = startInfo;
    proc.EnableRaisingEvents = true;

    try {
      // Starting under the lock ensures CancelOperations sees either no process or a started one.
      lock (mProcessLock) {
        if (canceled) return CanceledExitCode; // canceled before this step even started
        mCurProcess = proc;
        proc.Start();
      }

      proc.WaitForExit();
      return proc.ExitCode;
    }
    finally {
      lock (mProcessLock) {
        if (mCurProcess == proc)
          mCurProcess = null;
      }
      proc.Dispose();
    }
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

    if (!kb.StartsWith("KB", StringComparison.OrdinalIgnoreCase) || !int.TryParse(kb.Substring(2), out _)) {
      AppLog.Line("Cannot uninstall update with no known KB article: {0}", kb);
      ok = false;
    }
    else {
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
        else if (exitCode == WuSNotInstalled) {
          AppLog.Line("Update is not installed: {0}", kb);
        }
        else if (exitCode != 1 && exitCode != 0) {
          AppLog.Line("Error, exit code: 0x{0:X8}", exitCode);
          ok = false; // some error
        }
      }
      catch (Exception e) {
        ok = false;
        AppLog.Line("Error removing update: {0}", e.Message);
      }
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
    public List<MsUpdate> Succeeded;

    //public MultiValueDictionary<string, string> AllFiles;
    public bool Success => errorCount == 0;
  }
}
