using Avalonia.Controls;
using Avalonia.Controls.Templates;
using MaaFramework.Binding;

namespace MFAWPF.Avalonia.Selectors;

public class DeviceWindowTemplateSelector : IDataTemplate
{
    public IDataTemplate? DeviceInfoTemplate { get; set; }
    public IDataTemplate? WindowInfoTemplate { get; set; }

    public Control? Build(object? param)
    {
        if (param is null) return null;

        return param switch
        {
            AdbDeviceInfo => DeviceInfoTemplate?.Build(param),
            DesktopWindowInfo => WindowInfoTemplate?.Build(param),
            _ => null
        };
    }

    public bool Match(object? data)
    {
        return data is AdbDeviceInfo or DesktopWindowInfo;
    }
} 