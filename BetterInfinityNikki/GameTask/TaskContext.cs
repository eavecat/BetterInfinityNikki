using System.IO;
using System.Threading;
using BetterInfinityNikki.Core.Config;
using BetterInfinityNikki.Core.Simulator;
using BetterInfinityNikki.GameTask.Model;
using BetterInfinityNikki.Helpers;
using BetterInfinityNikki.Service;

namespace BetterInfinityNikki.GameTask
{
    /// <summary>
    /// 任务上下文
    /// </summary>
    public class TaskContext
    {
        private static TaskContext? _uniqueInstance;
        private static object? InstanceLocker;
        // public ScriptGroupProject? CurrentScriptProject { get; set; }

#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。

        private TaskContext()
        {
        }

#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。

        public static TaskContext Instance()
        {
            return LazyInitializer.EnsureInitialized(ref _uniqueInstance, ref InstanceLocker, () => new TaskContext());
        }

        public void Init(IntPtr hWnd)
        {
            GameHandle = hWnd;
            PostMessageSimulator = Simulation.PostMessage(GameHandle);
            SystemInfo = new SystemInfo(hWnd);
            DpiScale = DpiHelper.ScaleY;
            //MaskWindowHandle = new WindowInteropHelper(MaskWindow.Instance()).Handle;
            IsInitialized = true;
        }

        public bool IsInitialized { get; set; }

        public IntPtr GameHandle { get; set; }

        public PostMessageSimulator PostMessageSimulator { get; private set; }

        //public IntPtr MaskWindowHandle { get; set; }

        public float DpiScale { get; set; }

        public ISystemInfo SystemInfo { get; set; }

        public AllConfig Config
        {
            get
            {
                if (ConfigService.Config == null)
                {
                    // 如果配置未初始化，返回默认配置而不是抛出异常
                    // 这样可以避免在应用启动时（OCR服务初始化）出现错误
                    return new AllConfig();
                }

                return ConfigService.Config;
            }
        }

        // public SettingsContainer? GameSettings { get; set; }

        /// <summary>
        /// 关联启动原神的时间
        /// 注意 IsInitialized = false 时，这个值就会被设置
        /// </summary>
        public DateTime LinkedStartGenshinTime { get; set; } = DateTime.MinValue;

        public List<string> GetNikkiGameProcessNameList()
        {
            if (IsInitialized)
            {
                return [SystemInfo.GameProcessName];
            }
            else
            {
                // 无限暖暖可能的进程名
                List<string> list = ["InfinityNikki", "X6Game-Win64-Shipping", "无限暖暖"];
                try
                {
                    var installPath = Config.GameStartConfig.InstallPath;
                    if (!string.IsNullOrEmpty(installPath))
                    {
                        var customName = Path.GetFileNameWithoutExtension(installPath);
                        if (!string.IsNullOrEmpty(customName) && !list.Contains(customName))
                        {
                            // list.Insert(0, customName); // 将用户自定义的进程名放在列表前面，优先匹配
                            list.Add(customName);
                        }
                    }
                }
                catch
                {
                    /* ignore */
                }

                return list;
            }
        }
    }
}