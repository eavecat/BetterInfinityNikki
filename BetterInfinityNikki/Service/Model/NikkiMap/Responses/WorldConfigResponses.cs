using System.Text.Json.Serialization;

namespace BetterInfinityNikki.Service.Model.NikkiMap.Responses;

/// <summary>
/// 世界配置列表响应数据
/// </summary>
public class WorldConfigListData
{
    [JsonPropertyName("dir_config")]
    public List<WorldDirConfig> DirConfig { get; set; } = new();

    [JsonPropertyName("list")]
    public List<WorldConfigItem> List { get; set; } = new();
}

/// <summary>
/// 世界目录配置
/// </summary>
public class WorldDirConfig
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("order")]
    public int Order { get; set; }

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();

    [JsonPropertyName("world_id")]
    public int WorldId { get; set; }
}

/// <summary>
/// 世界配置项
/// </summary>
public class WorldConfigItem
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("map_resource_url")]
    public string MapResourceUrl { get; set; } = string.Empty;

    [JsonPropertyName("zoom_range")]
    public string ZoomRange { get; set; } = string.Empty;

    [JsonPropertyName("map_name")]
    public string MapName { get; set; } = string.Empty;

    [JsonPropertyName("max_bounds")]
    public string MaxBounds { get; set; } = string.Empty;

    [JsonPropertyName("map_size")]
    public int MapSize { get; set; }

    [JsonPropertyName("world_type")]
    public int WorldType { get; set; }

    [JsonPropertyName("parent_world_id")]
    public int ParentWorldId { get; set; }

    [JsonPropertyName("place_name")]
    public string PlaceName { get; set; } = string.Empty;

    [JsonPropertyName("quick_positioning")]
    public string QuickPositioning { get; set; } = string.Empty;

    [JsonPropertyName("layer_lists")]
    public string LayerLists { get; set; } = string.Empty;

    /// <summary>
    /// 本地特征数据目录名（由 MapFeatureConfig 匹配设置）
    /// </summary>
    [JsonIgnore]
    public string? FeatureDir { get; set; }

    /// <summary>
    /// 是否有本地特征数据
    /// </summary>
    [JsonIgnore]
    public bool HasFeature { get; set; }
}

/// <summary>
/// 世界配置列表响应
/// </summary>
public class WorldConfigListResponse
{
    public WorldConfigListData Data { get; set; } = new();
}
