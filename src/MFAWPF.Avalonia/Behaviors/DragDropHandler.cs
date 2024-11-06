using Avalonia.Input;
using Avalonia.Controls;
using System.Collections;
using System.Collections.ObjectModel;
using MFAWPF.Data;
using MFAWPF.ViewModels;
using MFAWPF.Core.Models;

namespace MFAWPF.Avalonia.Behaviors;

public class DragDropBehavior
{
    private ItemsControl? _itemsControl;
    private Point _dragStartPoint;
    private bool _isDragging;
    private object? _draggedItem;
    private int _draggedIndex;

    public void Attach(ItemsControl itemsControl)
    {
        _itemsControl = itemsControl;
        
        // 设置拖拽事件
        itemsControl.PointerPressed += OnPointerPressed;
        itemsControl.PointerMoved += OnPointerMoved;
        itemsControl.PointerReleased += OnPointerReleased;
        
        // 允许拖放
        itemsControl.DragDrop.AllowDrop = true;
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_itemsControl == null) return;
        
        // 记录起始点
        _dragStartPoint = e.GetPosition(_itemsControl);
        var point = e.GetPosition(_itemsControl);
        
        // 获取点击的项
        if (_itemsControl.GetItemAt(point) is { } item)
        {
            _draggedItem = item;
            _draggedIndex = _itemsControl.ItemsSource.Cast<object>().ToList().IndexOf(item);
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isDragging && _draggedItem != null && _itemsControl != null)
        {
            var point = e.GetPosition(_itemsControl);
            var diff = point - _dragStartPoint;

            // 判断是否开始拖动
            if (Math.Abs(diff.X) > 3 || Math.Abs(diff.Y) > 3)
            {
                _isDragging = true;
                
                // 开始拖动操作
                var dragData = new DataObject();
                dragData.Set("DraggedItem", _draggedItem);
                
                DragDrop.DoDragDrop(e, dragData, DragDropEffects.Move);
            }
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_itemsControl == null || !_isDragging) return;

        var point = e.GetPosition(_itemsControl);
        var targetItem = _itemsControl.GetItemAt(point);
        
        if (targetItem != null && _draggedItem != null)
        {
            var targetIndex = _itemsControl.ItemsSource.Cast<object>().ToList().IndexOf(targetItem);
            
            if (_itemsControl.ItemsSource is ObservableCollection<DragItemViewModel> items)
            {
                // 执行移动
                items.Move(_draggedIndex, targetIndex);
                
                // 更新数据
                List<TaskInterfaceItem> tasks = new();
                foreach (var dragItem in items)
                {
                    if (dragItem.InterfaceItem != null)
                        tasks.Add(dragItem.InterfaceItem);
                }

                if (MaaInterface.Instance != null)
                    MaaInterface.Instance.Task = tasks;
                
                // 保存更新
                DataSet.SetData("TaskItems",
                    items.Select(model => model.InterfaceItem).ToList());
            }
        }

        // 重置状态
        _isDragging = false;
        _draggedItem = null;
    }
}