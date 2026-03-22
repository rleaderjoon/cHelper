using cHelper.Data;
using cHelper.Models;
using cHelper.Utils;

namespace cHelper.Services;

public class ClaudeCodeService
{
    private readonly ClaudeCodeReader _reader;

    // Usage cache (2-minute TTL)
    private List<ClaudeCodeUsageRecord>? _usageCache;
    private DateTime _usageCacheTime = DateTime.MinValue;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(2);

    public ClaudeCodeService(ClaudeCodeReader reader)
    {
        _reader = reader;
    }

    public string? GetVersion() => ProcessHelper.GetClaudeVersion();

    public List<ClaudeCodeSession> GetRecentSessions(int max = 50)
        => _reader.ReadRecentSessions(max);

    public List<(string ProjectPath, string ProjectName, int SessionCount, DateTime LastActive)> GetProjectSummary()
    {
        var stats = _reader.GetProjectStats();
        return stats
            .Select(kv => (
                ProjectPath: kv.Key,
                ProjectName: Path.GetFileName(kv.Key.TrimEnd('/', '\\')),
                SessionCount: kv.Value.SessionCount,
                LastActive: kv.Value.LastActive
            ))
            .OrderByDescending(p => p.LastActive)
            .ToList();
    }

    public void OpenInClaudeCode(string projectPath)
    {
        ProcessHelper.OpenClaudeCode(projectPath);
    }

    // ── Usage aggregation ──────────────────────────────────────────────────────

    public void InvalidateUsageCache() => _usageCache = null;

    private List<ClaudeCodeUsageRecord> GetRecords()
    {
        if (_usageCache == null || DateTime.UtcNow - _usageCacheTime > CacheTtl)
        {
            _usageCache = _reader.ReadUsageRecords(90);
            _usageCacheTime = DateTime.UtcNow;
        }
        return _usageCache;
    }

    public (long Input, long Output, long Cache, long Total) GetUsageToday()
    {
        var todayLocal = DateTime.Today;
        return Aggregate(GetRecords().Where(r => r.Timestamp.ToLocalTime().Date == todayLocal));
    }

    public (long Input, long Output, long Cache, long Total) GetUsageThisMonth()
    {
        var monthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        return Aggregate(GetRecords().Where(r => r.Timestamp.ToLocalTime().Date >= monthStart));
    }

    public Dictionary<DateTime, long> GetDailyTokens(int days = 119) // 17 weeks
    {
        var fromLocal = DateTime.Today.AddDays(-days + 1);
        return GetRecords()
            .Where(r => r.Timestamp.ToLocalTime().Date >= fromLocal)
            .GroupBy(r => r.Timestamp.ToLocalTime().Date)
            .ToDictionary(g => g.Key, g => g.Sum(r => r.TotalTokens));
    }

    public ClaudeCodeRateLimitInfo GetRateLimitInfo() => _reader.GetRateLimitInfo();

    public Dictionary<int, long> GetHourlyTokensToday()
    {
        var todayLocal = DateTime.Today;
        return GetRecords()
            .Where(r => r.Timestamp.ToLocalTime().Date == todayLocal)
            .GroupBy(r => r.Timestamp.ToLocalTime().Hour)
            .ToDictionary(g => g.Key, g => g.Sum(r => r.TotalTokens));
    }

    public double GetCacheHitRate(int days = 30)
    {
        var fromLocal = DateTime.Today.AddDays(-days + 1);
        var records = GetRecords()
            .Where(r => r.Timestamp.ToLocalTime().Date >= fromLocal)
            .ToList();
        long potential = records.Sum(r => r.InputTokens + r.CacheCreationTokens + r.CacheReadTokens);
        long cacheRead = records.Sum(r => r.CacheReadTokens);
        return potential == 0 ? 0 : (double)cacheRead / potential;
    }

    public int GetStreak()
    {
        var daily = GetDailyTokens(90);
        int streak = 0;
        var day = DateTime.Today;
        if (!daily.ContainsKey(day) || daily[day] == 0)
            day = day.AddDays(-1);
        while (daily.ContainsKey(day) && daily[day] > 0)
        {
            streak++;
            day = day.AddDays(-1);
        }
        return streak;
    }

    public List<(string Name, long Tokens)> GetProjectRanking(int topN = 5)
    {
        return GetRecords()
            .Where(r => !string.IsNullOrEmpty(r.ProjectPath))
            .GroupBy(r => r.ProjectPath)
            .Select(g => (
                Name: Path.GetFileName(g.Key.TrimEnd('/', '\\')),
                Tokens: g.Sum(r => r.TotalTokens)
            ))
            .OrderByDescending(x => x.Tokens)
            .Take(topN)
            .ToList();
    }

    public long GetDailyAverage(int days = 30)
    {
        var daily = GetDailyTokens(days);
        if (daily.Count == 0) return 0;
        return (long)daily.Values.Average();
    }

    private static (long Input, long Output, long Cache, long Total) Aggregate(
        IEnumerable<ClaudeCodeUsageRecord> records)
    {
        long input = 0, output = 0, cache = 0;
        foreach (var r in records)
        {
            input += r.InputTokens;
            output += r.OutputTokens;
            cache += r.CacheCreationTokens + r.CacheReadTokens;
        }
        return (input, output, cache, input + output + cache);
    }
}
