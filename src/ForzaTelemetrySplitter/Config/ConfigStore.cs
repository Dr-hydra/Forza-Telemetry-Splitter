using System.Text.Json;

namespace ForzaTelemetrySplitter.Config;

/// <summary>
/// Loads and saves <see cref="AppConfig"/> as JSON under
/// %APPDATA%\ForzaTelemetrySplitter\config.json. On first run (or if the file is missing or
/// corrupt) returns sensible defaults so the app is usable immediately.
/// </summary>
public static class ConfigStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    public static string ConfigDirectory =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ForzaTelemetrySplitter");

    public static string ConfigPath => Path.Combine(ConfigDirectory, "config.json");

    public static AppConfig Load()
    {
        try
        {
            if (!File.Exists(ConfigPath))
            {
                var fresh = AppConfig.CreateDefault();
                Save(fresh);
                return fresh;
            }

            var json = File.ReadAllText(ConfigPath);
            var cfg = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions);
            return cfg ?? AppConfig.CreateDefault();
        }
        catch
        {
            // A malformed config should never block launch — fall back to defaults.
            return AppConfig.CreateDefault();
        }
    }

    public static void Save(AppConfig config)
    {
        try
        {
            Directory.CreateDirectory(ConfigDirectory);
            var json = JsonSerializer.Serialize(config, JsonOptions);
            File.WriteAllText(ConfigPath, json);
        }
        catch
        {
            // Saving is best-effort; a transient write failure shouldn't crash the app.
        }
    }
}
