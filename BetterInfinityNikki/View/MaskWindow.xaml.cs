using BetterInfinityNikki.Core.Config;
using BetterInfinityNikki.Core.Recognition.OpenCv;
using BetterInfinityNikki.GameTask;
using BetterInfinityNikki.Helpers;
using BetterInfinityNikki.Helpers.DpiAwareness;
using BetterInfinityNikki.ViewModel;
using BetterInfinityNikki.View.Drawable;
using Microsoft.Extensions.Logging;
using Serilog.Sinks.RichTextBox.Abstraction;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Vanara.PInvoke;

namespace BetterInfinityNikki.View;

/// <summary>
/// 一个用于覆盖在游戏窗口上的窗口，用于显示识别结果、显示日志、设置区域位置等
/// 请使用 Instance 方法获取单例
/// </summary>
public partial class MaskWindow : Window
{
    private static MaskWindow? _maskWindow;

    private MaskWindowViewModel? _viewModel;

    private IRichTextBox? _richTextBox;

    private readonly ILogger<MaskWindow> _logger = App.GetLogger<MaskWindow>();

    private MaskWindowConfig? _maskWindowConfig;

    static MaskWindow()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(MaskWindow), new FrameworkPropertyMetadata(typeof(MaskWindow)));
    }

    public static MaskWindow Instance()
    {
        if (_maskWindow == null)
        {
            throw new Exception("MaskWindow 未初始化");
        }

        return _maskWindow;
    }

    public static MaskWindow? InstanceNullable()
    {
        return _maskWindow;
    }

    public bool IsExist()
    {
        return _maskWindow != null && PresentationSource.FromVisual(_maskWindow) != null;
    }

    public void BringToTop()
    {
        User32.BringWindowToTop(new WindowInteropHelper(this).Handle);
    }

    public void RefreshPosition()
    {
        try
        {
            var taskContext = TaskContext.Instance();
            if (taskContext.GameHandle == IntPtr.Zero)
            {
                _logger.LogDebug("无法刷新位置：GameHandle未初始化");
                return;
            }

            var currentRect = SystemControl.GetCaptureRect(taskContext.GameHandle);

            Invoke(() =>
            {
                double dpiScale = DpiHelper.ScaleY;

                Left = currentRect.Left / dpiScale;
                Top = currentRect.Top / dpiScale;
                Width = currentRect.Width / dpiScale;
                Height = currentRect.Height / dpiScale;

                // 强制更新布局
                UpdateLayout();

                BringToTop();
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刷新位置时发生异常");
        }
    }

    private new void UpdateLayout()
    {
        if (_viewModel != null)
        {
            _viewModel.MaskWindowWidth = Width;
            _viewModel.MaskWindowHeight = Height;

            // 直接更新布局位置
            _viewModel.UpdateLayoutPositionsPublic();
        }
    }

    public MaskWindow()
    {
        _maskWindow = this;

        InitializeComponent();
        this.InitializeDpiAwareness();

        LogTextBox.TextChanged += LogTextBoxTextChanged;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _richTextBox = App.GetService<IRichTextBox>();
        if (_richTextBox != null)
        {
            _richTextBox.RichTextBox = LogTextBox;
        }

        try
        {
            var taskContext = TaskContext.Instance();
            _maskWindowConfig = taskContext.Config.MaskWindowConfig;
            _maskWindowConfig.PropertyChanged += MaskWindowConfigOnPropertyChanged;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "初始化遮罩窗口配置时发生异常");
        }

        _viewModel = DataContext as MaskWindowViewModel;

        // 手动调用 ViewModel 的初始化逻辑
        if (_viewModel != null)
        {
            _viewModel.LoadedCommand.Execute(null);
            _viewModel.PropertyChanged += ViewModelOnPropertyChanged;
        }

        UpdateClickThroughState();

        RefreshPosition();
        PrintSystemInfo();
    }

    protected override void OnClosed(EventArgs e)
    {
        if (_maskWindowConfig != null)
        {
            _maskWindowConfig.PropertyChanged -= MaskWindowConfigOnPropertyChanged;
        }

        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= ViewModelOnPropertyChanged;
        }

        base.OnClosed(e);
    }

    private void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MaskWindowViewModel.IsInBigMapUi))
        {
            Dispatcher.Invoke(UpdateClickThroughState);
        }
    }

    private void MaskWindowConfigOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MaskWindowConfig.OverlayLayoutEditEnabled))
        {
            Dispatcher.Invoke(UpdateClickThroughState);
        }
    }

    private void UpdateClickThroughState()
    {
        try
        {
            var taskContext = TaskContext.Instance();
            var editEnabled = taskContext.Config.MaskWindowConfig.OverlayLayoutEditEnabled;
            
            // 获取 ViewModel 中的大地图状态
            var inBigMapUi = _viewModel?.IsInBigMapUi == true;
        
            if (editEnabled)
            {
                this.SetClickThrough(false);
                return;
            }
        
            // 在大地图界面时不穿透，其他情况穿透
            this.SetClickThrough(!inBigMapUi);
        }
        catch
        {
            this.SetClickThrough(true);
        }
    }

    private void PrintSystemInfo()
    {
        try
        {
            _logger.LogInformation("更好的无限暖暖 {Version}", Global.Version);
            var taskContext = TaskContext.Instance();
            var systemInfo = taskContext.SystemInfo;

            var width = systemInfo.GameScreenSize.Width;
            var height = systemInfo.GameScreenSize.Height;
            var dpiScale = taskContext.DpiScale;
            _logger.LogInformation("遮罩窗口已启动，游戏大小{Width}x{Height}，素材缩放{Scale}，DPI缩放{Dpi}",
                width, height, systemInfo.AssetScale.ToString("F"), dpiScale);

            if (width * 9 != height * 16)
            {
                _logger.LogWarning("当前游戏分辨率不是16:9，部分功能可能无法正常使用！");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "打印系统信息时发生异常");
        }
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        this.SetLayeredWindow();
        this.HideFromAltTab();
        UpdateClickThroughState();
    }

    private void LogTextBoxTextChanged(object sender, TextChangedEventArgs e)
    {
        if (LogTextBox.Document.Blocks.FirstBlock is Paragraph p && p.Inlines.Count > 1000)
        {
            (p.Inlines as System.Collections.IList).RemoveAt(0);
        }

        var textRange = new TextRange(LogTextBox.Document.ContentStart, LogTextBox.Document.ContentEnd);
        if (textRange.Text.Length > 10000)
        {
            LogTextBox.Document.Blocks.Clear();
        }

        LogTextBox.ScrollToEnd();
    }

    public void Refresh()
    {
        Dispatcher.Invoke(InvalidateVisual);
    }

    public void HideSelf()
    {
        try
        {
            var taskContext = TaskContext.Instance();
            if (taskContext.Config.MaskWindowConfig.OverlayLayoutEditEnabled)
            {
                return; // 编辑模式下不隐藏
            }

            this.Hide();
        }
        catch
        {
            // 忽略异常
        }
    }

    public void Invoke(Action action)
    {
        try
        {
            Dispatcher.Invoke(action);
        }
        catch (TaskCanceledException)
        {
        }
        catch (OperationCanceledException)
        {
        }
    }

    public void BeginInvoke(Action action)
    {
        try
        {
            Dispatcher.BeginInvoke(action);
        }
        catch (TaskCanceledException)
        {
        }
        catch (OperationCanceledException)
        {
        }
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        try
        {
            var cnt = VisionContext.Instance().DrawContent.RectList.Count + VisionContext.Instance().DrawContent.LineList.Count + VisionContext.Instance().DrawContent.TextList.Count;
            if (cnt == 0)
            {
                return;
            }

            var displayRecognitionResults = TaskContext.Instance().Config.MaskWindowConfig.DisplayRecognitionResultsOnMask;
            if (!displayRecognitionResults)
            {
                return;
            }

            if (displayRecognitionResults)
            {
                foreach (var kv in VisionContext.Instance().DrawContent.RectList)
                {
                    foreach (var drawable in kv.Value)
                    {
                        if (!drawable.IsEmpty)
                        {
                            drawingContext.DrawRectangle(
                                Brushes.Transparent,
                                new Pen(new SolidColorBrush(drawable.Pen.Color.ToWindowsColor()), drawable.Pen.Width),
                                drawable.Rect);
                        }
                    }
                }

                foreach (var kv in VisionContext.Instance().DrawContent.LineList)
                {
                    foreach (var drawable in kv.Value)
                    {
                        drawingContext.DrawLine(new Pen(new SolidColorBrush(drawable.Pen.Color.ToWindowsColor()), drawable.Pen.Width), drawable.P1, drawable.P2);
                    }
                }

                foreach (var kv in VisionContext.Instance().DrawContent.TextList)
                {
                    var systemInfo = TaskContext.Instance().SystemInfo;
                    var scaleTo1080 = systemInfo.ScaleTo1080PRatio;

                    foreach (var drawable in kv.Value)
                    {
                        if (!drawable.IsEmpty)
                        {
                            var pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
                            var renderPoint = new Point(drawable.Point.X / pixelsPerDip, drawable.Point.Y / pixelsPerDip);

                            double defaultFontSize = (36 * scaleTo1080) / pixelsPerDip;
                            var typeface = new Typeface("Microsoft YaHei");
                            drawingContext.DrawText(new FormattedText(drawable.Text,
                                CultureInfo.GetCultureInfo("zh-cn"),
                                FlowDirection.LeftToRight,
                                typeface,
                                defaultFontSize, Brushes.Black, pixelsPerDip), renderPoint);
                        }
                    }
                }
            }

        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
        }

        base.OnRender(drawingContext);
    }

    public RichTextBox LogBox => LogTextBox;
}

