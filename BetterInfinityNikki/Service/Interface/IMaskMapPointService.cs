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
}
