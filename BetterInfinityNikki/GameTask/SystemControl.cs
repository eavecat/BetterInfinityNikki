using BetterInfinityNikki.Core.Recognition.OCR;
using BetterInfinityNikki.View.Windows;
using OpenCvSharp;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using BetterInfinityNikki.Core.Recognition.OpenCv;
using BetterInfinityNikki.Core.Simulator;
using Fischless.GameCapture;
using Vanara.PInvoke;

namespace BetterInfinityNikki.GameTask;

public class SystemControl
{
    public static nint FindInfinityNikkiHandle()
    {
        // var processNames = TaskContext.Instance().GetNikkiGameProcessNameList();
        // return FindHandleByProcessName(processNames.ToArray());
        return FindHandleByProcessName(["X6Game-Win64-Shipping", "InfinityNikki", "无限暖暖"]);
    }

    public static nint FindLauncherHandle()
    {
        return FindHandleByProcessName(["InfinityNikki Launcher", "无限暖暖", "xstarter"]);
    }

    public static async Task<nint> StartLauncherAsync(string path)
    {
        if (!File.Exists(path))
        {
            await ThemedMessageBox.ErrorAsync($"无限暖暖启动器路径 {path} 不存在，请前往 启动——同时启动无限暖暖——无限暖暖安装路径 重新进行配置！");
            return IntPtr.Zero;
        }

        Process.Start(path);

        for (var i = 0; i < 5; i++)
        {
            var handle = FindLauncherHandle();
            if (handle != 0)
            {
                await Task.Delay(2000);
                handle = FindLauncherHandle();
                return handle;
            }

            await Task.Delay(3000);
        }

        return IntPtr.Zero;
    }

    /// <summary>
    /// 启动无限暖暖
    /// </summary>
    /// <param name="lWnd">启动器句柄</param>
    /// <param name="captureMode">截图模式</param>
    /// <returns></returns>
    public static async Task<nint> StartGameAsync(nint lWnd, CaptureModes captureMode)
    {
        if (lWnd != IntPtr.Zero)
        {
            // 激活启动器窗口
            ActivateWindow(lWnd);
            await Task.Delay(1000);

            // 尝试通过OCR识别并点击"启动游戏"按钮
            var clicked = await ClickStartButtonByOcrAsync(lWnd, captureMode);
            if (!clicked)
            {
                ClickLauncherStartButton(lWnd);
            }

            await Task.Delay(2000);
        }

        // 等待游戏启动
        for (var i = 0; i < 10; i++)
        {
            var handle = FindInfinityNikkiHandle();
            if (handle != 0)
            {
                await Task.Delay(3000);
                handle = FindInfinityNikkiHandle();
                if (handle != 0)
                {
                    return handle;
                }
            }

            await Task.Delay(3000);
        }

        return IntPtr.Zero;
    }

