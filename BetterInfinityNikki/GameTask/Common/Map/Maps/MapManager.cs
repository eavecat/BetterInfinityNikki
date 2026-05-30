using System.Collections.Generic;
using BetterInfinityNikki.GameTask.Common.Map.Maps.Base;

namespace BetterInfinityNikki.GameTask.Common.Map.Maps;

/// <summary>
/// 地图管理器
/// 负责管理和缓存所有地图实例，提供统一的地图访问接口
/// </summary>
public static class MapManager
{
    /// <summary>
    /// 地图实例缓存
    /// Key: 地图类型名称（如 "NikkiWorld"）
    /// Value: 地图实例
    /// </summary>
    private static readonly Dictionary<string, ISceneMap> _maps = new();

    /// <summary>
    /// 线程锁，确保多线程安全
    /// </summary>
    private static readonly object LockObject = new();

    /// <summary>
    /// 获取指定类型的地图实例（单例模式）
    /// </summary>
    /// <param name="mapType">地图类型</param>
    /// <returns>地图实例</returns>
    public static ISceneMap GetMap(MapTypes mapType)
    {
        var key = mapType.ToString();

        // 第一次检查（无锁）
        if (_maps.TryGetValue(key, out var map))
        {
            return map;
        }

        // 双重检查锁定
        lock (LockObject)
        {
            // 第二次检查（有锁）
            if (_maps.TryGetValue(key, out map))
            {
                return map;
            }

            // 创建新地图实例
            map = CreateMap(mapType);
            _maps[key] = map;
            return map;
        }
    }

    /// <summary>
    /// 根据地图类型创建对应的地图实例
    /// </summary>
    /// <param name="mapType">地图类型</param>
    /// <returns>新创建的地图实例</returns>
    /// <exception cref="System.ArgumentException">未知的地图类型</exception>
    private static ISceneMap CreateMap(MapTypes mapType)
    {
        return mapType switch
        {
            MapTypes.NikkiWorld => new NikkiWorldMap(),
            MapTypes.HuayuanTown => throw new System.NotImplementedException("花愿镇地图尚未实现"),
            MapTypes.Custom => throw new System.NotImplementedException("自定义地图尚未实现"),
            _ => throw new System.ArgumentException($"未知的地图类型: {mapType}", nameof(mapType))
        };
    }

    /// <summary>
    /// 清除所有缓存的地图实例
    /// 用于资源重载或测试场景
    /// </summary>
    public static void ClearCache()
    {
        lock (LockObject)
        {
            _maps.Clear();
        }
    }

    /// <summary>
    /// 清除指定类型的地图缓存
    /// </summary>
    /// <param name="mapType">地图类型</param>
    public static void ClearCache(MapTypes mapType)
    {
        lock (LockObject)
        {
            _maps.Remove(mapType.ToString());
        }
    }
}
