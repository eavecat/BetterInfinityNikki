using BetterInfinityNikki.Model;

namespace BetterInfinityNikki;

/// <summary>
/// 本地地图特征数据注册表（替代旧的 map_config.json）。
/// 每个条目描述一个已下载并生成特征数据的地图，Key 为 MapKey（本地 Assets/Map/{MapKey} 目录名）。
/// </summary>
public static class MapFeatureRegistry
{
    /// <summary>
    /// 默认 OffsetBase
    /// </summary>
    private const double DefaultOffsetBase = 16384 + 64 * -36;

    /// <summary>
    /// 默认 WebToImageScale
    /// </summary>
    private const double DefaultWebToImageScale = 1.0 / 64.0;

    public static IReadOnlyDictionary<string, MapFeatureConfig> Maps { get; } = new Dictionary<string, MapFeatureConfig>
    {
        ["NikkiWorld"] = new MapFeatureConfig
        {
            WorldId = 1,
            MapKey = "NikkiWorld",
            ImageWidth = 16384,
            ImageHeight = 16384,
            OriginX = 8192,
            OriginY = 8192,
            BigMapScaleFactor = 4,
            SplitRow = 16,
            SplitCol = 16,
            OffsetBase = DefaultOffsetBase,
            WebToImageScale = DefaultWebToImageScale,
        },
        ["WanXiangJing"] = new MapFeatureConfig
        {
            WorldId = 4020034,
            MapKey = "WanXiangJing",
            ImageWidth = 8192,
            ImageHeight = 6656,
            OriginX = 0,
            OriginY = 0,
            BigMapScaleFactor = 1,
            SplitRow = 16,
            SplitCol = 16,
            OffsetBase = DefaultOffsetBase,
            WebToImageScale = DefaultWebToImageScale,
        },
        ["DanQingYu"] = new MapFeatureConfig
        {
            WorldId = 10000010,
            MapKey = "DanQingYu",
            ImageWidth = 7936,
            ImageHeight = 9984,
            OriginX = 0,
            OriginY = 0,
            BigMapScaleFactor = 1,
            SplitRow = 16,
            SplitCol = 16,
            OffsetBase = DefaultOffsetBase,
            WebToImageScale = DefaultWebToImageScale,
        },
        ["WuYouDao"] = new MapFeatureConfig
        {
            WorldId = 10000002,
            MapKey = "WuYouDao",
            ImageWidth = 4096,
            ImageHeight = 3584,
            OriginX = 0,
            OriginY = 0,
            BigMapScaleFactor = 4,
            SplitRow = 16,
            SplitCol = 16,
            OffsetBase = 4096 + 64 * 32,
            WebToImageScale = 1.0 / 39.0,
        },
    };

    /// <summary>
    /// 根据 WorldConfigItem.Id 查找本地地图配置
    /// </summary>
    public static MapFeatureConfig? GetByWorldId(int worldId)
    {
        foreach (var cfg in Maps.Values)
        {
            if (cfg.WorldId == worldId) return cfg;
        }

        return null;
    }

    /// <summary>
    /// 根据 MapKey 查找本地地图配置
    /// </summary>
    public static MapFeatureConfig? GetByKey(string mapKey)
    {
        return Maps.TryGetValue(mapKey, out var cfg) ? cfg : null;
    }
}