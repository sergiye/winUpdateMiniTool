using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace winUpdateMiniTool;

/// <summary>
///     Verifies Authenticode signatures of downloaded update files before they are executed.
/// </summary>
internal static class SignatureVerifier {
  private const string MicrosoftOrganization = "O=Microsoft Corporation";
  private static readonly Guid WintrustActionGenericVerifyV2 = new("00AAC56B-CD44-11d0-8CC2-00C04FC295EE");
  private static readonly IntPtr InvalidHandleValue = new(-1);

  private const uint WtdUiNone = 2;
  private const uint WtdRevokeNone = 0;
  private const uint WtdChoiceFile = 1;
  private const uint WtdStateActionIgnore = 0;
  private const uint WtdRevocationCheckNone = 0x10;

  [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
  private struct WinTrustFileInfo {
    public uint cbStruct;
    [MarshalAs(UnmanagedType.LPWStr)] public string pcwszFilePath;
    public IntPtr hFile;
    public IntPtr pgKnownSubject;
  }

  [StructLayout(LayoutKind.Sequential)]
  private struct WinTrustData {
    public uint cbStruct;
    public IntPtr pPolicyCallbackData;
    public IntPtr pSIPClientData;
    public uint dwUIChoice;
    public uint fdwRevocationChecks;
    public uint dwUnionChoice;
    public IntPtr pFile;
    public uint dwStateAction;
    public IntPtr hWVTStateData;
    public IntPtr pwszURLReference;
    public uint dwProvFlags;
    public uint dwUIContext;
    public IntPtr pSignatureSettings;
  }

  [DllImport("wintrust.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
  private static extern int WinVerifyTrust(IntPtr hwnd, [MarshalAs(UnmanagedType.LPStruct)] Guid pgActionId,
      ref WinTrustData pWvtData);

  /// <summary>
  ///     Checks that the file carries a valid embedded Authenticode signature issued to Microsoft Corporation.
  /// </summary>
  /// <param name="fileName">The file to verify.</param>
  /// <param name="error">The reason the verification failed, if it did.</param>
  /// <returns>True if the signature is valid and the signer is Microsoft.</returns>
  public static bool IsMicrosoftSigned(string fileName, out string error) {
    var result = VerifyTrust(fileName);
    if (result != 0) {
      error = $"invalid or missing digital signature (0x{result:X8})";
      return false;
    }

    try {
      using var signer = X509Certificate.CreateFromSignedFile(fileName);
      using var certificate = new X509Certificate2(signer);
      var subject = certificate.SubjectName;
      // Compare whole RDNs, so a value like CN="O=Microsoft Corporation" does not match.
      var rdns = subject.Decode(X500DistinguishedNameFlags.UseNewLines)
          .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
      if (!rdns.Any(rdn => rdn.Trim().Equals(MicrosoftOrganization, StringComparison.Ordinal))) {
        error = $"file is not signed by Microsoft ({subject.Name})";
        return false;
      }
    }
    catch (Exception e) {
      error = $"cannot read the signing certificate: {e.Message}";
      return false;
    }

    error = null;
    return true;
  }

  private static int VerifyTrust(string fileName) {
    var fileInfo = new WinTrustFileInfo {
      cbStruct = (uint)Marshal.SizeOf<WinTrustFileInfo>(),
      pcwszFilePath = fileName
    };
    var pFile = Marshal.AllocHGlobal(Marshal.SizeOf<WinTrustFileInfo>());
    try {
      Marshal.StructureToPtr(fileInfo, pFile, false);
      var data = new WinTrustData {
        cbStruct = (uint)Marshal.SizeOf<WinTrustData>(),
        dwUIChoice = WtdUiNone,
        fdwRevocationChecks = WtdRevokeNone,
        dwUnionChoice = WtdChoiceFile,
        pFile = pFile,
        dwStateAction = WtdStateActionIgnore,
        dwProvFlags = WtdRevocationCheckNone
      };
      return WinVerifyTrust(InvalidHandleValue, WintrustActionGenericVerifyV2, ref data);
    }
    finally {
      Marshal.DestroyStructure<WinTrustFileInfo>(pFile);
      Marshal.FreeHGlobal(pFile);
    }
  }
}
