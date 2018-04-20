using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;

namespace winUpdateMiniTool.Common;

internal class FileOps {
  public const string MF_SID_NULL = "S-1-0-0"; //	Null SID
  public const string MF_SID_WORLS = "S-1-1-0"; //	World
  public const string MF_SID_LOCAL = "S-1-2-0"; //	Local
  public const string MF_SID_CONSOLE = "S-1-2-1"; //	Console Logon
  public const string MF_SID_OWNER_ID = "S-1-3-0"; //	Creator Owner ID
  public const string MF_SID_GROUPE_ID = "S-1-3-1"; //	Creator Group ID
  public const string MF_SID_OWNER_SVR = "S-1-3-2"; //	Creator Owner Server
  public const string MF_SID_CREATOR_SVR = "S-1-3-3"; //	Creator Group Server
  public const string MF_SID_OWNER_RIGHTS = "S-1-3-4"; //	Owner Rights
  public const string MF_SID_NON_UNIQUE = "S-1-4"; //	Non-unique Authority
  public const string MF_SID_NT_AUTH = "S-1-5"; //	NT Authority
  public const string MF_SID_DIAL_UP = "S-1-5-1"; //	Dialup
  public const string MF_SID_LOCAL_ACC = "S-1-5-113"; //	Local account
  public const string MF_SID_LOCAL_ACC_ADMIN = "S-1-5-114"; //	Local account and member of Administrators-Group
  public const string MF_SID_NET = "S-1-5-2"; //	Network
  public const string MF_SID_NATCH = "S-1-5-3"; //	Batch

  public const string MF_SID_INTERACTIVE = "S-1-5-4"; //	Interactive

  //public const string SID_ = "S-1-5-5- *X*- *Y* Logon Session
  public const string MF_SID_SERVICE = "S-1-5-6"; //	Service
  public const string MF_SID_ANON_LOGIN = "S-1-5-7"; //	Anonymous Logon
  public const string MF_SID_PROXY = "S-1-5-8"; //	Proxy
  public const string MF_SID_EDC = "S-1-5-9"; //	Enterprise Domain Controllers
  public const string MF_SID_SELF = "S-1-5-10"; //	Self
  public const string MF_SID_AUTHENTICATED_USER = "S-1-5-11"; //	Authenticated Users
  public const string MF_SID_RESTRICTED = "S-1-5-12"; //	Restricted Code
  public const string MF_SID_TERM_USER = "S-1-5-13"; //	Terminal Server User
  public const string MF_SID_REMOTE_LOGIN = "S-1-5-14"; //	Remote Interactive Logon
  public const string MF_SID_THIS_O_RG = "S-1-5-15"; //	This Organization
  public const string MF_SID_IIS = "S-1-5-17"; //	IIS_USRS
  public const string MF_SID_SYSTEM = "S-1-5-18"; //	System(or LocalSystem)
  public const string MF_SID_NT_AUTH_L = "S-1-5-19"; //	NT Authority(LocalService)
  public const string MF_SID_NET_SERVICES = "S-1-5-20"; //	Network Service
  private const string SidAdmins = "S-1-5-32-544"; //	Administrators
  private const string SidUsers = "S-1-5-32-545"; //	Users
  public const string MF_SID_GUESTS = "S-1-5-32-546"; //	Guests
  public const string MF_SID_POWER_USERS = "S-1-5-32-547"; //	Power-Users
  public const string MF_SID_ACC_OPS = "S-1-5-32-548"; //	Account Operators
  public const string MF_SID_SERVER_OPS = "S-1-5-32-549"; //	Server Operators
  public const string MF_SID_PRINT_OPS = "S-1-5-32-550"; //	Print Operators
  public const string MF_SID_BACKUP_OPS = "S-1-5-32-551"; //	Backup Operators
  public const string MF_SID_REPLICATORS = "S-1-5-32-552"; //	Replicators
  public const string MF_SID_NTLM_AUTH = "S-1-5-64-10"; //	NTLM Authentication
  public const string MF_SID_S_CH_AUTH = "S-1-5-64-14"; //	SChannel Authentication
  public const string MF_SID_DIGEST_AUTH = "S-1-5-64-21"; //	Digest Authentication
  public const string MF_SID_NT_SERVICE = "S-1-5-80"; //	NT Service
  public const string MF_SID_ALL_SERVICES = "S-1-5-80-0"; //	All Services
  public const string MF_SID_VM = "S-1-5-83-0"; //	NT VIRTUAL MACHINE\Virtual Machines
  public const string MF_SID_UNTRUSTED_LEVEL = "S-1-16-0"; //	Untrusted Mandatory Level
  public const string MF_SID_LOW_LEVEL = "S-1-16-4096"; //	Low Mandatory Level
  public const string MF_SID_MEDIUM_LEVEL = "S-1-16-8192"; //	Medium Mandatory Level
  public const string MF_SID_MEDIUM_P_LEVEL = "S-1-16-8448"; //	Medium Plus Mandatory Level
  public const string MF_SID_HIGH_LEVEL = "S-1-16-12288"; //	High Mandatory Level
  public const string MF_SID_SYS_LEVEL = "S-1-16-16384"; //	System Mandatory Level
  public const string MF_SID_PP_LEVEL = "S-1-16-20480"; //	Protected Process Mandatory Level
  public const string MF_SID_SP_LEVEL = "S-1-16-28672"; //	Secure Process Mandatory Level

