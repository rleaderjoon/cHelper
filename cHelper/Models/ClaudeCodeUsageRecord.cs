namespace cHelper.Models;

public class ClaudeCodeUsageRecord
{
    public DateTime Timestamp { get; init; } // UTC
    public string Model { get; init; } = "";
    public string SessionId { get; init; } = "";
    public string ProjectPath { get; init; } = "";
    public long InputTokens { get; init; }
    public long OutputTokens { get; init; }
    public long CacheCreationTokens { get; init; }
    public long CacheReadTokens { get; init; }

    public long TotalTokens => InputTokens + OutputTokens + CacheCreationTokens + CacheReadTokens;
}
