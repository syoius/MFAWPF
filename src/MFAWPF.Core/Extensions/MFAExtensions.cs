using System.Collections.ObjectModel;
using Avalonia;
using MaaFramework.Binding;
using MaaFramework.Binding.Buffers;
using MFAWPF.Core.Models;
using MFAWPF.Core.Services;

namespace MFAWPF.Core.Extensions;

public static class MFAExtensions
{
    public static Dictionary<TKey, TaskModel> MergeTaskModels<TKey>(
        this IEnumerable<KeyValuePair<TKey, TaskModel>>? taskModels,
        IEnumerable<KeyValuePair<TKey, TaskModel>>? additionalModels) where TKey : notnull
    {
        if (additionalModels == null)
            return taskModels?.ToDictionary() ?? new Dictionary<TKey, TaskModel>();
        return taskModels?
            .Concat(additionalModels)
            .GroupBy(pair => pair.Key)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    var mergedModel = group.First().Value;
                    foreach (var taskModel in group.Skip(1))
                    {
                        mergedModel.Merge(taskModel.Value);
                    }

                    return mergedModel;
                }
            ) ?? new Dictionary<TKey, TaskModel>();
    }

    public static void BindLocalization(this StyledElement control, string resourceKey,
        AvaloniaProperty? property = null)
    {
        property ??= StyledElement.NameProperty;
        control.Bind(property, LocalizationService.GetString(resourceKey));
    }

    public static string GetLocalizationString(this string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return string.Empty;
        return LocalizationService.GetString(key);
    }

    public static string GetLocalizedFormattedString(this string? key, params object[] args)
    {
        if (string.IsNullOrWhiteSpace(key))
            return string.Empty;
        string localizedString = LocalizationService.GetString(key);
        return string.Format(localizedString, args);
    }

    // ... 其他扩展方法保持不变 ...
} 