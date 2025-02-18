using System.IO;
using Timer = System.Timers.Timer;

namespace MFAWPF.Utils;

public static class LogCleaner
{
    private static readonly Timer CleanupTimer;
    private const int CheckIntervalHours = 3;
    
    // 使用 LoggerService 的配置
    private static readonly string LogDirectory;
    private static readonly string LogFilePattern;
    private static readonly long MaxSizeInBytes;
    private static readonly int MaxArchiveFiles;
    private static readonly string ArchivePath;

    static LogCleaner()
    {
        // 从 LoggerService 获取配置
        LogDirectory = LoggerService.GetLogDirectory();
        LogFilePattern = LoggerService.GetLogFilePattern();
        MaxSizeInBytes = LoggerService.GetMaxFileSizeBytes();
        MaxArchiveFiles = LoggerService.GetMaxArchiveFiles();
        
        // 设置归档路径
        ArchivePath = Path.Combine(LogDirectory, "archive");
        
        // 确保目录存在
        Directory.CreateDirectory(LogDirectory);
        Directory.CreateDirectory(ArchivePath);

        // 初始化定时器
        CleanupTimer = new Timer(CheckIntervalHours * 60 * 60 * 1000);
        CleanupTimer.Elapsed += (_, _) => CleanupLargeDebugLogs();
        CleanupTimer.AutoReset = true;
        CleanupTimer.Start();
    }

    public static void CleanupLargeDebugLogs()
    {
        try
        {
            if (!Directory.Exists(LogDirectory) || !Directory.Exists(ArchivePath))
            {
                return;
            }

            // 处理当前日志文件
            var logFiles = Directory.GetFiles(LogDirectory, $"*{LogFilePattern}*")
                                  .Where(f => !Path.GetFileName(f).StartsWith("archive_"));

            foreach (var logFile in logFiles)
            {
                try
                {
                    var fileInfo = new FileInfo(logFile);
                    if (fileInfo.Length > MaxSizeInBytes)
                    {
                        string archiveName = Path.Combine(
                            ArchivePath,
                            $"archive_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}_{Path.GetFileName(logFile)}");

                        if (!IsFileLocked(logFile))
                        {
                            File.Copy(logFile, archiveName, true);
                            File.WriteAllText(logFile, string.Empty);
                            LoggerService.LogInfo($"已归档大型日志文件: {logFile} -> {archiveName}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    LoggerService.LogError($"处理日志文件失败: {logFile}, 错误: {ex.Message}");
                }
            }

            // 清理旧的归档文件
            var archiveFiles = Directory.GetFiles(ArchivePath)
                                      .OrderByDescending(f => File.GetCreationTime(f))
                                      .Skip(MaxArchiveFiles);

            foreach (var oldFile in archiveFiles)
            {
                try
                {
                    if (!IsFileLocked(oldFile))
                    {
                        File.Delete(oldFile);
                        LoggerService.LogInfo($"已删除旧的归档文件: {oldFile}");
                    }
                }
                catch (Exception ex)
                {
                    LoggerService.LogError($"删除归档文件失败: {oldFile}, 错误: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            LoggerService.LogError($"清理日志文件时发生错误: {ex.Message}");
        }
    }

    private static bool IsFileLocked(string filePath)
    {
        try
        {
            using (FileStream stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                return false;
            }
        }
        catch (IOException)
        {
            return true;
        }
    }
}