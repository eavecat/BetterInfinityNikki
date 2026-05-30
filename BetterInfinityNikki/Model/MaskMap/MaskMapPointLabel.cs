using System;
using System.Collections.Generic;
using System.Drawing;

namespace BetterInfinityNikki.Model.MaskMap;

/// <summary>
/// 地图点位标签
/// </summary>
public class MaskMapPointLabel
{
    /// <summary>
    /// 标签ID
    /// </summary>
    public string LabelId { get; set; } = string.Empty;

    /// <summary>
    /// 关联的标签ID列表（用于一对多关系）
    /// </summary>
    public IReadOnlyList<string> LabelIds { get; set; } = Array.Empty<string>();

    /// <summary>
    /// 父级标签ID
    /// </summary>
    public string ParentId { get; set; } = string.Empty;

    /// <summary>
    /// 标签名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 图标URL
    /// </summary>
    public string IconUrl { get; set; } = string.Empty;

    /// <summary>
    /// 该标签下的点位数量
    /// </summary>
    public int PointCount { get; set; }

    /// <summary>
    /// 子标签列表
    /// </summary>
    public IReadOnlyList<MaskMapPointLabel> Children { get; set; } = Array.Empty<MaskMapPointLabel>();

    /// <summary>
    /// 颜色（如果没有图片时使用，为空则随机生成）
    /// </summary>
    public Color? Color { get; set; }
}
