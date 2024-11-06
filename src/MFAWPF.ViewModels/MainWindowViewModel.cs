using System;
using MFAWPF.Core.Models;
using MFAWPF.ViewModels.Base;

namespace MFAWPF.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel()
    {
        MaaInterface.InstanceChanged += OnMaaInterfaceChanged;
    }

    private void OnMaaInterfaceChanged(object? sender, EventArgs e)
    {
        var instance = MaaInterface.Instance;
        if (instance == null) return;

        ResourceName = instance.Name;
        ResourceVersion = instance.Version;
        CustomTitle = instance.CustomTitle;
        
        // 通知属性更新
        OnPropertyChanged(nameof(ResourceName));
        OnPropertyChanged(nameof(ResourceVersion));
        OnPropertyChanged(nameof(CustomTitle));
    }
} 