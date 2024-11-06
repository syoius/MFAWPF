using System.Globalization;
using Avalonia.Localization;

namespace MFAWPF.Core.Services;

public static class LanguageService
{
    private static LocalizationManager? _localizationManager;
    public static event EventHandler? LanguageChanged;

    public static void Initialize(LocalizationManager localizationManager)
    {
        _localizationManager = localizationManager;
    }

    public static void ChangeLanguage(CultureInfo newCulture)
    {
        if (_localizationManager == null) return;

        // 设置应用程序的文化
        Thread.CurrentThread.CurrentCulture = newCulture;
        Thread.CurrentThread.CurrentUICulture = newCulture;
        _localizationManager.CurrentCulture = newCulture;

        // 触发语言变化事件
        LanguageChanged?.Invoke(null, EventArgs.Empty);
    }

    public static string GetString(string key)
    {
        return _localizationManager?.GetString(key) ?? key;
    }
} 