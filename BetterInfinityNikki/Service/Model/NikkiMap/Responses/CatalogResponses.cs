using System.Text.Json;
using System.Text.Json.Serialization;

namespace BetterInfinityNikki.Service.Model.NikkiMap.Responses;

/// <summary>
/// API通用响应包装
/// </summary>
public class NikkiMapApiResponse<T>
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public T? Data { get; set; }
}

/// <summary>
/// 点位类型目录响应数据
/// </summary>
public class CatalogListData
{
    [JsonPropertyName("list")]
    public List<Catalog> List { get; set; } = new();
}

/// <summary>
/// 点位类型目录
/// </summary>
public class Catalog
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// 名称（JSON字符串格式，需要解析）
    /// 格式: "[{\"lang\":\"zh-cn\",\"text\":\"传送点\"}]"
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("order")]
    public int Order { get; set; }

    [JsonPropertyName("catalogs")]
    public List<CatalogItem> Catalogs { get; set; } = new();
}

/// <summary>
/// 目录子项
/// </summary>
public class CatalogItem
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// 名称（JSON字符串格式，需要解析）
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 图标URL
    /// </summary>
    [JsonPropertyName("icon")]
    public string Icon { get; set; } = string.Empty;

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("can_marking")]
    public int CanMarking { get; set; }

    /// <summary>
    /// 图标列表（JSON字符串格式）
    /// </summary>
    [JsonPropertyName("icons")]
    public string Icons { get; set; } = string.Empty;
}

/// <summary>
/// 点位列表响应数据
/// </summary>
public class SpawnerListData
{
    [JsonPropertyName("list")]
    public List<Spawner> List { get; set; } = new();
}

/// <summary>
/// 资源点位信息
/// </summary>
public class Spawner
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }

    [JsonPropertyName("z")]
    public double Z { get; set; }

    [JsonPropertyName("catalog")]
    public int Catalog { get; set; }

    [JsonPropertyName("world_id")]
    public string WorldId { get; set; } = string.Empty;

    [JsonPropertyName("stage_id")]
    public string StageId { get; set; } = string.Empty;

    /// <summary>
    /// 描述（包含语言和文本）
    /// API返回的是JSON字符串格式的数组，如: "[{\"lang\":\"zh-cn\",\"text\":\"...\"}]"
    /// </summary>
    [JsonPropertyName("description")]
    [JsonConverter(typeof(DescriptionListConverter))]
    public List<Description> Description { get; set; } = new();

    [JsonPropertyName("tag")]
    public string Tag { get; set; } = string.Empty;
}

/// <summary>
/// 描述信息
/// </summary>
public class Description
{
    [JsonPropertyName("lang")]
    public string Lang { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

/// <summary>
/// 处理 description 字段的双重编码 JSON 字符串
/// API 返回格式类似: "\"[{\\\"lang\\\":\\\"zh-cn\\\",\\\"text\\\":\\\"...\\\"}]\""
/// 经过外层反序列化后仍需要再解一层 JSON 字符串
/// </summary>
public class DescriptionListConverter : JsonConverter<List<Description>>
{
    public override List<Description>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var rawStr = reader.GetString();
            if (string.IsNullOrEmpty(rawStr))
                return new List<Description>();

            var jsonStr = UnwrapJsonString(rawStr);
            return ParseDescriptions(jsonStr);
        }

        if (reader.TokenType == JsonTokenType.StartArray)
        {
            return JsonSerializer.Deserialize<List<Description>>(ref reader) ?? new List<Description>();
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            var single = JsonSerializer.Deserialize<Description>(ref reader);
            return single != null ? new List<Description> { single } : new List<Description>();
        }

        return new List<Description>();
    }

    private static string UnwrapJsonString(string raw)
    {
        // 如果字符串以引号包裹，说明还有一层 JSON 字符串编码需要解开
        if (raw.Length >= 2 && raw[0] == '"' && raw[^1] == '"')
        {
            return raw.Substring(1, raw.Length - 2).Replace("\\\"", "\"");
        }

        return raw;
    }

    private static List<Description> ParseDescriptions(string jsonStr)
    {
        if (string.IsNullOrWhiteSpace(jsonStr))
            return new List<Description>();

        var trimmed = jsonStr.AsSpan().TrimStart();
        if (trimmed.Length == 0)
            return new List<Description>();

        try
        {
            if (trimmed[0] == '[')
            {
                return JsonSerializer.Deserialize<List<Description>>(jsonStr) ?? new List<Description>();
            }

            var single = JsonSerializer.Deserialize<Description>(jsonStr);
            return single != null ? new List<Description> { single } : new List<Description>();
        }
        catch
        {
            return new List<Description>();
        }
    }

    public override void Write(Utf8JsonWriter writer, List<Description> value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }
}

/// <summary>
/// 点位详情响应数据
/// </summary>
public class SpawnerInfoData
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }

    [JsonPropertyName("z")]
    public double Z { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("icon")]
    public string Icon { get; set; } = string.Empty;

    [JsonPropertyName("link_title")]
    public string LinkTitle { get; set; } = string.Empty;

    [JsonPropertyName("link_href")]
    public string LinkHref { get; set; } = string.Empty;

    [JsonPropertyName("updated_at")]
    public string UpdatedAt { get; set; } = string.Empty;
}

// 包装类
public class CatalogListResponse
{
    public CatalogListData Data { get; set; } = new();
}

public class SpawnerListResponse
{
    public SpawnerListData Data { get; set; } = new();
}

public class SpawnerInfoResponse
{
    public SpawnerInfoData Data { get; set; } = new();
}
