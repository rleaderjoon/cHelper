using System.Text.Json;
using System.Text.Json.Nodes;
using cHelper.Models;

namespace cHelper.Data;

public class ClaudeCodeReader
{
    private readonly string _claudeDir;

    public ClaudeCodeReader(string claudeDir)
    {
        _claudeDir = claudeDir;
    }

    public List<ClaudeCodeSession> ReadRecentSessions(int maxSessions = 50)
    {
        var sessions = new List<ClaudeCodeSession>();
        var historyFile = Path.Combine(_claudeDir, "history.jsonl");
        if (!File.Exists(historyFile)) return sessions;

        try
        {
            var lines = File.ReadAllLines(historyFile);
            var seen = new HashSet<string>();

            foreach (var line in lines.Reverse())
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                try
                {
                    var node = JsonNode.Parse(line);
                    if (node == null) continue;

                    var sessionId = node["sessionId"]?.GetValue<string>() ?? "";
                    var project = node["project"]?.GetValue<string>() ?? "";
                    var display = node["display"]?.GetValue<string>() ?? "";
                    var ts = node["timestamp"]?.GetValue<long>() ?? 0;

                    if (!seen.Add(sessionId)) continue;

                    var lastActive = ts > 0
                        ? DateTimeOffset.FromUnixTimeMilliseconds(ts).LocalDateTime
                        : DateTime.MinValue;

                    sessions.Add(new ClaudeCodeSession
                    {
                        SessionId = sessionId,
                        ProjectPath = project,
                        LastActive = lastActive,
                        LastCommand = display
                    });

                    if (sessions.Count >= maxSessions) break;
                }
                catch { }
            }
        }
        catch { }

        return sessions;
    }

    public Dictionary<string, (int SessionCount, DateTime LastActive)> GetProjectStats()
    {
        var stats = new Dictionary<string, (int, DateTime)>(StringComparer.OrdinalIgnoreCase);
        var sessions = ReadRecentSessions(1000);

        foreach (var session in sessions)
        {
            if (string.IsNullOrEmpty(session.ProjectPath)) continue;
            if (stats.TryGetValue(session.ProjectPath, out var existing))
            {
                stats[session.ProjectPath] = (
                    existing.Item1 + 1,
                    session.LastActive > existing.Item2 ? session.LastActive : existing.Item2
                );
            }
            else
            {
                stats[session.ProjectPath] = (1, session.LastActive);
            }
        }

        return stats;
    }

    // ── Project JSONL usage parsing ────────────────────────────────────────────

    public List<ClaudeCodeUsageRecord> ReadUsageRecords(int daysBack = 90)
    {
        var records = new List<ClaudeCodeUsageRecord>();
        var projectsDir = Path.Combine(_claudeDir, "projects");
        if (!Directory.Exists(projectsDir)) return records;

        var cutoff = DateTime.UtcNow.AddDays(-daysBack);

        foreach (var projectDir in Directory.GetDirectories(projectsDir))
        {
            foreach (var jsonlFile in Directory.GetFiles(projectDir, "*.jsonl"))
            {
                if (new FileInfo(jsonlFile).LastWriteTimeUtc < cutoff) continue;
                ParseUsageFromFile(jsonlFile, records, cutoff);
            }
        }

        return records;
    }

    private static void ParseUsageFromFile(
        string filePath, List<ClaudeCodeUsageRecord> records, DateTime cutoff)
    {
        try
        {
            foreach (var line in File.ReadLines(filePath))
            {
                if (string.IsNullOrWhiteSpace(line) || !line.Contains("output_tokens")) continue;
                try
                {
                    var node = JsonNode.Parse(line);
                    if (node == null) continue;
                    if (node["type"]?.GetValue<string>() != "assistant") continue;
                    if (node["error"]?.GetValue<string>() == "rate_limit") continue;

                    var tsStr = node["timestamp"]?.GetValue<string>();
                    if (!DateTime.TryParse(tsStr, null,
                            System.Globalization.DateTimeStyles.RoundtripKind, out var ts)) continue;
                    if (ts.Kind == DateTimeKind.Unspecified)
                        ts = DateTime.SpecifyKind(ts, DateTimeKind.Utc);
                    if (ts < cutoff) continue;

                    var usage = node["message"]?["usage"];
                    if (usage == null) continue;

                    var outputTokens = usage["output_tokens"]?.GetValue<long>() ?? 0;
                    if (outputTokens == 0) continue; // streaming partial

                    records.Add(new ClaudeCodeUsageRecord
                    {
                        Timestamp = ts,
                        Model = node["message"]?["model"]?.GetValue<string>() ?? "",
                        SessionId = node["sessionId"]?.GetValue<string>() ?? "",
                        ProjectPath = node["cwd"]?.GetValue<string>() ?? "",
                        InputTokens = usage["input_tokens"]?.GetValue<long>() ?? 0,
                        OutputTokens = outputTokens,
                        CacheCreationTokens = usage["cache_creation_input_tokens"]?.GetValue<long>() ?? 0,
                        CacheReadTokens = usage["cache_read_input_tokens"]?.GetValue<long>() ?? 0,
                    });
                }
                catch { }
            }
        }
        catch { }
    }

    // ── Rate limit detection ───────────────────────────────────────────────────

    public ClaudeCodeRateLimitInfo GetRateLimitInfo()
    {
        var projectsDir = Path.Combine(_claudeDir, "projects");
        if (!Directory.Exists(projectsDir)) return new ClaudeCodeRateLimitInfo();

        ClaudeCodeRateLimitInfo? latest = null;
        var lookback = DateTime.UtcNow.AddDays(-7);

        foreach (var projectDir in Directory.GetDirectories(projectsDir))
        {
            var recentFiles = Directory.GetFiles(projectDir, "*.jsonl")
                .Where(f => new FileInfo(f).LastWriteTimeUtc > lookback)
                .OrderByDescending(f => new FileInfo(f).LastWriteTimeUtc)
                .Take(10);

            foreach (var jsonlFile in recentFiles)
            {
                var info = FindRateLimitInFile(jsonlFile);
                if (info != null && (latest == null || info.RateLimitedAt > latest.RateLimitedAt))
                    latest = info;
            }
        }

        return latest ?? new ClaudeCodeRateLimitInfo();
    }

    private static ClaudeCodeRateLimitInfo? FindRateLimitInFile(string filePath)
    {
        ClaudeCodeRateLimitInfo? latest = null;
        try
        {
            foreach (var line in File.ReadLines(filePath))
            {
                if (!line.Contains("rate_limit")) continue;
                try
                {
                    var node = JsonNode.Parse(line);
                    if (node == null) continue;
                    if (node["error"]?.GetValue<string>() != "rate_limit") continue;

                    var tsStr = node["timestamp"]?.GetValue<string>();
                    if (!DateTime.TryParse(tsStr, null,
                            System.Globalization.DateTimeStyles.RoundtripKind, out var ts)) continue;
                    if (ts.Kind == DateTimeKind.Unspecified)
                        ts = DateTime.SpecifyKind(ts, DateTimeKind.Utc);

                    var content = node["message"]?["content"] as JsonArray;
                    string resetText = content != null && content.Count > 0
                        ? content[0]?["text"]?.GetValue<string>() ?? ""
                        : "";

                    if (latest == null || ts > latest.RateLimitedAt)
                    {
                        latest = new ClaudeCodeRateLimitInfo
                        {
                            IsRateLimited = true,
                            RateLimitedAt = ts,
                            ResetText = resetText,
                        };
                    }
                }
                catch { }
            }
        }
        catch { }
        return latest;
    }
}
