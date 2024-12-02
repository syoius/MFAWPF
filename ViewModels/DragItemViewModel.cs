﻿using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using MFAWPF.Data;
using MFAWPF.Utils;
using MFAWPF.Views;
using Newtonsoft.Json;

namespace MFAWPF.ViewModels;

public class DragItemViewModel : ObservableObject
{
    public DragItemViewModel(TaskInterfaceItem? interfaceItem)
    {
        InterfaceItem = interfaceItem;
        Name = interfaceItem?.Name ?? "未命名";
        LanguageManager.LanguageChanged += OnLanguageChanged;
    }


    private string _name = string.Empty;

    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    private bool? _isCheckedWithNull = false;
    private bool _isInitialized;

    /// <summary>
    /// Gets or sets a value indicating whether gets or sets whether the key is checked with null.
    /// </summary>
    [JsonIgnore]
    public bool? IsCheckedWithNull
    {
        get => _isCheckedWithNull;
        set
        {
            if (!_isInitialized)
            {
                // 这是初始化操作
                _isInitialized = true; // 标记已初始化
                SetProperty(ref _isCheckedWithNull, value); // 可以选择在初始化时不修改 value
                if (InterfaceItem != null)
                    InterfaceItem.Check = IsChecked;
            }
            else
            {
                // 这是后续的 set 操作
                value ??= false; // 只有在后续 set 时才将 null 设置为 false
                SetProperty(ref _isCheckedWithNull, value);
                if (InterfaceItem != null)
                    InterfaceItem.Check = IsChecked;
                DataSet.SetData("TaskItems",
                    MainWindow.Data?.TaskItemViewModels.ToList().Select(model => model.InterfaceItem));
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether gets or sets whether the key is checked.
    /// </summary>
    public bool IsChecked
    {
        get => IsCheckedWithNull != false;
        set => IsCheckedWithNull = value;
    }


    private bool _enableSetting;

    /// <summary>
    /// Gets or sets a value indicating whether gets or sets whether the setting enabled.
    /// </summary>
    [JsonIgnore]
    public bool EnableSetting
    {
        get => _enableSetting;
        set
        {
            SetProperty(ref _enableSetting, value);
            MainWindow.Instance?.SetOption(this, value);
        }
    }

    private TaskInterfaceItem? _interfaceItem;

    public TaskInterfaceItem? InterfaceItem
    {
        get => _interfaceItem;
        set
        {
            if (value != null)
            {
                if (value.Name != null)
                    Name = value.Name;
                SettingVisibility = value is { Option.Count: > 0 } || value.Repeatable.IsTrue() || !string.IsNullOrWhiteSpace(value.Document)
                    ? Visibility.Visible
                    : Visibility.Hidden;
                if (value.Check.HasValue)
                    IsChecked = value.Check.Value;
            }

            SetProperty(ref _interfaceItem, value);
        }
    }

    private Visibility _visibility = Visibility.Visible;

    public Visibility SettingVisibility
    {
        get => _visibility;
        set => SetProperty(ref _visibility, value);
    }

        private void UpdateContent()
    {
        if (!string.IsNullOrEmpty(InterfaceItem?.Name))
        {
            Name = LanguageManager.GetLocalizedString(Name);
        }
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        UpdateContent();
    }

    /// <summary>
    /// Creates a deep copy of the current <see cref="DragItemViewModel"/> instance.
    /// </summary>
    /// <returns>A new <see cref="DragItemViewModel"/> instance that is a deep copy of the current instance.</returns>
    public DragItemViewModel Clone()
    {
        // Clone the InterfaceItem if it's not null
        TaskInterfaceItem? clonedInterfaceItem = InterfaceItem?.Clone();

        // Create a new DragItemViewModel instance with the cloned InterfaceItem
        DragItemViewModel clone = new DragItemViewModel(clonedInterfaceItem);

        // Copy all other properties to the new instance
        clone.Name = this.Name;
        clone.IsCheckedWithNull = this.IsCheckedWithNull;
        clone.EnableSetting = this.EnableSetting;
        clone.SettingVisibility = this.SettingVisibility;

        return clone;
    }
}