using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Localization;
using Avalonia.Threading;
using MFAWPF.Core.Services;
using System.Threading.Tasks;

namespace MFAWPF.Avalonia;

public partial class App : Application
{
    public App()
    {
        // 注册全局异常处理
        SetupExceptionHandling();
        
        // 注册退出事件
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Exit += OnExit;
        }
    }

    private void SetupExceptionHandling()
    {
        // UI线程未捕获异常
        Dispatcher.UIThread.UnhandledException += (sender, args) =>
        {
            args.Handled = true;
            LoggerService.LogError(args.Exception);
            ErrorWindow.ShowException(args.Exception);
        };

        // Task线程异常
        TaskScheduler.UnobservedTaskException += (sender, args) =>
        {
            args.SetObserved();
            LoggerService.LogError(args.Exception);
            foreach (var ex in args.Exception.InnerExceptions)
            {
                var errorMessage = $"异常类型：{ex.GetType()}\n来自：{ex.Source}\n异常内容：{ex.Message}";
                LoggerService.LogError(errorMessage);
            }
            ErrorWindow.ShowException(args.Exception);
        };

        // 非UI线程异常
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            if (args.ExceptionObject is Exception ex)
            {
                var message = args.IsTerminating ? "非UI线程发生致命错误：" : "非UI线程异常：";
                LoggerService.LogError($"{message}{ex}");
                ErrorWindow.ShowException(ex, args.IsTerminating);
            }
        };
    }

    private void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        // 程序退出时需要处理的业务
        LoggerService.LogInfo("Application exiting...");
    }

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
            localizationManager.LoadResources("avares://MFAWPF.Avalonia/Assets/i18n");
            LanguageService.Initialize(localizationManager);

            // 清理大型日志文件
            LogCleaner.CleanupLargeDebugLogs();

            // 创建主窗口
            desktop.MainWindow = new MainWindow();

            // 初始化通知服务
            NotificationService.Initialize(new WindowNotificationManager(desktop.MainWindow));
        }

        base.OnFrameworkInitializationCompleted();
    }
}
