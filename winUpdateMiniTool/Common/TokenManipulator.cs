using System;
using System.Runtime.InteropServices;

namespace winUpdateMiniTool.Common;

public abstract class TokenManipulator {
  private const int MfSePrivilegeDisabled = 0x00000000;
  private const int MfSePrivilegeEnabled = 0x00000002;
  private const int MfTokenQuery = 0x00000008;
  private const int MfTokenAdjustPrivileges = 0x00000020;

  public const string MF_SE_ASSIGNPRIMARYTOKEN_NAME = "SeAssignPrimaryTokenPrivilege";
  public const string MF_SE_AUDIT_NAME = "SeAuditPrivilege";
  public const string MF_SE_BACKUP_NAME = "SeBackupPrivilege";
  public const string MF_SE_CHANGE_NOTIFY_NAME = "SeChangeNotifyPrivilege";
  public const string MF_SE_CREATE_GLOBAL_NAME = "SeCreateGlobalPrivilege";
  public const string MF_SE_CREATE_PAGEFILE_NAME = "SeCreatePagefilePrivilege";
  public const string MF_SE_CREATE_PERMANENT_NAME = "SeCreatePermanentPrivilege";
  public const string MF_SE_CREATE_SYMBOLIC_LINK_NAME = "SeCreateSymbolicLinkPrivilege";
  public const string MF_SE_CREATE_TOKEN_NAME = "SeCreateTokenPrivilege";
  public const string MF_SE_DEBUG_NAME = "SeDebugPrivilege";
  public const string MF_SE_ENABLE_DELEGATION_NAME = "SeEnableDelegationPrivilege";
  public const string MF_SE_IMPERSONATE_NAME = "SeImpersonatePrivilege";
  public const string MF_SE_INC_BASE_PRIORITY_NAME = "SeIncreaseBasePriorityPrivilege";
  public const string MF_SE_INCREASE_QUOTA_NAME = "SeIncreaseQuotaPrivilege";
  public const string MF_SE_INC_WORKING_SET_NAME = "SeIncreaseWorkingSetPrivilege";
  public const string MF_SE_LOAD_DRIVER_NAME = "SeLoadDriverPrivilege";
  public const string MF_SE_LOCK_MEMORY_NAME = "SeLockMemoryPrivilege";
  public const string MF_SE_MACHINE_ACCOUNT_NAME = "SeMachineAccountPrivilege";
  public const string MF_SE_MANAGE_VOLUME_NAME = "SeManageVolumePrivilege";
  public const string MF_SE_PROF_SINGLE_PROCESS_NAME = "SeProfileSingleProcessPrivilege";
  public const string MF_SE_RELABEL_NAME = "SeRelabelPrivilege";
  public const string MF_SE_REMOTE_SHUTDOWN_NAME = "SeRemoteShutdownPrivilege";
  public const string MF_SE_RESTORE_NAME = "SeRestorePrivilege";
  public const string MF_SE_SECURITY_NAME = "SeSecurityPrivilege";
  public const string MF_SE_SHUTDOWN_NAME = "SeShutdownPrivilege";
  public const string MF_SE_SYNC_AGENT_NAME = "SeSyncAgentPrivilege";
  public const string MF_SE_SYSTEM_ENVIRONMENT_NAME = "SeSystemEnvironmentPrivilege";
  public const string MF_SE_SYSTEM_PROFILE_NAME = "SeSystemProfilePrivilege";
  public const string MF_SE_SYSTEMTIME_NAME = "SeSystemtimePrivilege";
  public const string MF_SE_TAKE_OWNERSHIP_NAME = "SeTakeOwnershipPrivilege";
  public const string MF_SE_TCB_NAME = "SeTcbPrivilege";
  public const string MF_SE_TIME_ZONE_NAME = "SeTimeZonePrivilege";
  public const string MF_SE_TRUSTED_CREDMAN_ACCESS_NAME = "SeTrustedCredManAccessPrivilege";
  public const string MF_SE_UNDOCK_NAME = "SeUndockPrivilege";
  public const string MF_SE_UNSOLICITED_INPUT_NAME = "SeUnsolicitedInputPrivilege";


  [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
  private static extern bool AdjustTokenPrivileges(IntPtr htok, bool disall,
      ref TokPriv1Luid newst, int len, IntPtr prev, IntPtr relen);


  [DllImport("kernel32.dll", ExactSpelling = true)]
  private static extern IntPtr GetCurrentProcess();


  [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
  private static extern bool OpenProcessToken(IntPtr h, int acc, ref IntPtr
      phtok);


  [DllImport("advapi32.dll", SetLastError = true)]
  private static extern bool LookupPrivilegeValue(string host, string name,
      ref long pluid);

  public static bool AddPrivilege(string privilege) {
    TokPriv1Luid tp;
    var hproc = GetCurrentProcess();
    var htok = IntPtr.Zero;
    OpenProcessToken(hproc, MfTokenAdjustPrivileges | MfTokenQuery, ref htok);
    tp.Count = 1;
    tp.Luid = 0;
    tp.Attr = MfSePrivilegeEnabled;
    LookupPrivilegeValue(null, privilege, ref tp.Luid);
    var retVal = AdjustTokenPrivileges(htok, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero);
    return retVal;
  }

  public static bool RemovePrivilege(string privilege) {
    TokPriv1Luid tp;
    var hproc = GetCurrentProcess();
    var htok = IntPtr.Zero;
    OpenProcessToken(hproc, MfTokenAdjustPrivileges | MfTokenQuery, ref htok);
    tp.Count = 1;
    tp.Luid = 0;
    tp.Attr = MfSePrivilegeDisabled;
    LookupPrivilegeValue(null, privilege, ref tp.Luid);
    var retVal = AdjustTokenPrivileges(htok, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero);
    return retVal;
  }


  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  private struct TokPriv1Luid {
    public int Count;
    public long Luid;
    public int Attr;
  }
}
