using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using MaaFramework.Binding;
using MFAWPF.Core.Services;
using MFAWPF.Data;
using MFAWPF.Utils;
using MFAWPF.ViewModels;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace MFAWPF.Avalonia.Views;

public partial class MainWindow : Window
{
    public static MainWindow? Instance { get; private set; }
    private readonly MaaToolkit _maaToolkit;

    public static MainViewModel? Data { get; private set; }

    public static readonly string Version =
        $"v{Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "DEBUG"}";

    public Dictionary<string, TaskModel> TaskDictionary = new();

    public MainWindow()
    {
        InitializeComponent();
        Instance = this;
        this.FindControl<TextBlock>("version")!.Text = Version;
        _maaToolkit = new MaaToolkit(init: true);
        Data = DataContext as MainViewModel;
        Loaded += (_, _) => { LoadUI(); };
        InitializeData();
        OCRHelper.Initialize();
        VersionChecker.CheckVersion();

        MaaProcessor.Instance.TaskStackChanged += OnTaskStackChanged;

        SetIconFromExeDirectory();
    }

    private void SetIconFromExeDirectory()
    {
        string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string iconPath = Path.Combine(exeDirectory, "logo.ico");

        if (File.Exists(iconPath))
        {
            var bitmap = new Bitmap(iconPath);
            this.Icon = bitmap;
        }
    }

    private bool InitializeData()
    {
        DataSet.Data = JsonHelper.ReadFromConfigJsonFile("config", new Dictionary<string, object>());
        if (!File.Exists($"{AppDomain.CurrentDomain.BaseDirectory}/interface.json"))
        {
            LoggerService.LogInfo("未找到interface文件，生成interface.json...");
            MaaInterface.Instance = new MaaInterface
            {
                Version = "1.0",
                Name = "Debug",
                Task = new List<TaskInterfaceItem>(),
                Resource = new List<MaaInterface.MaaCustomResource>
                {
                    new()
                    {
                        Name = "默认", Path = new List<string> { "{PROJECT_DIR}/resource/base" }
                    }
                },
                Recognition = new Dictionary<string, MaaInterface.CustomExecutor>(),
                Action = new Dictionary<string, MaaInterface.CustomExecutor>(),
                Option = new Dictionary<string, MaaInterface.MaaInterfaceOption>
                {
                    {
                        "测试", new MaaInterface.MaaInterfaceOption()
                        {
                            Cases = new List<MaaInterface.MaaInterfaceOptionCase>
                            {
                                new() { Name = "测试1", PipelineOverride = new Dictionary<string, TaskModel>() },
                                new() { Name = "测试2", PipelineOverride = new Dictionary<string, TaskModel>() }
                            }
                        }
                    }
                }
            };
            JsonHelper.WriteToJsonFilePath(AppDomain.CurrentDomain.BaseDirectory, "interface",
                MaaInterface.Instance, new MaaInterfaceSelectOptionConverter(true));
        }
        else
        {
            MaaInterface.Instance =
                JsonHelper.ReadFromJsonFilePath(AppDomain.CurrentDomain.BaseDirectory, "interface",
                    new MaaInterface(),
                    () => { }, new MaaInterfaceSelectOptionConverter(false));
        }

        if (MaaInterface.Instance != null)
        {
            Data?.TasksSource.Clear();
            LoadTasks(MaaInterface.Instance.Task ?? new List<TaskInterfaceItem>());
        }

        ConnectToMAA();
        return LoadTask();
    }

    private void OnTaskStackChanged(object? sender, EventArgs e)
    {
        ToggleTaskButtonsVisibility(isRunning: MaaProcessor.Instance.TaskQueue.Count > 0);
    }

    public void ToggleTaskButtonsVisibility(bool isRunning)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var startButton = this.FindControl<Button>("startButton")!;
            var stopButton = this.FindControl<Button>("stopButton")!;
            
