using BetterInfinityNikki.Core.Config;
using BetterInfinityNikki.Core.Recognition.ONNX;
using BetterInfinityNikki.GameTask;
using BetterInfinityNikki.Service.Interface;
using BetterInfinityNikki.ViewModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Fischless.GameCapture;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BetterInfinityNikki.Helpers;
using BetterInfinityNikki.View;
using BetterInfinityNikki.View.Pages.View;
using BetterInfinityNikki.View.Windows;
using Wpf.Ui.Controls;
using Wpf.Ui.Violeta.Controls;

namespace BetterInfinityNikki.ViewModel.Pages;

public partial class HomePageViewModel : ObservableObject, IViewModel
{
    [ObservableProperty]
    private IEnumerable<EnumItem<CaptureModes>> _modeNames = EnumExtensions.ToEnumItems<CaptureModes>();

    [ObservableProperty] private bool _taskDispatcherEnabled = false;

    [ObservableProperty] 
    private InferenceDeviceType[] _inferenceDeviceTypes = Enum.GetValues<InferenceDeviceType>();

    public AllConfig Config { get; set; }

    private readonly ILogger<HomePageViewModel> _logger;
    private readonly IConfigService _configService;
    private readonly TaskTriggerDispatcher _taskDispatcher;

    [ObservableProperty] private ImageSource? _bannerImageSource;

    private const string DefaultBannerImagePath = "pack://application:,,,/Resources/Images/banner.jpg";
    private readonly string _customBannerImagePath;

    public HomePageViewModel(IConfigService configService, TaskTriggerDispatcher taskTriggerDispatcher)
    {
        _logger = App.GetLogger<HomePageViewModel>();
        _configService = configService;
        _taskDispatcher = taskTriggerDispatcher;
        Config = configService.Get();
        _customBannerImagePath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "User", "Images", "custom_banner.jpg");

        InitializeBannerImage();

