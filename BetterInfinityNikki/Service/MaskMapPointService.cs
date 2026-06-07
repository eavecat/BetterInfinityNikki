using System.Threading;
using BetterInfinityNikki.Model.MaskMap;
using BetterInfinityNikki.Service.Interface;
using BetterInfinityNikki.Service.Model.NikkiMap.Responses;

namespace BetterInfinityNikki.Service;

public sealed class MaskMapPointService : IMaskMapPointService
{
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

            var response = await _apiService.GetCatalogListAsync(ct: ct);

            if (response?.Data?.List == null || response.Data.List.Count == 0)
            {
                _logger.LogWarning("未获取到任何点位分类");
                return Array.Empty<MaskMapPointLabel>();
            }

            var categories = new List<MaskMapPointLabel>();

            foreach (var catalog in response.Data.List)
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

            _logger.LogDebug("开始预加载所有点位数据");

            var spawnerResponse = await _apiService.GetSpawnerListAsync(Array.Empty<int>(), ct: ct);
            _allSpawners = spawnerResponse?.Data?.List ?? new List<Spawner>();

            _logger.LogInformation("预加载完成，共 {Count} 个点位", _allSpawners.Count);
        }
        finally
        {
            _loadLock.Release();
        }
    }
}
