using System;
using BetterInfinityNikki.GameTask.Common.Map.Layers;
using BetterInfinityNikki.GameTask.Common.Map.Maps.Base;
using OpenCvSharp;

namespace BetterInfinityNikki.GameTask.Common.Map.Maps;

/// <summary>
/// 无限暖暖世界地图
/// </summary>
public class NikkiWorldMap : SceneBaseMap
{
    // 地图配置常量
    /// <summary>
    /// 特征地图图像的块大小（像素）
    /// </summary>
    public new const int MapImageBlockWidth = 2048;

    /// <summary>
    /// 地图行数（用于分块索引）
    /// </summary>
    public const int GameMapRows = 8;

    /// <summary>
    /// 地图列数（用于分块索引）
    /// </summary>
    public const int GameMapCols = 8;

    /// <summary>
    /// 大地图缩放比例（截图缩小倍数）
    /// </summary>
    public const int BigMapScaleFactor = 4;

    /// <summary>
    /// 构造函数
    /// </summary>
    public NikkiWorldMap() : base(
        type: "NikkiWorld",
        mapSize: new Size(GameMapCols * MapImageBlockWidth, GameMapRows * MapImageBlockWidth),
        mapOriginInImageCoordinate: new Point2f(
            (GameMapCols / 2) * MapImageBlockWidth,  // 地图中心作为原点
            (GameMapRows / 2) * MapImageBlockWidth
        ),
        mapImageBlockWidth: MapImageBlockWidth,
        splitRow: GameMapRows * 2,
        splitCol: GameMapCols * 2
    )
    {
    }

    /// <summary>
    /// 获取大地图在游戏世界地图中的位置
    /// 使用 SIFT 特征匹配算法
    /// </summary>
    /// <param name="greyBigMapMat">灰度化的大地图截图</param>
    /// <returns>在游戏世界地图中的矩形区域</returns>
    public override Rect GetBigMapRect(Mat greyBigMapMat)
    {
        var layer = BigMapNikkiLayer.GetInstance(this);
        return layer.GetBigMapRect(greyBigMapMat);
    }

    /// <summary>
    /// 获取大地图位置（带上一帧位置的自适应搜索）
    /// </summary>
    /// <param name="greyBigMapMat">灰度化的大地图截图</param>
    /// <param name="prevRect">上一帧检测到的视口位置</param>
    /// <returns>在游戏世界地图中的矩形区域</returns>
    public override Rect GetBigMapRect(Mat greyBigMapMat, Rect prevRect)
    {
        var layer = BigMapNikkiLayer.GetInstance(this);
        return layer.GetBigMapRect(greyBigMapMat, prevRect);
    }

    /// <summary>
    /// 预热：提前加载地图特征数据
    /// </summary>
    public void WarmUp()
    {
        var layer = BigMapNikkiLayer.GetInstance(this);
        layer.WarmUp();
    }
}
