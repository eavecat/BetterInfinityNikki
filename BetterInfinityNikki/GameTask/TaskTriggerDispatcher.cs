using BetterInfinityNikki.Core.Config;
using BetterInfinityNikki.GameTask.Common;
using BetterInfinityNikki.Helpers;
using Fischless.GameCapture;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using BetterInfinityNikki.GameTask.Common.BgiVision;
using BetterInfinityNikki.GameTask.GameLoading;
using Rect = OpenCvSharp.Rect;

namespace BetterInfinityNikki.GameTask
{
    public class TaskTriggerDispatcher : IDisposable
    {
        private readonly ILogger<TaskTriggerDispatcher> _logger = App.GetLogger<TaskTriggerDispatcher>();

        private static TaskTriggerDispatcher? _instance;

        private readonly System.Timers.Timer _timer = new();
        private List<ITaskTrigger>? _triggers;

        public IGameCapture? GameCapture { get; private set; }

        private static readonly object _locker = new();
        private int _frameIndex = 0;

        private DateTime _prevManualGc = DateTime.MinValue;

        private static readonly object _triggerListLocker = new();

        public event EventHandler? UiTaskStopTickEvent;
        public event EventHandler? UiTaskStartTickEvent;

        private GameUiCategory PrevGameUiCategory = GameUiCategory.Unknown;
        private DateTime PrevGameUiChangeTime = DateTime.Now;

        public TaskTriggerDispatcher()
        {
            _instance = this;
            _timer.Elapsed += Tick;
        }

        public static TaskTriggerDispatcher Instance()
        {
            if (_instance == null)
            {
                throw new Exception("请先在启动页启动BetterIN，如果已经启动请重启");
            }

            return _instance;
        }

        public void ClearTriggers()
        {
            lock (_triggerListLocker)
            {
                GameTaskManager.ClearTriggers();
                _triggers?.Clear();
            }
        }

        public void SetTriggers(List<ITaskTrigger> list)
        {
            lock (_triggerListLocker)
            {
                _triggers = list;
            }
        }

        public bool AddTrigger(string name, object? externalConfig)
        {
            lock (_triggerListLocker)
            {
                if (GameTaskManager.AddTrigger(name, externalConfig))
                {
                    SetTriggers(GameTaskManager.ConvertToTriggerList(true));
                    return true;
                }

                return false;
            }
        }

        public void Start(IntPtr hWnd, CaptureModes mode, int interval = 50)
        {
            // 初始化截图器
            GameCapture = GameCaptureFactory.Create(mode);
            // 激活窗口
            SystemControl.ActivateWindow(hWnd);

            // 初始化任务上下文(一定要在初始化触发器前完成)
            TaskContext.Instance().Init(hWnd);

            // 初始化触发器(一定要在任务上下文初始化完毕后使用)
            _triggers = GameTaskManager.LoadInitialTriggers();
            GameLoadingTrigger.GlobalEnabled = TaskContext.Instance().Config.GameStartConfig.AutoEnterGameEnabled;

            // 启动截图
            GameCapture.Start(hWnd, new Dictionary<string, object>());

            // 启动定时器
            _frameIndex = 0;
            _timer.Interval = interval;
            if (!_timer.Enabled)
            {
                _timer.Start();
            }

            _logger.LogInformation("截图器已启动，模式: {Mode}, 间隔: {Interval}ms", mode, interval);
        }

        public void Stop()
        {
            _timer.Stop();
            GameCapture?.Stop();
            
            _logger.LogInformation("截图器已停止");
        }

        public void StartTimer()
        {
            if (!_timer.Enabled)
            {
                _timer.Start();
            }
        }

        public void StopTimer()
        {
            if (_timer.Enabled)
            {
                _timer.Stop();
            }
        }

        public void Tick(object? sender, EventArgs e)
        {
            var hasLock = false;
            try
            {
                Monitor.TryEnter(_locker, ref hasLock);
                if (!hasLock)
                {
                    // 正在执行时跳过
                    return;
                }

                // 检查截图器是否初始化
                if (GameCapture == null || !GameCapture.IsCapturing)
                {
                    if (!TaskContext.Instance().SystemInfo.GameProcess.HasExited)
                    {
                        _logger.LogError("截图器未初始化!");
                    }
                    else
                    {
                        _logger.LogInformation("游戏已退出，自动停止截图器");
                    }

                    UiTaskStopTickEvent?.Invoke(sender, e);
                    return;
                }

                // 帧序号自增
                _frameIndex = (_frameIndex + 1) % (int)(CaptureContent.MaxFrameIndexSecond * 1000d / _timer.Interval);

                var speedTimer = new SpeedTimer();
                // 捕获游戏画面
                var bitmap = GameCapture.Capture();
                speedTimer.Record("截图");

                if (bitmap == null)
                {
                    _logger.LogWarning("截图失败!");
                    return;
                }

                // 循环执行所有触发器
                using var content = new CaptureContent(bitmap, _frameIndex, _timer.Interval);

                var hasEnabledTriggers = _triggers != null && _triggers.Exists(t => t.IsEnabled);
                if (!hasEnabledTriggers)
                {
                    return;
                }

                lock (_triggerListLocker)
                {
                    var needRunTriggers = new List<ITaskTrigger>();
                    var exclusiveTrigger = _triggers!.FirstOrDefault(t => t is { IsEnabled: true, IsExclusive: true });
                    if (exclusiveTrigger != null)
                    {
                        needRunTriggers.Add(exclusiveTrigger);
                    }
                    else
                    {
                        var runningTriggers = _triggers!.Where(t => t.IsEnabled);
                        needRunTriggers.AddRange(runningTriggers);
                    }

                    if (needRunTriggers.Count > 0)
                    {
                        // 判断当前UI
                        content.CurrentGameUiCategory = Bv.WhichGameUiForTriggers(content.CaptureRectArea);

                        if (content.CurrentGameUiCategory != PrevGameUiCategory)
                        {
                            PrevGameUiChangeTime = DateTime.Now;
                        }

                        foreach (var trigger in needRunTriggers)
                        {
                            if ((PrevGameUiCategory != content.CurrentGameUiCategory ||
                                 (DateTime.Now - PrevGameUiChangeTime).TotalSeconds <= 30)
                                || trigger.SupportedGameUiCategory == content.CurrentGameUiCategory)
                            {
                                trigger.OnCapture(content);
                            }
                        }
                    }
                }

                PrevGameUiCategory = content.CurrentGameUiCategory;

                speedTimer.Record("总耗时");
                if (speedTimer.GetRecordTime("总耗时") > _timer.Interval)
                {
                    _logger.LogDebug("处理超时: {Time}ms", speedTimer.GetRecordTime("总耗时"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tick执行异常");
            }
            finally
            {
                if (hasLock)
                {
                    Monitor.Exit(_locker);
                }
            }
        }

        public void Dispose()
        {
            Stop();
            _timer.Dispose();
            GameCapture?.Dispose();
        }
    }
}
