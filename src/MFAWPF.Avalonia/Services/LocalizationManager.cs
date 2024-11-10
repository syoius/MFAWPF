using Avalonia.Platform;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;

namespace MFAWPF.Core.Services;

public class LocalizationManager
{
    private readonly Dictionary<string, ResourceDictionary> _resources = new();
    private CultureInfo _currentCulture = new("zh-CN");

    public CultureInfo CurrentCulture
    {
        get => _currentCulture;
        set
        {
            if (_currentCulture != value && _resources.ContainsKey(value.Name))
            {
                _currentCulture = value;
            }
        }
    }

    public void LoadResources(string basePath)
    {
        var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
        if (assets == null) return;

        foreach (var culture in new[] { "zh-CN", "en-US" })
        {
            var uri = new Uri($"{basePath}/{culture}.axaml");
            if (assets.Exists(uri))
            {
                _resources[culture] = new ResourceDictionary
                {
                    Source = uri
                };
            }
        }
    }

    public string GetString(string key)
    {
        if (_resources.TryGetValue(_currentCulture.Name, out var dict))
        {
            if (dict.TryGetValue(key, out var value) && value is string str)
            {
                return str;
            }
        }
        return key;
    }
}