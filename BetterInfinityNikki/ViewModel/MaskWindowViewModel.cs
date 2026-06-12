using BetterInfinityNikki.Core.Config;
using BetterInfinityNikki.Model;
using BetterInfinityNikki.Model.MaskMap;
using BetterInfinityNikki.Service.Interface;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows;
using Wpf.Ui.Controls;

namespace BetterInfinityNikki.ViewModel;

public partial class MaskWindowViewModel : ObservableObject
{
    private readonly ILogger<MaskWindowViewModel> _logger = App.GetLogger<MaskWindowViewModel>();

    public AllConfig? Config { get; set; }

    public MaskMapPointInfoPopupViewModel PointInfoPopup { get; } = new();

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

    /// <summary>
    /// 是否处于大地图界面
    /// </summary>
    [ObservableProperty]
    private bool _isInBigMapUi;

    #region 点位选择器相关

    /// <summary>
    /// 是否打开点位选择器面板
    /// </summary>
    [ObservableProperty]
    private bool _isMapPointPickerOpen;

    /// <summary>
    /// 点位分类树（一级分类）
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<MaskMapPointLabel> _mapLabelCategories = new();

    /// <summary>
    /// 当前选中的分类（用于显示子分类列表）
    /// </summary>
    [ObservableProperty]
    private MaskMapPointLabel? _selectedCategory;

    partial void OnSelectedCategoryChanged(MaskMapPointLabel? value)
    {
        // 显示当前分类的子分类
        if (value?.Children != null)
        {
            UpdateMapLabelItems(value.Children);
        }
        else
        {
            MapLabelItems.Clear();
        }
    }

    /// <summary>
    /// 搜索关键词
    /// </summary>
    [ObservableProperty]
    private string _mapLabelSearchText = string.Empty;

    /// <summary>
    /// 右侧显示的标签列表（子分类或搜索结果）
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<MaskMapPointLabel> _mapLabelItems = new();

    /// <summary>
    /// 已选中的标签列表
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<MaskMapPointLabel> _selectedMapLabelItems = new();

    /// <summary>
    /// 是否正在加载分类树
    /// </summary>
    [ObservableProperty]
    private bool _isMapLabelTreeLoading;

    /// <summary>
    /// 是否正在加载点位
    /// </summary>
    [ObservableProperty]
    private bool _isLoadingPoints;

    private readonly IMaskMapPointService? _mapPointService;
    private CancellationTokenSource? _loadCategoriesCts;
    private CancellationTokenSource? _loadPointsCts;
    private readonly SemaphoreSlim _iconLoadSemaphore = new(10, 10);

    #endregion

