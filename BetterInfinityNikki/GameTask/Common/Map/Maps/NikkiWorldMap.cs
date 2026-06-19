using System;
using BetterInfinityNikki.GameTask.Common.Map.Layers;
using BetterInfinityNikki.GameTask.Common.Map.Maps.Base;
using BetterInfinityNikki.Model;
using OpenCvSharp;

namespace BetterInfinityNikki.GameTask.Common.Map.Maps;

/// <summary>
/// 无限暖暖世界地图
/// </summary>
public class NikkiWorldMap : SceneBaseMap
{
    /// <summary>
    /// 大地图截图缩放比例
    /// </summary>
    public int BigMapScaleFactor { get; private set; } = 4;

    /// <summary>
    /// 构造函数（使用默认 NikkiWorld 配置）
    /// </summary>
    public NikkiWorldMap() : base(
        type: "NikkiWorld",
        mapSize: new Size(16384, 16384),
        mapOriginInImageCoordinate: new Point2f(8192, 8192),
        mapImageBlockWidth: 2048,
        splitRow: 16,
        splitCol: 16
    )
    {
    }

    /// <summary>
    /// 根据 MapFeatureConfig 动态更新地图参数
    /// </summary>
    public void UpdateConfig(MapFeatureConfig config)
    {
        MapSize = new Size(config.ImageWidth, config.ImageHeight);
        MapOriginInImageCoordinate = new Point2f(config.OriginX, config.OriginY);
        BigMapScaleFactor = config.BigMapScaleFactor;
        SplitRow = config.SplitRow;
        SplitCol = config.SplitCol;

        MapImageBlockWidth = config.ImageWidth / (config.SplitCol / 2);
        UpdateBlockWidthScale();
    }

    public override Rect GetBigMapRect(Mat greyBigMapMat)
    {
        var layer = BigMapNikkiLayer.GetInstance(this);
        return layer.GetBigMapRect(greyBigMapMat);
    }

    public override Rect GetBigMapRect(Mat greyBigMapMat, Rect prevRect)
    {
        var layer = BigMapNikkiLayer.GetInstance(this);
        return layer.GetBigMapRect(greyBigMapMat, prevRect);
    }

    public void WarmUp()
    {
        var layer = BigMapNikkiLayer.GetInstance(this);
        layer.WarmUp();
    }
}
