using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Avalonia.Collections;

namespace MFAWPF.ViewModels;

public partial class AddTaskDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private AvaloniaList<DragItemViewModel> _dataList = new();

    [ObservableProperty]
    private int _selectedIndex = -1;

    public AddTaskDialogViewModel()
    {
        SelectedIndex = -1;
    }
}