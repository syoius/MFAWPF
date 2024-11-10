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
    public bool FirstTask = true;

    private void LoadTasks(IEnumerable<TaskInterfaceItem> tasks)
    {
        foreach (var task in tasks)
        {
            var dragItem = new DragItemViewModel(task);

            if (FirstTask)
            {
                if (MaaInterface.Instance?.Resources != null &&
                    MaaInterface.Instance.Resources.Count > DataSet.GetData("ResourceIndex", 0))
                {
                    var resourceKeys = MaaInterface.Instance.Resources.Keys.ToList();
                    var resourceIndex = DataSet.GetData("ResourceIndex", 0);
                    MaaProcessor.CurrentResources = MaaInterface.Instance.Resources[resourceKeys[resourceIndex]];
                }
                else
                {
                    MaaProcessor.CurrentResources = new List<string> { MaaProcessor.ResourceBase };
                }
                FirstTask = false;
            }

            Data?.TasksSource.Add(dragItem);
        }

        if (Data?.TaskItemViewModels.Count == 0)
        {
            var items = DataSet.GetData("TaskItems", new List<TaskInterfaceItem>()) ?? new List<TaskInterfaceItem>();
            var dragItemViewModels = items
                .Select(interfaceItem => new DragItemViewModel(interfaceItem))
                .ToList();

            Data.TaskItemViewModels.AddRange(dragItemViewModels);

            if (Data.TaskItemViewModels.Count == 0 && Data.TasksSource.Count != 0)
            {
                foreach (var item in Data.TasksSource)
                {
                    Data.TaskItemViewModels.Add(item);
                }
            }
        }
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
    private bool LoadTask()
    {
        try
        {
            var taskDictionary = new Dictionary<string, TaskModel>();
            if (MaaProcessor.CurrentResources != null)
        {
            foreach (var resourcePath in MaaProcessor.CurrentResources)
            {
                var jsonFiles = Directory.GetFiles($"{resourcePath}/pipeline/", "*.json");
                var taskDictionaryA = new Dictionary<string, TaskModel>();
                foreach (var file in jsonFiles)
                {
                    var content = File.ReadAllText(file);
                    var taskData = JsonConvert.DeserializeObject<Dictionary<string, TaskModel>>(content);
                    if (taskData == null || taskData.Count == 0)
                        break;
                    foreach (var task in taskData)
                    {
                        if (!taskDictionaryA.TryAdd(task.Key, task.Value))
                        {
                            await MessageBoxService.ShowDialog(
                                "错误".GetLocalizationString(),
                                string.Format("DuplicateTaskError".GetLocalizationString(), task.Key));
                            return false;
                        }
                    }
                }

                taskDictionary = taskDictionary.MergeTaskModels(taskDictionaryA);
            }
        }

        if (taskDictionary.Count == 0)
        {
            string? directoryPath = Path.GetDirectoryName($"{MaaProcessor.ResourceBase}/pipeline");
            if (!string.IsNullOrWhiteSpace(directoryPath) && !Directory.Exists(directoryPath))
            {
                try
                {
                    Directory.CreateDirectory(directoryPath);
                }
                catch (Exception ex)
                {
                    LoggerService.LogError($"创建目录时发生错误: {ex.Message}");
                    await MessageBoxService.ShowDialog("错误".GetLocalizationString(), ex.Message);
                }
            }

            if (!File.Exists($"{MaaProcessor.ResourceBase}/pipeline/sample.json"))
            {
                try
                {
                    File.WriteAllText($"{MaaProcessor.ResourceBase}/pipeline/sample.json",
                        JsonConvert.SerializeObject(new Dictionary<string, TaskModel>
                        {
                            {
                                "MFAWPF", new TaskModel()
                                {
                                    Action = "DoNothing"
                                }
                            }
                        }, new JsonSerializerSettings()
                        {
                            Formatting = Formatting.Indented,
                            NullValueHandling = NullValueHandling.Ignore,
                            DefaultValueHandling = DefaultValueHandling.Ignore
                        }));
                }
                catch (Exception ex)
                {
                    LoggerService.LogError($"创建文件时发生错误: {ex.Message}");
                    await MessageBoxService.ShowDialog("错误".GetLocalizationString(), ex.Message);
                }
            }
        }

    PopulateTasks(taskDictionary);
        return true;
        }
        catch (Exception ex)
        {
            LoggerService.LogError($"加载Pipeline时发生错误: {ex}");
            await MessageBoxService.ShowDialog(
                "错误".GetLocalizationString(),
                string.Format("PipelineLoadError".GetLocalizationString(), ex.Message));
            return false;
        }
    }

    private void PopulateTasks(Dictionary<string, TaskModel> taskDictionary)
    {
        TaskDictionary = taskDictionary;
        foreach (var task in taskDictionary)
        {
            task.Value.Name = task.Key;
            ValidateTaskLinks(taskDictionary, task);
        }
    }

    private void ValidateTaskLinks(Dictionary<string, TaskModel> taskDictionary, KeyValuePair<string, TaskModel> task)
    {
        ValidateNextTasks(taskDictionary, task.Value.Next);
    ValidateNextTasks(taskDictionary, task.Value.OnError, "on_error");
    ValidateNextTasks(taskDictionary, task.Value.Interrupt, "interrupt");
    }

    private async void ValidateNextTasks(Dictionary<string, TaskModel> taskDictionary, object? nextTasks, string name = "next")
    {
        if (nextTasks is List<string> tasks)
    {
        foreach (var task in tasks)
        {
            if (!taskDictionary.ContainsKey(task))
            {
                await MessageBoxService.ShowDialog(
                    "错误".GetLocalizationString(),
                    string.Format("TaskNotFoundError".GetLocalizationString(), name, task));
            }
        }
    }
    }
    public async void Start()
    {
        if (Data == null) return;
        Data.Idle = false;

        var tasks = Data.TaskItemViewModels
            .Where(model => model.IsChecked)
            .Select(model => model.InterfaceItem)
            .ToList();

        if (!tasks.Any())
        {
            await MessageBoxService.ShowDialog("提示".GetLocalizationString(), 
                "NoTaskSelected".GetLocalizationString());
            return;
        }

        await Task.Run(async () =>
        {
            try
            {
                await MaaProcessor.Instance.Start(tasks);
            }
            catch (Exception ex)
            {
                LoggerService.LogError($"Start task error: {ex}");
                await MessageBoxService.ShowDialog("错误".GetLocalizationString(), ex.Message);
            }
        });
    }

    public async void Stop()
    {
        if (Data == null) return;
        Data.Idle = true;

        var result = await MessageBoxService.ShowYesNoDialog(
            "提示".GetLocalizationString(),
            "StopTask".GetLocalizationString());

        if (result)
        {
            await Task.Run(() =>
            {
                try
                {
                    MaaProcessor.Instance.Stop();
                }
                catch (Exception ex)
                {
                    LoggerService.LogError($"Stop task error: {ex}");
                    MessageBoxService.ShowDialog("错误".GetLocalizationString(), ex.Message);
                }
            });
        }
    }
    private void TabControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is TabControl tabControl && Data != null)
        {
            Data.IsAdb = tabControl.SelectedIndex == 0;
            if (Data.IsAdb)
            {
                AutoDetectDevice();
            }

            MaaProcessor.Instance.SetCurrentTasker();

            var connectSettingButton = this.FindControl<ToggleButton>("ConnectSettingButton");
            if (connectSettingButton?.IsChecked == true)
            {
                ConfigureSettingsPanel();
            }
        }
    }
    private async void DeviceComboBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox deviceComboBox)
        {
            if (deviceComboBox.SelectedItem is DesktopWindowInfo window)
            {
                await MessageBoxService.ShowDialog(
                    "提示".GetLocalizationString(),
                    string.Format("WindowSelectionMessage".GetLocalizationString(), window.Name));

                MaaProcessor.Config.DesktopWindow.HWnd = window.Handle;
                MaaProcessor.Instance.SetCurrentTasker();
            }
            else if (deviceComboBox.SelectedItem is AdbDeviceInfo device)
            {
                await MessageBoxService.ShowDialog(
                    "提示".GetLocalizationString(),
                    string.Format("EmulatorSelectionMessage".GetLocalizationString(), device.Name));

                MaaProcessor.Config.AdbDevice.Name = device.Name;
                MaaProcessor.Config.AdbDevice.AdbPath = device.AdbPath;
                MaaProcessor.Config.AdbDevice.AdbSerial = device.AdbSerial;
                MaaProcessor.Config.AdbDevice.Config = device.Config;
                MaaProcessor.Instance.SetCurrentTasker();
                DataSet.SetData("AdbDevice", device);
            }
        }
    }

    private void Refresh(object sender, RoutedEventArgs e)
    {
        AutoDetectDevice();
    }

    private void CustomAdb(object sender, RoutedEventArgs e)
    {
        var deviceInfo =
            deviceComboBox.Items.Count > 0 && deviceComboBox.SelectedItem is AdbDeviceInfo device
                ? device
                : null;
        AdbEditorDialog dialog = new AdbEditorDialog(deviceInfo);
        if (dialog.ShowDialog().IsTrue())
        {
            deviceComboBox.ItemsSource = new List<AdbDeviceInfo> { dialog.Output };
            deviceComboBox.SelectedIndex = 0;
            MaaProcessor.Config.IsConnected = true;
        }
    }
    public bool IsFirstStart = true;

    public bool TryGetIndexFromConfig(string config, out int index)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(config);
            if (doc.RootElement.TryGetProperty("extras", out JsonElement extras) &&
                extras.TryGetProperty("mumu", out JsonElement mumu) &&
                mumu.TryGetProperty("index", out JsonElement indexElement))
            {
                index = indexElement.GetInt32();
                return true;
            }
        }
        catch (Exception ex)
        {
            LoggerService.LogError($"解析 Config 时出错: {ex.Message}");
        }
        index = 0;
        return false;
    }

    public static int ExtractNumberFromEmulatorConfig(string emulatorConfig)
    {
        var match = Regex.Match(emulatorConfig, @"\d+");
        return match.Success ? int.Parse(match.Value) : 0;
    }

    private async void AutoDetectDevice()
    {
        try
        {
            await MessageBoxService.ShowDialog(
                "提示".GetLocalizationString(),
                (Data?.IsAdb).IsTrue()
                    ? "EmulatorDetectionStarted".GetLocalizationString()
                    : "WindowDetectionStarted".GetLocalizationString());

            MaaProcessor.Config.IsConnected = false;
            var deviceComboBox = this.FindControl<ComboBox>("deviceComboBox");
            if (deviceComboBox == null) return;

            if ((Data?.IsAdb).IsTrue())
            {
                var devices = await _maaToolkit.AdbDevice.FindAsync();
                deviceComboBox.ItemsSource = devices;
                MaaProcessor.Config.IsConnected = devices.Count > 0;

                var emulatorConfig = DataSet.GetData("EmulatorConfig", string.Empty);
                if (!string.IsNullOrWhiteSpace(emulatorConfig))
                {
                    var extractedNumber = ExtractNumberFromEmulatorConfig(emulatorConfig);

                    foreach (var device in devices)
                    {
                        if (TryGetIndexFromConfig(device.Config, out int index))
                        {
                            if (index == extractedNumber)
                            {
                                deviceComboBox.SelectedIndex = devices.IndexOf(device);
                            }
                        }
                        else deviceComboBox.SelectedIndex = 0;
                    }
                }
                else
                    deviceComboBox.SelectedIndex = 0;
            }
            else
            {
                var windows = _maaToolkit.Desktop.Window.Find();
                deviceComboBox.ItemsSource = windows;
                MaaProcessor.Config.IsConnected = windows.Count > 0;
                deviceComboBox.SelectedIndex = windows.Count > 0
                    ? windows.ToList().FindIndex(win => !string.IsNullOrWhiteSpace(win.Name))
                    : 0;
            }

            if (!MaaProcessor.Config.IsConnected)
            {
                await MessageBoxService.ShowDialog(
                    "提示".GetLocalizationString(),
                    (Data?.IsAdb).IsTrue()
                        ? "NoEmulatorFound".GetLocalizationString()
                        : "NoWindowFound".GetLocalizationString());
            }
        }
        catch (Exception ex)
        {
            LoggerService.LogError(ex);
            await MessageBoxService.ShowDialog(
                "错误".GetLocalizationString(),
                string.Format("TaskStackError".GetLocalizationString(),
                    (Data?.IsAdb).IsTrue()
                        ? "Emulator".GetLocalizationString()
                        : "Window".GetLocalizationString(),
                    ex.Message));
            MaaProcessor.Config.IsConnected = false;
        }
    }


    private void ConfigureSettingsPanel(object? sender = null, RoutedEventArgs? e = null)
    {
        var settingPanel = this.FindControl<Panel>("settingPanel");
        if (settingPanel == null) return;

        settingPanel.Children.Clear();

        var tabControl = new TabControl
        {
            TabStripPlacement = Dock.Bottom,
            Height = 400,
            Background = null,
            BorderThickness = new Thickness(0),
            VerticalAlignment = VerticalAlignment.Stretch
        };

        var s1 = new StackPanel { Margin = new Thickness(2) };
        var s2 = new StackPanel { Margin = new Thickness(2) };

        AddResourcesOption(s1);
        AddAutoStartOption(s2);

        if ((Data?.IsAdb).IsTrue())
        {
            AddSettingOption(s1, "CaptureModeOption",
                new[]
                {
                    "Default", "RawWithGzip", "RawByNetcat",
                    "Encode", "EncodeToFileAndPull", "MinicapDirect", "MinicapStream",
                    "EmulatorExtras"
                },
                "AdbControlScreenCapType");

            AddBindSettingOption(s1, "InputModeOption",
                new[] { "MiniTouch", "MaaTouch", "AdbInput", "AutoDetect" },
                "AdbControlInputType");

            AddAfterTaskOption(s2);
            AddStartSettingOption(s2);
            AddStartEmulatorOption(s2);
            AddRememberAdbOption(s2);
        }
        else
        {
            AddSettingOption(s1, "CaptureModeOption",
                new[] { "FramePool", "DXGIDesktopDup", "GDI" },
                "Win32ControlScreenCapType");

            AddSettingOption(s1, "InputModeOption",
                new[] { "Seize", "SendMessage" },
                "Win32ControlInputType");
        }

        AddThemeOption(s1);
        AddLanguageOption(s1);
        AddGpuOption(s2);

        var sv1 = new ScrollViewer
        {
            Content = s1,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        var sv2 = new ScrollViewer
        {
            Content = s2,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        var commonSettingTabItem = new TabItem
        {
            Header = "CommonSetting".GetLocalizationString(),
            Content = sv1
        };

        var advancedSettingTabItem = new TabItem
        {
            Header = "AdvancedSetting".GetLocalizationString(),
            Content = sv2
        };

        tabControl.Items.Add(commonSettingTabItem);
        tabControl.Items.Add(advancedSettingTabItem);

        settingPanel.Children.Add(tabControl);
    }

    private void AddSwitchConfiguration(Panel? panel = null, int defaultValue = 0)
    {
        panel ??= this.FindControl<Panel>("settingPanel");
        if (panel == null) return;

        var comboBox = new ComboBox
        {
            Margin = new Thickness(5)
        };

        string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config");
        foreach (string file in Directory.GetFiles(configPath))
        {
            string fileName = Path.GetFileName(file);
            if (fileName.EndsWith(".json") && fileName != "maa_option.json")
            {
                comboBox.Items.Add(fileName);
            }
        }

        // 添加标题
        var titleBlock = new TextBlock
        {
            Text = "SwitchConfiguration".GetLocalizationString(),
            Margin = new Thickness(5, 5, 5, 2)
        };

        comboBox.SelectionChanged += async (sender, _) =>
        {
            if (comboBox.SelectedItem is string selectedItem)
            {
                if (selectedItem == "config.json")
                {
                    // 不做任何操作
                }
                else if (selectedItem == "config.json.bak")
                {
                    string currentFile = Path.Combine(configPath, "config.json");
                    string selectedPath = Path.Combine(configPath, "config.json.bak");
                    string bakContent = File.ReadAllText(selectedPath);
                    File.WriteAllText(currentFile, bakContent);
                    RestartMFA();
                }
                else
                {
                    string currentFile = Path.Combine(configPath, "config.json");
                    string selectedPath = Path.Combine(configPath, selectedItem);
                    SwapFiles(currentFile, selectedPath);
                    RestartMFA();
                }
            }
        };

        var stackPanel = new StackPanel();
        stackPanel.Children.Add(titleBlock);
        stackPanel.Children.Add(comboBox);
        panel.Children.Add(stackPanel);
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
    private void About(object? sender = null, RoutedEventArgs? e = null)
    {
        var settingPanel = this.FindControl<Panel>("settingPanel");
        if (settingPanel == null) return;
        
        settingPanel.Children.Clear();
        
        var s1 = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(3),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        
        var s2 = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(3),
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var t1 = new TextBlock
        {
            Text = "ProjectLink".GetLocalizationString(),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(2),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        
        s1.Children.Add(t1);
        
        var projectButton = new Button
        {
            Content = "MFAWPF - Github",
            Margin = new Thickness(2),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        
        projectButton.Click += (_, _) => OpenUrl("https://github.com/SweetSmellFox/MFAWPF");
        s1.Children.Add(projectButton);

        var resourceLink = MaaInterface.Instance?.Url;
        if (!string.IsNullOrWhiteSpace(resourceLink))
        {
            var t2 = new TextBlock
            {
                Text = "ResourceLink".GetLocalizationString(),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(2)
            };
            
            s2.Children.Add(t2);
            
            var resourceButton = new Button
            {
                Content = $"{MaaInterface.Instance?.Name ?? "Resource"} - Github",
                Margin = new Thickness(2),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            
            resourceButton.Click += (_, _) => OpenUrl(resourceLink);
            s2.Children.Add(resourceButton);
        }

        settingPanel.Children.Add(s1);
        settingPanel.Children.Add(s2);
    }

    private void OpenUrl(string url)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            LoggerService.LogError($"打开URL时发生错误: {ex.Message}");
        }
    }

    private void AddBindSettingOption(Panel? panel, string titleKey, IEnumerable<string> options, string datatype, int defaultValue = 0)
    {
        panel ??= this.FindControl<Panel>("settingPanel");
        if (panel == null) return;

        var titleBlock = new TextBlock
        {
            Text = titleKey.GetLocalizationString(),
            Margin = new Thickness(5, 5, 5, 2)
        };

        var comboBox = new ComboBox
        {
            SelectedIndex = DataSet.GetData(datatype, defaultValue),
            Margin = new Thickness(5),
            IsEnabled = Data?.Idle ?? false
        };

        // 设置数据绑定
        comboBox.Bind(ComboBox.IsEnabledProperty, new Binding
        {
            Source = Data,
            Path = "Idle",
            Mode = BindingMode.OneWay
        });

        // 添加本地化的选项
        foreach (var option in options)
        {
            var comboBoxItem = new ComboBoxItem
            {
                Content = option.GetLocalizationString()
            };
            comboBox.Items.Add(comboBoxItem);
        }

        comboBox.SelectionChanged += (sender, _) =>
        {
            if (sender is ComboBox cb)
            {
                var index = cb.SelectedIndex;
                DataSet.SetData(datatype, index);
                MaaProcessor.Instance.SetCurrentTasker();
            }
        };

        var stackPanel = new StackPanel();
        stackPanel.Children.Add(titleBlock);
        stackPanel.Children.Add(comboBox);
        panel.Children.Add(stackPanel);
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

    private void AddResourcesOption(Panel? panel = null, int defaultValue = 0)
    {
        panel ??= this.FindControl<Panel>("settingPanel");
        if (panel == null) return;

        var titleBlock = new TextBlock
        {
            Text = "ResourceOption".GetLocalizationString(),
            Margin = new Thickness(5, 5, 5, 2)
        };

        var comboBox = new ComboBox
        {
            SelectedIndex = DataSet.GetData("ResourceIndex", defaultValue),
            Margin = new Thickness(5),
            IsEnabled = Data?.Idle ?? false
        };

        // 设置数据绑定
        comboBox.Bind(ComboBox.IsEnabledProperty, new Binding
        {
            Source = Data,
            Path = "Idle",
            Mode = BindingMode.OneWay
        });

        // 设置显示成员路径
        comboBox.ItemTemplate = new FuncDataTemplate<object>((item, _) =>
            new TextBlock { Text = (item as dynamic)?.Name?.ToString() });

        if (MaaInterface.Instance?.Resource != null)
        {
            comboBox.ItemsSource = MaaInterface.Instance.Resource;
        }

        comboBox.SelectionChanged += (sender, _) =>
        {
            if (sender is ComboBox cb)
            {
                var index = cb.SelectedIndex;
                if (MaaInterface.Instance?.Resources != null &&
                    MaaInterface.Instance.Resources.Count > index)
                {
                    MaaProcessor.CurrentResources =
                        MaaInterface.Instance.Resources[MaaInterface.Instance.Resources.Keys.ToList()[index]];
                }
                DataSet.SetData("ResourceIndex", index);
            }
        };

        var stackPanel = new StackPanel();
        stackPanel.Children.Add(titleBlock);
        stackPanel.Children.Add(comboBox);
        panel.Children.Add(stackPanel);
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

    private static void FollowSystemTheme()
    {
        if (Application.Current == null) return;

        // 在 Avalonia 中，设置为 null 表示跟随系统主题
        Application.Current.RequestedThemeVariant = null;
    }
    private void AddThemeOption(Panel? panel = null, int defaultValue = 0)
    {
        panel ??= this.FindControl<Panel>("settingPanel");
        if (panel == null) return;

        var titleBlock = new TextBlock
        {
            Text = "ThemeOption".GetLocalizationString(),
            Margin = new Thickness(5, 5, 5, 2)
        };

        var comboBox = new ComboBox
        {
            Margin = new Thickness(5),
            IsEnabled = Data?.Idle ?? false
        };

        // 添加主题选项
        var light = new ComboBoxItem
        {
            Content = "LightColor".GetLocalizationString()
        };
        var dark = new ComboBoxItem
        {
            Content = "DarkColor".GetLocalizationString()
        };
        var followSystem = new ComboBoxItem
        {
            Content = "FollowingSystem".GetLocalizationString()
        };

        comboBox.Items.Add(light);
        comboBox.Items.Add(dark);
        comboBox.Items.Add(followSystem);

        // 设置数据绑定
        comboBox.Bind(ComboBox.IsEnabledProperty, new Binding
        {
            Source = Data,
            Path = "Idle",
            Mode = BindingMode.OneWay
        });

        comboBox.SelectionChanged += (sender, _) =>
        {
            if (sender is ComboBox cb)
            {
                var index = cb.SelectedIndex;
                var app = Application.Current;
                if (app == null) return;

                switch (index)
                {
                    case 0: // Light
                        app.RequestedThemeVariant = ThemeVariant.Light;
                        break;
                    case 1: // Dark
                        app.RequestedThemeVariant = ThemeVariant.Dark;
                        break;
                    default: // System
                        app.RequestedThemeVariant = null;
                        break;
                }

                DataSet.SetData("ThemeIndex", index);
            }
        };

        comboBox.SelectedIndex = DataSet.GetData("ThemeIndex", defaultValue);

        var stackPanel = new StackPanel();
        stackPanel.Children.Add(titleBlock);
        stackPanel.Children.Add(comboBox);
        panel.Children.Add(stackPanel);
    }

    private void AddLanguageOption(Panel? panel = null, int defaultValue = 0)
    {
        panel ??= this.FindControl<Panel>("settingPanel");
        if (panel == null) return;

        var titleBlock = new TextBlock
        {
            Text = "LanguageOption".GetLocalizationString(),
            Margin = new Thickness(5, 5, 5, 2)
        };

        var comboBox = new ComboBox
        {
            ItemsSource = new List<string> { "简体中文", "English" },
            Margin = new Thickness(5),
            IsEnabled = Data?.Idle ?? false
        };

        // 设置数据绑定
        comboBox.Bind(ComboBox.IsEnabledProperty, new Binding
        {
            Source = Data,
            Path = "Idle",
            Mode = BindingMode.OneWay
        });

        comboBox.SelectionChanged += (sender, _) =>
        {
            if (sender is ComboBox cb)
            {
                var index = cb.SelectedIndex;
                var culture = CultureInfo.CreateSpecificCulture(index == 0 ? "zh-cn" : "en-us");

                // 更改应用程序的语言
                var app = Application.Current;
                if (app != null)
                {
                    app.RequestedThemeVariant = null; // 重置主题以触发语言更新
                    Thread.CurrentThread.CurrentCulture = culture;
                    Thread.CurrentThread.CurrentUICulture = culture;
                }

                DataSet.SetData("LangIndex", index);
            }
        };

        comboBox.SelectedIndex = DataSet.GetData("LangIndex", defaultValue);

        var stackPanel = new StackPanel();
        stackPanel.Children.Add(titleBlock);
        stackPanel.Children.Add(comboBox);
        panel.Children.Add(stackPanel);
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

    private void AddSettingOption(Panel? panel, string titleKey, IEnumerable<string> options, string datatype, int defaultValue = 0)
    {
        panel ??= this.FindControl<Panel>("settingPanel");
        if (panel == null) return;

        var titleBlock = new TextBlock
        {
            Text = titleKey.GetLocalizationString(),
            Margin = new Thickness(5, 5, 5, 2)
        };

        var comboBox = new ComboBox
        {
            ItemsSource = options,
            SelectedIndex = DataSet.GetData(datatype, defaultValue),
            Margin = new Thickness(5),
            IsEnabled = Data?.Idle ?? false
        };

        // 设置数据绑定
        comboBox.Bind(ComboBox.IsEnabledProperty, new Binding
        {
            Source = Data,
            Path = "Idle",
            Mode = BindingMode.OneWay
        });

        comboBox.SelectionChanged += (sender, _) =>
        {
            if (sender is ComboBox cb)
            {
                var index = cb.SelectedIndex;
                DataSet.SetData(datatype, index);
                MaaProcessor.Instance.SetCurrentTasker();
            }
        };

        var stackPanel = new StackPanel();
        stackPanel.Children.Add(titleBlock);
        stackPanel.Children.Add(comboBox);
        panel.Children.Add(stackPanel);
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


    private void ToggleWindowTopMost(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton toggleButton)
        {
            Topmost = toggleButton.IsChecked ?? false;
        }
    }

    private void SelectAll(object sender, RoutedEventArgs e)
    {
        if (Data == null) return;
        foreach (var task in Data.TaskItemViewModels)
            task.IsChecked = true;
    }

    private void SelectNone(object sender, RoutedEventArgs e)
    {
        if (Data == null) return;
        foreach (var task in Data.TaskItemViewModels)
            task.IsChecked = false;
    }

    private async void Add(object sender, RoutedEventArgs e)
    {
        if (Data != null)
            Data.Idle = false;
        var addTaskDialog = new AddTaskDialog(Data?.TasksSource);
        var result = await addTaskDialog.ShowDialog<bool>(this);
        if (result && addTaskDialog.OutputContent != null)
        {
            Data?.TaskItemViewModels.Add(addTaskDialog.OutputContent.Clone());
            DataSet.SetData("TaskItems", Data?.TaskItemViewModels.ToList().Select(model => model.InterfaceItem));
        }
    }

    private async void Edit(object sender, RoutedEventArgs e)
    {
        if (!MaaProcessor.Config.IsConnected)
        {
            await MessageBoxService.ShowDialog(
                "提示".GetLocalizationString(),
                string.Format("Warning_CannotConnect".GetLocalizationString(),
                    (Data?.IsAdb).IsTrue()
                        ? "Emulator".GetLocalizationString()
                        : "Window".GetLocalizationString()));
            return;
        }

        if (Data != null)
        {
            Data.Idle = false;
            var editTaskDialog = new EditTaskDialog(Data.TaskItemViewModels.ToList());
            var result = await editTaskDialog.ShowDialog<bool>(this);
            if (result)
            {
                Data.TaskItemViewModels.Clear();
                foreach (var item in editTaskDialog.DataList)
                {
                    Data.TaskItemViewModels.Add(item);
                }
                DataSet.SetData("TaskItems", Data.TaskItemViewModels.ToList().Select(model => model.InterfaceItem));
            }
        }
    }

    private async void OpenConfig(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "OpenConfig".GetLocalizationString(),
            Filters = new List<FileDialogFilter>
            {
                new()
                {
                    Name = "JSON",
                    Extensions = new List<string> { "json" }
                }
            }
        };

        var result = await dialog.ShowAsync(this);
        if (result != null && result.Length > 0)
        {
            var selectedPath = result[0];
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config");
            var configFile = Path.Combine(configPath, "config.json");

            try
            {
                if (!Directory.Exists(configPath))
                {
                    Directory.CreateDirectory(configPath);
                }

                File.Copy(selectedPath, configFile, true);
                RestartMFA();
            }
            catch (Exception ex)
            {
                await MessageBoxService.ShowDialog("错误".GetLocalizationString(), ex.Message);
            }
        }
    }

    private async void SaveConfig(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "SaveConfig".GetLocalizationString(),
            DefaultExtension = "json",
            Filters = new List<FileDialogFilter>
            {
                new()
                {
                    Name = "JSON",
                    Extensions = new List<string> { "json" }
                }
            }
        };

        var result = await dialog.ShowAsync(this);
        if (!string.IsNullOrEmpty(result))
        {
            try
            {
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "config.json");
                File.Copy(configPath, result, true);
            }
            catch (Exception ex)
            {
                await MessageBoxService.ShowDialog("错误".GetLocalizationString(), ex.Message);
            }
        }
    }

    public void ConnectToMAA()
    {
        ConfigureMaaProcessorForADB();
        ConfigureMaaProcessorForWin32();
    }

    private void ConfigureMaaProcessorForADB()
    {
        if ((Data?.IsAdb).IsTrue())
        {
            var adbInputType = ConfigureAdbInputTypes();
            var adbScreenCapType = ConfigureAdbScreenCapTypes();

            MaaProcessor.Config.AdbDevice.Input = adbInputType;
            MaaProcessor.Config.AdbDevice.ScreenCap = adbScreenCapType;

            LoggerService.LogInfo(
                $"{LocExtension.GetLocalizedValue<string>("AdbInputMode")}{adbInputType},{LocExtension.GetLocalizedValue<string>("AdbCaptureMode")}{adbScreenCapType}");
        }
    }

    public string ScreenshotType()
    {
        if ((Data?.IsAdb).IsTrue())
            return ConfigureAdbScreenCapTypes().ToString();
        return ConfigureWin32ScreenCapTypes().ToString();
    }

    private AdbInputMethods ConfigureAdbInputTypes()
    {
        return DataSet.GetData("AdbControlInputType", 0) switch
        {
            0 => AdbInputMethods.MinitouchAndAdbKey,
            1 => AdbInputMethods.Maatouch,
            2 => AdbInputMethods.AdbShell,
            3 => AdbInputMethods.All,
            _ => 0
        };
    }

    private AdbScreencapMethods ConfigureAdbScreenCapTypes()
    {
        return DataSet.GetData("AdbControlScreenCapType", 0) switch
        {
            0 => AdbScreencapMethods.Default,
            1 => AdbScreencapMethods.RawWithGzip,
            2 => AdbScreencapMethods.RawByNetcat,
            3 => AdbScreencapMethods.Encode,
            4 => AdbScreencapMethods.EncodeToFileAndPull,
            5 => AdbScreencapMethods.MinicapDirect,
            6 => AdbScreencapMethods.MinicapStream,
            7 => AdbScreencapMethods.EmulatorExtras,
            _ => 0
        };
    }

    private void ConfigureMaaProcessorForWin32()
    {
        if (!(Data?.IsAdb).IsTrue())
        {
            var win32InputType = ConfigureWin32InputTypes();
            var winScreenCapType = ConfigureWin32ScreenCapTypes();

            MaaProcessor.Config.DesktopWindow.Input = win32InputType;
            MaaProcessor.Config.DesktopWindow.ScreenCap = winScreenCapType;

            var message = $"{"AdbInputMode".GetLocalizationString()}{win32InputType},{"AdbCaptureMode".GetLocalizationString()}{winScreenCapType}";
            Console.WriteLine(message);
            LoggerService.LogInfo(message);
        }
    }

    private Win32ScreencapMethod ConfigureWin32ScreenCapTypes()
    {
        return DataSet.GetData("Win32ControlScreenCapType", 0) switch
        {
            0 => Win32ScreencapMethod.FramePool,
            1 => Win32ScreencapMethod.DXGIDesktopDup,
            2 => Win32ScreencapMethod.GDI,
            _ => 0
        };
    }

    private Win32InputMethod ConfigureWin32InputTypes()
    {
        return DataSet.GetData("Win32ControlInputType", 0) switch
        {
            0 => Win32InputMethod.Seize,
            1 => Win32InputMethod.SendMessage,
            _ => 0
        };
    }

    private void Delete(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem &&
            menuItem.Parent is ContextMenu contextMenu &&
            contextMenu.PlacementTarget is Grid item &&
            item.DataContext is DragItemViewModel taskItemViewModel &&
            Data != null)
        {
            int index = Data.TaskItemViewModels.IndexOf(taskItemViewModel);
            Data.TaskItemViewModels.RemoveAt(index);
            DataSet.SetData("TaskItems", Data.TaskItemViewModels.ToList().Select(model => model.InterfaceItem));
        }
    }



    private void InitializeConfigurationComboBox(StackPanel panel)
    {
        var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config");
        if (!Directory.Exists(configPath))
        {
            Directory.CreateDirectory(configPath);
            return;
        }

        var files = Directory.GetFiles(configPath, "*.json");
        if (files.Length <= 1) return;

        var comboBox = new ComboBox
        {
            Width = 120,
            Margin = new Thickness(5),
            ItemsSource = files.Select(Path.GetFileNameWithoutExtension)
        };

        comboBox.SelectionChanged += async (_, e) =>
        {
            if (e.AddedItems.Count == 0) return;
            var selectedItem = e.AddedItems[0].ToString();
            if (string.IsNullOrEmpty(selectedItem)) return;

            var result = await MessageBoxService.ShowYesNoDialog(
                "提示".GetLocalizationString(),
                "SwitchConfiguration".GetLocalizationString());

            if (!result) return;

            string currentFile = Path.Combine(configPath, "config.json");
            string selectedItemPath = Path.Combine(configPath, $"{selectedItem}.json");
            SwapFiles(currentFile, selectedItemPath);
            RestartMFA();
        };

        panel.Children.Add(comboBox);
    }
}
