using Avalonia.Media;
using Avalonia.Media.Imaging;
using MFAWPF.Core.Models;
using MFAWPF.Core.Services;
using MaaFramework.Binding;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;

namespace MFAWPF.Core.Services;

public class MaaProcessor
{
    private static MaaProcessor? _instance;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isStopped;

    // ... 其他属性保持不变 ...

    public void Start(List<DragItemViewModel>? tasks)
    {
        if (!Config.IsConnected)
        {
            NotificationService.Warning(
                LocalizationService.GetString("Warning_CannotConnect")
                    .FormatWith(MainViewModel.Instance.IsAdb
                        ? LocalizationService.GetString("Emulator")
                        : LocalizationService.GetString("Window")));
            return;
        }

        // ... 其他逻辑保持不变 ...
    }

    private void DisplayFocusTip(TaskModel taskModel)
    {
        if (taskModel.FocusTip == null) return;

        for (int i = 0; i < taskModel.FocusTip.Count; i++)
        {
            IBrush? brush = null;
            var tip = taskModel.FocusTip[i];
            try
            {
                if (taskModel.FocusTipColor != null && taskModel.FocusTipColor.Count > i)
                {
                    var color = Color.Parse(taskModel.FocusTipColor[i]);
                    brush = new SolidColorBrush(color);
                }
            }
            catch (Exception e)
            {
                LoggerService.LogError(e);
            }

            MainViewModel.Instance?.AddLog(HandleStringsWithVariables(tip), brush);
        }
    }

    public Bitmap? GetBitmapImage()
    {
        using var buffer = GetImage(GetCurrentTasker()?.Controller);
        if (buffer == null) return null;

        var encodedDataHandle = buffer.GetEncodedData(out var size);
        if (encodedDataHandle == IntPtr.Zero)
        {
            NotificationService.Error("Handle为空！");
            return null;
        }

        var imageData = new byte[size];
        Marshal.Copy(encodedDataHandle, imageData, 0, (int)size);

        if (imageData.Length == 0) return null;

        using var stream = new MemoryStream(imageData);
        return new Bitmap(stream);
    }

    // ... 其他方法保持不变 ...
} 