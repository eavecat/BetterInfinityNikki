using System;
using System.Collections.Generic;
using OpenCvSharp;
using OpenCvSharp.Features2D;
using OpenCvSharp.XFeatures2D;

namespace BetterInfinityNikki.Core.Recognition.OpenCv.FeatureMatch;

/// <summary>
/// 特征检测器工厂类
/// 用于创建和管理 SIFT、SURF 等特征检测器实例
/// 使用单例模式避免重复创建，提高性能
/// </summary>
public static class Feature2DFactory
{
    /// <summary>
    /// 特征检测器实例缓存
    /// Key: 特征检测器类型
    /// Value: 对应的 Feature2D 实例
    /// </summary>
    private static readonly Dictionary<Feature2DType, Feature2D> Instances = new();

    /// <summary>
    /// 线程锁，确保多线程安全
    /// </summary>
    private static readonly object Lock = new();

    /// <summary>
    /// 获取指定类型的特征检测器实例（单例）
    /// </summary>
    /// <param name="type">特征检测器类型</param>
    /// <returns>Feature2D 实例</returns>
    /// <exception cref="ArgumentException">不支持的特征检测器类型</exception>
    public static Feature2D Get(Feature2DType type)
    {
        // 先检查缓存，避免不必要的锁竞争
        if (Instances.TryGetValue(type, out var cachedInstance))
        {
            return cachedInstance;
        }

        lock (Lock)
        {
            // 双重检查锁定（Double-Check Locking）
            if (Instances.TryGetValue(type, out var instance))
            {
                return instance;
            }

            // 根据类型创建对应的特征检测器
            instance = type switch
            {
                Feature2DType.SIFT => CreateSift(),
                Feature2DType.SURF => CreateSurf(),
                _ => throw new ArgumentException($"不支持的特征检测器类型: {type}")
            };

            // 缓存实例
            Instances[type] = instance;
            return instance;
        }
    }

    /// <summary>
    /// 创建 SIFT 特征检测器
    /// </summary>
    /// <returns>SIFT 实例</returns>
    private static Feature2D CreateSift()
    {
        // SIFT 参数说明：
        // nFeatures: 保留的最佳特征数量（基于响应强度），0 表示保留所有
        // nOctaveLayers: 每个 octave 中的层数，默认 3
        // contrastThreshold: 对比度阈值，过滤弱特征，值越小特征越多
        // edgeThreshold: 边缘阈值，过滤边缘响应，值越大特征越多
        // sigma: 高斯模糊的标准差，用于构建尺度空间
        return SIFT.Create(
            nFeatures: 0,              // 不限制特征数量
            nOctaveLayers: 3,          // 每个 octave 3 层
            contrastThreshold: 0.03,   // 平衡点：0.03（介于 0.02 和 0.04 之间）
            edgeThreshold: 12,         // 平衡点：12（介于 10 和 15 之间）
            sigma: 1.5                 // 平衡点：1.5（介于 1.4 和 1.6 之间）
        );
    }

    /// <summary>
    /// 创建 SURF 特征检测器
    /// </summary>
    /// <returns>SURF 实例</returns>
    private static Feature2D CreateSurf()
    {
        // SURF 参数说明：
        // hessianThreshold: Hessian 矩阵行列式阈值，值越大检测到的特征越少
        // nOctaves: octave 数量，默认 4
        // nOctaveLayers: 每个 octave 中的层数，默认 3
        // extended: 是否使用扩展描述子（128 维 vs 64 维）
        // upright: 是否计算旋转不变性，true 表示不计算（速度更快）
        return SURF.Create(
            hessianThreshold: 100,     // Hessian 阈值 100
            nOctaves: 4,               // 4 个 octave
            nOctaveLayers: 3,          // 每个 octave 3 层
            extended: false,           // 使用 64 维描述子
            upright: false             // 计算旋转不变性
        );
    }

    /// <summary>
    /// 清除所有缓存的实例
    /// 用于释放资源或重新初始化
    /// </summary>
    public static void ClearCache()
    {
        lock (Lock)
        {
            foreach (var instance in Instances.Values)
            {
                instance?.Dispose();
            }
            Instances.Clear();
        }
    }
}
