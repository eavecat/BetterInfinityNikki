using System.IO;
using System.Text.Json;
using System.Threading;
using BetterInfinityNikki.Core.Config;
using BetterInfinityNikki.Model;
using BetterInfinityNikki.Model.MaskMap;
using BetterInfinityNikki.Service.Interface;
using BetterInfinityNikki.Service.Model.NikkiMap.Responses;

namespace BetterInfinityNikki.Service;

public sealed class MaskMapPointService : IMaskMapPointService
{
    private static readonly string CacheDir = Global.Absolute(Path.Combine("User", "Cache", "MaskMapData"));
    private static readonly string CollectedCachePath = Path.Combine(CacheDir, "user_collected.json");

    private readonly ILogger<MaskMapPointService> _logger;
    private readonly NikkiMapApiService _apiService;

    private string? _currentWorldId;
    private List<Spawner>? _allSpawners;
    private readonly SemaphoreSlim _loadLock = new(1, 1);

    private HashSet<int> _collectedSpawnerIds = new();
    private bool _collectedDataLoaded = false;

    public event EventHandler? CollectedDataUpdated;

    public MaskMapPointService(ILogger<MaskMapPointService> logger, NikkiMapApiService apiService)
    {
        _logger = logger;
        _apiService = apiService;
    }

    private static string GetCatalogCachePath(string worldId) =>
        Path.Combine(CacheDir, $"catalog_list_{worldId}.json");

    private static string GetSpawnerCachePath(string worldId) =>
        Path.Combine(CacheDir, $"spawner_list_{worldId}.json");

