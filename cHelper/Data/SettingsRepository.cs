using System.Text.Json;
using cHelper.Models;

namespace cHelper.Data;

public class SettingsRepository
{
    private static readonly string AppDataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "cHelper");
    private static readonly string ConfigPath = Path.Combine(AppDataDir, "config.json");

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public AppSettings Load()
    {
        EnsureDir();
        if (!File.Exists(ConfigPath)) return new AppSettings();
        try
        {
            var json = File.ReadAllText(ConfigPath);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOpts) ?? new AppSettings();
        }
        catch { return new AppSettings(); }
    }

    public void Save(AppSettings settings)
    {
        EnsureDir();
        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(settings, JsonOpts));
    }

    private static void EnsureDir() => Directory.CreateDirectory(AppDataDir);
}
