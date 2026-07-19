using BetterInfinityNikki.GameTask;
using BetterInfinityNikki.Helpers.Extensions;
using Fischless.GameCapture;
using System;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OpenCvSharp;
using Wpf.Ui.Violeta.Controls;

namespace BetterInfinityNikki.View;

public partial class CaptureTestWindow
{
    private IGameCapture? _capture;
    private Size _cacheSize;

    private long _captureTime;
    private long _transferTime;
    private long _captureCount;

    public CaptureTestWindow()
    {
        _captureTime = 0;
        _transferTime = 0;
        _captureCount = 0;
        InitializeComponent();
        Closed += (sender, args) =>
        {
            CompositionTarget.Rendering -= Loop;
            _capture?.Stop();

            Debug.WriteLine("平均截图耗时:" + _captureTime * 1.0 / _captureCount);
            Debug.WriteLine("平均转换耗时:" + _transferTime * 1.0 / _captureCount);
            Debug.WriteLine("平均总耗时:" + (_captureTime + _transferTime) * 1.0 / _captureCount);
        };
    }

    public void StartCapture(IntPtr hWnd, CaptureModes captureMode)
    {
        if (hWnd == IntPtr.Zero)
        {
            Toast.Warning("请选择窗口");
            return;
        }

        _capture = GameCaptureFactory.Create(captureMode);

        _capture.Start(hWnd,
            new Dictionary<string, object>()
            {
                { "autoFixWin11BitBlt", true }
            });

        CompositionTarget.Rendering += Loop;
    }

    private void Loop(object? sender, EventArgs e)
    {
        var sw = new Stopwatch();
        sw.Start();
        using var captureFrame = _capture?.Capture();
        var mat = captureFrame?.Frame;
        sw.Stop();
        _captureTime += sw.ElapsedMilliseconds;

        if (mat != null)
        {
            _captureCount++;
            sw.Reset();
            sw.Start();
            if (_cacheSize != mat.Size())
            {
                DisplayCaptureResultImage.Source = mat.ToWriteableBitmap();
                _cacheSize = mat.Size();
            }
            else
            {
                mat.UpdateWriteableBitmap((WriteableBitmap)DisplayCaptureResultImage.Source);
            }

            sw.Stop();
            _transferTime += sw.ElapsedMilliseconds;
        }
        else
        {
            Debug.WriteLine("截图失败");
        }
    }
}