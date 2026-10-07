using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.Win32;
using WiFiQrGenerator.Models;
using WiFiQrGenerator.Services;

namespace WiFiQrGenerator;

public partial class MainWindow : Window
{
    private const string MadeBy = "made by Alexander Marchenko";

    private AppConfig _config;
    private readonly string _baseDirectory = AppContext.BaseDirectory;

    // Path used for auto-save on close. Defaults to the standard location
    // but is updated whenever the user loads or saves a config.
    private string _currentConfigPath;

    public MainWindow()
    {
        InitializeComponent();
        LogBox.Document.Blocks.Clear();

        _currentConfigPath = ConfigService.DefaultConfigPath;
        _config = ConfigService.Load(_currentConfigPath);
        if (!string.IsNullOrWhiteSpace(_config.LastConfigPath))
            _currentConfigPath = _config.LastConfigPath;

        ApplyConfigToUi(_config);
        ApplyLocalization();

        Loaded += MainWindow_Loaded;
    }

    // ------------------------------------------------------------------
    // Startup: restore window size/state AFTER the window is loaded.
    // ------------------------------------------------------------------

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            // Restore window size (in Loaded so WPF respects the values).
            if (_config.WindowWidth >= MinWidth) Width = _config.WindowWidth;
            if (_config.WindowHeight >= MinHeight) Height = _config.WindowHeight;

            if (string.Equals(_config.WindowState, "Maximized", StringComparison.OrdinalIgnoreCase))
                WindowState = WindowState.Maximized;

            LogInfo(Loc.T("LogStarted"));
            LogInfo($"{Loc.T("LogExeDir")}: {_baseDirectory}");

            if (!CanWriteToDirectory(_baseDirectory))
                LogWarning(Loc.T("WarnNoWriteAccess"));

