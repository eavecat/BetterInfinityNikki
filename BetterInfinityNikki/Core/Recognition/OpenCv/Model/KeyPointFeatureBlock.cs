using System.Collections.Generic;
using OpenCvSharp;

namespace BetterInfinityNikki.Core.Recognition.OpenCv.Model;

/// <summary>
/// 特征块
/// 对特征按图像区域进行划分，用于加速搜索
/// </summary>
public class KeyPointFeatureBlock
{
    /// <summary>
    /// 关键点列表
    /// </summary>
    public List<KeyPoint> KeyPointList { get; set; } = new();

    private KeyPoint[]? _keyPointArray;

    /// <summary>
    /// 关键点数组（懒加载）
    /// </summary>
    public KeyPoint[] KeyPointArray
    {
        get
        {
            _keyPointArray ??= KeyPointList.ToArray();
            return _keyPointArray;
        }
    }

    /// <summary>
    /// 在完整 KeyPoint[] 中的下标索引
    /// 用于快速定位原始数据
    /// </summary>
    public List<int> KeyPointIndexList { get; set; } = new();

    /// <summary>
    /// 描述子矩阵
    /// 每一行对应一个关键点的描述子
    /// </summary>
    public Mat? Descriptor;

    /// <summary>
    /// 合并后的中心区块列索引
    /// </summary>
    public int MergedCenterCellCol = -1;

    /// <summary>
    /// 合并后的中心区块行索引
    /// </summary>
    public int MergedCenterCellRow = -1;
}
