using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Avalonia.Collections;
using MFAWPF.Core.Models;
using MFAWPF.Core.Services;
using MFAWPF.ViewModels;
using System.Collections;

namespace MFAWPF.Avalonia.Behaviors;

public class DragDropBehavior : IDragDrop
{
    private ItemsControl? _itemsControl;
    private Point _dragStartPoint;
    private bool _isDragging;
    private object? _draggedItem;
    private int _draggedIndex;

    public void Attach(ItemsControl itemsControl)
    {
        _itemsControl = itemsControl;
        
        itemsControl.AddHandler(DragDrop.DragOverEvent, DragOver);
        itemsControl.AddHandler(DragDrop.DropEvent, Drop);
        itemsControl.PointerPressed += OnPointerPressed;
        itemsControl.PointerMoved += OnPointerMoved;
        itemsControl.PointerReleased += OnPointerReleased;
        
        DragDrop.SetAllowDrop(itemsControl, true);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_itemsControl == null) return;
        
        _dragStartPoint = e.GetPosition(_itemsControl);
        var point = e.GetPosition(_itemsControl);
        
        _draggedItem = GetItemUnderPointer(point);
        if (_draggedItem != null)
        {
            _draggedIndex = _itemsControl.ItemsSource.Cast<object>().ToList().IndexOf(_draggedItem);
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isDragging && _draggedItem != null && _itemsControl != null)
        {
            var point = e.GetPosition(_itemsControl);
            var diff = point - _dragStartPoint;

            if (Math.Abs(diff.X) > 3 || Math.Abs(diff.Y) > 3)
            {
                _isDragging = true;
                
                var dragData = new DataObject();
                dragData.Set(DataFormats.Text, _draggedItem);
                
                DragDrop.DoDragDrop(e, dragData, DragDropEffects.Move);
            }
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_itemsControl == null || !_isDragging) return;

        var point = e.GetPosition(_itemsControl);
        var targetItem = GetItemUnderPointer(point);
        
        if (targetItem != null && _draggedItem != null)
        {
            if (_itemsControl.ItemsSource is AvaloniaList<DragItemViewModel> items)
            {
                var targetIndex = items.IndexOf((DragItemViewModel)targetItem);
                if (targetIndex != -1)
                {
                    items.Move(_draggedIndex, targetIndex);
                    UpdateTaskData(items);
                }
            }
        }

        _isDragging = false;
        _draggedItem = null;
    }

    private void DragOver(object? sender, DragEventArgs e)
    {
        if (!_isDragging) return;
        
        e.DragEffects = DragDropEffects.Move;
        e.Handled = true;
    }

    private void Drop(object? sender, DragEventArgs e)
    {
        if (_itemsControl == null || !_isDragging) return;

        var targetItem = GetItemUnderPointer(e.GetPosition(_itemsControl));
        
        if (targetItem != null && _draggedItem != null)
        {
            if (_itemsControl.ItemsSource is AvaloniaList<DragItemViewModel> items)
            {
                var targetIndex = items.IndexOf((DragItemViewModel)targetItem);
                if (targetIndex != -1)
                {
                    items.Move(_draggedIndex, targetIndex);
                    UpdateTaskData(items);
                }
            }
        }

        _isDragging = false;
        _draggedItem = null;
        e.Handled = true;
    }

    private object? GetItemUnderPointer(Point point)
    {
        if (_itemsControl == null) return null;
        
        var element = _itemsControl.InputHitTest(point) as Visual;
        if (element == null) return null;

        return _itemsControl.GetVisualAt(point)
            ?.FindAncestorOfType<ListBoxItem>()
            ?.DataContext;
    }

    private void UpdateTaskData(IEnumerable<DragItemViewModel> items)
    {
        var tasks = items
            .Where(x => x.InterfaceItem != null)
            .Select(x => x.InterfaceItem!)
            .ToList();

        if (MaaInterface.Instance != null)
            MaaInterface.Instance.Task = tasks;
        
        DataSet.SetData("TaskItems", tasks);
    }

    public void Detach()
    {
        if (_itemsControl != null)
        {
            _itemsControl.RemoveHandler(DragDrop.DragOverEvent, DragOver);
            _itemsControl.RemoveHandler(DragDrop.DropEvent, Drop);
            _itemsControl.PointerPressed -= OnPointerPressed;
            _itemsControl.PointerMoved -= OnPointerMoved;
            _itemsControl.PointerReleased -= OnPointerReleased;
        }
    }
}