    public MaskWindowViewModel()
    {
        // 获取点位服务实例
        try
        {
            _mapPointService = App.GetService<IMaskMapPointService>();
            if (_mapPointService != null)
            {
                _mapPointService.CollectedDataUpdated += OnCollectedDataUpdated;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "无法获取 IMaskMapPointService 实例");
        }
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

    #region 点位选择器命令

    /// <summary>
    /// 切换点位选择器面板开关
    /// </summary>
    [RelayCommand]
    private async Task ToggleMapPointPickerAsync()
    {
        if (!IsInBigMapUi)
        {
            IsMapPointPickerOpen = false;
            return;
        }

        IsMapPointPickerOpen = !IsMapPointPickerOpen;
        
        if (IsMapPointPickerOpen && MapLabelCategories.Count == 0)
        {
            await LoadLabelCategoriesAsync();
        }
    }

    /// <summary>
    /// 加载点位分类树
    /// </summary>
    private async Task LoadLabelCategoriesAsync()
    {
        if (_mapPointService == null)
        {
            _logger.LogWarning("IMaskMapPointService 未初始化");
            return;
        }

        // 取消之前的加载任务
        _loadCategoriesCts?.Cancel();
        _loadCategoriesCts = new CancellationTokenSource();
        var ct = _loadCategoriesCts.Token;

        try
        {
            IsMapLabelTreeLoading = true;
            
            var categories = await _mapPointService.GetLabelCategoriesAsync(ct);
            
            if (ct.IsCancellationRequested)
            {
                return;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                MapLabelCategories.Clear();
                foreach (var category in categories)
                {
                    MapLabelCategories.Add(category);
                }
            });

            _logger.LogDebug("成功加载 {Count} 个点位分类", categories.Count);
        }
        catch (OperationCanceledException)
        {
            // 忽略取消异常
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "加载点位分类失败");
        }
        finally
        {
            IsMapLabelTreeLoading = false;
        }
    }

    /// <summary>
    /// 选中/取消选中分类
    /// </summary>
    [RelayCommand]
    private async Task SelectCategoryAsync(MaskMapPointLabel? category)
    {
        SelectedCategory = category;
        
        // 显示当前分类的子分类
        if (category?.Children != null)
        {
            UpdateMapLabelItems(category.Children);
        }
        else
        {
            MapLabelItems.Clear();
        }
        await Task.CompletedTask;
    }

    /// <summary>
    /// 选中/取消选中具体标签
    /// </summary>
    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task SelectLabelItemAsync(MaskMapPointLabel? label)
    {
        if (label == null) return;

        var existing = SelectedMapLabelItems.FirstOrDefault(x => x.LabelId == label.LabelId);

        if (existing != null)
        {
            SelectedMapLabelItems.Remove(existing);
        }
        else
        {
            SelectedMapLabelItems.Add(label);
            _ = LoadIconAsync(label, CancellationToken.None);
        }

        SyncMapPointLabels();
        await RefreshSelectedMapPointsAsync();
    }

    /// <summary>
    /// 搜索标签
    /// </summary>
    [RelayCommand]
    private async Task SearchLabelsAsync()
    {
        if (string.IsNullOrWhiteSpace(MapLabelSearchText))
        {
            // 清空搜索时，显示当前选中分类的子分类
            if (SelectedCategory?.Children != null)
            {
                UpdateMapLabelItems(SelectedCategory.Children);
            }
            else
            {
                MapLabelItems.Clear();
            }
            return;
        }

        // 在所有分类中搜索
        var searchText = MapLabelSearchText.ToLower();
        var results = new List<MaskMapPointLabel>();

        foreach (var category in MapLabelCategories)
        {
            if (category.Children != null)
            {
                foreach (var child in category.Children)
                {
                    if (child.Name.ToLower().Contains(searchText))
                    {
                        results.Add(child);
                    }
                }
            }
        }

        UpdateMapLabelItems(results);
        await Task.CompletedTask;
    }

    /// <summary>
    /// 清除所有选中标签
    /// </summary>
    [RelayCommand]
    private async Task ClearSelectedLabelsAsync()
    {
        SelectedMapLabelItems.Clear();
        MapPoints = new ObservableCollection<MaskMapPoint>();
        MapPointLabels = new ObservableCollection<MaskMapPointLabel>();
        await Task.CompletedTask;
    }

    /// <summary>
    /// 重置已选标签（与 ClearSelectedLabels 相同，为了兼容 BGI 的命令名）
    /// </summary>
    [RelayCommand]
    private async Task ResetSelectedMapLabelSelectionAsync()
    {
        await ClearSelectedLabelsAsync();
    }

    /// <summary>
    /// 刷新已选标签对应的点位
    /// </summary>
    private async Task RefreshSelectedMapPointsAsync()
    {
        if (_mapPointService == null || SelectedMapLabelItems.Count == 0)
        {
            MapPoints = new ObservableCollection<MaskMapPoint>();
            return;
        }

        _loadPointsCts?.Cancel();
        _loadPointsCts = new CancellationTokenSource();
        var ct = _loadPointsCts.Token;

        try
        {
            IsLoadingPoints = true;

            var selectedLabels = SelectedMapLabelItems.ToList();
            var result = await _mapPointService.GetPointsAsync(selectedLabels, ct);

            if (ct.IsCancellationRequested)
            {
                return;
            }

            var newCollection = new ObservableCollection<MaskMapPoint>(result.Points);
            MapPoints = newCollection;

            _logger.LogDebug("成功加载 {Count} 个点位", result.Points.Count);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "加载点位失败");
        }
        finally
        {
            IsLoadingPoints = false;
        }
    }

    /// <summary>
    /// 更新右侧标签列表（并批量加载图标）
    /// </summary>
    private void UpdateMapLabelItems(IEnumerable<MaskMapPointLabel> labels)
    {
        var snapshot = labels.Where(x => x.PointCount > 0).ToList();

        Application.Current.Dispatcher.Invoke(() =>
        {
            MapLabelItems.Clear();
            foreach (var label in snapshot)
            {
                MapLabelItems.Add(label);
            }
        });

        foreach (var label in snapshot)
        {
            _ = LoadIconAsync(label, CancellationToken.None);
        }
    }

    private void SyncMapPointLabels()
    {
        MapPointLabels = new ObservableCollection<MaskMapPointLabel>(SelectedMapLabelItems);
    }

    /// <summary>
    /// 异步加载图标（带并发控制）
    /// </summary>
    private async Task LoadIconAsync(MaskMapPointLabel label, CancellationToken ct)
    {
        if (label.IconImage != null || string.IsNullOrEmpty(label.IconUrl))
        {
            return;
        }

        try
        {
            await _iconLoadSemaphore.WaitAsync(ct);
            try
            {
                var image = await MapIconImageCache.GetAsync(label.IconUrl, ct);
                
                if (image != null)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        label.IconImage = image;
                    }, System.Windows.Threading.DispatcherPriority.Background);
                }
            }
            finally
            {
                _iconLoadSemaphore.Release();
            }
        }
        catch (OperationCanceledException)
        {
            // 忽略取消异常
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "加载图标失败: {IconUrl}", label.IconUrl);
        }
    }

    #endregion

    partial void OnIsInBigMapUiChanged(bool value)
    {
        if (!value)
        {
            PointInfoPopup.Close();
        }
    }

    [RelayCommand]
    private async Task OnPointClick(MaskMapPointClickArgs? args)
    {
        var point = args?.Point;
        if (point == null || !IsInBigMapUi) return;

        var title = ResolvePointTitle(point);
        await PointInfoPopup.ShowAsync(point, args!.AnchorPosition, title);
    }

    private string ResolvePointTitle(MaskMapPoint point)
    {
        var label = MapPointLabels.FirstOrDefault(l => l.LabelId == point.LabelId);
        return label?.Name ?? $"点位 {point.Id}";
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

    private void OnCollectedDataUpdated(object? sender, EventArgs e)
    {
        Application.Current.Dispatcher.BeginInvoke(async () =>
        {
            await RefreshSelectedMapPointsAsync();
        });
    }

    public void Cleanup()
    {
        if (_mapPointService != null)
        {
            _mapPointService.CollectedDataUpdated -= OnCollectedDataUpdated;
        }
    }
}
