using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace winUpdateMiniTool.Common;

internal static class WinConsole {
  public static bool Initialize(bool alwaysCreateNewConsole = true) {
    if (AttachConsole(MfAttachParrent) != 0)
      return true;
    if (!alwaysCreateNewConsole)
      return false;
    if (AllocConsole() == 0) 
      return false;
    InitializeOutStream();
    InitializeInStream();
    return true;
  }

  private static void InitializeOutStream() {
    var fs = CreateFileStream("CONOUT$", MfGenericWrite, MfFileShareWrite, FileAccess.Write);
    if (fs == null) return;
    StreamWriter writer = new(fs) { AutoFlush = true };
    Console.SetOut(writer);
    Console.SetError(writer);
  }

  private static void InitializeInStream() {
    var fs = CreateFileStream("CONIN$", MfGenericRead, MfFileShareRead, FileAccess.Read);
    if (fs != null) Console.SetIn(new StreamReader(fs));
  }

  private static FileStream CreateFileStream(string name, uint win32DesiredAccess, uint win32ShareMode,
      FileAccess dotNetFileAccess) {
    SafeFileHandle file = new(
        CreateFileW(name, win32DesiredAccess, win32ShareMode, IntPtr.Zero, MfOpenExisting,
            MfFileAttributeNormal,
            IntPtr.Zero), true);
    if (file.IsInvalid) return null;
    FileStream fs = new(file, dotNetFileAccess);
    return fs;
  }

  #region Win API Functions and Constants

  [DllImport("kernel32.dll",
      EntryPoint = "AllocConsole",
      SetLastError = true,
      CharSet = CharSet.Auto,
      CallingConvention = CallingConvention.StdCall)]
  private static extern int AllocConsole();

  [DllImport("kernel32.dll",
      EntryPoint = "AttachConsole",
      SetLastError = true,
      CharSet = CharSet.Auto,
      CallingConvention = CallingConvention.StdCall)]
  private static extern uint AttachConsole(uint dwProcessId);

  [DllImport("kernel32.dll",
      EntryPoint = "CreateFileW",
      SetLastError = true,
      CharSet = CharSet.Auto,
      CallingConvention = CallingConvention.StdCall)]
  private static extern IntPtr CreateFileW(
      string lpFileName,
      uint dwDesiredAccess,
      uint dwShareMode,
      IntPtr lpSecurityAttributes,
      uint dwCreationDisposition,
      uint dwFlagsAndAttributes,
      IntPtr hTemplateFile
  );

  private const uint MfGenericWrite = 0x40000000;
  private const uint MfGenericRead = 0x80000000;
  private const uint MfFileShareRead = 0x00000001;
  private const uint MfFileShareWrite = 0x00000002;
  private const uint MfOpenExisting = 0x00000003;
  private const uint MfFileAttributeNormal = 0x80;
  private const uint MfErrorAccessDenied = 5;
  private const uint MfAttachParrent = 0xFFFFFFFF;

  #endregion
}
