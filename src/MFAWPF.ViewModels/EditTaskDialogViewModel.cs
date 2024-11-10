using System.Collections.ObjectModel;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MFAWPF.Core.Models;
using MFAWPF.Core.Services;
using MFAWPF.Views;
using Newtonsoft.Json;

namespace MFAWPF.ViewModels;

public partial class EditTaskDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private AvaloniaList<TaskItemViewModel>? _dataList;

    [ObservableProperty]
    private AvaloniaList<TaskItemViewModel>? _colors;

    [ObservableProperty]
    private int _selectedIndex = -1;

    [ObservableProperty]
    private TaskItemViewModel? _currentTask;

    [ObservableProperty]
    private AttributeButton? _selectedAttribute;

    public EditTaskDialog? Dialog { get; set; }
    public Stack<IRelayCommand> UndoStack { get; } = new();
    public Stack<IRelayCommand> UndoTaskStack { get; } = new();

    public IRelayCommand CopyCommand { get; }
    public IRelayCommand PasteCommand { get; }
    public IRelayCommand UndoCommand { get; }
    public IRelayCommand SaveCommand { get; }
    public IRelayCommand DeleteCommand { get; }

    public EditTaskDialogViewModel()
    {
        SelectedIndex = -1;
        DataList = new AvaloniaList<TaskItemViewModel>();
        InitializeColors();

        CopyCommand = new RelayCommand(Copy);
        PasteCommand = new RelayCommand(Paste);
        SaveCommand = new RelayCommand(Save);
        UndoCommand = new RelayCommand(Undo);
        DeleteCommand = new RelayCommand(Delete);
    }

    private void InitializeColors()
    {
        Colors = new AvaloniaList<TaskItemViewModel>();
        // 这里需要重新实现颜色列表的初始化
        // Avalonia 使用 Colors 类而不是 Brushes
        foreach (var colorProperty in typeof(Avalonia.Media.Colors)
                     .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
        {
            Colors.Add(new TaskItemViewModel { Name = colorProperty.Name });
        }
    }

    private async void Copy()
    {
        if (CurrentTask != null)
        {
            await Application.Current?.Clipboard?.SetTextAsync(CurrentTask.ToString());
        }
    }

    private async void Paste()
    {
        if (CurrentTask != null && Dialog != null)
        {
            var clipboardText = await Application.Current?.Clipboard?.GetTextAsync();
            if (!string.IsNullOrEmpty(clipboardText))
            {
                try
                {
                    var taskModels = JsonConvert.DeserializeObject<Dictionary<string, TaskModel>>(clipboardText);
                    if (taskModels == null || taskModels.Count == 0) return;

                    foreach (var pair in taskModels)
                    {
                        pair.Value.Name = pair.Key;
                        var newItem = new TaskItemViewModel
                        {
                            Name = pair.Key,
                            Task = pair.Value
                        };
                        
                        int index = DataList?.IndexOf(CurrentTask) ?? -1;
                        if (index >= 0)
                        {
                            DataList?.Insert(index + 1, newItem);
                            UndoStack.Push(new RelayCommand(() => DataList?.Remove(newItem)));
                        }
                    }
                }
                catch (Exception ex)
                {
                    // 使用 Avalonia 的通知服务
                    NotificationService.Show("错误", "粘贴失败：" + ex.Message, NotificationType.Error);
                }
            }
            else
            {
                NotificationService.Show("警告", "剪贴板中没有有效的文本数据", NotificationType.Warning);
            }
        }
    }

    private void Undo()
    {
        if (UndoStack.Count > 0)
        {
            var command = UndoStack.Pop();
            command.Execute(null);
        }
    }

    private void Save()
    {
        Dialog?.SavePipeline();
    }

    private void Delete()
    {
        if (CurrentTask != null && DataList != null)
        {
            var itemToDelete = CurrentTask;
            int index = DataList.IndexOf(itemToDelete);
            DataList.Remove(itemToDelete);
            UndoStack.Push(new RelayCommand(() => DataList.Insert(index, itemToDelete)));
        }
    }
}