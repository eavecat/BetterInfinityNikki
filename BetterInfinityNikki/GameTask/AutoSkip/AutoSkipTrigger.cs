using BetterInfinityNikki.Core.Simulator;
using BetterInfinityNikki.GameTask.AutoSkip.Assets;
using BetterInfinityNikki.GameTask.Common;
using BetterInfinityNikki.GameTask.Common.BgiVision;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using Vanara.PInvoke;

namespace BetterInfinityNikki.GameTask.AutoSkip;

/// <summary>
/// 自动剧情触发器
/// </summary>
public class AutoSkipTrigger : ITaskTrigger
{
    private readonly ILogger<AutoSkipTrigger> _logger = App.GetLogger<AutoSkipTrigger>();

    public string Name => "自动剧情";
    public bool IsEnabled { get; set; }
    public int Priority => 20;
    public bool IsExclusive => false;
    
    public GameUiCategory SupportedGameUiCategory => GameUiCategory.Talk;

    public bool IsBackgroundRunning { get; private set; }

    private readonly AutoSkipAssets _autoSkipAssets;
    private readonly AutoSkipConfig _config;

    private DateTime _prevExecute = DateTime.MinValue;
    private const int ExecuteIntervalMs = 300; // 执行间隔，避免过于频繁的识别

    public AutoSkipTrigger()
    {
        _autoSkipAssets = AutoSkipAssets.Instance;
        _config = TaskContext.Instance().Config.AutoSkipConfig;
    }

    public void Init()
    {
        IsEnabled = _config.Enabled;
        _logger.LogInformation("自动剧情 初始化: IsEnabled={Enabled}", IsEnabled);
    }

    public void OnCapture(CaptureContent content)
    {
        // 限制执行频率
        if ((DateTime.Now - _prevExecute).TotalMilliseconds <= ExecuteIntervalMs)
        {
            return;
        }
        
        _prevExecute = DateTime.Now;

        // 如果未启用自动点击选项，则不执行
        if (!_config.ClickOptionEnabled)
        {
            return;
        }
        // 查找左上角剧情播放标识
        using var plotPlaybackRa = content.CaptureRectArea.Find(_autoSkipAssets.PlotPlaybackRo);

        if (plotPlaybackRa.IsExist())
        {
            // _logger.LogInformation("自动剧情触发");
            
            // 找到剧情标识后，查找右下角是否有跳过标识
            using var plotSkipRa = content.CaptureRectArea.Find(_autoSkipAssets.PlotSkipRo);
            
            if (plotSkipRa.IsExist())
            {
                // _logger.LogInformation("检测到跳过标识，按下F键跳过剧情");
                // 按F键跳过剧情
                Simulation.SendInput.Keyboard.KeyDown(User32.VK.VK_F);
                Thread.Sleep(40);
                Simulation.SendInput.Keyboard.KeyUp(User32.VK.VK_F);
                
                // 短暂延迟，避免重复触发
                TaskControl.Sleep(50);
            }
        }
    }
}
