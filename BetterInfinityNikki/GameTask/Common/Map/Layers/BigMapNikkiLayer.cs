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
    /// 加载地图特征数据
    /// </summary>
    private void LoadFeatures()
    {
        try
        {
            var featuresDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Map", "NikkiWorld", "Features");

            if (!Directory.Exists(featuresDir))
            {
                _logger.LogWarning("特征数据目录不存在: {Dir}", featuresDir);
                return;
            }

            _logger.LogInformation("开始加载地图特征数据...");

            var allKeyPointsList = new List<KeyPoint>();
            var allDescriptorsList = new List<Mat>();
            var totalBlocks = 0;
            var loadedBlocks = 0;

            // 遍历所有特征块文件
            var kpFiles = Directory.GetFiles(featuresDir, "*_SIFT.kp.bin");
            totalBlocks = kpFiles.Length;

            foreach (var kpFile in kpFiles)
            {
                var blockName = Path.GetFileNameWithoutExtension(kpFile).Replace("_SIFT.kp", "");
                var descFile = Path.Combine(featuresDir, $"{blockName}_SIFT.mat.png");

                if (!File.Exists(descFile))
                {
                    _logger.LogWarning("缺少描述子文件: {File}", descFile);
                    continue;
                }

                // 加载关键点
                var keyPoints = FeatureStorageHelper.LoadKeyPointArray(kpFile);
                if (keyPoints == null || keyPoints.Length == 0)
                {
                    continue;
                }

                // 加载描述子
                using var descriptors = FeatureStorageHelper.LoadDescriptors(descFile);
                if (descriptors == null || descriptors.Empty())
                {
                    continue;
                }

                // 解析分块坐标（文件名格式: Teyvat_{row}_{col}）
                var parts = blockName.Split('_');
                if (parts.Length >= 3 && int.TryParse(parts[1], out int blockRow) && int.TryParse(parts[2], out int blockCol))
                {
                    // 将特征点坐标从分块局部坐标转换为地图全局坐标
                    // 分块大小 = FeatureBlockSize = 256
                    float offsetX = blockCol * NikkiWorldMap.FeatureBlockSize;
                    float offsetY = blockRow * NikkiWorldMap.FeatureBlockSize;
                    
                    for (int i = 0; i < keyPoints.Length; i++)
                    {
                        var kp = keyPoints[i];
                        kp.Pt = new Point2f(kp.Pt.X + offsetX, kp.Pt.Y + offsetY);
                        keyPoints[i] = kp;
                    }
                }

                // 记录全局索引偏移
                int startIndex = allKeyPointsList.Count;
                for (int i = 0; i < keyPoints.Length; i++)
                {
                    keyPoints[i].ClassId = startIndex + i;
                }

                allKeyPointsList.AddRange(keyPoints);
                allDescriptorsList.Add(descriptors.Clone());
                loadedBlocks++;
            }

            if (allKeyPointsList.Count == 0)
            {
                _logger.LogWarning("未加载到任何特征数据");
                return;
            }

            // 合并所有描述子
            _allKeyPoints = allKeyPointsList.ToArray();
            _allDescriptors = new Mat();
            Cv2.VConcat(allDescriptorsList.ToArray(), _allDescriptors);

            _logger.LogInformation(
                "特征数据加载完成: {Blocks} 个块, {Points} 个特征点, 描述子尺寸: {Rows}x{Cols}",
                loadedBlocks,
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

            // 释放临时描述子列表
            foreach (var desc in allDescriptorsList)
            {
                desc.Dispose();
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

            _logger.LogInformation("分块范围: row[{RowStart}-{RowEnd}], col[{ColStart}-{ColEnd}]",
                rowStart, rowEnd, colStart, colEnd);

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
