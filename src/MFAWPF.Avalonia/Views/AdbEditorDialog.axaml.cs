using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using MaaFramework.Binding;
using MFAWPF.Core.Extensions;
using MFAWPF.Core.Models;

namespace MFAWPF.Avalonia.Views;

public partial class AdbEditorDialog : Window
{
    public AdbEditorDialog(AdbDeviceInfo? info = null)
    {
        InitializeComponent();
        if (info != null)
        {
            AdbName = info.Name;
            AdbPath = info.AdbPath;
            AdbSerial = info.AdbSerial;
            AdbConfig = info.Config;
        }
    }

    private async void Load(object? sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "LoadFileTitle".GetLocalizationString(),
            Filters = new List<FileDialogFilter>
            {
                new() { Name = "AllFilter".GetLocalizationString(), Extensions = new List<string> { "*" } }
            }
        };

        var result = await dialog.ShowAsync(this);
        if (result != null && result.Length > 0)
        {
            AdbPath = result[0];
        }
    }

    private void Save(object? sender, RoutedEventArgs e)
    {
        Console.WriteLine($"{AdbName},{AdbPath},{AdbSerial}");
        Close(true);
    }

    private void Cancel(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    public static readonly StyledProperty<string> AdbNameProperty =
        AvaloniaProperty.Register<AdbEditorDialog, string>(
            nameof(AdbName),
            defaultValue: "Emulator".GetLocalizationString());

    public string AdbName
    {
        get => GetValue(AdbNameProperty);
        set => SetValue(AdbNameProperty, value);
    }

    public static readonly StyledProperty<string> AdbPathProperty =
        AvaloniaProperty.Register<AdbEditorDialog, string>(
            nameof(AdbPath),
            defaultValue: string.Empty);

    public string AdbPath
    {
        get => GetValue(AdbPathProperty);
        set => SetValue(AdbPathProperty, value);
    }

    public static readonly StyledProperty<string> AdbSerialProperty =
        AvaloniaProperty.Register<AdbEditorDialog, string>(
            nameof(AdbSerial),
            defaultValue: string.Empty);

    public string AdbSerial
    {
        get => GetValue(AdbSerialProperty);
        set => SetValue(AdbSerialProperty, value);
    }

    public static readonly StyledProperty<string> AdbConfigProperty =
        AvaloniaProperty.Register<AdbEditorDialog, string>(
            nameof(AdbConfig),
            defaultValue: "{}");

    public string AdbConfig
    {
        get => GetValue(AdbConfigProperty);
        set => SetValue(AdbConfigProperty, value);
    }

    public AdbDeviceInfo Output => new(AdbName, AdbPath, AdbSerial, AdbScreencapMethods.Default,
        AdbInputMethods.MinitouchAndAdbKey, AdbConfig);
}