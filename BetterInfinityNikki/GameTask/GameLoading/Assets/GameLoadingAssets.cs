using BetterInfinityNikki.Core.Recognition;
using BetterInfinityNikki.GameTask.Common;
using BetterInfinityNikki.GameTask.Model;
using OpenCvSharp;

namespace BetterInfinityNikki.GameTask.GameLoading.Assets;

public class GameLoadingAssets : BaseAssets<GameLoadingAssets>
{
    /// <summary>
    /// "进入游戏"按钮识别对象
    /// </summary>
    public RecognitionObject EnterGameRo;

    private GameLoadingAssets()
    {
        // 初始化"进入游戏"按钮识别
        EnterGameRo = new RecognitionObject
        {
            Name = "EnterGame",
            RecognitionType = RecognitionTypes.TemplateMatch,
            TemplateImageMat = GameTaskManager.LoadAssetImage("GameLoading", "enter_game.png"),
            RegionOfInterest = new Rect(
                CaptureRect.Width / 3, // X: 从屏幕宽度的 1/3 处开始
                CaptureRect.Height / 2, // Y: 从屏幕高度的 1/2 处开始（下半部分）
                CaptureRect.Width / 3, // 宽度: 屏幕宽度的 1/3（中间区域）
                CaptureRect.Height - CaptureRect.Height / 2 // 高度: 屏幕高度的 1/2（下半部分）
            ),
            Threshold = 0.7,
            DrawOnWindow = false,
        }.InitTemplate();
    }
}