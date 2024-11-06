using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Localization;

namespace MFAWPF.Avalonia;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // 初始化语言服务
            var localizationManager = new LocalizationManager();
            // 加载语言资源
            localizationManager.LoadResources("avares://MFAWPF.Avalonia/Assets/i18n");
            LanguageService.Initialize(localizationManager);
            // 创建主窗口
            desktop.MainWindow = new MainWindow();
            // 初始化通知服务
            NotificationService.Initialize(new WindowNotificationManager(desktop.MainWindow));
        }
        base.OnFrameworkInitializationCompleted();
    }
}
