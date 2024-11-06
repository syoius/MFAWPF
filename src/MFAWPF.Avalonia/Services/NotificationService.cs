using Avalonia.Threading;
using Avalonia.Notifications;

namespace MFAWPF.Avalonia.Services;

public static class NotificationService
{
    private static INotificationManager? _notificationManager;

    public static void Initialize(INotificationManager notificationManager)
    {
        _notificationManager = notificationManager;
    }

    public static void Warning(string message, string title = "警告")
    {
        ShowNotification(title, message, NotificationType.Warning);
    }

    public static void Error(string message, string title = "错误")
    {
        ShowNotification(title, message, NotificationType.Error);
    }

    public static void Info(string message, string title = "信息")
    {
        ShowNotification(title, message, NotificationType.Information);
    }

    private static void ShowNotification(string title, string message, NotificationType type)
    {
        if (_notificationManager == null) return;

        Process(() =>
        {
            _notificationManager.Show(new Notification(title, message, type));
        });
    }

    private static void Process(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess())
            action();
        else
            Dispatcher.UIThread.InvokeAsync(action);
    }
}
