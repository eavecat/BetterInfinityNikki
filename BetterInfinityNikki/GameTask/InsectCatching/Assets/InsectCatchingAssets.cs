using BetterInfinityNikki.Core.Recognition;
using BetterInfinityNikki.Core.Recognition.OpenCv;
using BetterInfinityNikki.GameTask;
using OpenCvSharp;
using System;
using OpenCvRect = OpenCvSharp.Rect;

namespace BetterInfinityNikki.GameTask.InsectCatching.Assets;

/// <summary>
/// 璨花捕影资源
/// </summary>
public class InsectCatchingAssets
{
    private static readonly object LockObj = new();
    private static InsectCatchingAssets? _instance;

    public static InsectCatchingAssets Instance()
    {
        if (_instance != null)
        {
            return _instance;
        }

        lock (LockObj)
        {
            _instance ??= new InsectCatchingAssets();
        }

        return _instance;
    }

    public static void DestroyInstance()
    {
        lock (LockObj)
        {
            _instance?.Dispose();
            _instance = null;
        }
    }

    private static OpenCvRect CaptureRect => new OpenCvRect(
        TaskContext.Instance().SystemInfo.CaptureAreaRect.Left,
        TaskContext.Instance().SystemInfo.CaptureAreaRect.Top,
        TaskContext.Instance().SystemInfo.CaptureAreaRect.Width,
        TaskContext.Instance().SystemInfo.CaptureAreaRect.Height);

    /// <summary>
    /// 捕虫按钮识别对象（普通模式）
    /// </summary>
    public RecognitionObject InsectCatchingNormalRo { get; }

    public InsectCatchingAssets()
    {
        // 初始化璨花捕影识别对象（普通模式捕虫按钮）
        InsectCatchingNormalRo = new RecognitionObject
        {
            Name = "InsectCatchingNormal",
            RecognitionType = RecognitionTypes.TemplateMatch,
            TemplateImageMat = GameTaskManager.LoadAssetImage("InsectCatching", "insect_catching_normal.png"),
            RegionOfInterest = new OpenCvRect(
                (int)(CaptureRect.Width * 0.8),      // X: 从80%位置开始
                (int)(CaptureRect.Height * 0.8),     // Y: 从80%位置开始
                (int)(CaptureRect.Width * 0.15),     // Width: 宽度为15%
                (int)(CaptureRect.Height * 0.15)     // Height: 高度为15%
            ),
            Threshold = 0.7,
            DrawOnWindow = true
        }.InitTemplate();
    }

    public void Dispose()
    {
        InsectCatchingNormalRo.TemplateImageMat?.Dispose();
    }
}
