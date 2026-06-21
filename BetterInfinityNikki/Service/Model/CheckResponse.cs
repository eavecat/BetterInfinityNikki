using Newtonsoft.Json;

namespace BetterInfinityNikki.Service.Model;

/// <summary>
/// 版本检查接口响应：{ code, data }
/// </summary>
public class CheckResponse
{
    [JsonProperty("code")]
    public int Code { get; set; }

    [JsonProperty("data")]
    public CheckResponseData? Data { get; set; }
}

/// <summary>
/// 版本检查接口 data 部分
/// </summary>
public class CheckResponseData
{
    [JsonProperty("has_update")]
    public bool HasUpdate { get; set; }

    [JsonProperty("version")]
    public string Version { get; set; } = "";

    [JsonProperty("release_notes")]
    public string? ReleaseNotes { get; set; }

    [JsonProperty("download_page_url")]
    public string? DownloadPageUrl { get; set; }

    /// <summary>
    /// 强制更新时隐藏"不再提示"按钮
    /// </summary>
    [JsonProperty("force")]
    public bool Force { get; set; }
}
