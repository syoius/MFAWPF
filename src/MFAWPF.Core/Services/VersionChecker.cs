using System.Text.RegularExpressions;
using System.Net.Http;
using System.Reflection;
using Newtonsoft.Json.Linq;
using MFAWPF.Core.Services;

namespace MFAWPF.Core.Services;

public class VersionChecker
{
    private static readonly VersionChecker Checker = new();
    private static readonly HttpClient Client = new()
    {
        DefaultRequestHeaders = { UserAgent = { new("request") } }
    };

    public static void CheckVersion()
    {
        TaskManager.RunTaskAsync(() => Checker.CheckForGUIUpdatesAsync(), null, "检测MFA版本");
        TaskManager.RunTaskAsync(() => Checker.CheckForResourceUpdatesAsync(), null, "检测资源版本");
    }

    public async Task CheckForGUIUpdatesAsync(string owner = "SweetSmellFox", string repo = "MFAWPF")
    {
        try
        {
            string latestVersion = await GetLatestVersionFromGitHub(owner, repo);
            string localVersion = GetLocalVersion();
            if (IsNewVersionAvailable(latestVersion, localVersion))
            {
                NotificationService.Info($"MFA有新版本可用！最新版本: {latestVersion}");
            }
        }
        catch (Exception ex)
        {
            NotificationService.Error($"检查MFA最新版时发生错误: {ex.Message}");
            LoggerService.LogError(ex);
        }
    }

    private static string ConvertToApiUrl(string githubUrl)
    {
        string pattern = @"^https://github\.com/(?<owner>[^/]+)/(?<repo>[^/]+)$";
        var match = Regex.Match(githubUrl, pattern);

        if (!match.Success)
        {
            throw new FormatException($"输入的 GitHub URL 格式不正确: {githubUrl}");
        }

        string owner = match.Groups["owner"].Value;
        string repo = match.Groups["repo"].Value;
        return $"https://api.github.com/repos/{owner}/{repo}/releases/latest";
    }

    public async Task CheckForResourceUpdatesAsync()
    {
        try
        {
            string? url = MaaInterface.Instance?.Url;
            if (string.IsNullOrWhiteSpace(url))
            {
                return;
            }

            url = ConvertToApiUrl(url);
            string latestVersion = await GetLatestVersionFromGitHubUrl(url);
            if (string.IsNullOrWhiteSpace(latestVersion))
            {
                return;
            }

            string localVersion = GetResourceVersion();
            if (string.IsNullOrWhiteSpace(localVersion))
            {
                return;
            }

            if (IsNewVersionAvailable(latestVersion, localVersion))
            {
                NotificationService.Info($"资源有新版本可用！最新版本: {latestVersion}");
            }
        }
        catch (Exception ex)
        {
            NotificationService.Error($"检查资源最新版时发生错误: {ex.Message}");
            LoggerService.LogError(ex);
        }
    }

    private async Task<string> GetLatestVersionFromGitHubUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return string.Empty;
        }

        try
        {
            HttpResponseMessage response = await Client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string jsonResponse = await response.Content.ReadAsStringAsync();
            JObject releaseData = JObject.Parse(jsonResponse);
            return releaseData["tag_name"]?.ToString() ?? string.Empty;
        }
        catch (HttpRequestException e) when (e.Message.Contains("403"))
        {
            const string message = "GitHub API速率限制已超出，请稍后再试。";
            LoggerService.LogError(message);
            throw new Exception(message);
        }
        catch (HttpRequestException e)
        {
            var message = $"请求GitHub时发生错误: {e.Message}";
            LoggerService.LogError(message);
            throw new Exception("请求GitHub时发生错误。");
        }
        catch (Exception e)
        {
            var message = $"处理GitHub响应时发生错误: {e.Message}";
            LoggerService.LogError(message);
            throw new Exception("处理GitHub响应时发生错误。");
        }
    }

    private async Task<string> GetLatestVersionFromGitHub(string owner, string repo)
    {
        string url = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";
        return await GetLatestVersionFromGitHubUrl(url);
    }

    private string GetLocalVersion()
    {
        return Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "DEBUG";
    }

    private string GetResourceVersion()
    {
        return MaaInterface.Instance?.Version ?? "DEBUG";
    }

    private bool IsNewVersionAvailable(string latestVersion, string localVersion)
    {
        try
        {
            string latestVersionNumber = ExtractVersionNumber(latestVersion);
            string localVersionNumber = ExtractVersionNumber(localVersion);

            Version latest = new(latestVersionNumber);
            Version local = new(localVersionNumber);

            return latest.CompareTo(local) > 0;
        }
        catch (Exception ex)
        {
            LoggerService.LogError(ex);
            return false;
        }
    }

    private string ExtractVersionNumber(string versionString)
    {
        string pattern = @"^[vV]?(?<version>\d+(\.\d+){0,3})([-_][a-zA-Z0-9]+)?$";
        var match = Regex.Match(versionString, pattern);

        if (!match.Success)
        {
            throw new FormatException($"无法解析版本号: {versionString}");
        }

        string versionPart = match.Groups["version"].Value;
        string[] versionComponents = versionPart.Split('.');

        while (versionComponents.Length < 4)
        {
            versionPart += ".0";
            versionComponents = versionPart.Split('.');
        }

        if (!Version.TryParse(versionPart, out _))
        {
            throw new FormatException($"无法解析版本号: {versionString}");
        }

        return versionPart;
    }
}