using BetterInfinityNikki.Helpers.Ui;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Vanara.PInvoke;
using Wpf.Ui.Controls;

namespace BetterInfinityNikki.View;

public class CapturableWindow
{
    public IntPtr Handle { get; }
    public string Name { get; }
    public string ProcessName { get; }
    public ImageSource? Icon { get; }

    public CapturableWindow(IntPtr handle, string name, string processName, ImageSource? icon)
    {
        Handle = handle;
        Name = name;
        ProcessName = processName;
        Icon = icon;
    }
}

public partial class PickerWindow : FluentWindow
{
    private bool _isSelected;
    private readonly bool _captureTest;

    private const User32.WindowStylesEx IgnoreExStyle = User32.WindowStylesEx.WS_EX_TOOLWINDOW |
                                                        User32.WindowStylesEx.WS_EX_NOREDIRECTIONBITMAP |
                                                        User32.WindowStylesEx.WS_EX_LAYERED;

    public PickerWindow(bool captureTest = false)
    {
        InitializeComponent();

        SourceInitialized += OnSourceInitialized;
        Loaded += OnLoaded;
        MouseLeftButtonDown += PickerWindow_MouseLeftButtonDown;
        _captureTest = captureTest;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        WindowHelper.TryApplySystemBackdrop(this);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        FindWindows();
    }

    private void PickerWindow_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // 当用户按住鼠标左键时，允许拖拽窗口
        if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
        {
            this.DragMove();
        }
    }

    public bool PickCaptureTarget(IntPtr hWnd, out IntPtr pickedWindow)
    {
        new WindowInteropHelper(this).Owner = hWnd;
        ShowDialog();
        if (!_isSelected)
        {
            pickedWindow = IntPtr.Zero;
            return false;
        }

        pickedWindow = ((CapturableWindow?)WindowList.SelectedItem)?.Handle ?? IntPtr.Zero;
        return true;
    }

    private void FindWindows()
    {
        var wih = new WindowInteropHelper(this);
        var windows = new List<CapturableWindow>();

        User32.EnumWindows((hWnd, lParam) =>
        {
            // 检查可见性和自身窗口
            if (!User32.IsWindowVisible(hWnd))
            {
                return true;
            }

            if (wih.Handle == (IntPtr)hWnd)
            {
                return true;
            }

            var exStyle = User32.GetWindowLong<User32.WindowStylesEx>(hWnd, User32.WindowLongFlags.GWL_EXSTYLE);
            if ((exStyle & IgnoreExStyle) != 0)
            {
                _ = User32.GetWindowThreadProcessId(hWnd, out var filterProcessId1);
                var filterProcessName1 = "未知";
                try
                {
                    var filterProcess1 = Process.GetProcessById((int)filterProcessId1);
                    filterProcessName1 = filterProcess1.ProcessName;
                }
                catch
                {
                }

                // 如果是无限暖暖启动器，允许通过
                if (filterProcessName1.Equals("xstarter", StringComparison.OrdinalIgnoreCase))
                {
                    // 继续处理，不返回
                }
                else
                {
                    return true;
                }
            }

            var title = new StringBuilder(1024);
            _ = User32.GetWindowText(hWnd, title, title.Capacity);
            if (string.IsNullOrWhiteSpace(title.ToString()))
            {
                return true;
            }

            _ = User32.GetWindowThreadProcessId(hWnd, out var processId);
            var process = Process.GetProcessById((int)processId);

            // 获取窗口图标
            var icon = GetWindowIcon((IntPtr)hWnd);

            windows.Add(new CapturableWindow((IntPtr)hWnd, title.ToString(), process.ProcessName, icon));

            return true;
        }, IntPtr.Zero);

        var sortedWindows = windows.OrderByDescending(IsGameWindow)
            .ThenByDescending(x => x.Handle).ToList();

        WindowList.ItemsSource = sortedWindows;
    }

    private ImageSource? GetWindowIcon(IntPtr hWnd)
    {
        try
        {
            const int ICON_BIG = 1; // WM_GETICON large icon constant
            const int ICON_SMALL = 0; // WM_GETICON small icon constant
            const int GCL_HICON = -14; // GetClassLong index for icon

            // 尝试获取窗口大图标
            var iconHandle = User32.SendMessage(hWnd, User32.WindowMessage.WM_GETICON, (IntPtr)ICON_BIG, IntPtr.Zero);

            if (iconHandle == IntPtr.Zero)
            {
                // 尝试获取窗口小图标
                iconHandle = User32.SendMessage(hWnd, User32.WindowMessage.WM_GETICON, (IntPtr)ICON_SMALL, IntPtr.Zero);
            }

            if (iconHandle == IntPtr.Zero)
            {
                // 尝试获取窗口类图标
                iconHandle = User32.GetClassLong(hWnd, GCL_HICON);
            }

            if (iconHandle != IntPtr.Zero)
            {
                return Imaging.CreateBitmapSourceFromHIcon(
                    iconHandle,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"获取窗口图标失败: {ex.Message}");
        }

        // 如果获取失败，返回null
        return null;
    }

    private static bool IsGameWindow(CapturableWindow window)
    {
        // 检查是否是无限暖暖窗口
        return window.Name.Contains("无限暖暖", StringComparison.OrdinalIgnoreCase) ||
               window.Name.Contains("Infinity Nikki", StringComparison.OrdinalIgnoreCase) ||
               window.ProcessName.Contains("InfinityNikki", StringComparison.OrdinalIgnoreCase);
    }

    private void WindowsOnMouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (WindowList.SelectedItem is not CapturableWindow selectedWindow)
            return;

        _isSelected = true;
        Close();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        WindowHelper.TryApplySystemBackdrop(this);
        FindWindows();
    }

    private void FluentWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            _isSelected = false;
            Close();
        }
    }

    private void cancelButton_Click(object sender, RoutedEventArgs e)
    {
        _isSelected = false;
        Close();
    }
}