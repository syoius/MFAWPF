using Avalonia;
using Avalonia.Controls;
using Avalonia.Collections;
using System.Collections.Specialized;
using Avalonia.Interactivity;
using MFAWPF.Core.Models;

namespace MFAWPF.Avalonia.Controls;

public partial class CustomListControl : UserControl
{
    public static readonly StyledProperty<AvaloniaList<CustomValue<string>>> ItemsProperty =
        AvaloniaProperty.Register<CustomListControl, AvaloniaList<CustomValue<string>>>(
            nameof(Items),
            defaultValue: new AvaloniaList<CustomValue<string>>(),
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public AvaloniaList<CustomValue<string>> Items
    {
        get => GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    public ICommand DeleteCommand { get; }

    public CustomListControl()
    {
        InitializeComponent();
        DataContext = this;
        
        DeleteCommand = ReactiveCommand.Create<CustomValue<string>>(OnDeleteItem);
        Items.CollectionChanged += OnCollectionChanged;
    }

    private void OnDeleteItem(CustomValue<string> item)
    {
        Items.Remove(item);
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add when e.NewItems != null:
                foreach (CustomValue<string> item in e.NewItems)
                {
                    item.PropertyChanged += Item_PropertyChanged;
                }
                break;
            case NotifyCollectionChangedAction.Remove when e.OldItems != null:
                foreach (CustomValue<string> item in e.OldItems)
                {
                    item.PropertyChanged -= Item_PropertyChanged;
                }
                break;
            case NotifyCollectionChangedAction.Reset:
                foreach (CustomValue<string> item in Items)
                {
                    item.PropertyChanged += Item_PropertyChanged;
                }
                break;
        }
    }

    private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CustomValue<string>.Value))
        {
            // 通知 UI 更新
            SetCurrentValue(ItemsProperty, Items);
        }
    }
}