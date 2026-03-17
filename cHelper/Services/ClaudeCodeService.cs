using cHelper.Data;
using cHelper.Models;
using cHelper.Utils;

namespace cHelper.Services;

public class ClaudeCodeService
{
    private readonly ClaudeCodeReader _reader;

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
}
