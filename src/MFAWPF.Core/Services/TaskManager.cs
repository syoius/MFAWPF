using MFAWPF.Core.Services;

namespace MFAWPF.Core.Services;

public static class TaskManager
{
    public static void RunTask(
        Action action,
        string name = nameof(Action),
        string prompt = ">>> ",
        bool catchException = true)
    {
        NotificationService.Info($"{prompt}任务 {name} 开始.");

        if (catchException)
        {
            try
            {
                action.Invoke();
            }
            catch (Exception e)
            {
                NotificationService.Error($"{prompt}任务 {name} 失败.");
                LoggerService.LogError(e.ToString());
            }
        }
        else action();

        NotificationService.Info($"{prompt}任务 {name} 完成.");
    }

    public static async Task RunTaskAsync(
        Action action, Action? handleError = null,
        string name = nameof(Action),
        string prompt = ">>> ",
        bool catchException = true)
    {
        LoggerService.LogInfo($"异步任务 {name} 开始.");
        if (catchException)
        {
            var task = Task.Run(action);
            await task.ContinueWith(t =>
            {
                if (t.Exception != null)
                {
                    handleError?.Invoke();
                    LoggerService.LogError($"{prompt}异步任务 {name} 失败: {t.Exception.GetBaseException().Message}");
                }
            }, TaskContinuationOptions.OnlyOnFaulted);
        }
        else await Task.Run(action);

        LoggerService.LogInfo($"{prompt}异步任务 {name} 已完成.");
    }
}