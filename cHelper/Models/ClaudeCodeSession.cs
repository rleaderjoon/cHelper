namespace cHelper.Models;

public class ClaudeCodeSession
{
    public string SessionId { get; init; } = "";
    public string ProjectPath { get; init; } = "";
    public string ProjectName => Path.GetFileName(ProjectPath.TrimEnd('/', '\\'));
    public DateTime LastActive { get; init; }
    public string LastCommand { get; init; } = "";
}
