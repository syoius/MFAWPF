using System.IO;
using Timer = System.Timers.Timer;

namespace MFAWPF.Helper;

public static class LogCleaner
{
    private static readonly Timer CleanupTimer;
    private const int CheckIntervalHours = 3;
    
    // 添加 debug 日志配置
    private static readonly string[] LogDirectories;
    private static readonly Dictionary<string, string> LogFilePatterns;
    private static readonly long MaxSizeInBytes;
    private static readonly int MaxArchiveFiles;
    private static readonly Dictionary<string, string> ArchivePaths;

    static LogCleaner()
    {
        // 配置多个日志目录
        LogDirectories = new[] { 
            LoggerService.GetLogDirectory(),  // logs 目录
            "debug"                          // debug 目录
        };
        
        LogFilePatterns = new Dictionary<string, string> {
            { LoggerService.GetLogDirectory(), LoggerService.GetLogFilePattern() },
            { "debug", "*.log" }
        };
        
        MaxSizeInBytes = LoggerService.GetMaxFileSizeBytes();
        MaxArchiveFiles = LoggerService.GetMaxArchiveFiles();
        
        // 为每个目录设置归档路径
        ArchivePaths = new Dictionary<string, string>();
        foreach (var dir in LogDirectories)
        {
            ArchivePaths[dir] = Path.Combine(dir, "archive");
            Directory.CreateDirectory(dir);
            Directory.CreateDirectory(ArchivePaths[dir]);
        }

        // 初始化定时器
        CleanupTimer = new Timer(CheckIntervalHours * 60 * 60 * 1000);
        CleanupTimer.Elapsed += (_, _) => CleanupLargeDebugLogs();
        CleanupTimer.AutoReset = true;
        CleanupTimer.Start();
    }

    public static void CleanupLargeDebugLogs()
    {
        foreach (var logDir in LogDirectories)
        {
            try
            {
                if (!Directory.Exists(logDir) || !Directory.Exists(ArchivePaths[logDir]))
                {
                    continue;
                }

                var pattern = LogFilePatterns[logDir];
                var archivePath = ArchivePaths[logDir];

                // 处理当前日志文件
                var logFiles = Directory.GetFiles(logDir, pattern)
                                      .Where(f => !Path.GetFileName(f).StartsWith("archive_"));

                foreach (var logFile in logFiles)
                {
                    ProcessLogFile(logFile, archivePath);
                }

                // 清理旧的归档文件
                CleanupOldArchives(archivePath);
            }
            catch (Exception ex)
            {
                LoggerService.LogError($"清理日志目录 {logDir} 时发生错误: {ex.Message}");
            }
        }
    }

    private static void ProcessLogFile(string logFile, string archivePath)
    {
        try
        {
            var fileInfo = new FileInfo(logFile);
            if (fileInfo.Length > MaxSizeInBytes)
            {
                string archiveName = Path.Combine(
                    archivePath,
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

    private static void CleanupOldArchives(string archivePath)
    {
        var archiveFiles = Directory.GetFiles(archivePath)
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