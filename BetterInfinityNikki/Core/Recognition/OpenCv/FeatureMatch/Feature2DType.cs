namespace BetterInfinityNikki.Core.Recognition.OpenCv.FeatureMatch;

/// <summary>
/// 特征检测器类型枚举
/// </summary>
public enum Feature2DType
{
    /// <summary>
    /// SIFT (Scale-Invariant Feature Transform)
    /// 优点：精度高，对旋转、缩放、亮度变化鲁棒
    /// 缺点：速度较慢
    /// 适用场景：地图匹配、物体识别
    /// </summary>
    SIFT,

    /// <summary>
    /// SURF (Speeded-Up Robust Features)
    /// 优点：速度快，对旋转、缩放鲁棒
    /// 缺点：精度略低于 SIFT
    /// 注意：SURF 有专利保护，商业使用需要授权
    /// </summary>
    SURF
}
