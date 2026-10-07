using System.IO;
using System.Text.Json;
using WiFiQrGenerator.Models;

namespace WiFiQrGenerator.Services;

/// <summary>
/// Loads and saves the application configuration as JSON.
/// </summary>
public static class ConfigService
{
    private const string ConfigFileName = "config.json";

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// Default config path: next to the executable.
    /// </summary>
    public static string DefaultConfigPath =>
        Path.Combine(AppContext.BaseDirectory, ConfigFileName);

    /// <summary>
    /// Loads the configuration from the given path.
    /// Returns defaults if the file is missing or invalid.
    /// </summary>
    public static AppConfig Load(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    var cfg = JsonSerializer.Deserialize<AppConfig>(json, _jsonOptions);
                    if (cfg != null)
                    {
                        cfg.LastConfigPath = path;
                        return cfg;
                    }
                }
            }
        }
        catch
        {
            // Fall through to defaults.
        }

        return new AppConfig { LastConfigPath = path };
    }

    /// <summary>
    /// Serializes and writes the configuration to the given path.
    /// </summary>
    public static void Save(AppConfig config, string path)
    {
        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        string json = JsonSerializer.Serialize(config, _jsonOptions);
        File.WriteAllText(path, json);
    }
}