    /// <summary>
    /// 使用OCR识别并点击启动器的"启动游戏"按钮
    /// </summary>
    private static async Task<bool> ClickStartButtonByOcrAsync(nint launcherWnd, CaptureModes captureMode)
    {
        try
        {
            using var capture = GameCaptureFactory.Create(captureMode);
            capture.Start(launcherWnd);

            // 捕获启动器窗口截图
            Mat? mat = null;
            for (var i = 0; i < 5; i++)
            {
                mat = capture.Capture();
                if (mat == null || mat.Empty())
                {
                    await Task.Delay(1000);
                    continue;
                }

                break;
            }

            capture.Stop();
            if (mat == null || mat.Empty())
            {
                return false;
            }

            // 只识别整个截图的底部区域（因为截图可能只是客户区）
            var roiRect = new Rect(
                (int)(mat.Width * 0.4), // 从40%开始（右60%区域）
                (int)(mat.Height * 0.5), // 从50%开始（下半部分）
                (int)(mat.Width * 0.6), // 宽度为60%
                (int)(mat.Height * 0.5) // 高度为50%
            ).ClampTo(mat);

            // 裁剪出ROI区域
            using var roiMat = new Mat(mat, roiRect);

            // 使用OCR识别文字（只在ROI区域）
            var ocrResult = OcrFactory.Paddle.OcrResult(mat);
            mat.Dispose();
            capture.Stop();

            // 查找包含"启动"、"开始"或"Start"的文字区域
            OcrResultRegion? buttonRegion = null;
            foreach (var region in ocrResult.Regions)
            {
                var text = region.Text.ToLower();
                // Console.WriteLine($"识别文字: {region.Text}");
                if (text.Contains("启动游戏") || text.Contains("启动") || text.Contains("start"))
                {
                    buttonRegion = region;
                    // Console.WriteLine($"找到启动按钮文字: {region.Text}, 位置: {region}");
                    break;
                }
            }

            if (buttonRegion == null)
            {
                Console.WriteLine("未找到启动按钮文字");
                return false;
            }

            Rect? reg = buttonRegion?.Rect.BoundingRect();
            RECT launcherPos = GetWindowRect(launcherWnd);
            // 计算按钮中心位置
            var centerX = (launcherPos.X + reg?.X + reg?.Width / 2) ?? 0;
            var centerY = (launcherPos.Y + reg?.Y + reg?.Height / 2) ?? 0;
            if (centerX == 0 && centerY == 0)
            {
                return false;
            }

            Simulation.MouseEvent.Click(centerX, centerY);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"OCR识别启动按钮失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 点击启动器右下角的启动按钮（备用方案）
    /// </summary>
    private static void ClickLauncherStartButton(nint launcherWnd)
    {
        try
        {
            var rect = GetWindowRect(launcherWnd);

            var clickX = rect.Right - (rect.Width * 0.175);
            var clickY = rect.Bottom - (rect.Height * 0.1);

            Console.WriteLine($"点击启动按钮: {clickX}-{clickY}");
            Simulation.MouseEvent.Click((int)clickX, (int)clickY);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"点击启动按钮失败: {ex.Message}");
        }
    }

    public static bool IsInfinityNikkiActiveByProcess()
    {
        var name = GetActiveProcessName();
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        var processNames = TaskContext.Instance().GetNikkiGameProcessNameList();
        return processNames.Any(p => string.Equals(p, name, StringComparison.OrdinalIgnoreCase));
    }

    public static string GetActiveByProcess()
    {
        return GetActiveProcessName() ?? "Unknown";
    }

    public static bool IsInfinityNikkiActive()
    {
        var hWnd = User32.GetForegroundWindow();
        return hWnd == TaskContext.Instance().GameHandle;
    }

    public static bool IsInfinityNikkiMinimized()
    {
        return User32.IsIconic(TaskContext.Instance().GameHandle);
    }

    public static nint GetForegroundWindowHandle()
    {
        return (nint)User32.GetForegroundWindow();
    }

    public static nint FindHandleByProcessName(params string[] names)
    {
        foreach (var name in names)
        {
            var pros = Process.GetProcessesByName(name);
            if (pros.Length is not 0 && pros[0].MainWindowHandle != IntPtr.Zero)
            {
                return pros[0].MainWindowHandle;
            }
        }

        return IntPtr.Zero;
    }

    public static nint FindHandleByWindowName()
    {
        var handle = (nint)User32.FindWindow("UnityWndClass", "无限暖暖");
        if (handle != 0)
        {
            return handle;
        }

        handle = (nint)User32.FindWindow("UnityWndClass", "Infinity Nikki");
        if (handle != 0)
        {
            return handle;
        }

        return 0;
    }

    public static string? GetActiveProcessName()
    {
        try
        {
            var hWnd = User32.GetForegroundWindow();
            _ = User32.GetWindowThreadProcessId(hWnd, out var pid);
            var p = Process.GetProcessById((int)pid);
            return p.ProcessName;
        }
        catch
        {
            return null;
        }
    }

    public static Process? GetProcessByHandle(nint hWnd)
    {
        try
        {
            _ = User32.GetWindowThreadProcessId(hWnd, out var pid);
            var p = Process.GetProcessById((int)pid);
            return p;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            return null;
        }
    }

    /// <summary>
    /// 获取窗口位置
    /// </summary>
    /// <param name="hWnd"></param>
    /// <returns></returns>
    public static RECT GetWindowRect(nint hWnd)
    {
        // User32.GetWindowRect(hWnd, out var windowRect);
        DwmApi.DwmGetWindowAttribute<RECT>(hWnd, DwmApi.DWMWINDOWATTRIBUTE.DWMWA_EXTENDED_FRAME_BOUNDS,
            out var windowRect);
        return windowRect;
    }

    /// <summary>
    /// 游戏本身分辨率获取
    /// </summary>
    /// <param name="hWnd"></param>
    /// <returns></returns>
    public static RECT GetGameScreenRect(nint hWnd)
    {
        User32.GetClientRect(hWnd, out var clientRect);
        return clientRect;
    }

    /// <summary>
    /// GetWindowRect or GetGameScreenRect
    /// </summary>
    /// <param name="hWnd"></param>
    /// <returns></returns>
    public static RECT GetCaptureRect(nint hWnd)
    {
        var windowRect = GetWindowRect(hWnd);
        var gameScreenRect = GetGameScreenRect(hWnd);
        var left = windowRect.Left;
        var top = windowRect.Top + windowRect.Height - gameScreenRect.Height;
        var right = left + gameScreenRect.Width;
        var bottom = top + gameScreenRect.Height;
        return new RECT(left, top, right, bottom);
    }

    public static void ActivateWindow(nint hWnd)
    {
        User32.ShowWindow(hWnd, ShowWindowCommand.SW_RESTORE);
        User32.SetForegroundWindow(hWnd);
    }

    public static void ActivateWindow()
    {
        if (!TaskContext.Instance().IsInitialized)
        {
            throw new Exception("请先启动BetterGI");
        }

        ActivateWindow(TaskContext.Instance().GameHandle);
    }

    public static void RestartApplication(string[] newArgs)
    {
        // 获取当前程序路径
        string exePath = Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;

        // 构建参数字符串
        string arguments = string.Join(" ", [..newArgs, "--no-single"]);

        // 启动新进程
        Process.Start(new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = arguments,
            UseShellExecute = false
        });

        // 关闭当前程序
        Environment.Exit(0);
    }

