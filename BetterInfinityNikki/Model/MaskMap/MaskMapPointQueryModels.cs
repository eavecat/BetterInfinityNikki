using System;
using System.Collections.Generic;

namespace BetterInfinityNikki.Model.MaskMap;

/// <summary>
/// 地图点位查询结果
/// </summary>
public sealed class MaskMapPointsResult
{
    /// <summary>
    /// 标签列表
    /// </summary>
    public IReadOnlyList<MaskMapPointLabel> Labels { get; set; } = Array.Empty<MaskMapPointLabel>();

    /// <summary>
    /// 点位列表
    /// </summary>
    public IReadOnlyList<MaskMapPoint> Points { get; set; } = Array.Empty<MaskMapPoint>();
}

/// <summary>
/// 地图点位详细信息
/// </summary>
public sealed class MaskMapPointInfo
{
    /// <summary>
    /// 点位描述文本
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// 点位图片URL
    /// </summary>
    public string ImageUrl { get; set; } = string.Empty;

    /// <summary>
    /// 相关链接列表
    /// </summary>
    public IReadOnlyList<MaskMapLink> UrlList { get; set; } = Array.Empty<MaskMapLink>();
}

/// <summary>
/// 地图链接
/// </summary>
public sealed class MaskMapLink
{
    /// <summary>
    /// 链接文本
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// 链接地址
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// 显示文本（如果Text为空则显示默认文本）
    /// </summary>
    public string DisplayText => string.IsNullOrWhiteSpace(Text) ? "视频攻略" : Text;
}