file static class MaskWindowExtension
{
    public static void HideFromAltTab(this Window window)
    {
        HideFromAltTab(new WindowInteropHelper(window).Handle);
    }

    public static void HideFromAltTab(nint hWnd)
    {
        int style = User32.GetWindowLong(hWnd, User32.WindowLongFlags.GWL_EXSTYLE);

        style |= (int)User32.WindowStylesEx.WS_EX_TOOLWINDOW;
        User32.SetWindowLong(hWnd, User32.WindowLongFlags.GWL_EXSTYLE, style);
    }

    public static void SetLayeredWindow(this Window window, bool isLayered = true)
    {
        SetLayeredWindow(new WindowInteropHelper(window).Handle, isLayered);
    }

    private static void SetLayeredWindow(nint hWnd, bool isLayered = true)
    {
        int style = User32.GetWindowLong(hWnd, User32.WindowLongFlags.GWL_EXSTYLE);

        if (isLayered)
        {
            style |= (int)User32.WindowStylesEx.WS_EX_TRANSPARENT;
            style |= (int)User32.WindowStylesEx.WS_EX_LAYERED;
        }
        else
        {
            style &= ~(int)User32.WindowStylesEx.WS_EX_TRANSPARENT;
            style &= ~(int)User32.WindowStylesEx.WS_EX_LAYERED;
        }

        _ = User32.SetWindowLong(hWnd, User32.WindowLongFlags.GWL_EXSTYLE, style);
    }

    public static void SetClickThrough(this Window window, bool isClickThrough)
    {
        SetLayeredWindow(new WindowInteropHelper(window).Handle, isClickThrough);
    }
}