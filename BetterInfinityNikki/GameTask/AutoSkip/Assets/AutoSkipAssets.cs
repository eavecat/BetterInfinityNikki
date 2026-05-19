using BetterInfinityNikki.Core.Recognition;
using BetterInfinityNikki.GameTask.Model;
using OpenCvSharp;

namespace BetterInfinityNikki.GameTask.AutoSkip.Assets;

public class AutoSkipAssets : BaseAssets<AutoSkipAssets>
{
    /// <summary>
    /// 左上角剧情播放标识
    /// </summary>
    public RecognitionObject PlotPlaybackRo;

    /// <summary>
    /// 右下角剧情跳过标识
    /// </summary>
    public RecognitionObject PlotSkipRo;

    private AutoSkipAssets()
    {
        // 初始化剧情播放标识识别对象（左上角）
        PlotPlaybackRo = new RecognitionObject
        {
            Name = "PlotPlayback",
            Use3Channels = true,
            Threshold = 0.7,
            RecognitionType = RecognitionTypes.TemplateMatch,
            TemplateImageMat = GameTaskManager.LoadAssetImage("AutoSkip", "plot_playback.png"),
            RegionOfInterest = new Rect(0, 0, (int)(CaptureRect.Width * 0.2), (int)(CaptureRect.Height * 0.2)),
            DrawOnWindow = true
        }.InitTemplate();

        // 初始化剧情跳过标识识别对象（右下角）
        PlotSkipRo = new RecognitionObject
        {
            Name = "PlotSkip",
            RecognitionType = RecognitionTypes.TemplateMatch,
            TemplateImageMat = GameTaskManager.LoadAssetImage("AutoSkip", "plot_skip.png"),
            RegionOfInterest = new Rect((int)(CaptureRect.Width * 0.8), (int)(CaptureRect.Height * 0.8), 
                (int)(CaptureRect.Width * 0.2), (int)(CaptureRect.Height * 0.2)),
            DrawOnWindow = false
        }.InitTemplate();
    }
}
