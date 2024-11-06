using System.IO;
using System.Timers;
using MFAWPF.Core.Services;

namespace MFAWPF.Core.Utils;

public static class LogCleaner
{
    private static readonly Timer CleanupTimer;
    private const int CheckIntervalHours = 3;
    private const long MaxSizeInBytes = 5 * 1024 * 1024;

    static LogCleaner()
    {
        CleanupTimer = new Timer(CheckIntervalHours * 60 * 60 * 1000);
        CleanupTimer.Elapsed += (_, _) => CleanupLargeDebugLogs();
        CleanupTimer.AutoReset = true;
        CleanupTimer.Start();
    }

    public static void CleanupLargeDebugLogs()
    {
        try
        {
            string debugPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug");
            if (!Directory.Exists(debugPath)) return;

            var logFiles = Directory.GetFiles(debugPath, "*.log");
            foreach (var logFile in logFiles)
            {
                ProcessLogFile(logFile, debugPath);
            }

            CleanupOldBackups(debugPath);
        }
        catch (Exception ex)
        {
            LoggerService.LogError($"清理日志文件时发生错误: {ex.Message}");
        }
    }

    private static void ProcessLogFile(string logFile, string debugPath)
    {
        try
        {
            var fileInfo = new FileInfo(logFile);
            if (fileInfo.Length <= MaxSizeInBytes) return;

            string backupName = Path.Combine(
                debugPath,
                $"old_{DateTime.Now:yyyyMMdd_HHmmss}_{Path.GetFileName(logFile)}");

            File.Move(logFile, backupName);
            LoggerService.LogInfo($"已备份大型日志文件: {logFile} -> {backupName}");
            File.Create(logFile).Dispose();
        }
        catch (IOException) { /* 文件正在使用，跳过 */ }
        catch (Exception ex)
        {
            LoggerService.LogError($"处理日志文件失败: {logFile}, 错误: {ex.Message}");
        }
    }

    private static void CleanupOldBackups(string debugPath)
    {
        var oldFiles = Directory.GetFiles(debugPath, "old_*.log")
                               .OrderByDescending(f => File.GetCreationTime(f))
                               .Skip(5);

        foreach (var oldFile in oldFiles)
        {
            try
            {
                File.Delete(oldFile);
                LoggerService.LogInfo($"已删除旧的备份日志文件: {oldFile}");
            }
            catch (Exception ex)
            {
                LoggerService.LogError($"删除旧备份文件失败: {oldFile}, 错误: {ex.Message}");
            }
        }
    }
}