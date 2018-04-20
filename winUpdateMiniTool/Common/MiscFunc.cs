using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;

namespace winUpdateMiniTool.Common;

internal struct Lastinputinfo(uint dwTime) {
  public uint CbSize;
  public readonly uint DwTime = dwTime;
}

internal class MiscFunc {
  private const long MfAppmodelErrorNoPackage = 15700L;

  public static bool IsWindows7OrLower {
    get {
      var versionMajor = Environment.OSVersion.Version.Major;
      var versionMinor = Environment.OSVersion.Version.Minor;
      var version = versionMajor + (double)versionMinor / 10;
      return version <= 6.1;
    }
  }

  [DllImport("User32.dll")]
  private static extern bool GetLastInputInfo(ref Lastinputinfo plii);

  [DllImport("Kernel32.dll")]
  private static extern uint GetLastError();

  public static uint GetIdleTime() // in seconds
  {
    Lastinputinfo lastInPut = new();
    lastInPut.CbSize = (uint)Marshal.SizeOf(lastInPut);
    if (!GetLastInputInfo(ref lastInPut)) throw new Exception(GetLastError().ToString());
    return ((uint)Environment.TickCount - lastInPut.DwTime) / 1000;
  }

  public static int ParseInt(string str, int def = 0) {
    try {
      return int.Parse(str);
    }
    catch {
      return def;
    }
  }

  public static Color? ParseColor(string input) {
    ColorConverter c = new();
    if (Regex.IsMatch(input, "^(#[0-9A-Fa-f]{3})$|^(#[0-9A-Fa-f]{6})$"))
      return (Color)c.ConvertFromString(input)!;

    var svc = (TypeConverter.StandardValuesCollection)c.GetStandardValues();
    foreach (Color o in svc!)
      if (o.Name.Equals(input, StringComparison.OrdinalIgnoreCase))
        return (Color)c.ConvertFromString(input)!;
    return null;
  }

  /*public static String fmt(string str, params object[] args)
  {
      return string.Format(str, args);
  }*/

  public static bool IsAdministrator() {
    var identity = WindowsIdentity.GetCurrent();
    WindowsPrincipal principal = new(identity);
    return principal.IsInRole(WindowsBuiltInRole.Administrator);
  }

  [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
  private static extern bool CheckRemoteDebuggerPresent(IntPtr hProcess, ref bool isDebuggerPresent);

  public static bool IsDebugging() {
    var isDebuggerPresent = false;
    CheckRemoteDebuggerPresent(Process.GetCurrentProcess().Handle, ref isDebuggerPresent);
    return isDebuggerPresent;
  }

  [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
  private static extern int GetCurrentPackageFullName(ref int packageFullNameLength, StringBuilder packageFullName);

  public static bool IsRunningAsUwp() {
    if (IsWindows7OrLower) return false;

    var length = 0;
    StringBuilder sb = new(0);

    sb = new StringBuilder(length);
    var result = GetCurrentPackageFullName(ref length, sb);

    return result != MfAppmodelErrorNoPackage;
  }
}
