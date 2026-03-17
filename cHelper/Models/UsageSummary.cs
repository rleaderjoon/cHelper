namespace cHelper.Models;

public class UsageSummary
{
    public long TotalInputTokens { get; set; }
    public long TotalOutputTokens { get; set; }
    public long TotalCacheCreationTokens { get; set; }
    public long TotalCacheReadTokens { get; set; }
    public decimal TotalCostUsd { get; set; }
    public int RequestCount { get; set; }

    public long TotalTokens => TotalInputTokens + TotalOutputTokens;
}
