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
}
