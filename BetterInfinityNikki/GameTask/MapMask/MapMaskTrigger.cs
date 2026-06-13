using System;
using System.Threading;
using BetterInfinityNikki.Core.Config;
using BetterInfinityNikki.Core.Recognition;
using BetterInfinityNikki.GameTask;
using BetterInfinityNikki.GameTask.Common;
using BetterInfinityNikki.GameTask.Common.Element.Assets;
using BetterInfinityNikki.GameTask.Common.Map.Maps;
using BetterInfinityNikki.GameTask.MapMask.Assets;
using BetterInfinityNikki.Helpers;
using BetterInfinityNikki.View;
using BetterInfinityNikki.ViewModel;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using Rect = System.Windows.Rect;

namespace BetterInfinityNikki.GameTask.MapMask;

/// <summary>
/// 地图遮罩触发器
/// </summary>
public class MapMaskTrigger : ITaskTrigger
{
    private readonly ILogger<MapMaskTrigger> _logger;

    public string Name => "地图遮罩";
    public bool IsEnabled { get; set; }
    public int Priority => 1; // 低优先级
    public bool IsExclusive => false;

    private readonly MapMaskConfig _config;
    private readonly MapMaskAssets _mapMaskAssets;
    private readonly ElementAssets _elementAssets;
    private readonly NikkiWorldMap _worldMap;
    private DateTime _prevExecute = DateTime.MinValue;
    private OpenCvSharp.Rect _prevRect = default;

    public MapMaskTrigger()
    {
        _logger = App.GetLogger<MapMaskTrigger>();
        _config = TaskContext.Instance().Config.MapMaskConfig;
        _mapMaskAssets = MapMaskAssets.Instance;
        _elementAssets = ElementAssets.Instance;
        _worldMap = new NikkiWorldMap();
    }

    /// <summary>
    /// 初始化触发器状态，并在关闭时同步隐藏遮罩UI
    /// </summary>
    public void Init()
    {
        IsEnabled = _config.Enabled;

        // 启用时预加载地图资源
        if (IsEnabled)
        {
            _ = Task.Run(() =>
            {
                try
                {
                    _worldMap.WarmUp();
                    _logger.LogInformation("地图遮罩资源预加载完成");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "地图遮罩资源预加载失败");
                }
            });
        }

