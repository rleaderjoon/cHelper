using System.Runtime.InteropServices;
using System.Text;

namespace cHelper.Utils;

public static class CredentialHelper
{
    private const string TargetName = "cHelper/AnthropicApiKey";

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct CREDENTIAL
    {
        public uint Flags;
        public uint Type;
        [MarshalAs(UnmanagedType.LPWStr)] public string TargetName;
        [MarshalAs(UnmanagedType.LPWStr)] public string Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public uint Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        [MarshalAs(UnmanagedType.LPWStr)] public string TargetAlias;
        [MarshalAs(UnmanagedType.LPWStr)] public string UserName;
    }

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredRead(string target, uint type, int reservedFlag, out IntPtr credentialPtr);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredWrite([In] ref CREDENTIAL userCredential, [In] uint flags);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool CredDelete(string target, uint type, int flags);

    [DllImport("advapi32.dll")]
    private static extern void CredFree([In] IntPtr buffer);

    public static void SaveApiKey(string apiKey)
    {
        var credBlob = Encoding.Unicode.GetBytes(apiKey);
        var handle = GCHandle.Alloc(credBlob, GCHandleType.Pinned);
        try
        {
            var cred = new CREDENTIAL
            {
                Type = 1,
                TargetName = TargetName,
                CredentialBlobSize = (uint)credBlob.Length,
                CredentialBlob = handle.AddrOfPinnedObject(),
                Persist = 2,
                UserName = Environment.UserName
            };
            CredWrite(ref cred, 0);
        }
        finally
        {
            handle.Free();
        }
    }

    public static string? LoadApiKey()
    {
        // Environment variable takes priority
        var envKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        if (!string.IsNullOrEmpty(envKey)) return envKey;

        if (!CredRead(TargetName, 1, 0, out IntPtr ptr)) return null;
        try
        {
            var cred = Marshal.PtrToStructure<CREDENTIAL>(ptr);
            if (cred.CredentialBlobSize == 0) return null;
            var bytes = new byte[cred.CredentialBlobSize];
            Marshal.Copy(cred.CredentialBlob, bytes, 0, bytes.Length);
            return Encoding.Unicode.GetString(bytes);
        }
        finally
        {
            CredFree(ptr);
        }
    }

    public static void DeleteApiKey()
    {
        CredDelete(TargetName, 1, 0);
    }

    public static bool HasApiKey() => LoadApiKey() != null;
}
