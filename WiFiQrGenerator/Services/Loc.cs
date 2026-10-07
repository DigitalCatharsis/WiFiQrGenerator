namespace WiFiQrGenerator.Services;

/// <summary>
/// Very small runtime localization helper (Russian / English).
/// </summary>
public static class Loc
{
    public static bool IsEnglish { get; set; }

    private static readonly Dictionary<string, (string Ru, string En)> _map = new()
    {
        ["AppTitle"] = ("Генератор QR-кода Wi-Fi", "Wi-Fi QR Code Generator"),
        ["SettingsHeader"] = ("Параметры", "Settings"),
        ["SsidLabel"] = ("Имя сети (SSID):", "Network name (SSID):"),
        ["PasswordLabel"] = ("Пароль:", "Password:"),
        ["AuthLabel"] = ("Тип безопасности:", "Security type:"),
        ["AuthWpa"] = ("WPA/WPA2", "WPA/WPA2"),
        ["AuthWep"] = ("WEP", "WEP"),
        ["AuthNone"] = ("Без пароля (nopass)", "No password (nopass)"),
        ["SavePathLabel"] = ("Папка сохранения:", "Save folder:"),
        ["Browse"] = ("Обзор...", "Browse..."),
        ["SaveAsImage"] = ("Сохранить как PNG (иначе Word)", "Save as PNG (otherwise Word)"),
        ["OpenAfter"] = ("Открыть файл после создания", "Open file after generation"),
        ["EnglishLabel"] = ("English", "English"),
        ["LoadConfig"] = ("Загрузить конфиг", "Load config"),
        ["SaveConfig"] = ("Сохранить конфиг", "Save config"),
        ["Generate"] = ("Сгенерировать", "Generate"),

        ["LogStarted"] = ("Приложение запущено", "Application started"),
        ["LogExeDir"] = ("Каталог программы", "Program directory"),
        ["LogConfigFound"] = ("Найден файл конфигурации", "Configuration file found"),
        ["LogConfigNotFound"] = ("Файл конфигурации не найден, используются значения по умолчанию",
                                   "Configuration file not found, using defaults"),
        ["WarnNoWriteAccess"] = ("ВНИМАНИЕ: нет прав на запись в каталог программы. Укажите другую папку сохранения.",
                                   "WARNING: no write access to the program directory. Choose another save folder."),

        ["MsgSsidRequired"] = ("Укажите имя сети (SSID)", "Please enter the network name (SSID)"),
        ["MsgNoWriteAccess"] = ("Нет прав на запись в указанную папку", "No write access to the specified folder"),
        ["MsgCannotCreateDir"] = ("Не удалось создать папку", "Failed to create folder"),
        ["MsgError"] = ("Ошибка", "Error"),
        ["MsgSuccess"] = ("Готово", "Done"),
        ["MsgConfigLoaded"] = ("Конфигурация загружена", "Configuration loaded"),
        ["MsgConfigSaved"] = ("Конфигурация сохранена", "Configuration saved"),
        ["MsgOpenFailed"] = ("Не удалось открыть файл", "Failed to open file"),

        ["LogPayload"] = ("Данные QR", "QR payload"),
        ["LogQrGenerated"] = ("QR-код сгенерирован", "QR code generated"),
        ["LogSaved"] = ("Файл сохранён", "File saved"),
        ["LogOpened"] = ("Файл открыт", "File opened"),

        ["WordNetworkLabel"] = ("Имя сети", "Network name"),
        ["WordPasswordLabel"] = ("Пароль", "Password"),
    };

    /// <summary>
    /// Returns the localized string for the given key.
    /// </summary>
    public static string T(string key)
    {
        if (_map.TryGetValue(key, out var pair))
            return IsEnglish ? pair.En : pair.Ru;
        return key;
    }
}