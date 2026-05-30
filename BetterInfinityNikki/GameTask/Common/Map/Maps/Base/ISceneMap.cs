using OpenCvSharp;

namespace BetterInfinityNikki.GameTask.Common.Map.Maps.Base;

/// <summary>
/// 场景地图接口
/// 定义地图的基本操作
/// </summary>
public interface ISceneMap
{
    /// <summary>
    /// 地图类型
    /// </summary>
    string Type { get; set; }

    /// <summary>
    /// 地图大小（像素）
    /// </summary>
    Size MapSize { get; set; }

    /// <summary>
    /// 获取大地图在游戏世界地图中的位置
    /// </summary>
    /// <param name="greyBigMapMat">灰度化的大地图截图</param>
    /// <returns>在游戏世界地图中的矩形区域</returns>
    Rect GetBigMapRect(Mat greyBigMapMat);

    /// <summary>
    /// 获取大地图位置（带上一帧位置的自适应搜索）
    /// </summary>
    /// <param name="greyBigMapMat">灰度化的大地图截图</param>
    /// <param name="prevRect">上一帧检测到的视口位置</param>
    /// <returns>在游戏世界地图中的矩形区域</returns>
    Rect GetBigMapRect(Mat greyBigMapMat, Rect prevRect);
}
