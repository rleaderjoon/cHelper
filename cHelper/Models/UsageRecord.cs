namespace cHelper.Models;

public class UsageRecord
{
    public DateTime Timestamp { get; init; }
    public string Model { get; init; } = "";
    public long InputTokens { get; init; }
    public long OutputTokens { get; init; }
    public long CacheCreationTokens { get; init; }
    public long CacheReadTokens { get; init; }
    public decimal CostUsd { get; init; }
    public string Source { get; init; } = "app"; // "app" | "claude-code"
}
