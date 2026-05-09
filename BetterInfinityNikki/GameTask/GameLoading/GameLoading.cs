using BetterInfinityNikki.Core.Config;
using BetterInfinityNikki.Core.Recognition;
using BetterInfinityNikki.Core.Recognition.OCR;
using BetterInfinityNikki.GameTask.Common.BgiVision;
using BetterInfinityNikki.GameTask.GameLoading.Assets;
using BetterInfinityNikki.GameTask.Model.Area;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using System;
using System.Diagnostics;

namespace BetterInfinityNikki.GameTask.GameLoading;

/// <summary>
/// 自动进入游戏触发器
/// </summary>
public class GameLoadingTrigger : ITaskTrigger
{
    /// <summary>
    /// 全局启用状态
    /// </summary>
    public static bool GlobalEnabled = true;

    public string Name => "自动进入游戏";

    public bool IsEnabled
    {
        get => GlobalEnabled;
        set { }
    }

    public int Priority => 999;

    public bool IsExclusive => false;

    public bool IsBackgroundRunning => true;

    /// <summary>
    /// 支持所有UI类别（包括Unknown），确保在任何界面都能检测
    /// </summary>
    public GameUiCategory SupportedGameUiCategory => GameUiCategory.Unknown;

    private readonly GameLoadingAssets _assets;
    private readonly GameStartConfig _config = TaskContext.Instance().Config.GameStartConfig;
    private static readonly ILogger<GameLoadingTrigger> _logger = App.GetLogger<GameLoadingTrigger>();

    private DateTime _prevExecuteTime = DateTime.MinValue;
    private DateTime _triggerStartTime = DateTime.Now;
    private bool _hasClickedEnterGame = false;

    public GameLoadingTrigger()
    {
        GameLoadingAssets.DestroyInstance();
        _assets = GameLoadingAssets.Instance;
    }

    public void InnerSetEnabled(bool enabled)
    {
        GlobalEnabled = enabled;
    }

    public void Init()
    {
        _logger.LogInformation($"自动进入游戏功能 Init() 被调用，配置 AutoEnterGameEnabled={_config.AutoEnterGameEnabled}");

        // 如果配置未启用，则禁用此触发器
        if (!_config.AutoEnterGameEnabled)
        {
            _logger.LogWarning("自动进入游戏功能已禁用（配置未开启）");
            InnerSetEnabled(false);
            return;
        }

        // 重置状态
        _triggerStartTime = DateTime.Now;
        _hasClickedEnterGame = false;

        _logger.LogInformation("自动进入游戏功能已初始化，GlobalEnabled={GlobalEnabled}", GlobalEnabled);
    }

    public void OnCapture(CaptureContent content)
    {
        // 限流：每 2 秒执行一次
        if ((DateTime.Now - _prevExecuteTime).TotalMilliseconds <= 2000)
        {
            return;
        }

        _logger.LogDebug("OnCapture 被调用，GlobalEnabled={GlobalEnabled}, IsEnabled={IsEnabled}", GlobalEnabled,
            IsEnabled);

        _prevExecuteTime = DateTime.Now;

        // 5 分钟后自动停止
        if ((DateTime.Now - _triggerStartTime).TotalMinutes >= 5)
        {
            _logger.LogInformation("自动进入游戏超时（5分钟），已自动停止");
            InnerSetEnabled(false);
            return;
        }

        try
        {
            ProcessGameLoading(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "自动进入游戏处理异常");
        }
    }

    /// <summary>
    /// 处理游戏加载逻辑
    /// </summary>
    private void ProcessGameLoading(CaptureContent content)
    {
        // 检测是否已经进入游戏主界面
        if (IsInMainUi(content.CaptureRectArea))
        {
            InnerSetEnabled(false);
            return;
        }

        // 已经点击过按钮，无需重复操作
        if (_hasClickedEnterGame)
        {
            return;
        }

        var roi = _assets.EnterGameRo.RegionOfInterest;

        // 使用 OCR 识别 ROI 区域内的文字
        try
        {
            // 提取 ROI 区域
            using var roiMat = new Mat(content.CaptureRectArea.SrcMat, roi);

            // 执行 OCR 识别
            var ocrResult = OcrFactory.Paddle.OcrResult(roiMat);

            // 查找包含"进入游戏"关键词的文字区域
            foreach (var region in ocrResult.Regions)
            {
                var text = region.Text.Trim();

                // 匹配关键词（支持中英文）
                if (text.Contains("点击进入游戏") /*||
                    text.Contains("开始游戏") ||
                    text.ToLower().Contains("start") ||
                    text.ToLower().Contains("enter")*/)
                {
                    _logger.LogInformation("✅ OCR 检测到按钮文字: '{Text}'", text);

                    // 计算按钮在屏幕上的绝对位置
                    var buttonRect = region.Rect.BoundingRect();
                    var absoluteX = roi.X + buttonRect.X + buttonRect.Width / 2;
                    var absoluteY = roi.Y + buttonRect.Y + buttonRect.Height / 2;

                    // 点击按钮中心
                    content.CaptureRectArea.ClickTo(absoluteX, absoluteY);
                    _hasClickedEnterGame = true;

                    _logger.LogInformation("已点击\"进入游戏\"按钮");
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OCR 识别失败");
        }
    }

    /// <summary>
    /// 判断是否在游戏主界面
    /// </summary>
    /// <param name="captureRectArea">截图区域</param>
    /// <returns>是否在主界面</returns>
    private bool IsInMainUi(ImageRegion captureRectArea)
    {
        try
        {
            var meiyaliMenu = captureRectArea.Find(_assets.MeiyaliMenuRo);
            if (!meiyaliMenu.IsEmpty())
            {
                _logger.LogDebug("检测到\"美鸭梨\"菜单按钮，确认在主界面");
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "IsInMainUi检测异常");
            return false;
        }
    }
}