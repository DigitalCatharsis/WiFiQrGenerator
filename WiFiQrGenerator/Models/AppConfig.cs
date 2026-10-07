namespace WiFiQrGenerator.Models;

/// <summary>
/// Application configuration persisted to JSON.
/// </summary>
public class AppConfig
{
    public double WindowWidth { get; set; } = 720;
    public double WindowHeight { get; set; } = 650;

    // "Normal" | "Maximized" | "Minimized"
    public string WindowState { get; set; } = "Normal";

    public string SavePath { get; set; } = "";
    public string Ssid { get; set; } = "";
    public string Password { get; set; } = "";
    public string AuthType { get; set; } = "WPA";

    public bool SaveAsImage { get; set; } = false;
    public bool OpenAfterGeneration { get; set; } = true;
    public bool UseEnglish { get; set; } = false;

    // Path of the last loaded/saved config file (used for auto-save on close).
    public string LastConfigPath { get; set; } = "";
}