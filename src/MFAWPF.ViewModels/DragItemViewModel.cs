using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using MFAWPF.Data;
using MFAWPF.Core.Services;
using Newtonsoft.Json;

namespace MFAWPF.ViewModels;

public partial class DragItemViewModel : ObservableObject
{
    public DragItemViewModel(TaskInterfaceItem? interfaceItem)
    {
        InterfaceItem = interfaceItem;
        Name = interfaceItem?.Name ?? "未命名";
    }

    [ObservableProperty]
    private string _name = string.Empty;

    private bool? _isCheckedWithNull = false;
    private bool _isInitialized;

    [JsonIgnore]
    public bool? IsCheckedWithNull
    {
        get => _isCheckedWithNull;
        set
        {
            if (!_isInitialized)
            {
                _isInitialized = true;
                SetProperty(ref _isCheckedWithNull, value);
                if (InterfaceItem != null)
                    InterfaceItem.Check = IsChecked;
            }
            else
            {
                value ??= false;
                SetProperty(ref _isCheckedWithNull, value);
                if (InterfaceItem != null)
                    InterfaceItem.Check = IsChecked;
                DataSet.SetData("TaskItems",
                    MainWindow.Data?.TaskItemViewModels.ToList().Select(model => model.InterfaceItem));
            }
        }
    }

    public bool IsChecked
    {
        get => IsCheckedWithNull != false;
        set => IsCheckedWithNull = value;
    }

    [ObservableProperty]
    private bool _enableSetting;

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
                    ? Avalonia.Controls.Avalonia.Controls.Visibility.Visible
                    : Avalonia.Controls.Avalonia.Controls.Visibility.Collapsed;
                if (value.Check.HasValue)
                    IsChecked = value.Check.Value;
            }

            SetProperty(ref _interfaceItem, value);
        }
    }

    [ObservableProperty]
    private Avalonia.Controls.Visibility _settingVisibility = Avalonia.Controls.Visibility.Visible;

    public DragItemViewModel Clone()
    {
        TaskInterfaceItem? clonedInterfaceItem = InterfaceItem?.Clone();
        var clone = new DragItemViewModel(clonedInterfaceItem)
        {
            Name = this.Name,
            IsCheckedWithNull = this.IsCheckedWithNull,
            EnableSetting = this.EnableSetting,
            SettingVisibility = this.SettingVisibility
        };
        return clone;
    }
}