    public static void FocusWindow(nint hWnd)
    {
        if (User32.IsWindow(hWnd))
        {
            _ = User32.SendMessage(hWnd, User32.WindowMessage.WM_SYSCOMMAND, User32.SysCommand.SC_RESTORE, 0);
            _ = User32.SetForegroundWindow(hWnd);

            while (User32.IsIconic(hWnd))
            {
                continue;
            }

            _ = User32.BringWindowToTop(hWnd);
            _ = User32.SetActiveWindow(hWnd);
        }
    }

    public static void MinimizeAndActivateWindow(nint hWnd)
    {
        HWND hShell = User32.FindWindow("Shell_TrayWnd", null);
        User32.SendMessage(hShell, 0x0111, (IntPtr)419, IntPtr.Zero);
        Thread.Sleep(500);
        FocusWindow(hWnd);
    }

    public static void RestoreWindow(nint hWnd)
    {
        if (User32.IsWindow(hWnd))
        {
            _ = User32.SendMessage(hWnd, User32.WindowMessage.WM_SYSCOMMAND, User32.SysCommand.SC_RESTORE, 0);
            _ = User32.SetForegroundWindow(hWnd);

            if (User32.IsIconic(hWnd))
            {
                _ = User32.ShowWindow(hWnd, ShowWindowCommand.SW_RESTORE);
            }

            _ = User32.BringWindowToTop(hWnd);
            _ = User32.SetActiveWindow(hWnd);
        }
    }

    public static bool IsFullScreenMode(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero)
        {
            return false;
        }

        var exStyle = User32.GetWindowLong(hWnd, User32.WindowLongFlags.GWL_EXSTYLE);

        return (exStyle & (int)User32.WindowStylesEx.WS_EX_TOPMOST) != 0;
    }

    // private static void StartFromLauncher(string path)
    // {
    //     // 通过launcher启动
    //     var process = Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    //     Thread.Sleep(1000);
    //     // 获取launcher窗口句柄
    //     var hWnd = FindHandleByProcessName("launcher");
    //     var rect = GetWindowRect(hWnd);
    //     var dpiScale = Helpers.DpiHelper.ScaleY;
    //     // 对于launcher，启动按钮的位置时固定的，在launcher窗口的右下角
    //     Thread.Sleep(1000);
    //     Simulation.MouseEvent.Click((int)((float)rect.right * dpiScale) - (rect.Width / 5), (int)((float)rect.bottom * dpiScale) - (rect.Height / 8));
    // }
    //
    // private static void StartCloudYaunShen(string path)
    // {
    //     // 通过launcher启动
    //     var process = Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    //     Thread.Sleep(10000);
    //     // 获取launcher窗口句柄
    //     var hWnd = FindHandleByProcessName("Genshin Impact Cloud Game");
    //     var rect = GetWindowRect(hWnd);
    //     var dpiScale = Helpers.DpiHelper.ScaleY;
    //     // 对于launcher，启动按钮的位置时固定的，在launcher窗口的右下角
    //     Simulation.MouseEvent.Click(rect.right - (rect.Width / 6), rect.bottom - (rect.Height / 13 * 3));
    //     // TODO：点完之后有个15s的倒计时，好像不处理也没什么问题，直接睡个20s吧
    //     Thread.Sleep(20000);
    // }
    public static void CloseGame()
    {
        try
        {
            var processNames = TaskContext.Instance().GetNikkiGameProcessNameList();
            var processes = processNames
                .SelectMany(Process.GetProcessesByName)
                .GroupBy(p => p.Id)
                .Select(g => g.First())
                .ToArray();

            if (processes.Length > 0)
            {
                foreach (var process in processes)
                {
                    try
                    {
                        // 尝试正常关闭进程
                        process.CloseMainWindow();

                        // 给进程一些时间来响应关闭请求
                        if (!process.WaitForExit(5000))
                        {
                            // 如果进程没有在5秒内关闭，则强制终止它
                            process.Kill();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"关闭游戏进程时出错: {ex.Message}");
                    }
                    finally
                    {
                        process.Dispose();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"CloseGame方法执行出错: {ex.Message}");
        }
    }

    public static void Shutdown()
    {
        try
        {
            // 使用Windows API安全关闭系统
            // 这里使用的是标准的Windows关机命令，需要适当的权限
            Process.Start("shutdown", "/s /t 60 /c \"系统将在60秒后关闭，请保存您的工作。\"");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Shutdown方法执行出错: {ex.Message}");
        }
    }
}