            startButton.IsVisible = !isRunning;
            startButton.IsEnabled = !isRunning;
            stopButton.IsVisible = isRunning;
            stopButton.IsEnabled = isRunning;
        });
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    private void TaskList_OnPreviewMouseWheel(object? sender, PointerWheelEventArgs e)
    {
        if (sender is Control control)
        {
            var scrollViewer = control.FindAncestorOfType<ScrollViewer>();
            if (scrollViewer != null)
            {
                scrollViewer.Offset = new Vector(
                    scrollViewer.Offset.X,
                    scrollViewer.Offset.Y - e.Delta.Y / 3);
                e.Handled = true;
            }
        }
    }

    public void ShowResourceName(string name)
    {
        Dispatcher.UIThread.Post(() =>
        {
            this.FindControl<TextBlock>("resourceName")!.IsVisible = true;
            this.FindControl<TextBlock>("resourceNameText")!.IsVisible = true;
            this.FindControl<TextBlock>("resourceName")!.Text = name;
        });
    }

    public void ShowResourceVersion(string v)
    {
        Dispatcher.UIThread.Post(() =>
        {
            this.FindControl<TextBlock>("resourceVersion")!.IsVisible = true;
            this.FindControl<TextBlock>("resourceVersionText")!.IsVisible = true;
            this.FindControl<TextBlock>("resourceVersion")!.Text = v;
        });
    }

    public void ShowCustomTitle(string v)
    {
        Dispatcher.UIThread.Post(() =>
        {
            this.FindControl<TextBlock>("title")!.IsVisible = false;
            this.FindControl<TextBlock>("version")!.IsVisible = false;
            this.FindControl<TextBlock>("resourceName")!.IsVisible = false;
            this.FindControl<TextBlock>("resourceNameText")!.IsVisible = false;
            this.FindControl<TextBlock>("resourceVersionText")!.IsVisible = false;
            this.FindControl<TextBlock>("customTitle")!.IsVisible = true;
            this.FindControl<TextBlock>("customTitle")!.Text = v;
        });
    }

    public void LoadUI()
    {
        Dispatcher.UIThread.Post(() =>
        {
            var tabControl = this.FindControl<TabControl>("TabControl")!;
            tabControl.SelectedIndex = MaaInterface.Instance?.DefaultController == "win32" ? 1 : 0;
            WaitEmulator();

            tabControl.SelectionChanged += TabControl_OnSelectionChanged;
            if (Data != null)
                Data.NotLock = MaaInterface.Instance?.LockController != true;
                
            this.FindControl<ToggleButton>("ConnectSettingButton")!.IsChecked = true;
            
            var value = DataSet.GetData("EnableEdit", true);
            var editButton = this.FindControl<Button>("EditButton")!;
            if (!value)
                editButton.IsVisible = false;
            DataSet.SetData("EnableEdit", value);
        });
    }

    public async void WaitEmulator()
    {
        await Task.Run(async () =>
        {
            if (DataSet.GetData("StartEmulator", false))
            {
                await MaaProcessor.Instance.StartEmulator();
            }

            if ((Data?.IsAdb).IsTrue() && DataSet.GetData("RememberAdb", true) &&
                "adb".Equals(MaaProcessor.Config.AdbDevice.AdbPath) &&
                DataSet.TryGetData<JObject>("AdbDevice", out var jObject))
            {
                var settings = new JsonSerializerSettings();
                settings.Converters.Add(new AdbInputMethodsConverter());
                settings.Converters.Add(new AdbScreencapMethodsConverter());

                var device = jObject?.ToObject<AdbDeviceInfo>(JsonSerializer.Create(settings));
                if (device != null)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        var deviceComboBox = this.FindControl<ComboBox>("deviceComboBox");
                        if (deviceComboBox != null)
                        {
                            deviceComboBox.ItemsSource = new List<AdbDeviceInfo> { device };
                            deviceComboBox.SelectedIndex = 0;
                        }
                        MaaProcessor.Config.IsConnected = true;
                        if (DataSet.GetData("AutoStartIndex", 0) == 1)
                        {
                            if (MaaProcessor.Instance.ShouldEndStart)
                            {
                                MaaProcessor.Instance.EndAutoStart();
                            }
                            else
                            {
                                Start();
                            }
                        }
                    });
                }
            }
            else
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    AutoDetectDevice();
                    if ((Data?.IsAdb).IsTrue() && DataSet.GetData("AutoStartIndex", 0) == 1)
                    {
                        if (MaaProcessor.Instance.ShouldEndStart)
                        {
                            MaaProcessor.Instance.EndAutoStart();
                        }
                        else
                        {
                            Start();
                        }
                    }
                });
            }
        });
    }

    private void AutoDetectDevice()
    {
        var devices = MaaToolkit.GetDevices();
        var deviceComboBox = this.FindControl<ComboBox>("deviceComboBox");
        if (deviceComboBox != null)
        {
            deviceComboBox.ItemsSource = devices;
            if (devices.Count > 0)
            {
                deviceComboBox.SelectedIndex = 0;
            }
        }
    }

    private void SwapFiles(string file1Path, string file2Path)
    {
        string backupFilePath = $"{file1Path}.bak";
        File.Copy(file1Path, backupFilePath, true);

        string file1Content = File.ReadAllText(file1Path);
        string file2Content = File.ReadAllText(file2Path);

        File.WriteAllText(file1Path, file2Content);
    }

    private async void RestartMFA()
    {
        var result = await MessageBoxService.ShowYesNoDialog(
            "提示".GetLocalizationString(),
            "RestartApplication".GetLocalizationString());

        if (result)
        {
            var info = new ProcessStartInfo
            {
                FileName = Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty,
                UseShellExecute = true
            };
            Process.Start(info);
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
        }
    }

    private void ToggleWindowTopMost(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton toggleButton)
        {
            Topmost = toggleButton.IsChecked ?? false;
        }
    }
}