            if (File.Exists(_currentConfigPath))
                LogInfo($"{Loc.T("LogConfigFound")}: {_currentConfigPath}");
            else
                LogInfo(Loc.T("LogConfigNotFound"));
        }
        catch (Exception ex)
        {
            LogError(ex.ToString());
        }
    }

    // ------------------------------------------------------------------
    // Localization
    // ------------------------------------------------------------------

    private void EnglishCheck_Changed(object sender, RoutedEventArgs e)
    {
        try { ApplyLocalization(); }
        catch (Exception ex) { LogError(ex.ToString()); }
    }

    private void ApplyLocalization()
    {
        Loc.IsEnglish = EnglishCheck.IsChecked == true;

        // Title includes the author tag (issue #3).
        Title = $"{Loc.T("AppTitle")} \u2014 {MadeBy}";

        SettingsGroup.Header = Loc.T("SettingsHeader");
        SsidLabel.Text = Loc.T("SsidLabel");
        PasswordLabel.Text = Loc.T("PasswordLabel");
        AuthLabel.Text = Loc.T("AuthLabel");
        SavePathLabel.Text = Loc.T("SavePathLabel");
        BrowseButton.Content = Loc.T("Browse");
        SaveAsImageCheck.Content = Loc.T("SaveAsImage");
        OpenAfterCheck.Content = Loc.T("OpenAfter");
        EnglishCheck.Content = Loc.T("EnglishLabel");
        LoadConfigButton.Content = Loc.T("LoadConfig");
        SaveConfigButton.Content = Loc.T("SaveConfig");
        GenerateButton.Content = Loc.T("Generate");

        if (AuthCombo.Items.Count >= 3)
        {
            ((ComboBoxItem)AuthCombo.Items[0]).Content = Loc.T("AuthWpa");
            ((ComboBoxItem)AuthCombo.Items[1]).Content = Loc.T("AuthWep");
            ((ComboBoxItem)AuthCombo.Items[2]).Content = Loc.T("AuthNone");
        }
    }

    // ------------------------------------------------------------------
    // Config <-> UI
    // ------------------------------------------------------------------

    private void ApplyConfigToUi(AppConfig cfg)
    {
        SsidBox.Text = cfg.Ssid;
        PasswordInput.Password = cfg.Password;

        SavePathBox.Text = string.IsNullOrWhiteSpace(cfg.SavePath)
            ? _baseDirectory
            : cfg.SavePath;

        SaveAsImageCheck.IsChecked = cfg.SaveAsImage;
        OpenAfterCheck.IsChecked = cfg.OpenAfterGeneration;
        EnglishCheck.IsChecked = cfg.UseEnglish;

        AuthCombo.SelectedIndex = 0;
        foreach (ComboBoxItem item in AuthCombo.Items)
        {
            if ((string?)item.Tag == cfg.AuthType)
            {
                AuthCombo.SelectedItem = item;
                break;
            }
        }
    }

    private AppConfig CollectConfig()
    {
        // When maximized, Width/Height return the maximized size.
        // Use RestoreBounds so we always store the "normal" size.
        double w = Width;
        double h = Height;

        if (WindowState != WindowState.Normal)
        {
            Rect rb = RestoreBounds;
            if (!double.IsNaN(rb.Width) && rb.Width >= MinWidth) w = rb.Width;
            if (!double.IsNaN(rb.Height) && rb.Height >= MinHeight) h = rb.Height;
        }

        return new AppConfig
        {
            WindowWidth = w,
            WindowHeight = h,
            WindowState = WindowState.ToString(),
            SavePath = SavePathBox.Text.Trim(),
            Ssid = SsidBox.Text.Trim(),
            Password = PasswordInput.Password,
            AuthType = (AuthCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "WPA",
            SaveAsImage = SaveAsImageCheck.IsChecked == true,
            OpenAfterGeneration = OpenAfterCheck.IsChecked == true,
            UseEnglish = EnglishCheck.IsChecked == true,
            LastConfigPath = _currentConfigPath
        };
    }

    // ------------------------------------------------------------------
    // Buttons
    // ------------------------------------------------------------------

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new OpenFolderDialog
            {
                Title = Loc.T("SavePathLabel"),
                InitialDirectory = Directory.Exists(SavePathBox.Text)
                    ? SavePathBox.Text
                    : _baseDirectory
            };

            if (dialog.ShowDialog(this) == true)
                SavePathBox.Text = dialog.FolderName;
        }
        catch (Exception ex) { LogError(ex.ToString()); }
    }

    /// <summary>
    /// Loads configuration from a file chosen by the user.
    /// </summary>
    private void LoadConfigButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dlg = new OpenFileDialog
            {
                Title = Loc.T("LoadConfig"),
                Filter = "JSON (*.json)|*.json|All files (*.*)|*.*",
                InitialDirectory = Directory.Exists(Path.GetDirectoryName(_currentConfigPath))
                    ? Path.GetDirectoryName(_currentConfigPath)!
                    : _baseDirectory,
                FileName = Path.GetFileName(_currentConfigPath)
            };

            if (dlg.ShowDialog(this) != true) return;

            _currentConfigPath = dlg.FileName;
            _config = ConfigService.Load(_currentConfigPath);
            ApplyConfigToUi(_config);

            // Re-apply window size/state from the freshly loaded config.
            if (_config.WindowWidth >= MinWidth) Width = _config.WindowWidth;
            if (_config.WindowHeight >= MinHeight) Height = _config.WindowHeight;

            WindowState = string.Equals(_config.WindowState, "Maximized",
                StringComparison.OrdinalIgnoreCase)
                ? WindowState.Maximized
                : WindowState.Normal;

            LogInfo($"{Loc.T("MsgConfigLoaded")}: {_currentConfigPath}");
        }
        catch (Exception ex)
        {
            LogError($"{Loc.T("MsgError")}: {ex.Message}");
            LogError(ex.ToString());
        }
    }

    /// <summary>
    /// Saves configuration to a file chosen by the user.
    /// </summary>
    private void SaveConfigButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dlg = new SaveFileDialog
            {
                Title = Loc.T("SaveConfig"),
                Filter = "JSON (*.json)|*.json|All files (*.*)|*.*",
                InitialDirectory = Directory.Exists(Path.GetDirectoryName(_currentConfigPath))
                    ? Path.GetDirectoryName(_currentConfigPath)!
                    : _baseDirectory,
                FileName = Path.GetFileName(_currentConfigPath),
                AddExtension = true,
                DefaultExt = ".json"
            };

            if (dlg.ShowDialog(this) != true) return;

            _currentConfigPath = dlg.FileName;
            _config = CollectConfig();
            ConfigService.Save(_config, _currentConfigPath);
            LogInfo($"{Loc.T("MsgConfigSaved")}: {_currentConfigPath}");
        }
        catch (Exception ex)
        {
            LogError($"{Loc.T("MsgError")}: {ex.Message}");
            LogError(ex.ToString());
        }
    }

    private void GenerateButton_Click(object sender, RoutedEventArgs e)
    {
        try { GenerateQr(); }
        catch (Exception ex)
        {
            LogError($"{Loc.T("MsgError")}: {ex.Message}");
            LogError(ex.ToString());
        }
    }

    // ------------------------------------------------------------------
    // Generation
    // ------------------------------------------------------------------

    private void GenerateQr()
    {
        string ssid = SsidBox.Text.Trim();
        string password = PasswordInput.Password;
        string auth = (AuthCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "WPA";
        string savePath = SavePathBox.Text.Trim();
        bool asImage = SaveAsImageCheck.IsChecked == true;
        bool openAfter = OpenAfterCheck.IsChecked == true;

        if (string.IsNullOrWhiteSpace(ssid))
        {
            LogWarning(Loc.T("MsgSsidRequired"));
            return;
        }

        if (string.IsNullOrWhiteSpace(savePath))
        {
            savePath = _baseDirectory;
            SavePathBox.Text = savePath;
        }

        if (!Directory.Exists(savePath))
        {
            try { Directory.CreateDirectory(savePath); }
            catch (Exception ex)
            {
                LogError($"{Loc.T("MsgCannotCreateDir")}: {ex.Message}");
                return;
            }
        }

        if (!CanWriteToDirectory(savePath))
        {
            LogError(Loc.T("MsgNoWriteAccess"));
            return;
        }

        string payload = QrService.BuildWifiPayload(ssid, password, auth);
        LogInfo($"{Loc.T("LogPayload")}: {payload}");

        // Date format is intentionally obvious: 2026-10-06_14-32-05.
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string baseName = $"WiFi_QR_{timestamp}";
        string outputPath;

        if (asImage)
        {
            // Compose QR + labels into a single PNG.
            byte[] png = QrService.GeneratePngWithText(
                payload,
                ssid,
                password,
                Loc.T("WordNetworkLabel"),
                Loc.T("WordPasswordLabel"));

            outputPath = Path.Combine(savePath, baseName + ".png");
            File.WriteAllBytes(outputPath, png);
        }
        else
        {
            byte[] png = QrService.GeneratePng(payload);
            outputPath = Path.Combine(savePath, baseName + ".docx");

            WordService.Create(
                outputPath,
                png,
                ssid,
                password,
                Loc.T("WordNetworkLabel"),
                Loc.T("WordPasswordLabel"));
        }

        LogInfo(Loc.T("LogQrGenerated"));
        LogInfo($"{Loc.T("LogSaved")}: {outputPath}");

        if (openAfter)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = outputPath,
                    UseShellExecute = true
                });
                LogInfo(Loc.T("LogOpened"));
            }
            catch (Exception ex)
            {
                LogWarning($"{Loc.T("MsgOpenFailed")}: {ex.Message}");
            }
        }

        LogInfo(Loc.T("MsgSuccess"));
    }

    // ------------------------------------------------------------------
    // Logging
    // ------------------------------------------------------------------

    private void LogInfo(string message) => AppendLog(message, Colors.LimeGreen);
    private void LogWarning(string message) => AppendLog(message, Colors.Yellow);
    private void LogError(string message) => AppendLog(message, Colors.Red);

    private void AppendLog(string message, Color color)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => AppendLog(message, color));
            return;
        }

        try
        {
            var run = new Run($"[{DateTime.Now:HH:mm:ss}] {message}")
            {
                Foreground = new SolidColorBrush(color)
            };
            var paragraph = new Paragraph(run) { Margin = new Thickness(0) };

            LogBox.Document.Blocks.Add(paragraph);
            LogBox.ScrollToEnd();
        }
        catch
        {
            // Never let logging itself throw.
        }
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    /// <summary>
    /// Tests whether the current process can create and delete a file in the given directory.
    /// </summary>
    private static bool CanWriteToDirectory(string directory)
    {
        try
        {
            string testFile = Path.Combine(directory, $"wifiqr_write_test_{Guid.NewGuid():N}.tmp");
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
            return true;
        }
        catch { return false; }
    }

    /// <summary>
    /// Auto-saves the configuration on close (silently, so we never block shutdown).
    /// Saves to the last used config path.
    /// </summary>
    protected override void OnClosing(CancelEventArgs e)
    {
        try
        {
            _config = CollectConfig();
            ConfigService.Save(_config, _currentConfigPath);
        }
        catch
        {
            // Ignore on shutdown.
        }
        base.OnClosing(e);
    }
}