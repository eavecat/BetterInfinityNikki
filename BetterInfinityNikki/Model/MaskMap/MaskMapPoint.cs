using System.Collections.Generic;

namespace BetterInfinityNikki.Model.MaskMap;

/// <summary>
/// 地图点位
/// </summary>
public class MaskMapPoint
{
    /// <summary>
    /// 点位唯一标识
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Web地图坐标 X
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Web地图坐标 Y
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    /// 游戏内坐标 X
    /// </summary>
    public double GameX { get; set; }

    /// <summary>
    /// 游戏内坐标 Y
    /// </summary>
    public double GameY { get; set; }

    /// <summary>
    /// 图像地图坐标 X（基于特定缩放级别，如2048）
    /// </summary>
    public double ImageX { get; set; }

    /// <summary>
    /// 图像地图坐标 Y（基于特定缩放级别，如2048）
    /// </summary>
    public double ImageY { get; set; }

    /// <summary>
    /// 标签ID，用于区分点位类型
    /// </summary>
    public string LabelId { get; set; } = string.Empty;

    /// <summary>
    /// 视频攻略链接列表
    /// </summary>
    public List<MaskMapLink> VideoUrls { get; set; } = new();

    /// <summary>
    /// 判断坐标是否在点位范围内
    /// </summary>
    public bool Contains(double px, double py)
    {
        return px >= X - MaskMapPointStatic.Width / 2 && 
               px <= X + MaskMapPointStatic.Width / 2 &&
               py >= Y - MaskMapPointStatic.Height / 2 && 
               py <= Y + MaskMapPointStatic.Height / 2;
    }
}

/// <summary>
/// 地图点位静态属性
/// </summary>
public class MaskMapPointStatic
{
    /// <summary>
    /// 点位图标宽度
    /// </summary>
    public static readonly int Width = 32;

    /// <summary>
    /// 点位图标高度
    /// </summary>
    public static readonly int Height = 32;
}
