using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace BetterInfinityNikki.Core.Config;

/// <summary>
/// 地图遮罩配置
/// </summary>
[Serializable]
public partial class MapMaskConfig : ObservableObject
{
    /// <summary>
    /// 是否启用地图遮罩功能
    /// </summary>
    [ObservableProperty]
    private bool _enabled = false;

    /// <summary>
    /// 小地图遮罩是否启用
    /// </summary>
    [ObservableProperty]
    private bool _miniMapMaskEnabled = false;

    /// <summary>
    /// 自动记录路径功能是否启用
    /// </summary>
    [ObservableProperty]
    private bool _pathAutoRecordEnabled = false;

    /// <summary>
    /// 地图匹配方法
    /// 可选值: "SIFT", "TemplateMatch"
    /// </summary>
    [ObservableProperty]
    private string _mapMatchingMethod = "SIFT";

    /// <summary>
    /// SIFT 匹配的置信度阈值（0.0 - 1.0）
    /// 越高越严格，误匹配越少，但可能漏检
    /// </summary>
    [ObservableProperty]
    private double _siftMatchThreshold = 0.75;

    /// <summary>
    /// 大地图检测的 ROI 区域比例
    /// 相对于屏幕宽度的比例（0.0 - 1.0）
    /// </summary>
    [ObservableProperty]
    private double _bigMapDetectionRoiWidthRatio = 1.0;

    /// <summary>
    /// 大地图检测的 ROI 区域比例
    /// 相对于屏幕高度的比例（0.0 - 1.0）
    /// </summary>
    [ObservableProperty]
    private double _bigMapDetectionRoiHeightRatio = 1.0;

    /// <summary>
    /// 是否启用自适应搜索优化
    /// 基于上一帧位置缩小搜索范围，提升性能
    /// </summary>
    [ObservableProperty]
    private bool _enableAdaptiveSearch = true;

    /// <summary>
    /// 自适应搜索的扩展块数
    /// 值越大搜索范围越大，但性能开销也越大
    /// </summary>
    [ObservableProperty]
    private int _adaptiveSearchExpandBlocks = 1;

    /// <summary>
    /// 是否缓存地图特征数据
    /// 开启后首次加载较慢，但后续匹配更快
    /// </summary>
    [ObservableProperty]
    private bool _cacheMapFeatures = true;

    /// <summary>
    /// 地图预热开关
    /// 游戏启动时预加载地图特征数据，避免首次检测卡顿
    /// </summary>
    [ObservableProperty]
    private bool _warmUpOnStartup = false;

    /// <summary>
    /// 调试模式
    /// 开启后会绘制更多的调试信息
    /// </summary>
    [ObservableProperty]
    private bool _debugMode = false;
}