        // 关闭时隐藏UI
        if (!IsEnabled)
        {
            UIDispatcherHelper.BeginInvoke(() =>
            {
                var window = MaskWindow.InstanceNullable();
                if (window?.DataContext is MaskWindowViewModel vm)
                {
                    vm.IsInBigMapUi = false;
                }

                window?.MapPointsCanvas?.UpdateViewport(0, 0, 0, 0);
            });
        }
    }

    /// <summary>
    /// 接收每帧截图内容并驱动大地图的异步定位与UI更新
    /// </summary>
    /// <param name="content">捕获到的画面内容</param>
    public void OnCapture(CaptureContent content)
    {
        // 限制执行频率
        if ((DateTime.Now - _prevExecute).TotalMilliseconds <= 100)
        {
            return;
        }

        _prevExecute = DateTime.Now;

        try
        {
            // 检测是否在大地图界面
            var inBigMapUi = DetectBigMap(content);

            // 更新 ViewModel 中的大地图状态
            UIDispatcherHelper.BeginInvoke(() =>
            {
                var window = MaskWindow.InstanceNullable();
                if (window?.DataContext is MaskWindowViewModel vm)
                {
                    vm.IsInBigMapUi = inBigMapUi;
                }
            });

            if (inBigMapUi && _config.Enabled)
            {
                var viewport = CalculateBigMapViewport(content);

                if (viewport.HasValue)
                {
                    QueueUiUpdate(viewport.Value);
                }
            }
            else
            {
                QueueUiUpdate(null);
            }
        }
        catch (Exception e)
        {
            _logger.LogDebug(e, "地图遮罩处理异常");
        }
    }

    /// <summary>
    /// 检测是否处于大地图界面
    /// </summary>
    private bool DetectBigMap(CaptureContent content)
    {
        try
        {
            var region = content.CaptureRectArea;
            var hasMapTitle = false;
            var hasMapReturn = false;
            var hasMapFilter = false;

            // 检测1：左上角地图文字
            using var mapTitleRa = region.Find(_mapMaskAssets.MapTitleRo);
            if (mapTitleRa.IsExist())
            {
                hasMapTitle = true;
            }

            // 检测2：左上角返回按钮
            using var returnRa = region.Find(_elementAssets.ReturnButtonRo);
            if (returnRa.IsExist())
            {
                hasMapReturn = true;
            }

            // 检测3：右下角筛选按钮
            using var filterRa = region.Find(_elementAssets.FilterButtonRo);
            if (filterRa.IsExist())
            {
                hasMapFilter = true;
            }

            if (hasMapTitle && hasMapReturn && hasMapFilter)
            {
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "大地图检测异常");
            return false;
        }
    }


    /// <summary>
    /// 计算大地图视口位置（使用 SIFT 特征匹配）
    /// </summary>
    private Rect? CalculateBigMapViewport(CaptureContent content)
    {
        try
        {
            var region = content.CaptureRectArea;

            // 获取灰度化的大地图截图
            using var greyMat = region.CacheGreyMat.Clone();

            // 调用世界地图的 GetBigMapRect 方法（支持自适应搜索）
            var rect = _worldMap.GetBigMapRect(greyMat, _prevRect);

            if (rect != default)
            {
                // 验证矩形尺寸是否合理（地图坐标空间 16384x16384）
                // 视口宽度应在 200~14000，高度在 200~14000 之间
                if (rect.Width < 200 || rect.Height < 200 || rect.Width > 14000 || rect.Height > 14000)
                {
                    _logger.LogDebug("大地图视口尺寸异常，重置: {Rect}", rect);
                    _prevRect = default;
                    return null;
                }

                // 更新上一帧位置（用于自适应搜索）
                _prevRect = rect;

                // 转换为 System.Windows.Rect
                var resultRect = new Rect(rect.X, rect.Y, rect.Width, rect.Height);
                _logger.LogDebug("大地图视口检测成功: {Rect}", resultRect);
                return resultRect;
            }

            // 匹配失败时重置 prevRect，下次使用全图搜索
            if (_prevRect != default)
            {
                _logger.LogDebug("大地图视口检测失败，重置 prevRect 以使用全图搜索");
                _prevRect = default;
            }
            else
            {
                _logger.LogDebug("大地图视口检测失败，未找到匹配");
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "计算大地图视口异常");
            return null;
        }
    }

    private Rect? _pendingViewport;
    private int _uiApplyScheduled;
    private readonly object _viewportLock = new();

    /// <summary>
    /// 合并并异步投递UI更新
    /// </summary>
    private void QueueUiUpdate(Rect? viewport)
    {
        lock (_viewportLock)
        {
            _pendingViewport = viewport;
        }
        TryScheduleUiApply();
    }

    /// <summary>
    /// 确保仅有一个UI更新调度在队列中
    /// </summary>
    private void TryScheduleUiApply()
    {
        if (Interlocked.Exchange(ref _uiApplyScheduled, 1) == 0)
        {
            UIDispatcherHelper.BeginInvoke(ApplyPendingUiUpdate);
        }
    }

    /// <summary>
    /// 在UI线程应用合并后的更新
    /// </summary>
    private void ApplyPendingUiUpdate()
    {
        Rect? viewport;
        lock (_viewportLock)
        {
            viewport = _pendingViewport;
            _pendingViewport = null;
        }
        var window = MaskWindow.InstanceNullable();

        if (!_config.Enabled)
        {
            window?.MapPointsCanvas?.UpdateViewport(0, 0, 0, 0);
            Interlocked.Exchange(ref _uiApplyScheduled, 0);
            return;
        }

        if (viewport.HasValue)
        {
            window?.MapPointsCanvas?.UpdateViewport(
                viewport.Value.X, viewport.Value.Y,
                viewport.Value.Width, viewport.Value.Height);
        }
        else
        {
            // null 表示不在大地图，清空视口
            window?.MapPointsCanvas?.UpdateViewport(0, 0, 0, 0);
        }

        Interlocked.Exchange(ref _uiApplyScheduled, 0);
    }
}
