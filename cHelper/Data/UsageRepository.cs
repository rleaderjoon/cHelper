using System.Text.Json;
using cHelper.Models;

namespace cHelper.Data;

public class UsageRepository
{
    private static readonly string AppDataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "cHelper");
    private static readonly string UsagePath = Path.Combine(AppDataDir, "usage.json");
    private static readonly JsonSerializerOptions JsonOpts = new();

    private readonly List<UsageRecord> _records = new();
    private bool _loaded = false;

    public void Append(UsageRecord record)
    {
        EnsureLoaded();
        _records.Add(record);
        SaveAll();
    }

    public IReadOnlyList<UsageRecord> GetAll()
    {
        EnsureLoaded();
        return _records.AsReadOnly();
    }

    public IReadOnlyList<UsageRecord> GetSince(DateTime from)
    {
        EnsureLoaded();
        return _records.Where(r => r.Timestamp >= from).ToList().AsReadOnly();
    }

    private void EnsureLoaded()
    {
        if (_loaded) return;
        _loaded = true;
        Directory.CreateDirectory(AppDataDir);
        if (!File.Exists(UsagePath)) return;
        try
        {
            var json = File.ReadAllText(UsagePath);
            var list = JsonSerializer.Deserialize<List<UsageRecord>>(json, JsonOpts);
            if (list != null) _records.AddRange(list);
        }
        catch { }
    }

    private void SaveAll()
    {
        Directory.CreateDirectory(AppDataDir);
        File.WriteAllText(UsagePath, JsonSerializer.Serialize(_records, JsonOpts));
    }
}
