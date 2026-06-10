using System.Text.Json.Serialization;

namespace BetterInfinityNikki.Service.Model.NikkiMap.Requests;

public class UserInfoRequest
{
    [JsonPropertyName("client_id")]
    public int ClientId { get; set; }

    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("openid")]
    public string OpenId { get; set; } = string.Empty;
}
