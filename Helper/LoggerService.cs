using Serilog;
using Serilog.Core;
using System.IO;

namespace MFAWPF.Helper;

public static class LoggerService
{
    // 统一配置
    private const string LogDirectory = "logs";
    private const string LogFilePattern = "log-.txt";
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5MB
    private const int MaxArchiveFiles = 2;
    
    private static readonly string LogFilePath = Path.Combine(
        LogDirectory, 
        $"log-{DateTime.Now:yyyy-MM-dd}.txt"
    );

    private static readonly ILogger Logger = new LoggerConfiguration()
        .WriteTo.File(
            LogFilePath,
            rollingInterval: RollingInterval.Day,
            fileSizeLimitBytes: MaxFileSizeBytes,
            retainedFileCountLimit: MaxArchiveFiles,
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}][{Level:u3}] {Message:lj}{NewLine}{Exception}")
        .CreateLogger();

    public static void LogInfo(string message)
    {
        Logger.Information(message);
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}][INFO] {message}");
    }
    
    public static void LogInfo(object message)
    {
        Logger.Information(message?.ToString() ?? string.Empty);
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}][INFO] {message}");
    }

    public static void LogError(object e)
    {
        Logger.Error(e?.ToString() ?? string.Empty);
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}][ERROR] {e}");
    }

    public static void LogError(string message)
    {
        Logger.Error(message);
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}][ERROR] {message}");
    }

    public static void LogWarning(string message)
    {
        Logger.Warning(message);
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}][WARN] {message}");
    }

    public static string GetLogDirectory() => LogDirectory;
    public static string GetLogFilePattern() => LogFilePattern;
    public static long GetMaxFileSizeBytes() => MaxFileSizeBytes;
    public static int GetMaxArchiveFiles() => MaxArchiveFiles;
}