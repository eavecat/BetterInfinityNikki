using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using BetterInfinityNikki.Service.Model.NikkiMap.Requests;
using BetterInfinityNikki.Service.Model.NikkiMap.Responses;
using Snappier;

namespace BetterInfinityNikki.Service;

/// <summary>
/// 暖暖地图API服务
/// </summary>
public sealed class NikkiMapApiService : IDisposable
{
    private const string BaseUrl = "https://myl-api.nuanpaper.com/v1/strategy/map";
    private const int ClientId = 1106;
    private const string Token = "985b48b67d837fb39c8b8df4797f4f7de91cc8ed";
    private const string OpenId = "199696041";
    private const string DefaultWorldId = "1";

    private readonly HttpClient _httpClient;
    private readonly ILogger<NikkiMapApiService> _logger;

    public NikkiMapApiService(HttpClient httpClient, ILogger<NikkiMapApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// 获取点位类型目录（传送点、宝箱、钓鱼点……）
    /// </summary>
    public async Task<CatalogListResponse> GetCatalogListAsync(string? worldId = null, CancellationToken ct = default)
    {
        var request = new CatalogListRequest
        {
            ClientId = ClientId,
            Token = Token,
            OpenId = OpenId,
            WorldId = worldId ?? DefaultWorldId
        };

        try
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await _httpClient.PostAsync($"{BaseUrl}/catalog/list", content, ct);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<NikkiMapApiResponse<CatalogListData>>(responseJson);

            if (result?.Code != 0 || result.Data == null)
            {
                throw new Exception($"API返回错误: Code={result?.Code}, Message={result?.Message}");
            }

            return new CatalogListResponse { Data = result.Data };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取点位类型目录失败");
            throw;
        }
    }

    /// <summary>
    /// 获取资源点位信息列表（支持Gzip压缩响应）
    /// </summary>
    public async Task<SpawnerListResponse> GetSpawnerListAsync(int[] catalogTypeIds, string? worldId = null,
        CancellationToken ct = default)
    {
        var request = new SpawnerListRequest
        {
            ClientId = ClientId,
            Token = Token,
            OpenId = OpenId,
            WorldId = worldId ?? DefaultWorldId,
            CatalogTypeId = catalogTypeIds ?? Array.Empty<int>()
        };

        try
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // 不使用 ct 传递给 HttpClient，避免取消时破坏共享连接
            using var response =
                await _httpClient.PostAsync($"{BaseUrl}/spawner/list", content, CancellationToken.None);
            ct.ThrowIfCancellationRequested();
            response.EnsureSuccessStatusCode();

            var rawData = await response.Content.ReadAsByteArrayAsync(CancellationToken.None);
            ct.ThrowIfCancellationRequested();

            var responseJson = DecompressSnappy(rawData);

            var result = JsonSerializer.Deserialize<SpawnerListData>(responseJson);

            return new SpawnerListResponse { Data = result! };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取点位列表失败");
            throw;
        }
    }

    /// <summary>
    /// 获取单个点位的详细信息
    /// </summary>
    public async Task<SpawnerInfoResponse> GetSpawnerInfoAsync(int id, CancellationToken ct = default)
    {
        var request = new SpawnerInfoRequest
        {
            ClientId = ClientId,
            Token = Token,
            OpenId = OpenId,
            Id = id
        };

        try
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await _httpClient.PostAsync($"{BaseUrl}/spawner/info", content, ct);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<NikkiMapApiResponse<SpawnerInfoData>>(responseJson);

            if (result?.Code != 0 || result.Data == null)
            {
                throw new Exception($"API返回错误: Code={result?.Code}, Message={result?.Message}");
            }

            return new SpawnerInfoResponse { Data = result.Data };
        }
        catch (OperationCanceledException)
        {
            // 请求被取消是正常行为，不记录日志
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取点位详情失败，ID: {Id}", id);
            throw;
        }
    }

    /// <summary>
    /// 解码Snappy内容
    /// </summary>
    private static string DecompressSnappy(byte[] rawData)
    {
        if (rawData.Length == 0)
        {
            return string.Empty;
        }

        // 获取解压后的精确字节数
        int uncompressedLength = Snappy.GetUncompressedLength(rawData);

        byte[] outputBuffer = new byte[uncompressedLength];

        int bytesWritten = Snappy.Decompress(rawData, outputBuffer);
        var output = outputBuffer.AsSpan(0, bytesWritten).ToArray();

        return Encoding.UTF8.GetString(output);
    }

    /// <summary>
    /// 解析多语言JSON字符串
    /// 格式: "\"[{\"lang\":\"zh-cn\",\"text\":\"传送点\"}]\""
    /// </summary>
    public static string ParseMultiLangText(string jsonStr, string preferredLang = "zh-cn")
    {
        if (string.IsNullOrWhiteSpace(jsonStr))
        {
            return string.Empty;
        }

        try
        {
            // 移除转义字符
            var cleanJson = jsonStr
                .Substring(1, jsonStr.Length - 2)
                .Replace("\\\"", "\"");

            // 尝试解析为数组
            var langItems = JsonSerializer.Deserialize<List<LangItem>>(cleanJson);

            if (langItems != null && langItems.Count > 0)
            {
                // 优先查找指定语言
                var preferred = langItems.Find(x => x.lang == preferredLang);
                if (preferred != null)
                {
                    return preferred.text;
                }

                // 否则返回第一个
                return langItems[0].text;
            }

            return string.Empty;
        }
        catch
        {
            // 如果解析失败，直接返回原字符串
            return jsonStr;
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }

    #region Models

    private class LangItem
    {
        public string lang { get; set; } = string.Empty;
        public string text { get; set; } = string.Empty;
    }

    #endregion
}