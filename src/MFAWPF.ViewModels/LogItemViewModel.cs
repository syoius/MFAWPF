using System.Text.RegularExpressions;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using MFAWPF.Core.Extensions;
using MFAWPF.Core.Services;

namespace MFAWPF.ViewModels;

public partial class LogItemViewModel : ObservableObject
{
    private readonly string[]? _formatArgsKeys;

    public LogItemViewModel(string resourceKey, IBrush color, string weight = "Regular", bool useKey = false,
        string dateFormat = "MM'-'dd'  'HH':'mm':'ss", bool showTime = true, params string[]? formatArgsKeys)
    {
        _resourceKey = resourceKey;

        Time = DateTime.Now.ToString(dateFormat);
        Color = color;
        Weight = weight;
        ShowTime = showTime;
        if (useKey)
        {
            _formatArgsKeys = formatArgsKeys;
            UpdateContent();

            // 订阅语言切换事件
            LocalizationService.LanguageChanged += OnLanguageChanged;
        }
        else
            Content = resourceKey;
    }

    public LogItemViewModel(string content, IBrush color, string weight = "Regular",
        string dateFormat = "MM'-'dd'  'HH':'mm':'ss", bool showTime = true)
    {
        Time = DateTime.Now.ToString(dateFormat);
        Color = color;
        Weight = weight;
        ShowTime = showTime;
        Content = content;
    }

    [ObservableProperty]
    private string? _time;

    [ObservableProperty]
    private bool _showTime = true;

    [ObservableProperty]
    private string? _content;

    [ObservableProperty]
    private IBrush? _color;

    [ObservableProperty]
    private string _weight = "Regular";

    private string? _resourceKey;
    public string? ResourceKey
    {
        get => _resourceKey;
        set
        {
            if (SetProperty(ref _resourceKey, value))
            {
                UpdateContent();
            }
        }
    }

    private void UpdateContent()
    {
        if (_formatArgsKeys == null || _formatArgsKeys.Length == 0)
            Content = ResourceKey.GetLocalizationString();
        else
        {
            // 获取每个格式化参数的本地化字符串
            var formatArgs = _formatArgsKeys.Select(key => key.GetLocalizedFormattedString()).ToArray();

            // 使用本地化字符串更新内容
            try
            {
                Content = Regex.Unescape(
                    _resourceKey.GetLocalizedFormattedString(formatArgs.Cast<object>().ToArray()));
            }
            catch
            {
                Content = _resourceKey.GetLocalizedFormattedString(formatArgs.Cast<object>().ToArray());
            }
        }
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        UpdateContent();
    }
}