using Avalonia;
using Avalonia.Controls;
using Avalonia.Collections;
using System.Collections;
using System.Collections.Specialized;

namespace MFAWPF.Avalonia.Controls;

public partial class CustomAutoListControl : UserControl
{
    public static readonly StyledProperty<AvaloniaList<CustomValue<string>>> ItemsProperty =
        AvaloniaProperty.Register<CustomAutoListControl, AvaloniaList<CustomValue<string>>>(
            nameof(Items),
            defaultValue: new AvaloniaList<CustomValue<string>>(),
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<IEnumerable> TaskDialogDataListProperty =
        AvaloniaProperty.Register<CustomAutoListControl, IEnumerable>(
            nameof(TaskDialogDataList),
            defaultValue: new AvaloniaList<string>());

    public AvaloniaList<CustomValue<string>> Items
    {
        get => GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    public IEnumerable TaskDialogDataList
    {
        get => GetValue(TaskDialogDataListProperty);
        set => SetValue(TaskDialogDataListProperty, value);
    }

    public CustomAutoListControl()
    {
        InitializeComponent();
        
        Items.CollectionChanged += OnItemsCollectionChanged;
    }

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // 处理集合变更事件
    }
}
