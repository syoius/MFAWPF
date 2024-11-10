using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using MFAWPF.Core.Extensions;
using MFAWPF.ViewModels;
using System.Collections.ObjectModel;

namespace MFAWPF.Avalonia.Views;

public partial class AddTaskDialog : Window
{
    private DragItemViewModel? _outputContent;
    public AddTaskDialogViewModel? Data;

    public DragItemViewModel? OutputContent
    {
        get => _outputContent;
        set => _outputContent = value;
    }

    private readonly ObservableCollection<DragItemViewModel> _source = new();

    public AddTaskDialog(IList<DragItemViewModel>? dragItemViewModels)
    {
        InitializeComponent();
        Data = DataContext as AddTaskDialogViewModel;
        _source.AddRange(dragItemViewModels);
        if (Data != null)
        {
            Data.DataList.Clear();
            Data.DataList.AddRange(_source);
        }
    }

    private void Add(object? sender, RoutedEventArgs e)
    {
        Close(this.FindControl<ListBox>("TaskList")?.SelectedItem as DragItemViewModel);
    }

    protected override void OnClosed(EventArgs e)
    {
        // 通过 ViewModelLocator 获取 MainViewModel
        var mainViewModel = App.Current.Services.GetService<MainViewModel>();
        mainViewModel?.SetIdle(true);
        base.OnClosed(e);
    }

    private void SearchBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        var searchBox = sender as TextBox;
        string? key = searchBox?.Text;

        if (string.IsNullOrEmpty(key))
        {
            if (Data != null)
            {
                Data.DataList.Clear();
                Data.DataList.AddRange(_source);
            }
        }
        else
        {
            key = key.ToLower();
            Data?.DataList.Clear();
            foreach (DragItemViewModel item in _source)
            {
                string name = item.Name.ToLower();
                if (name.Contains(key))
                    Data?.DataList.Add(item);
            }
        }
    }
}