        // 过滤不支持的捕获模式
        if (!(Environment.OSVersion.Version.Build >= 18362))
        {
            _modeNames = _modeNames.Where(x => x.EnumName != CaptureModes.WindowsGraphicsCapture.ToString()).ToList();
        }
    }

    [RelayCommand]
    private void OnLoaded()
    {
        _logger.LogInformation("HomePage 已加载");
    }

    [RelayCommand]
    private async Task OnCaptureModeDropDownChanged()
    {
        if (TaskDispatcherEnabled)
        {
            _logger.LogInformation("切换捕获模式至 [{Mode}]", Config.CaptureMode);
            // TODO: 实现重启逻辑
        }
    }

    [RelayCommand]
    private void OnStartCaptureTest()
    {
        var picker = new PickerWindow(true);

        if (picker.PickCaptureTarget(new WindowInteropHelper(UIDispatcherHelper.MainWindow).Handle, out var hWnd))
        {
            if (hWnd != IntPtr.Zero)
            {
                var captureWindow = new CaptureTestWindow();
                captureWindow.StartCapture(hWnd, Config.CaptureMode.ToCaptureMode());
                captureWindow.Show();
            }
            else
            {
                ThemedMessageBox.Error("选择的窗体句柄为空");
            }
        }
    }

    [RelayCommand]
    private void OnManualPickWindow()
    {
        var picker = new PickerWindow();
        if (picker.PickCaptureTarget(new WindowInteropHelper(UIDispatcherHelper.MainWindow).Handle, out var hWnd))
        {
            if (hWnd != IntPtr.Zero)
            {
                var captureWindow = new CaptureTestWindow();
                captureWindow.StartCapture(hWnd, Config.CaptureMode.ToCaptureMode());
                captureWindow.Show();
            }
            else
            {
                ThemedMessageBox.Error("选择的窗体句柄为空！");
            }
        }
    }

    [RelayCommand]
    public void OnGoToWikiUrl()
    {
        try
        {
            Process.Start(new ProcessStartInfo("https://github.com/your-repo/better-infinity-nikki/wiki")
            {
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "打开文档链接失败");
        }
    }

    [RelayCommand]
    public void OnOpenHardwareAccelerationSettings()
    {
        var dialogWindow = new FluentWindow
        {
            Title = "硬件加速设置",
            Content = new HardwareAccelerationView(),
            Width = 800,
            Height = 600,
            MinWidth = 800,
            MaxWidth = 800,
            MinHeight = 600,
            Owner = Application.Current.MainWindow,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ExtendsContentIntoTitleBar = true,
            WindowBackdropType = WindowBackdropType.Auto,
        };
        dialogWindow.SourceInitialized += (s, e) => Helpers.Ui.WindowHelper.TryApplySystemBackdrop(dialogWindow);
        var result = dialogWindow.ShowDialog();
    }

    [RelayCommand]
    public async Task SelectInstallPathAsync()
    {
        await Task.Run(() =>
        {
            var dialog = new OpenFileDialog
            {
                Filter = "无限暖暖|launcher.exe|可执行文件|*.exe|所有文件|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                var path = dialog.FileName;
                if (!string.IsNullOrEmpty(path))
                {
                    Config.GameStartConfig.InstallPath = path;
                    _logger.LogInformation("设置游戏安装路径: {Path}", path);
                }
            }
        });
    }

    [RelayCommand]
    public async Task OnStartTriggerAsync()
    {
        _logger.LogInformation("开始启动截图器...");
        // 检查游戏路径配置
        if (string.IsNullOrEmpty(Config.GameStartConfig.InstallPath))
        {
            await ThemedMessageBox.ErrorAsync("请先配置无限暖暖安装路径！");
            return;
        }

        // 查找游戏窗口
        var hWnd = SystemControl.FindInfinityNikkiHandle();
        var lWnd = IntPtr.Zero;
        if (hWnd != IntPtr.Zero)
        {
            _logger.LogInformation("无限暖暖已启动，直接启动截图器...");
            Start(hWnd);
            return;
        }

        _logger.LogInformation("游戏未运行，查找启动器...");
        lWnd = SystemControl.FindLauncherHandle();
        if (lWnd == IntPtr.Zero)
        {
            _logger.LogInformation("启动器未运行，尝试运行启动器...");
            lWnd = await SystemControl.StartLauncherAsync(Config.GameStartConfig.InstallPath);
            if (lWnd == IntPtr.Zero)
            {
                await ThemedMessageBox.ErrorAsync("启动器启动失败，请配置无限暖暖启动器安装位置！");
                return;
            }
        }

        _logger.LogInformation("启动器已运行...");
        if (!Config.GameStartConfig.LinkedStartEnabled)
        {
            return;
        }

        _logger.LogInformation("联动启动已启用，尝试启动游戏...");
        TaskContext.Instance().LinkedStartGenshinTime = DateTime.Now;
        hWnd = await SystemControl.StartGameAsync(lWnd, Config.CaptureMode.ToCaptureMode());

        if (hWnd == IntPtr.Zero)
        {
            await ThemedMessageBox.ErrorAsync("未找到游戏窗口，请先启动游戏或启用“同时启动无限暖暖”选项！");
            return;
        }

        _logger.LogInformation("游戏启动成功，句柄: {HWnd}", hWnd);

        // 启动截图器
        Start(hWnd);
    }

    private void Start(IntPtr hWnd)
    {
        Debug.WriteLine($"无限暖暖启动句柄{hWnd}");
        lock (this)
        {
            if (Config.TriggerInterval <= 0)
            {
                ThemedMessageBox.Error("触发器触发频率必须大于0");
                return;
            }

            if (!TaskDispatcherEnabled)
            {
                _hWnd = hWnd;
                _taskDispatcher.Start(hWnd, GetCaptureMode(), Config.TriggerInterval);
                _taskDispatcher.UiTaskStopTickEvent -= OnUiTaskStopTick;
                _taskDispatcher.UiTaskStartTickEvent -= OnUiTaskStartTick;
                _taskDispatcher.UiTaskStopTickEvent += OnUiTaskStopTick;
                _taskDispatcher.UiTaskStartTickEvent += OnUiTaskStartTick;
                TaskDispatcherEnabled = true;
            }
        }
    }

    private CaptureModes GetCaptureMode()
    {
        try
        {
            return Config.CaptureMode.ToCaptureMode();
        }
        catch (Exception e)
        {
            TaskContext.Instance().Config.CaptureMode = CaptureModes.BitBlt.ToString();
            return CaptureModes.BitBlt;
        }
    }

    private void OnUiTaskStopTick(object? sender, EventArgs e)
    {
        Application.Current.Dispatcher.Invoke(Stop);
    }

    private void OnUiTaskStartTick(object? sender, EventArgs e)
    {
        Application.Current.Dispatcher.Invoke(() => Start(_hWnd));
    }

    private IntPtr _hWnd;

    [RelayCommand]
    public void OnStopTrigger()
    {
        Stop();
    }

    private void Stop()
    {
        lock (this)
        {
            if (TaskDispatcherEnabled)
            {
                _taskDispatcher.Stop();
                TaskDispatcherEnabled = false;
                TaskContext.Instance().IsInitialized = false;
            }
        }
    }

    #region 背景图片管理

    private void InitializeBannerImage()
    {
        try
        {
            if (File.Exists(_customBannerImagePath))
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(Path.GetFullPath(_customBannerImagePath));
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                BannerImageSource = bitmap;
                _logger.LogInformation("已加载自定义背景图片");
            }
            else
            {
                BannerImageSource = new BitmapImage(new Uri(DefaultBannerImagePath, UriKind.Absolute));
                _logger.LogInformation("已加载默认背景图片");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "初始化背景图片失败，使用默认图片");
            BannerImageSource = new BitmapImage(new Uri(DefaultBannerImagePath, UriKind.Absolute));
        }
    }

    [RelayCommand]
    private void ChangeBannerImage()
    {
        try
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "选择背景图片",
                Filter = "图片文件|*.jpg;*.jpeg;*.png;*.bmp;*.gif|所有文件|*.*",
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() == true)
            {
                ResetBannerImage();

                var selectedFile = openFileDialog.FileName;
                var directory = Path.GetDirectoryName(_customBannerImagePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.Copy(selectedFile, _customBannerImagePath, true);

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(Path.GetFullPath(_customBannerImagePath));
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bitmap.EndInit();
                BannerImageSource = bitmap;

                Toast.Success("背景图片更换成功！");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更换背景图片失败");
            Toast.Error($"更换背景图片失败: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ResetBannerImage()
    {
        try
        {
            var customImageFullPath = Path.GetFullPath(_customBannerImagePath);

            var defaultBitmap = new BitmapImage();
            defaultBitmap.BeginInit();
            defaultBitmap.UriSource = new Uri(DefaultBannerImagePath, UriKind.Absolute);
            defaultBitmap.CacheOption = BitmapCacheOption.OnLoad;
            defaultBitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            defaultBitmap.EndInit();
            BannerImageSource = defaultBitmap;

            if (File.Exists(customImageFullPath))
            {
                File.Delete(customImageFullPath);
                Toast.Success("已恢复为默认背景图片！");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "恢复默认背景图片失败");
            Toast.Warning("已恢复为默认背景图片！但清除自定义图片失败，请手动删除文件。");
        }
    }

    #endregion
}

// 枚举扩展类
public class EnumItem<T> where T : Enum
{
    public T EnumValue { get; set; }
    public string EnumName { get; set; }
    public string DisplayName { get; set; }

    public EnumItem(T value, string name, string displayName)
    {
        EnumValue = value;
        EnumName = name;
        DisplayName = displayName;
    }
}

public static class EnumExtensions
{
    public static List<EnumItem<T>> ToEnumItems<T>() where T : Enum
    {
        var items = new List<EnumItem<T>>();
        foreach (var value in Enum.GetValues(typeof(T)))
        {
            var enumValue = (T)value;
            var name = enumValue.ToString();

            // 获取 Description 属性作为显示名称
            var fieldInfo = typeof(T).GetField(name);
            var descriptionAttribute = fieldInfo
                ?.GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false)
                .FirstOrDefault() as System.ComponentModel.DescriptionAttribute;

            var displayName = descriptionAttribute?.Description ?? name;
            items.Add(new EnumItem<T>(enumValue, name, displayName));
        }

        return items;
    }
}