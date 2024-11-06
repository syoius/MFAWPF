using Avalonia.Controls;
using MFAWPF.Avalonia.Behaviors;

namespace MFAWPF.Avalonia.Views;

public partial class MainWindow : Window
{
    private readonly DragDropBehavior _dragDropBehavior;

    public MainWindow()
    {
        InitializeComponent();
        
        _dragDropBehavior = new DragDropBehavior();
        _dragDropBehavior.Attach(this.FindControl<ItemsControl>("DraggableList"));
    }
}
