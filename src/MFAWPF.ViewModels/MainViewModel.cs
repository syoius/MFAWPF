using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MFAWPF.Core.Services;
using MFAWPF.Core.Extensions;
using MFAWPF.Avalonia.Behaviors;

namespace MFAWPF.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private AvaloniaList<LogItemViewModel> _logItemViewModels = new();

    [ObservableProperty]
    private AvaloniaList<DragItemViewModel> _taskItemViewModels = new();

    [ObservableProperty]
    private AvaloniaList<DragItemViewModel> _tasksSource = new();

    [ObservableProperty]
    private bool _idle = true;

    [ObservableProperty]
    private bool _notLock = true;

    [ObservableProperty]
    private bool _isAdb = true;

    public void AddLog(string content, IBrush? color = null, string weight = "Regular",
        bool showTime = true)
    {
        color ??= new SolidColorBrush(Colors.Gray);
        Task.Run(() =>
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                LogItemViewModels.Add(new LogItemViewModel(content, color, weight, "HH':'mm':'ss",
                    showTime: showTime));
            });
        });
    }

    public void AddLogByKey(string key, IBrush? color = null, params string[]? formatArgsKeys)
    {
        color ??= new SolidColorBrush(Colors.Gray);
        Task.Run(() =>
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                LogItemViewModels.Add(new LogItemViewModel(key, color, "Regular", true, "HH':'mm':'ss",
                    true, formatArgsKeys));
            });
        });
    }

    public void SetIdle(bool value)
    {
        Idle = value;
    }

    public IDragDrop DropHandler { get; } = new DragDropBehavior();

    [RelayCommand]
    private void SwitchItem(object info)
    {
        if (info is MenuItem menuItem)
        {
            NotificationService.Show("信息", menuItem.Header?.ToString() ?? string.Empty, NotificationType.Info);
        }
    }
}