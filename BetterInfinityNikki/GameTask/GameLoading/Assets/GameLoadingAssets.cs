using BetterInfinityNikki.Core.Recognition;
using BetterInfinityNikki.GameTask.Model;
using OpenCvSharp;

namespace BetterInfinityNikki.GameTask.GameLoading.Assets;

public class GameLoadingAssets : BaseAssets<GameLoadingAssets>
{
    /// <summary>
    /// "进入游戏"按钮识别对象
    /// </summary>
    public RecognitionObject EnterGameRo;

    /// <summary>
    /// "美鸭梨"菜单按钮识别对象（主界面标志）
    /// </summary>
    public RecognitionObject MeiyaliMenuRo;

    private GameLoadingAssets()
    {
        // 初始化"进入游戏"按钮识别
        EnterGameRo = new RecognitionObject
        {
            Name = "EnterGame",
            RecognitionType = RecognitionTypes.TemplateMatch,
            TemplateImageMat = GameTaskManager.LoadAssetImage("GameLoading", "enter_game.png"),
            RegionOfInterest = new Rect(
                CaptureRect.Width / 6,          // X: 从屏幕宽度的 1/6 处开始（左侧留 1/6）
                CaptureRect.Height / 2,         // Y: 从屏幕高度的 1/2 处开始（下半部分）
                CaptureRect.Width * 2 / 3,      // 宽度: 屏幕宽度的 2/3（中间区域）
                CaptureRect.Height / 2          // 高度: 屏幕高度的 1/2（下半部分）
            ),
            Threshold = 0.7,  // 降低阈值以提高匹配率
            DrawOnWindow = true
        }.InitTemplate();

        // 初始化"美鸭梨"菜单按钮识别（TODO: 等待图标导入后启用）
        // MeiyaliMenuRo = new RecognitionObject
        // {
        //     Name = "MeiyaliMenu",
        //     RecognitionType = RecognitionTypes.TemplateMatch,
        //     TemplateImageMat = GameTaskManager.LoadAssetImage("GameLoading", "meiyali_menu.png"),
        //     RegionOfInterest = new Rect(0, 0, CaptureRect.Width / 5, CaptureRect.Height / 5), // 左上角 1/5×1/5
        //     DrawOnWindow = false
        // }.InitTemplate();
        
        // 初始化"美鸭梨"菜单按钮识别
        MeiyaliMenuRo = new RecognitionObject
        {
            Name = "MeiyaliMenu",
            RecognitionType = RecognitionTypes.TemplateMatch,
            TemplateImageMat = GameTaskManager.LoadAssetImage("GameLoading", "meiyali_menu.png"),
            RegionOfInterest = new Rect(0, 0, CaptureRect.Width / 5, CaptureRect.Height / 5), // 左上角 1/5×1/5
            DrawOnWindow = true
        }.InitTemplate();
    }
}
