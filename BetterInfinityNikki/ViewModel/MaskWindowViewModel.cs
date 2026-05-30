using BetterInfinityNikki.Core.Config;
using BetterInfinityNikki.GameTask;
using BetterInfinityNikki.Model;
using BetterInfinityNikki.Model.MaskMap;
using BetterInfinityNikki.Service.Interface;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using Wpf.Ui.Controls;

namespace BetterInfinityNikki.ViewModel;

public partial class MaskWindowViewModel : ObservableObject
{
    private readonly ILogger<MaskWindowViewModel> _logger = App.GetLogger<MaskWindowViewModel>();

    public AllConfig? Config { get; set; }

    [ObservableProperty]
    private ObservableCollection<StatusItem> _statusList = [];

    [ObservableProperty]
    private double _maskWindowWidth;

    [ObservableProperty]
    private double _maskWindowHeight;

    // 状态栏位置和大小（绑定到UI）
    [ObservableProperty]
    private double _statusLeft;

    [ObservableProperty]
    private double _statusTop;

    [ObservableProperty]
    private double _statusWidth;

    [ObservableProperty]
    private double _statusHeight;

    // 日志框位置和大小（绑定到UI）
    [ObservableProperty]
    private double _logLeft;

    [ObservableProperty]
    private double _logTop;

    [ObservableProperty]
    private double _logWidth;

    [ObservableProperty]
    private double _logHeight;

    /// <summary>
    /// 地图点位列表
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<MaskMapPoint> _mapPoints = new();

    /// <summary>
    /// 地图点位标签列表
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<MaskMapPointLabel> _mapPointLabels = new();

    public MaskWindowViewModel()
    {
    }

    private void InitializeStatusList()
    {
        if (Config != null)
        {
            // 清空旧的状态列表，避免重复
            StatusList.Clear();
            
            // 添加自动拾取状态（与 AutoPickConfig.Enabled 绑定）
            var autoPickItem = new StatusItem("拾取", SymbolRegular.HandWave24, Config.AutoPickConfig, "Enabled");
            StatusList.Add(autoPickItem);

            // 添加自动剧情状态（与 AutoSkipConfig.Enabled 绑定）
            var autoSkipItem = new StatusItem("剧情", SymbolRegular.Chat24, Config.AutoSkipConfig, "Enabled");
            StatusList.Add(autoSkipItem);

            // 添加自动钓鱼状态（与 AutoFishingConfig.Enabled 绑定）
            var autoFishingItem = new StatusItem("钓鱼", SymbolRegular.FoodFish24, Config.AutoFishingConfig, "Enabled");
            StatusList.Add(autoFishingItem);
        }
    }

    [RelayCommand]
    private void OnLoaded()
    {
        RefreshSettings();
        InitializeStatusList();
        
        // 延迟更新布局位置，等待窗口尺寸确定
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            if (MaskWindowWidth > 0 && MaskWindowHeight > 0)
            {
                UpdateLayoutPositions();
            }
        }, System.Windows.Threading.DispatcherPriority.Loaded);
    }

    [RelayCommand]
    private void OnWindowSizeChanged(SizeChangedEventArgs args)
    {
        MaskWindowWidth = args.NewSize.Width;
        MaskWindowHeight = args.NewSize.Height;
        UpdateLayoutPositions();
    }

    private void RefreshSettings()
    {
        InitConfig();
        if (Config != null)
        {
            OnPropertyChanged(nameof(Config));
        }
    }

    /// <summary>
    /// 这个窗口比较特殊，无法直接使用构造函数依赖注入
    /// </summary>
    private void InitConfig()
    {
        if (Config == null)
        {
            var configService = App.GetService<IConfigService>();
            if (configService != null)
            {
                Config = configService.Get();
            }
        }
    }

    private void UpdateLayoutPositions()
    {
        if (Config == null || MaskWindowWidth <= 0 || MaskWindowHeight <= 0)
        {
            return;
        }

        var maskConfig = Config.MaskWindowConfig;

        // 更新状态栏位置
        StatusLeft = maskConfig.StatusListLeftRatio * MaskWindowWidth;
        StatusTop = maskConfig.StatusListTopRatio * MaskWindowHeight;
        StatusWidth = maskConfig.StatusListWidthRatio * MaskWindowWidth;
        StatusHeight = maskConfig.StatusListHeightRatio * MaskWindowHeight;

        // 更新日志框位置
        LogLeft = maskConfig.LogTextBoxLeftRatio * MaskWindowWidth;
        LogTop = maskConfig.LogTextBoxTopRatio * MaskWindowHeight;
        LogWidth = maskConfig.LogTextBoxWidthRatio * MaskWindowWidth;
        LogHeight = maskConfig.LogTextBoxHeightRatio * MaskWindowHeight;
    }
    
    /// <summary>
    /// 公开方法，用于从代码后置调用
    /// </summary>
    public void UpdateLayoutPositionsPublic()
    {
        UpdateLayoutPositions();
    }
}
