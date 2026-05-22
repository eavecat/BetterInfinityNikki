using BetterInfinityNikki.GameTask.AutoFishing.Assets;
using BetterInfinityNikki.GameTask.Model.Area;
using BetterInfinityNikki.Core.Simulator;
using Fischless.WindowsInput;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
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
    private readonly GameLoadingAssets _gameLoadingAssets;
    private readonly InputSimulator _input = Simulation.SendInput;
    
    public string Name => "自动钓鱼";
    public bool IsEnabled { get; set; }
    public int Priority => 15;
    public bool IsExclusive => false;
    public bool IsBackgroundRunning => false;

    private DateTime _prevExecute = DateTime.MinValue;
    private const int ExecuteInterval = 67; // 执行间隔（毫秒）

    // 钓鱼状态机
    private enum FishingState
    {
        Idle,               // 空闲状态
        Fishing,            // 钓鱼中（已抛竿）
        FishBite,           // 鱼上钩（需要提竿）
        PullingLine,        // 拉扯鱼线
        Reeling,            // 收线
        Success             // 钓鱼成功（结算动画）
    }

    private FishingState _currentState = FishingState.Idle;
    private DateTime _stateStartTime = DateTime.MinValue;
    private bool _isPullingLeft = false; // 当前是否向左拉扯
    private DateTime _lastPullDirectionChange = DateTime.MinValue;
    private const int PullDirectionInterval = 500; // 拉扯方向切换间隔（毫秒）
    private DateTime _lastReelClick = DateTime.MinValue;
    private const int ReelClickInterval = 200; // 收线点击间隔（毫秒）

    public AutoFishingTrigger()
    {
        _assets = AutoFishingAssets.Instance;
        _gameLoadingAssets = GameLoadingAssets.Instance;
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
        if (!IsEnabled)
        {
            return;
        }

        // 执行间隔控制
        if ((DateTime.Now - _prevExecute).TotalMilliseconds <= ExecuteInterval)
        {
            return;
        }

        _prevExecute = DateTime.Now;

        try
        {
            ProcessFishingLogic(content.CaptureRectArea);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "自动钓鱼: 处理异常");
        }
    }

    /// <summary>
    /// 处理钓鱼逻辑（状态机）
    /// </summary>
    private void ProcessFishingLogic(ImageRegion captureRectArea)
    {
        // 优先检测是否退出钓鱼（检测到美鸭梨菜单）
        if (CheckExitFishing(captureRectArea))
        {
            return;
        }

        switch (_currentState)
        {
            case FishingState.Idle:
                CheckEnterFishing(captureRectArea);
                break;
            case FishingState.Fishing:
                CheckFishBite(captureRectArea);
                break;
            case FishingState.FishBite:
                HandleFishBite(captureRectArea);
                break;
            case FishingState.PullingLine:
                HandlePullingLine(captureRectArea);
                break;
            case FishingState.Reeling:
                HandleReeling(captureRectArea);
                break;
            case FishingState.Success:
                HandleSuccess(captureRectArea);
                break;
        }
    }

    /// <summary>
    /// 检测是否进入钓鱼状态（检测到取消钓鱼按钮）
    /// </summary>
    private void CheckEnterFishing(ImageRegion captureRectArea)
    {
        var cancelArea = captureRectArea.Find(_assets.CancelFishingRo);
        if (!cancelArea.IsEmpty())
        {
            _logger.LogInformation("🎣 检测到钓鱼界面，进入钓鱼状态");
            _currentState = FishingState.Fishing;
            _stateStartTime = DateTime.Now;
        }
    }

    /// <summary>
    /// 检测鱼是否上钩（检测到提竿按钮）
    /// </summary>
    private void CheckFishBite(ImageRegion captureRectArea)
    {
        var raiseRodArea = captureRectArea.Find(_assets.RaiseRodRo);
        if (!raiseRodArea.IsEmpty())
        {
            _logger.LogInformation("🐟 鱼上钩了！");
            _currentState = FishingState.FishBite;
            _stateStartTime = DateTime.Now;
            
            // 自动提竿
            _input.Keyboard.KeyDown(User32.VK.VK_S);
            Thread.Sleep(50);
            _input.Keyboard.KeyUp(User32.VK.VK_S);
            _logger.LogInformation("⬆️ 自动提竿");
        }
    }

    /// <summary>
    /// 处理鱼上钩状态（等待进入拉扯阶段）
    /// </summary>
    private void HandleFishBite(ImageRegion captureRectArea)
    {
        // 检测是否进入拉扯阶段
        var pullArea = captureRectArea.Find(_assets.PullFishingLineRo);
        if (!pullArea.IsEmpty())
        {
            _logger.LogInformation("🎯 进入拉扯鱼线阶段");
            _currentState = FishingState.PullingLine;
            _stateStartTime = DateTime.Now;
            _isPullingLeft = false;
            _lastPullDirectionChange = DateTime.Now;
        }
        
        // 如果长时间没有进入拉扯阶段，可能提竿失败，返回钓鱼状态
        if ((DateTime.Now - _stateStartTime).TotalSeconds > 3)
        {
            _logger.LogWarning("⚠️ 提竿后未进入拉扯阶段，返回钓鱼状态");
            _currentState = FishingState.Fishing;
        }
    }

    /// <summary>
    /// 处理拉扯鱼线阶段
    /// </summary>
    private void HandlePullingLine(ImageRegion captureRectArea)
    {
        // 检测是否还在拉扯阶段
        var pullArea = captureRectArea.Find(_assets.PullFishingLineRo);
        if (pullArea.IsEmpty())
        {
            // 拉扯结束，检测是否进入收线阶段
            var reelArea = captureRectArea.Find(_assets.ReelLineRo);
            if (!reelArea.IsEmpty())
            {
                _logger.LogInformation("✅ 拉扯完成，进入收线阶段");
                _currentState = FishingState.Reeling;
                _stateStartTime = DateTime.Now;
            }
            else
            {
                // 可能钓鱼失败或中断
                _logger.LogWarning("⚠️ 拉扯阶段异常结束");
                ResetToFishingOrIdle(captureRectArea);
            }
            return;
        }

        // 检测黄色圆形进度条，智能决定拉扯方向
        var pullDirection = DetectPullDirection(captureRectArea);
        
        var now = DateTime.Now;
        if ((now - _lastPullDirectionChange).TotalMilliseconds >= PullDirectionInterval)
        {
            _lastPullDirectionChange = now;
            
            if (pullDirection == PullDirection.Left)
            {
                _input.Keyboard.KeyDown(Vanara.PInvoke.User32.VK.VK_A);
                _input.Keyboard.KeyUp(Vanara.PInvoke.User32.VK.VK_D);
                //_logger.LogDebug("⬅️ 向左拉扯");
            }
            else if (pullDirection == PullDirection.Right)
            {
                _input.Keyboard.KeyDown(Vanara.PInvoke.User32.VK.VK_D);
                _input.Keyboard.KeyUp(Vanara.PInvoke.User32.VK.VK_A);
                //_logger.LogDebug("➡️ 向右拉扯");
            }
            else
            {
                // 无法判断方向，使用左右交替策略
                _isPullingLeft = !_isPullingLeft;
                if (_isPullingLeft)
                {
                    _input.Keyboard.KeyDown(Vanara.PInvoke.User32.VK.VK_A);
                    _input.Keyboard.KeyUp(Vanara.PInvoke.User32.VK.VK_D);
                }
                else
                {
                    _input.Keyboard.KeyDown(Vanara.PInvoke.User32.VK.VK_D);
                    _input.Keyboard.KeyUp(Vanara.PInvoke.User32.VK.VK_A);
                }
            }
        }
    }

    /// <summary>
    /// 拉扯方向枚举
    /// </summary>
    private enum PullDirection
    {
        Unknown,
        Left,
        Right
    }

    /// <summary>
    /// 检测拉扯方向（通过黄色圆形进度条）
    /// 检测范围：中心宽60%，高40%
    /// 黄色范围：RGB(255, 220, 165) - RGB(230, 250, 200)
    /// </summary>
    private PullDirection DetectPullDirection(ImageRegion captureRectArea)
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
                return PullDirection.Unknown;
            }

            // 定义黄色范围 RGB(255, 220, 165) - RGB(230, 250, 200)
            // OpenCV使用BGR格式，所以是 BGR(165, 220, 255) - BGR(200, 250, 230)
            var lowerYellow = new Scalar(165, 220, 230); // BGR格式
            var upperYellow = new Scalar(200, 250, 255); // BGR格式

            // 创建掩膜，提取黄色区域
            using var mask = new Mat();
            Cv2.InRange(roiMat, lowerYellow, upperYellow, mask);

            // 统计左半部分和右半部分的黄色像素数量
            var leftRoi = new OpenCvSharp.Rect(0, 0, roiWidth / 2, roiHeight);
            var rightRoi = new OpenCvSharp.Rect(roiWidth / 2, 0, roiWidth / 2, roiHeight);

            using var leftMask = new Mat(mask, leftRoi);
            using var rightMask = new Mat(mask, rightRoi);

            var leftYellowCount = Cv2.CountNonZero(leftMask);
            var rightYellowCount = Cv2.CountNonZero(rightMask);

            // 如果黄色像素太少，无法判断
            var totalYellow = leftYellowCount + rightYellowCount;
            if (totalYellow < 100)
            {
                return PullDirection.Unknown;
            }

            // 黄色越多表示进度越低，应该往黄色多的方向拉扯
            // 左边黄色多 -> 向左拉扯
            // 右边黄色多 -> 向右拉扯
            if (leftYellowCount > rightYellowCount * 1.2)
            {
                //_logger.LogDebug($"📊 黄色分布: 左={leftYellowCount}, 右={rightYellowCount} -> 向左");
                return PullDirection.Left;
            }
            else if (rightYellowCount > leftYellowCount * 1.2)
            {
                //_logger.LogDebug($"📊 黄色分布: 左={leftYellowCount}, 右={rightYellowCount} -> 向右");
                return PullDirection.Right;
            }
            else
            {
                return PullDirection.Unknown;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "检测拉扯方向时发生异常");
            return PullDirection.Unknown;
        }
    }

    /// <summary>
    /// 处理收线阶段
    /// </summary>
    private void HandleReeling(ImageRegion captureRectArea)
    {
        // 检测是否还在收线阶段
        var reelArea = captureRectArea.Find(_assets.ReelLineRo);
        if (reelArea.IsEmpty())
        {
            // 收线结束，检测是否成功
            var skipArea = captureRectArea.Find(_assets.SkipAnimRo);
            if (!skipArea.IsEmpty())
            {
                _logger.LogInformation("🎉 钓鱼成功！进入结算动画");
                _currentState = FishingState.Success;
                _stateStartTime = DateTime.Now;
            }
            else
            {
                // 可能回到拉扯阶段（继续钓）
                var pullArea = captureRectArea.Find(_assets.PullFishingLineRo);
                if (!pullArea.IsEmpty())
                {
                    _logger.LogInformation("🔄 收线完成，回到拉扯阶段");
                    _currentState = FishingState.PullingLine;
                    _stateStartTime = DateTime.Now;
                }
                else
                {
                    _logger.LogWarning("⚠️ 收线阶段异常结束");
                    ResetToFishingOrIdle(captureRectArea);
                }
            }
            return;
        }

        // 不断点击右键收线
        var now = DateTime.Now;
        if ((now - _lastReelClick).TotalMilliseconds >= ReelClickInterval)
        {
            _input.Mouse.RightButtonClick();
            _lastReelClick = now;
            //_logger.LogDebug("🖱️ 右键收线");
        }
    }

    /// <summary>
    /// 处理成功状态（跳过动画）
    /// </summary>
    private void HandleSuccess(ImageRegion captureRectArea)
    {
        // 检测跳过按钮
        var skipArea = captureRectArea.Find(_assets.SkipAnimRo);
        if (!skipArea.IsEmpty())
        {
            // 按F跳过动画
            _input.Keyboard.KeyPress(Vanara.PInvoke.User32.VK.VK_F);
            _logger.LogInformation("⏭️ 跳过结算动画");
        }
        
        // 等待动画结束，返回钓鱼状态或空闲状态
        if ((DateTime.Now - _stateStartTime).TotalSeconds > 2)
        {
            ResetToFishingOrIdle(captureRectArea);
        }
    }

    /// <summary>
    /// 重置到钓鱼状态或空闲状态
    /// </summary>
    private void ResetToFishingOrIdle(ImageRegion captureRectArea)
    {
        var cancelArea = captureRectArea.Find(_assets.CancelFishingRo);
        if (!cancelArea.IsEmpty())
        {
            _logger.LogInformation("🔄 返回钓鱼状态");
            _currentState = FishingState.Fishing;
        }
        else
        {
            _logger.LogInformation("🔚 退出钓鱼状态");
            _currentState = FishingState.Idle;
        }
        _stateStartTime = DateTime.MinValue;
    }

    /// <summary>
    /// 检测是否退出钓鱼（检测到美鸭梨菜单）
    /// 美鸭梨菜单位于左上角1/4×1/4区域
    /// </summary>
    private bool CheckExitFishing(ImageRegion captureRectArea)
    {
        // 只有在钓鱼相关状态下才需要检测退出
        if (_currentState == FishingState.Idle)
        {
            return false;
        }

        try
        {
            var meiyaliMenu = captureRectArea.Find(_gameLoadingAssets.MeiyaliMenuRo);
            if (!meiyaliMenu.IsEmpty())
            {
                _logger.LogInformation("🚪 检测到美鸭梨菜单，用户已退出钓鱼");
                _currentState = FishingState.Idle;
                _stateStartTime = DateTime.MinValue;
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "检测美鸭梨菜单时发生异常");
            return false;
        }
    }
}
