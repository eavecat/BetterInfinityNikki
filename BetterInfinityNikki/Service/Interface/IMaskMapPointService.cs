using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BetterInfinityNikki.Model.MaskMap;

namespace BetterInfinityNikki.Service.Interface;

/// <summary>
/// 地图点位服务接口
/// </summary>
public interface IMaskMapPointService
{
    /// <summary>
    /// 获取点位分类树
    /// </summary>
    Task<IReadOnlyList<MaskMapPointLabel>> GetLabelCategoriesAsync(CancellationToken ct = default);

    /// <summary>
    /// 根据选中的分类获取点位列表
    /// </summary>
    Task<MaskMapPointsResult> GetPointsAsync(IReadOnlyList<MaskMapPointLabel> selectedItems, CancellationToken ct = default);

    /// <summary>
    /// 获取单个点位的详细信息
    /// </summary>
    Task<MaskMapPointInfo> GetPointInfoAsync(MaskMapPoint point, CancellationToken ct = default);

    /// <summary>
    /// 清除缓存文件并重新从API获取数据
    /// </summary>
    Task UpdateCacheAsync(CancellationToken ct = default);

    /// <summary>
    /// 从API获取用户收集进度数据并缓存到本地
    /// </summary>
    Task UpdateCollectedCacheAsync(CancellationToken ct = default);

    /// <summary>
    /// 收集数据缓存被更新时触发
    /// </summary>
    event System.EventHandler? CollectedDataUpdated;
}