  /// <summary>
  ///     Formats the given size in bytes to a human-readable string.
  /// </summary>
  /// <param name="size">The size in bytes.</param>
  /// <returns>A formatted string representing the size in B, KB, MB, or GB.</returns>
  public static string FormatSize(decimal size) {
    if (size == 0)
      return "";
    if (size >= 1024 * 1024 * 1024)
      return (size / (1024 * 1024 * 1024)).ToString("F") + " GB";
    if (size >= 1024 * 1024)
      return (size / (1024 * 1024)).ToString("F") + " MB";
    if (size >= 1024)
      return (size / 1024).ToString("F") + " KB";
    return (long)size + " B";
  }

  /// <summary>
  ///     Moves a file from one location to another.
  /// </summary>
  /// <param name="from">The source file path.</param>
  /// <param name="to">The destination file path.</param>
  /// <param name="overwrite">Whether to overwrite the destination file if it exists.</param>
  /// <returns>True if the file was moved successfully, otherwise false.</returns>
  public static bool MoveFile(string from, string to, bool overwrite = false) {
    try {
      if (File.Exists(to)) {
        if (!overwrite)
          return false;
        File.Delete(to);
      }

      File.Move(from, to);

      if (File.Exists(from))
        return false;
    }
    catch (Exception e) {
      Console.WriteLine(@"The process failed: {0}", e);
      return false;
    }

    return true;
  }

  /// <summary>
  ///     Deletes a file at the specified path.
  /// </summary>
  /// <param name="path">The file path.</param>
  /// <returns>True if the file was deleted successfully, otherwise false.</returns>
  public static bool DeleteFile(string path) {
    try {
      File.Delete(path);
      return true;
    }
    catch {
      return false;
    }
  }

  /// <summary>
  ///     Tests the administrative security settings of a file.
  /// </summary>
  /// <param name="filePath">The file path.</param>
  /// <returns>
  ///     0 if the file has write or delete permissions for non-admin users, 1 if it does not, 2 if the file does not
  ///     exist.
  /// </returns>
  public static int TestFileAdminSec(string filePath) {
    //get file info
    FileInfo fi = new(filePath);
    if (!fi.Exists)
      return 2;

    //get security access
    var fs = fi.GetAccessControl();

    //get any special user access
    var
        rules = fs.GetAccessRules(true, true, typeof(SecurityIdentifier)); // get as SID not string


    //remove any special access
    foreach (FileSystemAccessRule rule in rules) {
      if (rule.AccessControlType != AccessControlType.Allow)
        continue;
      if (rule.IdentityReference.Value.Equals(SidAdmins) || rule.IdentityReference.Value.Equals(MF_SID_SYSTEM))
        continue;
      if ((rule.FileSystemRights & (FileSystemRights.Write | FileSystemRights.Delete)) != 0)
        return 0;
    }

    return 1;
  }

  /// <summary>
  ///     Sets the administrative security settings of a file.
  /// </summary>
  /// <param name="filePath">The file path.</param>
  public static void SetFileAdminSec(string filePath) {
    //get file info
    FileInfo fi = new(filePath);
    if (!fi.Exists) {
      var fOut = fi.OpenWrite();
      fOut.Close();
    }

    //get security access
    var fs = fi.GetAccessControl();

    //remove any inherited access
    fs.SetAccessRuleProtection(true, false);

    //get any special user access
    var rules = fs.GetAccessRules(true, true, typeof(NTAccount)); // show as names

    //remove any special access
    foreach (FileSystemAccessRule rule in rules)
      fs.RemoveAccessRule(rule);

    fs.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(SidAdmins), FileSystemRights.FullControl,
        AccessControlType.Allow));
    fs.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(MF_SID_SYSTEM), FileSystemRights.FullControl,
        AccessControlType.Allow));
    fs.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(SidUsers), FileSystemRights.Read,
        AccessControlType.Allow));

    //add current user with full control.
    //fs.AddAccessRule(new FileSystemAccessRule(domainName + "\\" + userName, FileSystemRights.FullControl, AccessControlType.Allow));

    //add all other users delete only permissions.
    //SecurityIdentifier sid = new SecurityIdentifier("S-1-5-11"); // Authenticated Users
    //fs.AddAccessRule(new FileSystemAccessRule(sid, FileSystemRights.Delete, AccessControlType.Allow));

    //flush security access.
    File.SetAccessControl(filePath, fs);
  }

  /// <summary>
  ///     Tests if the file can be written to.
  /// </summary>
  /// <param name="filePath">The file path.</param>
  /// <returns>True if the file can be written to, otherwise false.</returns>
  public static bool TestWrite(string filePath) {
    FileInfo fi = new(filePath);
    try {
      var fOut = fi.OpenWrite();
      fOut.Close();
      return true;
    }
    catch {
      return false;
    }
  }

  /// <summary>
  ///     Takes ownership of a file.
  /// </summary>
  /// <param name="path">The file path.</param>
  /// <returns>True if ownership was taken successfully, otherwise false.</returns>
  internal static bool TakeOwn(string path) {
    var ret = true;
    try {
      //TokenManipulator.AddPrivilege("SeRestorePrivilege");
      //TokenManipulator.AddPrivilege("SeBackupPrivilege");
      TokenManipulator.AddPrivilege("SeTakeOwnershipPrivilege");


      var ac = File.GetAccessControl(path);
      ac.SetOwner(new SecurityIdentifier(SidAdmins));
      File.SetAccessControl(path, ac);
    }
    catch (PrivilegeNotHeldException err) {
      AppLog.Line("Couldn't take Ownership {0}", err.ToString());
      ret = false;
    }
    finally {
      //TokenManipulator.RemovePrivilege("SeRestorePrivilege");
      //TokenManipulator.RemovePrivilege("SeBackupPrivilege");
      TokenManipulator.RemovePrivilege("SeTakeOwnershipPrivilege");
    }

    return ret;
  }
}
