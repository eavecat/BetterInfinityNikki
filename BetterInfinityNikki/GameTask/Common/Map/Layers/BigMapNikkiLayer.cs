using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using BetterInfinityNikki.Core.Config;
using BetterInfinityNikki.Core.Recognition.OpenCv.FeatureMatch;
using BetterInfinityNikki.Core.Recognition.OpenCv.Model;
using BetterInfinityNikki.GameTask.Common.Map.Maps;
using BetterInfinityNikki.GameTask.Common.Map.Maps.Base;
using BetterInfinityNikki.Model;
using Microsoft.Extensions.Logging;
using OpenCvSharp;

namespace BetterInfinityNikki.GameTask.Common.Map.Layers;

/// <summary>
/// 无限暖暖大地图图层
/// 负责大地图视口的 SIFT 特征匹配检测
/// </summary>
public class BigMapNikkiLayer
{
    private static readonly object Lock = new();
    private static BigMapNikkiLayer? _instance;

    private readonly ILogger<BigMapNikkiLayer> _logger;
    private readonly MapMaskConfig _config;
    private readonly NikkiWorldMap _worldMap;

    /// <summary>
    /// 所有关键点（完整列表）
    /// </summary>
    private KeyPoint[]? _allKeyPoints;

    /// <summary>
    /// 所有描述子（完整矩阵）
    /// </summary>
    private Mat? _allDescriptors;

    /// <summary>
    /// 分块特征（用于自适应搜索优化）
    /// </summary>
    private KeyPointFeatureBlock[][]? _splitBlocks;

    /// <summary>
    /// 是否已加载特征数据
    /// </summary>
    private bool _isLoaded;

    /// <summary>
    /// 加载状态（用于防止重复加载）
    /// </summary>
    private int _loading;

    private string _currentMapKey = "NikkiWorld";
    private MapFeatureConfig? _currentFeatureConfig;

    private BigMapNikkiLayer(NikkiWorldMap worldMap)
    {
        _logger = App.GetLogger<BigMapNikkiLayer>();
        _config = TaskContext.Instance().Config.MapMaskConfig;
        _worldMap = worldMap;
    }

    /// <summary>
    /// 获取单例实例
    /// </summary>
    public static BigMapNikkiLayer GetInstance(NikkiWorldMap worldMap)
    {
        if (_instance == null)
        {
            lock (Lock)
            {
                _instance ??= new BigMapNikkiLayer(worldMap);
            }
        }

        return _instance;
    }

    /// <summary>
    /// 切换地图特征数据
    /// </summary>
    public void SwitchMap(string mapKey, MapFeatureConfig? featureConfig)
    {
        if (_currentMapKey == mapKey && _isLoaded)
            return;

        _logger.LogInformation("切换地图特征: {OldKey} -> {NewKey}", _currentMapKey, mapKey);
        _currentMapKey = mapKey;
        _currentFeatureConfig = featureConfig;

        // 更新 NikkiWorldMap 配置
        if (featureConfig != null)
        {
            _worldMap.UpdateConfig(featureConfig);
        }

        // 清除旧特征，触发重新加载
        ClearFeatures();
    }

    private void ClearFeatures()
    {
        _allDescriptors?.Dispose();
        _allDescriptors = null;
        _allKeyPoints = null;
        _splitBlocks = null;
        _isLoaded = false;
    }

    private void EnsureLoaded()
    {
        if (_isLoaded)
        {
            return;
        }

        if (Interlocked.Exchange(ref _loading, 1) == 0)
        {
            try
            {
                LoadFeatures();
                _isLoaded = true;
            }
            finally
            {
                Interlocked.Exchange(ref _loading, 0);
            }
        }
        else
        {
            // 等待其他线程加载完成
            while (!_isLoaded)
            {
                Thread.Sleep(10);
            }
        }
    }

