using BetterInfinityNikki.GameTask.AutoFishing.Assets;
using BetterInfinityNikki.GameTask.Model.Area;
using BetterInfinityNikki.Core.Simulator;
using Fischless.WindowsInput;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using BetterInfinityNikki.GameTask.Common.Element.Assets;
using BetterInfinityNikki.GameTask.GameLoading.Assets;
using OpenCvSharp;
using Vanara.PInvoke;

namespace BetterInfinityNikki.GameTask.AutoFishing;

/// <summary>
/// 自动钓鱼触发器（半自动）
/// </summary>
public class AutoFishingTrigger : ITaskTrigger
{
    private readonly ILogger<AutoFishingTrigger> _logger = App.GetLogger<AutoFishingTrigger>();
    private readonly AutoFishingAssets _assets;
    private readonly ElementAssets _elementAssets;
    private readonly InputSimulator _input = Simulation.SendInput;

    public string Name => "自动钓鱼";
    public bool IsEnabled { get; set; }
    public int Priority => 15;
    public bool IsExclusive => false;
    public bool IsBackgroundRunning => false;

    private DateTime _prevExecute = DateTime.MinValue;
    private const int ExecuteInterval = 120; // 执行间隔（毫秒）

    // 钓鱼状态机
    private enum FishingState
    {
        Idle, // 空闲状态
        Fishing, // 钓鱼中（已抛竿）
        FishBite, // 鱼上钩（需要提竿）
        PullingLine, // 拉扯鱼线
        Reeling, // 收线
        Success, // 钓鱼成功（结算动画）
        Unknow,
    }

    private FishingState _currentState = FishingState.Idle;
    private FishingState _lastState = FishingState.Idle;
    private int _unknowStateCount;
    private bool _execing;

    // 拉扯方向检测相关
    private int _lastYellowCount; // 上次黄色像素数量
    private bool _isPullingLeft; // 当前拉扯方向（true=左，false=右）
    private DateTime _lastPullCheck = DateTime.MinValue; // 上次拉扯检测时间
    private const int PullCheckInterval = 300; // 拉扯检测间隔（毫秒）
    private bool _isKeyHeld; // 当前是否有按键被按住

    public AutoFishingTrigger()
    {
        _assets = AutoFishingAssets.Instance;
        _elementAssets = ElementAssets.Instance;
    }

    public void Init()
    {
        IsEnabled = TaskContext.Instance().Config.AutoFishingConfig.Enabled;
        _currentState = FishingState.Idle;

        _logger.LogInformation("自动钓鱼触发器初始化: IsEnabled={Enabled}", IsEnabled);
    }

    public void OnCapture(CaptureContent content)
    {
        // 检查触发器是否启用
        if (!IsEnabled || _execing) return;

        // 执行间隔控制
        if ((DateTime.Now - _prevExecute).TotalMilliseconds <= ExecuteInterval) return;

        _prevExecute = DateTime.Now;
        _lastState = _currentState;
        _execing = true;

        _currentState = GetFishingState(content);

        if (_unknowStateCount > 3)
        {
            _currentState = FishingState.Idle;
            _unknowStateCount = 0;
        }

        switch (_currentState)
        {
            case FishingState.Unknow:
                _unknowStateCount++;
                break;
            case FishingState.Idle:
                if (_lastState != FishingState.Idle)
                {
                    ReleaseAllKeys();
                }

                break;
            case FishingState.Fishing:
                if (_lastState != _currentState)
                {
                    _logger.LogInformation("开始钓鱼");
                }

                break;
            case FishingState.FishBite:
                if (_lastState != _currentState)
                {
                    _logger.LogInformation("鱼上钩啦，开始提竿");
                }

                HandleFishBite();
                break;
            case FishingState.PullingLine:
                if (_lastState != _currentState)
                {
                    _logger.LogInformation("拉扯中");
                }

                HandlePullingLine(content.CaptureRectArea);
                break;
            case FishingState.Reeling:
                if (_lastState != _currentState)
                {
                    _logger.LogInformation("收线中");
                }

                HandleReeling();
                break;
            case FishingState.Success:
                if (_lastState != _currentState)
                {
                    HandleSuccess();
                    ReleaseAllKeys();
                }

                break;
        }

        _execing = false;
    }

    private FishingState GetFishingState(CaptureContent content)
    {
        var meiyaliMenu = content.CaptureRectArea.Find(_elementAssets.MeiyaliMenuRo);
        if (!meiyaliMenu.IsEmpty())
        {
            return FishingState.Idle;
        }

        var cancelArea = content.CaptureRectArea.Find(_assets.CancelFishingRo);
        var reelRodArea = content.CaptureRectArea.Find(_assets.ReelRodRo);
        if (!cancelArea.IsEmpty() && !reelRodArea.IsEmpty())
        {
            return FishingState.Fishing;
        }

        var raiseRodArea = content.CaptureRectArea.Find(_assets.RaiseRodRo);
        if (!cancelArea.IsEmpty() && !raiseRodArea.IsEmpty())
        {
            return FishingState.FishBite;
        }

        var pullArea = content.CaptureRectArea.Find(_assets.PullFishingLineRo);
        if (!cancelArea.IsEmpty() && !pullArea.IsEmpty())
        {
            return FishingState.PullingLine;
        }

        var reelArea = content.CaptureRectArea.Find(_assets.ReelLineRo);
        if (!cancelArea.IsEmpty() && !reelArea.IsEmpty())
        {
            return FishingState.Reeling;
        }

        var skipArea = content.CaptureRectArea.Find(_assets.SkipAnimRo);
        if (!skipArea.IsEmpty() && (_lastState == FishingState.Reeling || _lastState == FishingState.PullingLine))
        {
            return FishingState.Success;
        }

        return FishingState.Unknow;
    }


