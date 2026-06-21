using BetterInfinityNikki.Core.Config;
using BetterInfinityNikki.Helpers.Ui;
using BetterInfinityNikki.Service.Interface;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using Wpf.Ui;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace BetterInfinityNikki.ViewModel;

public partial class MainWindowViewModel : ObservableObject, IViewModel
{
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly IConfigService _configService;
    private readonly INavigationService _navigationService;
    
    public string Title => $"BetterIN · 更好的无限暖暖 · {Global.Version}";

    [ObservableProperty] 
    private bool _isVisible = true;

    [ObservableProperty] 
    private WindowState _windowState = WindowState.Normal;

    [ObservableProperty] 
    private WindowBackdropType _currentBackdropType = WindowBackdropType.Auto;

    public AllConfig Config { get; set; }

    public MainWindowViewModel(INavigationService navigationService, IConfigService configService)
    {
        _navigationService = navigationService;
        _configService = configService;
        Config = _configService.Get();
        _logger = App.GetLogger<MainWindowViewModel>();
    }

    [RelayCommand]
    private void OnHide()
    {
        IsVisible = false;
    }

    [RelayCommand]
    private void OnClosing(CancelEventArgs e)
    {
        if (Config.CommonConfig.ExitToTray)
        {
            e.Cancel = true;
            OnHide();
        }
    }

    [RelayCommand]
    private async Task OnLoaded()
    {
        _logger.LogInformation("主窗口加载完成");

        // 应用上次保存的主题
        ApplyTheme(Config.CommonConfig.CurrentThemeType);

        // 启动时自动检查更新
        try
        {
            var updateService = App.GetService<IUpdateService>();
            if (updateService != null)
            {
                await updateService.CheckUpdateAsync(new BetterInfinityNikki.Model.UpdateOption
                {
                    Trigger = BetterInfinityNikki.Model.UpdateTrigger.Auto
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "启动时检查更新失败");
        }
    }

    /// <summary>
    /// 手动触发检查更新（可由设置页面或菜单调用）
    /// </summary>
    [RelayCommand]
    private async Task CheckUpdateManual()
    {
        var updateService = App.GetService<IUpdateService>();
        if (updateService != null)
        {
            await updateService.CheckUpdateAsync(new BetterInfinityNikki.Model.UpdateOption
            {
                Trigger = BetterInfinityNikki.Model.UpdateTrigger.Manual
            });
        }
    }

    /// <summary>
    /// 切换主题（深浅色 + 背景效果）
    /// </summary>
    [RelayCommand]
    private void OnSwitchBackdrop()
    {
        var currentTheme = Config.CommonConfig.CurrentThemeType;
        ThemeType newTheme;

        // 循环切换：DarkNone -> DarkMica -> LightNone -> LightMica -> DarkAcrylic -> LightAcrylic -> DarkNone
        switch (currentTheme)
        {
            case ThemeType.DarkNone:
                newTheme = ThemeType.DarkMica;
                break;
            case ThemeType.DarkMica:
                newTheme = ThemeType.LightNone;
                break;
            case ThemeType.LightNone:
                newTheme = ThemeType.LightMica;
                break;
            case ThemeType.LightMica:
                newTheme = ThemeType.DarkAcrylic;
                break;
            case ThemeType.DarkAcrylic:
                newTheme = ThemeType.LightAcrylic;
                break;
            case ThemeType.LightAcrylic:
            default:
                newTheme = ThemeType.DarkNone;
                break;
        }

        Config.CommonConfig.CurrentThemeType = newTheme;
        ApplyTheme(newTheme);
        _logger.LogInformation("切换主题至: {Theme}", newTheme);
    }

    /// <summary>
    /// 应用主题到应用程序
    /// </summary>
    private void ApplyTheme(ThemeType themeType)
    {
        // 切换深浅色
        switch (themeType)
        {
            case ThemeType.DarkNone:
            case ThemeType.DarkMica:
            case ThemeType.DarkAcrylic:
                ApplicationThemeManager.Apply(ApplicationTheme.Dark);
                break;
            case ThemeType.LightNone:
            case ThemeType.LightMica:
            case ThemeType.LightAcrylic:
                ApplicationThemeManager.Apply(ApplicationTheme.Light);
                break;
        }

        // 立即应用主题到当前窗口（包括背景类型）
        if (Application.Current.MainWindow != null)
        {
            WindowHelper.ApplyThemeToWindow(Application.Current.MainWindow, themeType);
        }
    }
}
