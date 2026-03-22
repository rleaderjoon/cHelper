namespace cHelper.Models;

public class ClaudeCodeRateLimitInfo
{
    public bool IsRateLimited { get; init; }
    public DateTime RateLimitedAt { get; init; } // UTC
    public string ResetText { get; init; } = "";

    // Pro plan window is 5h; use 6h as a safe margin
    public bool IsCurrentlyLimited =>
        IsRateLimited && DateTime.UtcNow - RateLimitedAt < TimeSpan.FromHours(6);
}
