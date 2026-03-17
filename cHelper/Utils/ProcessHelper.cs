using System.Diagnostics;

namespace cHelper.Utils;

public static class ProcessHelper
{
    public static void OpenClaudeCode(string? workingDirectory = null)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "wt.exe",
            Arguments = $"-d \"{workingDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}\" claude",
            UseShellExecute = true
        };
        try { Process.Start(psi); }
        catch
        {
            // Fallback: cmd.exe
            var fallback = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/k claude",
                WorkingDirectory = workingDirectory,
                UseShellExecute = true
            };
            Process.Start(fallback);
        }
    }

    public static string? GetClaudeVersion()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "claude",
                Arguments = "--version",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            return proc?.StandardOutput.ReadLine()?.Trim();
        }
        catch { return null; }
    }

    public static void OpenUrl(string url)
    {
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
}