    private void LoadFeatures()
    {
        try
        {
            var featuresDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Map", _currentMapKey);

            if (!Directory.Exists(featuresDir))
            {
                _logger.LogWarning("特征数据目录不存在: {Dir}", featuresDir);
                return;
            }

            _logger.LogInformation("开始加载地图特征数据: {MapKey}...", _currentMapKey);

            // 查找单个合并的特征文件
            var kpFiles = Directory.GetFiles(featuresDir, "*_SIFT.kp.bin");
            if (kpFiles.Length == 0)
            {
                _logger.LogWarning("未找到特征点文件");
                return;
            }

            // 取第一个（单文件模式下应该只有一个）
            var kpFilePath = kpFiles[0];
            var blockName = Path.GetFileNameWithoutExtension(kpFilePath).Replace("_SIFT.kp", "");
            var descFilePath = Path.Combine(featuresDir, $"{blockName}_SIFT.mat.png");

            if (!File.Exists(descFilePath))
            {
                _logger.LogWarning("缺少描述子文件: {File}", descFilePath);
                return;
            }

            // 加载关键点
            var keyPoints = FeatureStorageHelper.LoadKeyPointArray(kpFilePath);
            if (keyPoints == null || keyPoints.Length == 0)
            {
                _logger.LogWarning("特征点文件为空");
                return;
            }

            // 加载描述子（灰度 → CV_32FC1）
            var descriptors = FeatureStorageHelper.LoadDescriptorMat(descFilePath);
            if (descriptors == null || descriptors.Empty())
            {
                _logger.LogWarning("描述子文件为空");
                return;
            }

            _allKeyPoints = keyPoints;
            _allDescriptors = descriptors;

            _logger.LogInformation(
                "特征数据加载完成: {MapKey}, {Points} 个特征点, 描述子尺寸: {Rows}x{Cols}",
                _currentMapKey,
                _allKeyPoints.Length,
                _allDescriptors.Rows,
                _allDescriptors.Cols
            );

            // 构建分块索引（用于自适应搜索）
            if (_config.EnableAdaptiveSearch)
            {
                _logger.LogInformation("构建分块索引...");
                _splitBlocks = KeyPointFeatureBlockHelper.SplitFeatures(
                    _worldMap.MapSize,
                    _worldMap.SplitRow,
                    _worldMap.SplitCol,
                    _allKeyPoints,
                    _allDescriptors
                );
                _logger.LogInformation("分块索引构建完成: {Rows}x{Cols}", _worldMap.SplitRow, _worldMap.SplitCol);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载地图特征数据失败");
        }
    }

    /// <summary>
    /// 获取大地图在游戏世界地图中的位置
    /// </summary>
    /// <param name="greyBigMapMat">灰度化的大地图截图</param>
    /// <returns>在游戏世界地图中的矩形区域</returns>
    public Rect GetBigMapRect(Mat greyBigMapMat)
    {
        EnsureLoaded();

        if (_allKeyPoints == null || _allDescriptors == null)
        {
            _logger.LogWarning("特征数据未加载，无法检测大地图位置");
            return default;
        }

        try
        {
            using var scaledMat = new Mat();
            Cv2.Resize(greyBigMapMat, scaledMat, new Size(),
                1.0 / _worldMap.BigMapScaleFactor,
                1.0 / _worldMap.BigMapScaleFactor,
                InterpolationFlags.Area);

            // KNN 匹配获取矩形
            var sift = Feature2DFactory.Get(Feature2DType.SIFT);
            var resultRect = sift.KnnMatchRect(_allKeyPoints, _allDescriptors, scaledMat);

            if (resultRect == default)
            {
                return default;
            }

            return resultRect;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "大地图检测异常");
            return default;
        }
    }

    public Rect GetBigMapRectNoScale(Mat greyMat)
    {
        EnsureLoaded();

        if (_allKeyPoints == null || _allDescriptors == null)
        {
            return default;
        }

        try
        {
            var sift = Feature2DFactory.Get(Feature2DType.SIFT);
            var resultRect = sift.KnnMatchRect(_allKeyPoints, _allDescriptors, greyMat);
            return resultRect;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "不缩放匹配异常");
            return default;
        }
    }

    public Point2f GetMiniMapCenter(Mat greyMaskedMat)
    {
        EnsureLoaded();

        if (_allKeyPoints == null || _allDescriptors == null)
        {
            return default;
        }

        try
        {
            var sift = Feature2DFactory.Get(Feature2DType.SIFT);
            return sift.KnnMatchCenterRelaxed(_allKeyPoints, _allDescriptors, greyMaskedMat, scaleFactor: 3.0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "小地图匹配异常");
            return default;
        }
    }

    public Rect GetBigMapRect(Mat greyBigMapMat, Rect prevRect)
    {
        if (!_config.EnableAdaptiveSearch || _splitBlocks == null)
        {
            return GetBigMapRect(greyBigMapMat);
        }

        if (prevRect.Width <= 0 || prevRect.Height <= 0)
        {
            return GetBigMapRect(greyBigMapMat);
        }

        EnsureLoaded();

        if (_allKeyPoints == null || _allDescriptors == null)
        {
            return default;
        }

        try
        {
            using var scaledMat = new Mat();
            Cv2.Resize(greyBigMapMat, scaledMat, new Size(),
                1.0 / _worldMap.BigMapScaleFactor,
                1.0 / _worldMap.BigMapScaleFactor,
                InterpolationFlags.Area);

            var (rowStart, rowEnd, colStart, colEnd) = KeyPointFeatureBlockHelper.GetCellRange(
                _worldMap.MapSize,
                _worldMap.SplitRow,
                _worldMap.SplitCol,
                prevRect
            );

            int expandBlocks = _config.AdaptiveSearchExpandBlocks;
            rowStart = Math.Max(rowStart - expandBlocks, 0);
            rowEnd = Math.Min(rowEnd + expandBlocks, _worldMap.SplitRow - 1);
            colStart = Math.Max(colStart - expandBlocks, 0);
            colEnd = Math.Min(colEnd + expandBlocks, _worldMap.SplitCol - 1);

            var searchBlock = KeyPointFeatureBlockHelper.MergeFeaturesInRange(
                _splitBlocks,
                _allDescriptors,
                rowStart,
                rowEnd,
                colStart,
                colEnd
            );

            if (searchBlock.Descriptor == null || searchBlock.Descriptor.Empty())
            {
                _logger.LogDebug("搜索范围内无特征数据，使用全图搜索");
                return GetBigMapRect(greyBigMapMat);
            }

            var sift = Feature2DFactory.Get(Feature2DType.SIFT);
            var resultRect = sift.KnnMatchRect(
                searchBlock.KeyPointList.ToArray(),
                searchBlock.Descriptor,
                scaledMat
            );

            if (resultRect == default)
            {
                return default;
            }

            _logger.LogDebug("自适应搜索成功: {Rect}, 搜索范围: [{RowStart}-{RowEnd}, {ColStart}-{ColEnd}]",
                resultRect, rowStart, rowEnd, colStart, colEnd);
            return resultRect;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "自适应搜索异常");
            return default;
        }
    }

    public void WarmUp()
    {
        EnsureLoaded();
    }

    public void Dispose()
    {
        ClearFeatures();

        if (_instance != null)
        {
            lock (Lock)
            {
                _instance = null;
            }
        }
    }
}
