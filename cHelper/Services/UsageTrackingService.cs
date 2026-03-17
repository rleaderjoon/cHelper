using cHelper.Data;
using cHelper.Models;
using cHelper.Utils;

namespace cHelper.Services;

public class UsageTrackingService
{
    private readonly UsageRepository _repo;

    public UsageTrackingService(UsageRepository repo)
    {
        _repo = repo;
    }

    public void Record(string model, long inputTokens, long outputTokens,
        long cacheCreationTokens = 0, long cacheReadTokens = 0, string source = "app")
    {
        var cost = TokenCostCalculator.Calculate(model, inputTokens, outputTokens,
            cacheCreationTokens, cacheReadTokens);
        _repo.Append(new UsageRecord
        {
            Timestamp = DateTime.UtcNow,
            Model = model,
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            CacheCreationTokens = cacheCreationTokens,
            CacheReadTokens = cacheReadTokens,
            CostUsd = cost,
            Source = source
        });
    }

    public UsageSummary GetToday()
    {
        var today = DateTime.UtcNow.Date;
        return Aggregate(_repo.GetSince(today));
    }

    public UsageSummary GetThisMonth()
    {
        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return Aggregate(_repo.GetSince(monthStart));
    }

    public UsageSummary GetAllTime()
    {
        return Aggregate(_repo.GetAll());
    }

    public IReadOnlyList<UsageRecord> GetRecent(int count = 50)
    {
        return _repo.GetAll().OrderByDescending(r => r.Timestamp).Take(count).ToList();
    }

    public Dictionary<DateTime, UsageSummary> GetDailyBreakdown(int days = 30)
    {
        var from = DateTime.UtcNow.Date.AddDays(-days + 1);
        var records = _repo.GetSince(from);
        return records
            .GroupBy(r => r.Timestamp.Date)
            .ToDictionary(g => g.Key, g => Aggregate(g));
    }

    private static UsageSummary Aggregate(IEnumerable<UsageRecord> records)
    {
        var summary = new UsageSummary();
        foreach (var r in records)
        {
            summary.TotalInputTokens += r.InputTokens;
            summary.TotalOutputTokens += r.OutputTokens;
            summary.TotalCacheCreationTokens += r.CacheCreationTokens;
            summary.TotalCacheReadTokens += r.CacheReadTokens;
            summary.TotalCostUsd += r.CostUsd;
            summary.RequestCount++;
        }
        return summary;
    }
}
