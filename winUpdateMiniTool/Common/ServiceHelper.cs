using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.ServiceProcess;

namespace winUpdateMiniTool.Common;

public static class ServiceHelper {
  private const uint MfServiceNoChange = 0xFFFFFFFF;
  private const uint MfServiceQueryConfig = 0x00000001;
  private const uint MfServiceChangeConfig = 0x00000002;
  private const uint MfScManagerAllAccess = 0x000F003F;
  private const uint MfScManagerConnect = 0x0001;
  private const uint MfScManagerEnumerateService = 0x0004;

  [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
  private static extern bool ChangeServiceConfig(IntPtr hService, uint nServiceType, uint nStartType,
      uint nErrorControl, string lpBinaryPathName, string lpLoadOrderGroup, IntPtr lpdwTagId,
      [In] char[] lpDependencies, string lpServiceStartName, string lpPassword, string lpDisplayName);

  [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
  private static extern IntPtr OpenService(IntPtr hScManager, string lpServiceName, uint dwDesiredAccess);

  [DllImport("advapi32.dll", EntryPoint = "OpenSCManagerW", ExactSpelling = true, CharSet = CharSet.Unicode,
      SetLastError = true)]
  private static extern IntPtr OpenSCManager(string machineName, string databaseName, uint dwAccess);

  [DllImport("advapi32.dll", EntryPoint = "CloseServiceHandle")]
  private static extern int CloseServiceHandle(IntPtr hScObject);

  public static void ChangeStartMode(ServiceController svc, ServiceStartMode mode) {
    //var scManagerHandle = OpenSCManager(null, null, SC_MANAGER_ALL_ACCESS);
    var scManagerHandle = OpenSCManager(null, null, MfScManagerConnect + MfScManagerEnumerateService);
    if (scManagerHandle == IntPtr.Zero) throw new ExternalException("Open Service Manager Error");

    var serviceHandle = OpenService(
        scManagerHandle,
        svc.ServiceName,
        MfServiceQueryConfig | MfServiceChangeConfig);

    if (serviceHandle == IntPtr.Zero) throw new ExternalException("Open Service Error");

    var result = ChangeServiceConfig(serviceHandle, MfServiceNoChange, (uint)mode, MfServiceNoChange, null,
        null,
        IntPtr.Zero, null, null, null, null);

    if (result == false) {
      var nError = Marshal.GetLastWin32Error();
      Win32Exception win32Exception = new(nError);
      throw new ExternalException("Could not change service start type: " + win32Exception.Message);
    }

    CloseServiceHandle(serviceHandle);
    CloseServiceHandle(scManagerHandle);
  }
}
