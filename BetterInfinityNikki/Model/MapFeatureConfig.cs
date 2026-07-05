namespace BetterInfinityNikki.Model;

/// <summary>
/// 地图特征数据配置（运行时由 MapFeatureInfo 注册表提供，不再从 JSON 加载）
/// </summary>
public class MapFeatureConfig
{
    /// <summary>
    /// 对应 API 返回的 WorldConfigItem.Id，用于自动匹配
    /// </summary>
    public int WorldId { get; set; }

    /// <summary>
    /// 本地地图目录名
    /// </summary>
    public string MapKey { get; set; } = string.Empty;

    /// <summary>
    /// 地图名称
    /// </summary>
    public string MapName { get; set; } = string.Empty;

    /// <summary>
    /// 特征图像素宽度
    /// </summary>
    public int ImageWidth { get; set; }

    /// <summary>
    /// 特征图像素高度
    /// </summary>
    public int ImageHeight { get; set; }

    /// <summary>
    /// 游戏坐标 (0,0) 在特征图中的像素 X
    /// </summary>
    public int OriginX { get; set; }

    /// <summary>
    /// 游戏坐标 (0,0) 在特征图中的像素 Y
    /// </summary>
    public int OriginY { get; set; }

    /// <summary>
    /// 大地图截图缩放比例
    /// </summary>
    public int BigMapScaleFactor { get; set; } = 1;

    /// <summary>
    /// 分块行数（用于自适应搜索索引）
    /// </summary>
    public int SplitRow { get; set; } = 16;

    /// <summary>
    /// 分块列数（用于自适应搜索索引）
    /// </summary>
    public int SplitCol { get; set; } = 16;

    /// <summary>
    /// Web→Image 坐标转换用的偏移基准
    /// 每调整 64 对应图像坐标偏移 1 像素
    /// </summary>
    public double OffsetBaseX { get; set; } = 16384 + 64 * -36;

    /// <summary>
    /// Web→Image 坐标转换用的偏移基准
    /// 每调整 64 对应图像坐标偏移 1 像素
    /// </summary>
    public double OffsetBaseY { get; set; } = 16384 + 64 * -36;

    /// <summary>
    /// Web→Image 坐标转换用的缩放系数
    /// </summary>
    public double WebToImageScale { get; set; } = 1.0 / 64.0;
}
