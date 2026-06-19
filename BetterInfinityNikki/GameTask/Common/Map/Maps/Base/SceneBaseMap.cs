using System;
using BetterInfinityNikki.Core.Recognition.OpenCv.FeatureMatch;
using OpenCvSharp;

namespace BetterInfinityNikki.GameTask.Common.Map.Maps.Base;

/// <summary>
/// 场景地图基类
/// 用于定义独立地图的基本结构和坐标系转换
/// </summary>
public abstract class SceneBaseMap : ISceneMap
{
    /// <summary>
    /// 地图类型标识（如 "NikkiWorld"）
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// 地图大小（像素）
    /// 当前主要用于切割特征点
    /// </summary>
    public Size MapSize { get; set; }

    /// <summary>
    /// 地图原点位置（在图像坐标系中）
    /// 用于游戏坐标和图像坐标的转换
    /// </summary>
    public Point2f MapOriginInImageCoordinate { get; set; }

    /// <summary>
    /// 特征地图图像的块大小
    /// 通常为 1024 或 2048
    /// </summary>
    public int MapImageBlockWidth { get; set; }

    /// <summary>
    /// 特征点拆分行数（用于分块索引优化）
    /// 0 表示不分块
    /// </summary>
    public int SplitRow { get; set; }

    /// <summary>
    /// 特征点拆分列数（用于分块索引优化）
    /// 0 表示不分块
    /// </summary>
    public int SplitCol { get; set; }

    /// <summary>
    /// 特征地图图像的块大小 / 1024 的值，用于坐标系转换
    /// </summary>
    private float _mapImageBlockWidthScale;

    protected void UpdateBlockWidthScale()
    {
        _mapImageBlockWidthScale = MapImageBlockWidth / 1024f;
    }

    /// <summary>
    /// SIFT 特征匹配器
    /// </summary>
    public readonly Feature2D SiftMatcher = Feature2DFactory.Get(Feature2DType.SIFT);

    // ReSharper disable once ConvertToPrimaryConstructor
    protected SceneBaseMap(string type, Size mapSize, Point2f mapOriginInImageCoordinate, 
        int mapImageBlockWidth, int splitRow, int splitCol)
    {
        Type = type;
        MapSize = mapSize;
        MapOriginInImageCoordinate = mapOriginInImageCoordinate;
        MapImageBlockWidth = mapImageBlockWidth;
        _mapImageBlockWidthScale = mapImageBlockWidth / 1024f;
        SplitRow = splitRow;
        SplitCol = splitCol;
    }

    #region 核心方法（子类必须实现）

    /// <summary>
    /// 获取大地图在游戏世界地图中的位置
    /// </summary>
    /// <param name="greyBigMapMat">灰度化的大地图截图</param>
    /// <returns>在游戏世界地图中的矩形区域</returns>
    public abstract Rect GetBigMapRect(Mat greyBigMapMat);

    /// <summary>
    /// 获取大地图位置（带上一帧位置的自适应搜索）
    /// </summary>
    /// <param name="greyBigMapMat">灰度化的大地图截图</param>
    /// <param name="prevRect">上一帧检测到的视口位置</param>
    /// <returns>在游戏世界地图中的矩形区域</returns>
    public virtual Rect GetBigMapRect(Mat greyBigMapMat, Rect prevRect)
    {
        // 默认实现：直接调用全图搜索
        // 子类可以重写以实现自适应优化
        return GetBigMapRect(greyBigMapMat);
    }

    #endregion

    #region 坐标系转换

    /// <summary>
    /// 将图像坐标转换为游戏地图坐标
    /// </summary>
    /// <param name="imageCoordinates">图像坐标（像素）</param>
    /// <returns>游戏地图坐标，如果输入无效则返回 null</returns>
    public Point2f? ConvertImageCoordinatesToGameMapCoordinates(Point2f imageCoordinates)
    {
        if (imageCoordinates.X == 0 && imageCoordinates.Y == 0)
        {
            return null;
        }

        // 游戏坐标系是 1024 级别的，当图像坐标系不是 1024 级别时需要转换
        return new Point2f(
            (MapOriginInImageCoordinate.X - imageCoordinates.X) / _mapImageBlockWidthScale,
            (MapOriginInImageCoordinate.Y - imageCoordinates.Y) / _mapImageBlockWidthScale
        );
    }

