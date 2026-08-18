using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace MechrevoCustomOSD;

internal enum LanguageMode
{
    Auto,
    Chinese,
    English
}

internal static class AppDataPaths
{
    internal static readonly string DirectoryPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MechrevoCustomOSD");

    internal static readonly string SettingsPath = Path.Combine(DirectoryPath, "settings.json");
    internal static readonly string LogPath = Path.Combine(DirectoryPath, "MechrevoCustomOSD.log");
    internal static readonly string PreviousLogPath = Path.Combine(DirectoryPath, "MechrevoCustomOSD.previous.log");

    internal static void EnsureDirectory()
    {
        Directory.CreateDirectory(DirectoryPath);
    }
}

internal static class Localization
{
    private const int MaximumSettingsBytes = 4096;
    private static LanguageMode _mode = LanguageMode.Auto;
    private static bool _initialized;

    [DllImport("kernel32.dll")]
    private static extern ushort GetUserDefaultUILanguage();

    internal static LanguageMode Mode => _mode;

    internal static LocalizedStrings Text => IsChineseActive() ? Chinese : English;

    internal static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        try
        {
            AppDataPaths.EnsureDirectory();
            FileInfo settingsFile = new(AppDataPaths.SettingsPath);
            if (!settingsFile.Exists || settingsFile.Length > MaximumSettingsBytes) return;

            string json = File.ReadAllText(AppDataPaths.SettingsPath, Encoding.UTF8);
            LanguageSettings? settings = JsonSerializer.Deserialize<LanguageSettings>(json);
            _mode = ParseMode(settings?.Language);
        }
        catch
        {
            _mode = LanguageMode.Auto;
        }
    }

    internal static void SetMode(LanguageMode mode)
    {
        _mode = mode;
        try
        {
            AppDataPaths.EnsureDirectory();
            LanguageSettings settings = new() { Language = ModeName(mode) };
            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(AppDataPaths.SettingsPath, json, new UTF8Encoding(false));
        }
        catch (Exception exception)
        {
            EventLogger.Write("Unable to save language preference: " + exception.Message);
        }
    }

    private static bool IsChineseActive()
    {
        if (_mode == LanguageMode.Chinese) return true;
        if (_mode == LanguageMode.English) return false;

        try
        {
            CultureInfo culture = CultureInfo.GetCultureInfo(GetUserDefaultUILanguage());
            return string.Equals(culture.TwoLetterISOLanguageName, "zh", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return string.Equals(
                CultureInfo.CurrentUICulture.TwoLetterISOLanguageName,
                "zh",
                StringComparison.OrdinalIgnoreCase);
        }
    }

    private static LanguageMode ParseMode(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "zh" => LanguageMode.Chinese,
            "en" => LanguageMode.English,
            _ => LanguageMode.Auto
        };
    }

    private static string ModeName(LanguageMode mode)
    {
        return mode switch
        {
            LanguageMode.Chinese => "zh",
            LanguageMode.English => "en",
            _ => "auto"
        };
    }

    private sealed class LanguageSettings
    {
        public string Language { get; set; } = "auto";
    }

    private static readonly LocalizedStrings Chinese = new()
    {
        DemoTitle = "机械革命 OSD",
        DemoDetail = "3D Fluent 亚克力",
        Airplane = "飞行模式",
        KeyboardBacklight = "键盘背光",
        Touchpad = "触控板",
        FnLock = "Fn Lock",
        AmbientLight = "环境光感应",
        PerformanceMode = "性能模式",
        RefreshRate = "屏幕刷新率",
        WindowsKeyLock = "Windows 键锁定",
        Enabled = "已开启",
        Disabled = "已关闭",
        Low = "低",
        Medium = "中",
        High = "高",
        Automatic = "自动",
        HighPerformance = "高性能",
        Balanced = "均衡",
        Quiet = "静音",
        UnknownModeFormat = "未知模式 {0}",
        LevelFormat = "等级 {0}",
        ModeFormat = "模式 {0}",
        TrayTooltip = "机械革命 OSD",
        TrayLanguage = "语言",
        TrayFollowSystem = "自动（跟随系统）",
        TrayChinese = "简体中文",
        TrayEnglish = "English",
        TrayPreview = "显示测试",
        TrayExit = "退出"
    };

    private static readonly LocalizedStrings English = new()
    {
        DemoTitle = "MECHREVO OSD",
        DemoDetail = "3D Fluent Acrylic",
        Airplane = "Airplane mode",
        KeyboardBacklight = "Keyboard backlight",
        Touchpad = "Touchpad",
        FnLock = "Fn Lock",
        AmbientLight = "Ambient light",
        PerformanceMode = "Performance mode",
        RefreshRate = "Refresh rate",
        WindowsKeyLock = "Windows key lock",
        Enabled = "On",
        Disabled = "Off",
        Low = "Low",
        Medium = "Medium",
        High = "High",
        Automatic = "Automatic",
        HighPerformance = "Performance",
        Balanced = "Balanced",
        Quiet = "Quiet",
        UnknownModeFormat = "Unknown mode {0}",
        LevelFormat = "Level {0}",
        ModeFormat = "Mode {0}",
        TrayTooltip = "MECHREVO OSD",
        TrayLanguage = "Language",
        TrayFollowSystem = "Automatic (system)",
        TrayChinese = "简体中文",
        TrayEnglish = "English",
        TrayPreview = "Show preview",
        TrayExit = "Exit"
    };
}

internal sealed class LocalizedStrings
{
    internal required string DemoTitle { get; init; }
    internal required string DemoDetail { get; init; }
    internal required string Airplane { get; init; }
    internal required string KeyboardBacklight { get; init; }
    internal required string Touchpad { get; init; }
    internal required string FnLock { get; init; }
    internal required string AmbientLight { get; init; }
    internal required string PerformanceMode { get; init; }
    internal required string RefreshRate { get; init; }
    internal required string WindowsKeyLock { get; init; }
    internal required string Enabled { get; init; }
    internal required string Disabled { get; init; }
    internal required string Low { get; init; }
    internal required string Medium { get; init; }
    internal required string High { get; init; }
    internal required string Automatic { get; init; }
    internal required string HighPerformance { get; init; }
    internal required string Balanced { get; init; }
    internal required string Quiet { get; init; }
    internal required string UnknownModeFormat { get; init; }
    internal required string LevelFormat { get; init; }
    internal required string ModeFormat { get; init; }
    internal required string TrayTooltip { get; init; }
    internal required string TrayLanguage { get; init; }
    internal required string TrayFollowSystem { get; init; }
    internal required string TrayChinese { get; init; }
    internal required string TrayEnglish { get; init; }
    internal required string TrayPreview { get; init; }
    internal required string TrayExit { get; init; }
}
