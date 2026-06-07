using System.Text.Json.Serialization;

namespace BetterInfinityNikki.Service.Model.NikkiMap.Requests;

/// <summary>
/// 获取点位类型目录请求
/// </summary>
public class CatalogListRequest
{
    [JsonPropertyName("client_id")]
    public int ClientId { get; set; }

    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("openid")]
    public string OpenId { get; set; } = string.Empty;

    [JsonPropertyName("world_id")]
    public string WorldId { get; set; } = "1";
}

/// <summary>
/// 获取点位列表请求
/// </summary>
public class SpawnerListRequest
{
    [JsonPropertyName("client_id")]
    public int ClientId { get; set; }

    [JsonPropertyName("catalog_type_id")]
    public int[] CatalogTypeId { get; set; } = [];

    [JsonPropertyName("world_id")]
    public string WorldId { get; set; } = "1";

    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("openid")]
    public string OpenId { get; set; } = string.Empty;
}

/// <summary>
/// 获取点位详情请求
/// </summary>
public class SpawnerInfoRequest
{
    [JsonPropertyName("client_id")]
    public int ClientId { get; set; }

    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("openid")]
    public string OpenId { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public int Id { get; set; }
}