    /// <summary>
    /// 将图像坐标矩形转换为游戏地图坐标矩形
    /// </summary>
    /// <param name="rect">图像坐标矩形（像素）</param>
    /// <returns>游戏地图坐标矩形，如果输入无效则返回 null</returns>
    public Rect? ConvertImageCoordinatesToGameMapCoordinates(Rect rect)
    {
        if (rect.X == 0 && rect.Y == 0 && rect.Width == 0 && rect.Height == 0)
        {
            return null;
        }

        var center = new Point2f(rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f);
        var nullablePoint = ConvertImageCoordinatesToGameMapCoordinates(center);
        
        if (nullablePoint == null)
        {
            return null;
        }

        return new Rect(
            (int)(nullablePoint.Value.X - rect.Width / 2.0 / _mapImageBlockWidthScale),
            (int)(nullablePoint.Value.Y - rect.Height / 2.0 / _mapImageBlockWidthScale),
            (int)(rect.Width / _mapImageBlockWidthScale),
            (int)(rect.Height / _mapImageBlockWidthScale)
        );
    }

    /// <summary>
    /// 将游戏地图坐标转换为图像坐标
    /// </summary>
    /// <param name="gameCoordinates">游戏地图坐标</param>
    /// <returns>图像坐标（像素）</returns>
    public Point2f ConvertGameMapCoordinatesToImageCoordinates(Point2f gameCoordinates)
    {
        return new Point2f(
            MapOriginInImageCoordinate.X - gameCoordinates.X * _mapImageBlockWidthScale,
            MapOriginInImageCoordinate.Y - gameCoordinates.Y * _mapImageBlockWidthScale
        );
    }

    /// <summary>
    /// 将游戏地图坐标矩形转换为图像坐标矩形
    /// </summary>
    /// <param name="rect">游戏地图坐标矩形</param>
    /// <returns>图像坐标矩形（像素）</returns>
    public Rect ConvertGameMapCoordinatesToImageCoordinates(Rect rect)
    {
        var topLeft = ConvertGameMapCoordinatesToImageCoordinates(new Point2f(rect.X, rect.Y));
        var bottomRight = ConvertGameMapCoordinatesToImageCoordinates(
            new Point2f(rect.X + rect.Width, rect.Y + rect.Height));

        return new Rect(
            (int)topLeft.X,
            (int)topLeft.Y,
            (int)(bottomRight.X - topLeft.X),
            (int)(bottomRight.Y - topLeft.Y)
        );
    }

    #endregion

    #region 特征提取工具方法

    /// <summary>
    /// 提取并保存地图特征（离线预处理用）
    /// </summary>
    /// <param name="basePath">地图图片路径（不含扩展名）</param>
    protected void ExtractAndSaveFeature(string basePath)
    {
        var fileName = System.IO.Path.GetFileNameWithoutExtension(basePath);
        var folder = System.IO.Path.GetDirectoryName(basePath)!;

        string trainKeyPointsPath = System.IO.Path.Combine(folder, $"{fileName}_SIFT.kp.bin");
        string trainDescriptorsPath = System.IO.Path.Combine(folder, $"{fileName}_SIFT.mat.png");

        // 如果特征文件已存在，跳过
        if (System.IO.File.Exists(trainKeyPointsPath) && System.IO.File.Exists(trainDescriptorsPath))
        {
            return;
        }

        // 提取并保存特征
        SiftMatcher.SaveFeatures(basePath, trainKeyPointsPath, trainDescriptorsPath);
    }

    #endregion
}
