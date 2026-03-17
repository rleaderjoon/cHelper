namespace cHelper.Models;

public class AppSettings
{
    public string DefaultModel { get; set; } = "claude-sonnet-4-6";
    public bool StartWithWindows { get; set; } = false;
    public string Theme { get; set; } = "system";
    public int PollingIntervalSeconds { get; set; } = 60;
    public bool ShowBalloonOnRateLimit { get; set; } = true;
    public string ClaudeCodePath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude");
}
