using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using BetterInfinityNikki.Core.Config;
using BetterInfinityNikki.Core.Recognition.OpenCv.FeatureMatch;
using BetterInfinityNikki.Core.Recognition.OpenCv.Model;
using BetterInfinityNikki.GameTask.Common.Map.Maps;
using BetterInfinityNikki.GameTask.Common.Map.Maps.Base;
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
    /// 懒加载特征数据
    /// </summary>
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

    /// <summary>
    /// 加载地图特征数据（从单个合并的特征文件）
    /// </summary>
    private void LoadFeatures()
    {
        try
        {
            var featuresDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Map", "NikkiWorld");

            if (!Directory.Exists(featuresDir))
            {
                _logger.LogWarning("特征数据目录不存在: {Dir}", featuresDir);
                return;
            }

            _logger.LogInformation("开始加载地图特征数据...");

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
                "特征数据加载完成: {Points} 个特征点, 描述子尺寸: {Rows}x{Cols}",
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
            // 缩小截图以加快 SIFT 特征提取速度
            // 注意：homography 输出的坐标已经是训练地图坐标（16384 空间），
            // 因为 KnnMatchCorners 中的透视变换直接映射到训练特征坐标
            using var scaledMat = new Mat();
            Cv2.Resize(greyBigMapMat, scaledMat, new Size(), 
                1.0 / NikkiWorldMap.BigMapScaleFactor, 
                1.0 / NikkiWorldMap.BigMapScaleFactor,
                InterpolationFlags.Area);

            // KNN 匹配获取矩形
            var sift = Feature2DFactory.Get(Feature2DType.SIFT);
            var resultRect = sift.KnnMatchRect(_allKeyPoints, _allDescriptors, scaledMat);

            if (resultRect == default)
            {
                // _logger.LogDebug("特征匹配失败，未找到有效匹配");
                return default;
            }

            // _logger.LogDebug("大地图检测成功: {Rect}", resultRect);
            return resultRect;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "大地图检测异常");
            return default;
        }
    }

    /// <summary>
    /// 不缩放地进行 SIFT 匹配（适用于小图像，如小地图）
    /// </summary>
    /// <param name="greyMat">灰度图像（原始尺寸，不缩小）</param>
    /// <returns>在地图坐标系中的矩形区域</returns>
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

    /// <summary>
    /// 获取小地图中心点（放宽匹配阈值，上采样后匹配）
    /// </summary>
    /// <param name="greyMaskedMat">灰度化并已应用圆形mask的小地图图像</param>
    /// <returns>小地图中心在世界地图坐标系中的位置</returns>
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

    /// <summary>
    /// 获取大地图位置（带上一帧位置的自适应搜索）
    /// </summary>
    /// <param name="greyBigMapMat">灰度化的大地图截图</param>
    /// <param name="prevRect">上一帧检测到的视口位置</param>
    /// <returns>在游戏世界地图中的矩形区域</returns>
    public Rect GetBigMapRect(Mat greyBigMapMat, Rect prevRect)
    {
        // 如果自适应搜索未启用或无分块索引，使用全图搜索
        if (!_config.EnableAdaptiveSearch || _splitBlocks == null)
        {
            return GetBigMapRect(greyBigMapMat);
        }

        // 如果 prevRect 无效（默认值），使用全图搜索
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
            // 缩小截图以加快 SIFT 特征提取速度
            using var scaledMat = new Mat();
            Cv2.Resize(greyBigMapMat, scaledMat, new Size(),
                1.0 / NikkiWorldMap.BigMapScaleFactor,
                1.0 / NikkiWorldMap.BigMapScaleFactor,
                InterpolationFlags.Area);

            // _logger.LogDebug("自适应搜索: prevRect={PrevRect}, 地图尺寸={MapSize}, scaledMat={ScaledMat}",
                // prevRect, _worldMap.MapSize, new Size(scaledMat.Cols, scaledMat.Rows));

            // prevRect 已经是地图坐标（16384 空间），直接用于分块定位
            var (rowStart, rowEnd, colStart, colEnd) = KeyPointFeatureBlockHelper.GetCellRange(
                _worldMap.MapSize,
                _worldMap.SplitRow,
                _worldMap.SplitCol,
                prevRect
            );

            // _logger.LogInformation("分块范围: row[{RowStart}-{RowEnd}], col[{ColStart}-{ColEnd}]",
                // rowStart, rowEnd, colStart, colEnd);

            // 扩展搜索范围
            int expandBlocks = _config.AdaptiveSearchExpandBlocks;
            rowStart = Math.Max(rowStart - expandBlocks, 0);
            rowEnd = Math.Min(rowEnd + expandBlocks, _worldMap.SplitRow - 1);
            colStart = Math.Max(colStart - expandBlocks, 0);
            colEnd = Math.Min(colEnd + expandBlocks, _worldMap.SplitCol - 1);

            // 合并指定范围的特征
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

            // KNN 匹配（输出已经是地图坐标）
            var sift = Feature2DFactory.Get(Feature2DType.SIFT);
            var resultRect = sift.KnnMatchRect(
                searchBlock.KeyPointList.ToArray(),
                searchBlock.Descriptor,
                scaledMat
            );

            if (resultRect == default)
            {
                // _logger.LogDebug("自适应搜索失败，未找到有效匹配");
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

    /// <summary>
    /// 预热：提前加载特征数据
    /// </summary>
    public void WarmUp()
    {
        EnsureLoaded();
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _allDescriptors?.Dispose();
        _allDescriptors = null;
        _allKeyPoints = null;
        _splitBlocks = null;
        _isLoaded = false;

        if (_instance != null)
        {
            lock (Lock)
            {
                _instance = null;
            }
        }
    }
}
