using System.Text.Json.Serialization;

namespace BetterInfinityNikki.Service.Model.NikkiMap.Responses;

public class UserCollectedApiResponse
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("info")]
    public string Info { get; set; } = string.Empty;

    [JsonPropertyName("request_id")]
    public string RequestId { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public UserCollectedData? Data { get; set; }
}

public class UserCollectedData
{
    [JsonPropertyName("box")]
    public List<int> Box { get; set; } = new();

    [JsonPropertyName("cruise")]
    public List<int> Cruise { get; set; } = new();

    [JsonPropertyName("dewdrop")]
    public List<int> Dewdrop { get; set; } = new();

    [JsonPropertyName("mark")]
    public List<int> Mark { get; set; } = new();

    [JsonPropertyName("pillar")]
    public List<int> Pillar { get; set; } = new();

    [JsonPropertyName("read")]
    public List<int> Read { get; set; } = new();

    [JsonPropertyName("star")]
    public List<int> Star { get; set; } = new();

    [JsonPropertyName("status")]
    public int Status { get; set; }
}

public class UserCollectedResponse
{
    public UserCollectedData Data { get; set; } = new();
}