    public async Task<IReadOnlyList<MaskMapPointLabel>> GetLabelCategoriesAsync(string worldId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("开始获取点位分类树, worldId={WorldId}", worldId);

            var cachePath = GetCatalogCachePath(worldId);
            CatalogListData? catalogListData = null;

            if (File.Exists(cachePath))
            {
                try
                {
                    var cachedJson = await File.ReadAllTextAsync(cachePath, ct);
                    catalogListData = JsonSerializer.Deserialize<CatalogListData>(cachedJson);
                    _logger.LogDebug("从文件缓存加载点位分类, worldId={WorldId}", worldId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "读取分类缓存文件失败，将从API重新获取");
                    catalogListData = null;
                }
            }

            if (catalogListData == null)
            {
                var response = await _apiService.GetCatalogListAsync(worldId, ct);
                catalogListData = response?.Data;

                if (catalogListData != null)
                {
                    try
                    {
                        Directory.CreateDirectory(CacheDir);
                        var json = JsonSerializer.Serialize(catalogListData);
                        await File.WriteAllTextAsync(cachePath, json, ct);
                        _logger.LogDebug("点位分类数据已保存到缓存文件, worldId={WorldId}", worldId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "保存分类缓存文件失败");
                    }
                }
            }

            if (catalogListData?.List == null || catalogListData.List.Count == 0)
            {
                _logger.LogWarning("未获取到任何点位分类, worldId={WorldId}", worldId);
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

            _logger.LogDebug("成功获取 {Count} 个点位分类, worldId={WorldId}", categories.Count, worldId);
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

    public async Task<MaskMapPointsResult> GetPointsAsync(string worldId, IReadOnlyList<MaskMapPointLabel> selectedItems, CancellationToken ct = default)
    {
        try
        {
            if (selectedItems.Count == 0)
            {
                return new MaskMapPointsResult();
            }

            await EnsureAllPointsLoadedAsync(worldId, ct);
            await EnsureCollectedDataLoadedAsync(ct);

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

            var featureConfig = int.TryParse(worldId, out var wid)
                ? MapFeatureRegistry.GetByWorldId(wid)
                : null;

            var points = new List<MaskMapPoint>();
            foreach (var spawner in _allSpawners!)
            {
                if (!selectedIds.Contains(spawner.Catalog))
                    continue;

                var webX = spawner.X;
                var webY = spawner.Y;

                var (imageX, imageY) = featureConfig != null
                    ? GameWebMapCoordinateConverter.WebToImage(webX, webY, featureConfig)
                    : (0.0, 0.0);
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
                    LabelId = spawner.Catalog.ToString(),
                    IsCollected = _collectedSpawnerIds.Contains(spawner.Id)
                });
            }

            _logger.LogDebug("从缓存中筛选出 {Count} 个点位, worldId={WorldId}", points.Count, worldId);

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
                var linkTitle = NikkiMapApiService.ParseMultiLangText(data.LinkTitle, "zh-cn");
                urlList.Add(new MaskMapLink
                {
                    Text = string.IsNullOrWhiteSpace(linkTitle) ? "视频攻略" : linkTitle,
                    Url = data.LinkHref
                });
            }

            var description = NikkiMapApiService.ParseMultiLangText(data.Description, "zh-cn");

            return new MaskMapPointInfo
            {
                Text = string.IsNullOrWhiteSpace(description) ? "暂无描述" : description,
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

    public void SwitchWorld(string worldId)
    {
        if (_currentWorldId == worldId) return;

        _logger.LogInformation("切换世界: {OldWorldId} -> {NewWorldId}", _currentWorldId, worldId);
        _currentWorldId = worldId;
        _allSpawners = null;
    }

    private async Task EnsureAllPointsLoadedAsync(string worldId, CancellationToken ct)
    {
        if (_allSpawners != null && _currentWorldId == worldId) return;

        await _loadLock.WaitAsync(ct);
        try
        {
            if (_allSpawners != null && _currentWorldId == worldId) return;
            await LoadSpawnersCoreAsync(worldId, ct);
        }
        finally
        {
            _loadLock.Release();
        }
    }

    private async Task LoadSpawnersCoreAsync(string worldId, CancellationToken ct)
    {
        _logger.LogDebug("开始预加载所有点位数据, worldId={WorldId}", worldId);

        var cachePath = GetSpawnerCachePath(worldId);

        if (File.Exists(cachePath))
        {
            try
            {
                var cachedJson = await File.ReadAllTextAsync(cachePath, ct);
                var cachedData = JsonSerializer.Deserialize<SpawnerListData>(cachedJson);
                _allSpawners = cachedData?.List ?? new List<Spawner>();
                _currentWorldId = worldId;
                _logger.LogInformation("从文件缓存加载点位数据，共 {Count} 个点位, worldId={WorldId}", _allSpawners.Count, worldId);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "读取点位缓存文件失败，将从API重新获取");
            }
        }

        var spawnerResponse = await _apiService.GetSpawnerListAsync(Array.Empty<int>(), worldId, ct);
        _allSpawners = spawnerResponse?.Data?.List ?? new List<Spawner>();
        _currentWorldId = worldId;

        try
        {
            Directory.CreateDirectory(CacheDir);
            var dataToCache = new SpawnerListData { List = _allSpawners };
            var json = JsonSerializer.Serialize(dataToCache);
            await File.WriteAllTextAsync(cachePath, json, ct);
            _logger.LogDebug("点位数据已保存到缓存文件, worldId={WorldId}", worldId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "保存点位缓存文件失败");
        }

        _logger.LogInformation("预加载完成，共 {Count} 个点位, worldId={WorldId}", _allSpawners.Count, worldId);
    }

    public async Task UpdateCacheAsync(string worldId, CancellationToken ct = default)
    {
        await _loadLock.WaitAsync(ct);
        try
        {
            _allSpawners = null;

            var catalogCache = GetCatalogCachePath(worldId);
            var spawnerCache = GetSpawnerCachePath(worldId);

            if (File.Exists(catalogCache))
                File.Delete(catalogCache);
            if (File.Exists(spawnerCache))
                File.Delete(spawnerCache);

            _logger.LogInformation("已清除点位缓存文件，开始重新获取数据, worldId={WorldId}", worldId);

            await GetLabelCategoriesAsync(worldId, ct);
            await LoadSpawnersCoreAsync(worldId, ct);

            _logger.LogInformation("点位缓存数据更新完成, worldId={WorldId}", worldId);
        }
        finally
        {
            _loadLock.Release();
        }
    }

    private async Task EnsureCollectedDataLoadedAsync(CancellationToken ct)
    {
        if (_collectedDataLoaded) return;

        await _loadLock.WaitAsync(ct);
        try
        {
            if (_collectedDataLoaded) return;

            if (File.Exists(CollectedCachePath))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(CollectedCachePath, ct);
                    var data = JsonSerializer.Deserialize<UserCollectedData>(json);
                    if (data != null)
                    {
                        _collectedSpawnerIds = FlattenCollectedIds(data);
                    }
                    _logger.LogDebug("从缓存加载收集数据，共 {Count} 个已收集点位", _collectedSpawnerIds.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "读取收集缓存失败，将跳过收集过滤");
                    _collectedSpawnerIds = new HashSet<int>();
                }
            }

            _collectedDataLoaded = true;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    private static HashSet<int> FlattenCollectedIds(UserCollectedData data)
    {
        var set = new HashSet<int>();
        set.UnionWith(data.Box);
        set.UnionWith(data.Cruise);
        set.UnionWith(data.Dewdrop);
        set.UnionWith(data.Pillar);
        set.UnionWith(data.Read);
        set.UnionWith(data.Star);
        return set;
    }

    public async Task UpdateCollectedCacheAsync(CancellationToken ct = default)
    {
        await _loadLock.WaitAsync(ct);
        try
        {
            var response = await _apiService.GetUserCollectedInfoAsync(ct);
            var data = response?.Data;

            if (data != null)
            {
                Directory.CreateDirectory(CacheDir);
                var json = JsonSerializer.Serialize(data);
                await File.WriteAllTextAsync(CollectedCachePath, json, ct);

                _collectedSpawnerIds = FlattenCollectedIds(data);
                _collectedDataLoaded = true;

                _logger.LogInformation("收集数据缓存已更新，共 {Count} 个已收集点位", _collectedSpawnerIds.Count);
            }
        }
        finally
        {
            _loadLock.Release();
        }

        CollectedDataUpdated?.Invoke(this, EventArgs.Empty);
    }
}