    /// <summary>
    /// 鱼上钩，提竿
    /// </summary>
    private void HandleFishBite()
    {
        _input.Keyboard.KeyDown(User32.VK.VK_S);
        Thread.Sleep(50);
        _input.Keyboard.KeyUp(User32.VK.VK_S);
    }

    /// <summary>
    /// 处理拉扯鱼线阶段
    /// </summary>
    private void HandlePullingLine(ImageRegion captureRectArea)
    {
        // 检查上次拉扯时间间隔
        if ((DateTime.Now - _lastPullCheck).TotalMilliseconds < PullCheckInterval) return;
        // 获取当前黄色像素总数
        var currentYellowCount = GetYellowPixelCount(captureRectArea);

        // 如果没有按键被按住，开始新的拉扯
        if (!_isKeyHeld)
        {
            _lastYellowCount = currentYellowCount;
            StartPulling();
            return;
        }

        // 判断方向是否正确：黄色减少 > 5 像素
        if (_lastYellowCount - currentYellowCount > 5)
        {
            // 方向正确，继续按住
            _lastYellowCount = currentYellowCount;

            if (currentYellowCount == 0)
            {
                // 黄色完全消失，拉扯完成
                ReleaseCurrentKey();
            }
        }
        else
        {
            // 方向错误，黄色没有明显减少，切换方向
            SwitchPullDirection(captureRectArea);
            _lastYellowCount = currentYellowCount;
        }
    }

    /// <summary>
    /// 开始拉扯（先尝试向左）
    /// </summary>
    private void StartPulling()
    {
        _isPullingLeft = true; // 先尝试向左
        PressCurrentKey();
    }

    /// <summary>
    /// 按下当前方向的键
    /// </summary>
    private void PressCurrentKey()
    {
        var key = _isPullingLeft ? User32.VK.VK_A : User32.VK.VK_D;
        _input.Keyboard.KeyDown(key);
        Thread.Sleep(50); // 确保按键生效
        _isKeyHeld = true;
    }

    /// <summary>
    /// 释放当前按住的按键
    /// </summary>
    private void ReleaseCurrentKey()
    {
        if (_isKeyHeld)
        {
            var key = _isPullingLeft ? User32.VK.VK_A : User32.VK.VK_D;
            _input.Keyboard.KeyUp(key);
            Thread.Sleep(30);
            _isKeyHeld = false;
        }
    }

    /// <summary>
    /// 切换拉扯方向
    /// </summary>
    private void SwitchPullDirection(ImageRegion captureRectArea)
    {
        // 先抬起当前按住的键
        ReleaseCurrentKey();

        // 切换方向
        _isPullingLeft = !_isPullingLeft;

        // 按下新方向的键
        PressCurrentKey();
    }

    /// <summary>
    /// 获取黄色像素总数
    /// </summary>
    private int GetYellowPixelCount(ImageRegion captureRectArea)
    {
        try
        {
            var srcMat = captureRectArea.SrcMat;
            var width = srcMat.Width;
            var height = srcMat.Height;

            // 计算中心区域：宽60%，高40%
            var roiWidth = (int)(width * 0.6);
            var roiHeight = (int)(height * 0.4);
            var roiX = (width - roiWidth) / 2;
            var roiY = (height - roiHeight) / 2;

            // 裁剪中心区域
            using var roiMat = new Mat(srcMat, new OpenCvSharp.Rect(roiX, roiY, roiWidth, roiHeight));

            if (roiMat.Empty())
            {
                return 0;
            }

            // 定义黄色范围 RGB(255, 220, 165) - RGB(230, 250, 200)
            // OpenCV使用BGR格式，所以是 BGR(165, 220, 255) - BGR(200, 250, 230)
            var lowerYellow = new Scalar(165, 220, 230); // BGR格式
            var upperYellow = new Scalar(200, 250, 255); // BGR格式

            // 创建掩膜，提取黄色区域
            using var mask = new Mat();
            Cv2.InRange(roiMat, lowerYellow, upperYellow, mask);

            // 统计黄色像素数量
            return Cv2.CountNonZero(mask);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取黄色像素数量时发生异常");
            return 0;
        }
    }

    /// <summary>
    /// 处理收线阶段
    /// </summary>
    private void HandleReeling()
    {
        _input.Mouse.RightButtonDown();
        Thread.Sleep(50);
        _input.Mouse.RightButtonUp();
    }

    /// <summary>
    /// 处理成功状态（跳过动画）
    /// </summary>
    private void HandleSuccess()
    {
        _input.Keyboard.KeyDown(User32.VK.VK_F);
        Thread.Sleep(50);
        _input.Keyboard.KeyUp(User32.VK.VK_F);
    }

    /// <summary>
    /// 释放所有可能按下的按键
    /// </summary>
    private void ReleaseAllKeys()
    {
        // 抬起方向键
        _input.Keyboard.KeyUp(User32.VK.VK_A);
        _input.Keyboard.KeyUp(User32.VK.VK_D);

        // 抬起其他可能的按键
        _input.Keyboard.KeyUp(User32.VK.VK_S);
        _input.Keyboard.KeyUp(User32.VK.VK_F);

        // 抬起鼠标按键
        _input.Mouse.LeftButtonUp();
        _input.Mouse.RightButtonUp();

        _isKeyHeld = false;
        _isPullingLeft = false;
    }
}