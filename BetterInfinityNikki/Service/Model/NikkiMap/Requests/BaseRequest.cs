using System.Text.Json.Serialization;

namespace BetterInfinityNikki.Service.Model.NikkiMap.Requests;

/// <summary>
/// API请求基类，包含公共的认证字段
/// </summary>
public class BaseRequest
{
    [JsonPropertyName("client_id")]
    public int ClientId { get; set; }

    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("openid")]
    public string OpenId { get; set; } = string.Empty;
}

/// <summary>
/// 获取点位类型目录请求
/// </summary>
public class CatalogListRequest : BaseRequest
{
    [JsonPropertyName("world_id")]
    public string WorldId { get; set; } = "1";
}

/// <summary>
/// 获取点位列表请求
/// </summary>
public class SpawnerListRequest : BaseRequest
{
    [JsonPropertyName("catalog_type_id")]
    public int[] CatalogTypeId { get; set; } = [];

    [JsonPropertyName("world_id")]
    public string WorldId { get; set; } = "1";
}

/// <summary>
/// 获取点位详情请求
/// </summary>
public class SpawnerInfoRequest : BaseRequest
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
}

/// <summary>
/// 获取用户信息请求
/// </summary>
public class UserInfoRequest : BaseRequest;

/// <summary>
/// 获取世界配置列表请求
/// </summary>
public class WorldConfigListRequest : BaseRequest;
