using BetterInfinityNikki.Core.Recognition;
using BetterInfinityNikki.GameTask.Model;
using OpenCvSharp;

namespace BetterInfinityNikki.GameTask.AutoFishing.Assets;

/// <summary>
/// 自动钓鱼资源
/// </summary>
public class AutoFishingAssets : BaseAssets<AutoFishingAssets>
{
    /// <summary>
    /// 取消钓鱼按钮（右下角）
    /// </summary>
    public RecognitionObject CancelFishingRo = null!;

    /// <summary>
    /// 收竿按钮（等待鱼上钩时显示）
    /// </summary>
    public RecognitionObject ReelRodRo = null!;

    /// <summary>
    /// 提竿按钮（鱼上钩时显示）
    /// </summary>
    public RecognitionObject RaiseRodRo = null!;

    /// <summary>
    /// 拉扯鱼线提竿按钮（拉扯阶段显示）
    /// </summary>
    public RecognitionObject PullFishingLineRo = null!;

    /// <summary>
    /// 收线按钮（收线阶段显示）
    /// </summary>
    public RecognitionObject ReelLineRo = null!;

    /// <summary>
    /// 跳过动画按钮（钓鱼成功后显示）
    /// </summary>
    public RecognitionObject SkipAnimRo = null!;

    private AutoFishingAssets()
    {
        InitTemplates();
    }

    private void InitTemplates()
    {
        // 1. 取消钓鱼按钮 - 宽30%，高20%
        var cancelWidth = (int)(CaptureRect.Width * 0.3);
        var cancelHeight = (int)(CaptureRect.Height * 0.2);
        CancelFishingRo = new RecognitionObject
        {
            Name = "CancelFishing",
            RecognitionType = RecognitionTypes.TemplateMatch,
            TemplateImageMat = GameTaskManager.LoadAssetImage("AutoFishing", "cancel_fishing.png"),
            RegionOfInterest = new Rect(
                CaptureRect.Width - cancelWidth,
                CaptureRect.Height - cancelHeight,
                cancelWidth,
                cancelHeight
            ),
            Threshold = 0.7,
            DrawOnWindow = false
        }.InitTemplate();

        // 2. 提竿按钮 - 宽35%，高20%
        var raiseWidth = (int)(CaptureRect.Width * 0.35);
        var raiseHeight = (int)(CaptureRect.Height * 0.2);
        ReelRodRo = new RecognitionObject
        {
            Name = "ReelRodRo",
            RecognitionType = RecognitionTypes.TemplateMatch,
            TemplateImageMat = GameTaskManager.LoadAssetImage("AutoFishing", "reel_rod.png"),
            RegionOfInterest = new Rect(
                CaptureRect.Width - raiseWidth,
                CaptureRect.Height - raiseHeight,
                raiseWidth,
                raiseHeight
            ),
            Threshold = 0.7,
            DrawOnWindow = true
        }.InitTemplate();
        
        RaiseRodRo = new RecognitionObject
        {
            Name = "RaiseRod",
            RecognitionType = RecognitionTypes.TemplateMatch,
            TemplateImageMat = GameTaskManager.LoadAssetImage("AutoFishing", "raise_rod.png"),
            RegionOfInterest = new Rect(
                CaptureRect.Width - raiseWidth,
                CaptureRect.Height - raiseHeight,
                raiseWidth,
                raiseHeight
            ),
            Threshold = 0.6,
            DrawOnWindow = false
        }.InitTemplate();

        // 3. 拉扯鱼线按钮 - 宽35%，高20%
        PullFishingLineRo = new RecognitionObject
        {
            Name = "PullFishingLine",
            RecognitionType = RecognitionTypes.TemplateMatch,
            TemplateImageMat = GameTaskManager.LoadAssetImage("AutoFishing", "pull_fishing_line.png"),
            RegionOfInterest = new Rect(
                CaptureRect.Width - raiseWidth,
                CaptureRect.Height - raiseHeight,
                raiseWidth,
                raiseHeight
            ),
            Threshold = 0.7,
            DrawOnWindow = false
        }.InitTemplate();

        // 4. 收线按钮 - 宽35%，高20%
        ReelLineRo = new RecognitionObject
        {
            Name = "ReelLine",
            RecognitionType = RecognitionTypes.TemplateMatch,
            TemplateImageMat = GameTaskManager.LoadAssetImage("AutoFishing", "reel_line.png"),
            RegionOfInterest = new Rect(
                CaptureRect.Width - raiseWidth,
                CaptureRect.Height - raiseHeight,
                raiseWidth,
                raiseHeight
            ),
            Threshold = 0.7,
            DrawOnWindow = true
        }.InitTemplate();

        // 5. 跳过动画按钮 - 宽20%，高20%
        var skipWidth = (int)(CaptureRect.Width * 0.2);
        var skipHeight = (int)(CaptureRect.Height * 0.2);
        SkipAnimRo = new RecognitionObject
        {
            Name = "SkipAnim",
            RecognitionType = RecognitionTypes.TemplateMatch,
            TemplateImageMat = GameTaskManager.LoadAssetImage("AutoFishing", "skip_animo.png"),
            RegionOfInterest = new Rect(
                CaptureRect.Width - skipWidth,
                CaptureRect.Height - skipHeight,
                skipWidth,
                skipHeight
            ),
            Threshold = 0.5,
            DrawOnWindow = false
        }.InitTemplate();
    }
}