using System.IO;
using System.Text.Json;
using System.Threading;
using BetterInfinityNikki.Core.Config;
using BetterInfinityNikki.Model.MaskMap;
using BetterInfinityNikki.Service.Interface;
using BetterInfinityNikki.Service.Model.NikkiMap.Responses;

namespace BetterInfinityNikki.Service;

public sealed class MaskMapPointService : IMaskMapPointService
{
    private static readonly string CacheDir = Global.Absolute(Path.Combine("User", "Cache", "MaskMapData"));
    private static readonly string CatalogCachePath = Path.Combine(CacheDir, "catalog_list.json");
    private static readonly string SpawnerCachePath = Path.Combine(CacheDir, "spawner_list.json");

    private readonly ILogger<MaskMapPointService> _logger;
    private readonly NikkiMapApiService _apiService;

    private List<Spawner>? _allSpawners;
    private readonly SemaphoreSlim _loadLock = new(1, 1);

    public MaskMapPointService(ILogger<MaskMapPointService> logger, NikkiMapApiService apiService)
    {
        _logger = logger;
        _apiService = apiService;
    }

    public async Task<IReadOnlyList<MaskMapPointLabel>> GetLabelCategoriesAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("开始获取点位分类树");

            CatalogListData? catalogListData = null;

            if (File.Exists(CatalogCachePath))
            {
                try
                {
                    var cachedJson = await File.ReadAllTextAsync(CatalogCachePath, ct);
                    catalogListData = JsonSerializer.Deserialize<CatalogListData>(cachedJson);
                    _logger.LogDebug("从文件缓存加载点位分类");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "读取分类缓存文件失败，将从API重新获取");
                    catalogListData = null;
                }
            }

            if (catalogListData == null)
            {
                var response = await _apiService.GetCatalogListAsync(ct: ct);
                catalogListData = response?.Data;

                if (catalogListData != null)
                {
                    try
                    {
                        Directory.CreateDirectory(CacheDir);
                        var json = JsonSerializer.Serialize(catalogListData);
                        await File.WriteAllTextAsync(CatalogCachePath, json, ct);
                        _logger.LogDebug("点位分类数据已保存到缓存文件");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "保存分类缓存文件失败");
                    }
                }
            }

            if (catalogListData?.List == null || catalogListData.List.Count == 0)
            {
                _logger.LogWarning("未获取到任何点位分类");
                return Array.Empty<MaskMapPointLabel>();
            }

            var categories = new List<MaskMapPointLabel>();

            foreach (var catalog in catalogListData.List)
            {
                var categoryName = NikkiMapApiService.ParseMultiLangText(catalog.Name, "zh-cn");

                var children = new List<MaskMapPointLabel>();
                foreach (var item in catalog.Catalogs)
                {
                    var itemName = NikkiMapApiService.ParseMultiLangText(item.Name, "zh-cn");

                    children.Add(new MaskMapPointLabel
                    {
                        LabelId = item.Id.ToString(),
                        ParentId = catalog.Id.ToString(),
                        Name = itemName,
                        IconUrl = item.Icon,
                        PointCount = item.Count
                    });
                }

                categories.Add(new MaskMapPointLabel
                {
                    LabelId = catalog.Id.ToString(),
                    Name = categoryName,
                    Children = children
                });
            }

            _logger.LogDebug("成功获取 {Count} 个点位分类", categories.Count);
            return categories;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取点位分类树失败");
            return Array.Empty<MaskMapPointLabel>();
        }
    }

    public async Task<MaskMapPointsResult> GetPointsAsync(IReadOnlyList<MaskMapPointLabel> selectedItems, CancellationToken ct = default)
    {
        try
        {
            if (selectedItems.Count == 0)
            {
                return new MaskMapPointsResult();
            }

            await EnsureAllPointsLoadedAsync(ct);

            var selectedIds = new HashSet<int>();
            foreach (var item in selectedItems)
            {
                if (int.TryParse(item.LabelId, out var id))
                    selectedIds.Add(id);
            }

            if (selectedIds.Count == 0)
            {
                return new MaskMapPointsResult { Labels = selectedItems.ToList(), Points = Array.Empty<MaskMapPoint>() };
            }

            var points = new List<MaskMapPoint>();
            foreach (var spawner in _allSpawners!)
            {
                if (!selectedIds.Contains(spawner.Catalog))
                    continue;

                var webX = spawner.X;
                var webY = spawner.Y;

                var (imageX, imageY) = GameWebMapCoordinateConverter.WebToImage(webX, webY);
                var (gameX, gameY) = GameWebMapCoordinateConverter.WebToGame(webX, webY);

                points.Add(new MaskMapPoint
                {
                    Id = spawner.Id.ToString(),
                    X = webX,
                    Y = webY,
                    GameX = gameX,
                    GameY = gameY,
                    ImageX = imageX,
                    ImageY = imageY,
                    LabelId = spawner.Catalog.ToString()
                });
            }

            _logger.LogDebug("从缓存中筛选出 {Count} 个点位", points.Count);

            return new MaskMapPointsResult
            {
                Labels = selectedItems.ToList(),
                Points = points
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取点位列表失败");
            return new MaskMapPointsResult
            {
                Labels = selectedItems.ToList(),
                Points = Array.Empty<MaskMapPoint>()
            };
        }
    }

    public async Task<MaskMapPointInfo> GetPointInfoAsync(MaskMapPoint point, CancellationToken ct = default)
    {
        try
        {
            if (!int.TryParse(point.Id, out var pointId))
            {
                return new MaskMapPointInfo { Text = $"点位 ID 非法: {point.Id}" };
            }

            _logger.LogDebug("开始获取点位详情，ID: {Id}", pointId);

            var response = await _apiService.GetSpawnerInfoAsync(pointId, ct);

            if (response?.Data == null)
            {
                return new MaskMapPointInfo { Text = "未获取到点位信息" };
            }

            var data = response.Data;

            var urlList = new List<MaskMapLink>();
            if (!string.IsNullOrWhiteSpace(data.LinkHref))
            {
                urlList.Add(new MaskMapLink
                {
                    Text = string.IsNullOrWhiteSpace(data.LinkTitle) ? "视频攻略" : data.LinkTitle,
                    Url = data.LinkHref
                });
            }

            return new MaskMapPointInfo
            {
                Text = string.IsNullOrWhiteSpace(data.Description) ? "暂无描述" : data.Description,
                ImageUrl = data.Icon,
                UrlList = urlList
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取点位详情失败");
            return new MaskMapPointInfo
            {
                Text = "查询失败"
            };
        }
    }

    private async Task EnsureAllPointsLoadedAsync(CancellationToken ct)
    {
        if (_allSpawners != null) return;

        await _loadLock.WaitAsync(ct);
        try
        {
            if (_allSpawners != null) return;
            await LoadSpawnersCoreAsync(ct);
        }
        finally
        {
            _loadLock.Release();
        }
    }

    private async Task LoadSpawnersCoreAsync(CancellationToken ct)
    {
        _logger.LogDebug("开始预加载所有点位数据");

        if (File.Exists(SpawnerCachePath))
        {
            try
            {
                var cachedJson = await File.ReadAllTextAsync(SpawnerCachePath, ct);
                var cachedData = JsonSerializer.Deserialize<SpawnerListData>(cachedJson);
                _allSpawners = cachedData?.List ?? new List<Spawner>();
                _logger.LogInformation("从文件缓存加载点位数据，共 {Count} 个点位", _allSpawners.Count);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "读取点位缓存文件失败，将从API重新获取");
            }
        }

        var spawnerResponse = await _apiService.GetSpawnerListAsync(Array.Empty<int>(), ct: ct);
        _allSpawners = spawnerResponse?.Data?.List ?? new List<Spawner>();

        try
        {
            Directory.CreateDirectory(CacheDir);
            var dataToCache = new SpawnerListData { List = _allSpawners };
            var json = JsonSerializer.Serialize(dataToCache);
            await File.WriteAllTextAsync(SpawnerCachePath, json, ct);
            _logger.LogDebug("点位数据已保存到缓存文件");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "保存点位缓存文件失败");
        }

        _logger.LogInformation("预加载完成，共 {Count} 个点位", _allSpawners.Count);
    }

    public async Task UpdateCacheAsync(CancellationToken ct = default)
    {
        await _loadLock.WaitAsync(ct);
        try
        {
            _allSpawners = null;

            if (File.Exists(CatalogCachePath))
                File.Delete(CatalogCachePath);
            if (File.Exists(SpawnerCachePath))
                File.Delete(SpawnerCachePath);

            _logger.LogInformation("已清除点位缓存文件，开始重新获取数据");

            await GetLabelCategoriesAsync(ct);
            await LoadSpawnersCoreAsync(ct);

            _logger.LogInformation("点位缓存数据更新完成");
        }
        finally
        {
            _loadLock.Release();
        }
    }
}
