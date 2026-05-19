using System.Drawing;
using System.IO;
using BetterInfinityNikki.Core.Recognition;
using BetterInfinityNikki.GameTask.Model;
using BetterInfinityNikki.Helpers;
using OpenCvSharp;
using Vanara.PInvoke;

namespace BetterInfinityNikki.GameTask.AutoPick.Assets;

public class AutoPickAssets : BaseAssets<AutoPickAssets>
{
    private readonly ILogger<AutoPickAssets> _logger = App.GetLogger<AutoPickAssets>();

    public RecognitionObject FRo;
    public RecognitionObject ChatIconRo;
    public RecognitionObject SettingsIconRo;

    public User32.VK PickVk = User32.VK.VK_F; // VK_F
    public RecognitionObject PickRo;

    private AutoPickAssets()
    {
        FRo = new RecognitionObject
        {
            Name = "F",
            RecognitionType = RecognitionTypes.TemplateMatch,
            TemplateImageMat = GameTaskManager.LoadAssetImage("AutoPick", "F.png"),
            RegionOfInterest = new Rect(
                (int)(CaptureRect.Width * 0.25), // X: 从25%位置开始
                (int)(CaptureRect.Height * 0.25), // Y: 从25%位置开始
                (int)(CaptureRect.Width * 0.5), // Width: 宽度为50%
                (int)(CaptureRect.Height * 0.5) // Height: 高度为50%
            ),
            Threshold = 0.5, // 降低阈值以提高匹配率
            DrawOnWindow = false
        }.InitTemplate();

        PickRo = FRo;
        var keyName = TaskContext.Instance().Config.AutoPickConfig.PickKey;
        if (!string.IsNullOrEmpty(keyName))
        {
            try
            {
                PickRo = LoadCustomPickKey(keyName);
                PickVk = User32Helper.ToVk(keyName);
            }
            catch (Exception e)
            {
                _logger.LogDebug(e, "加载自定义拾取按键时发生异常");
                _logger.LogError("加载自定义拾取按键失败，继续使用默认的F键");
                TaskContext.Instance().Config.AutoPickConfig.PickKey = "F";
                return;
            }

            if (keyName != "F")
            {
                _logger.LogInformation("自定义拾取按键：{Key}", keyName);
            }
        }
    }

    public RecognitionObject LoadCustomPickKey(string key)
    {
        return new RecognitionObject
        {
            Name = key,
            RecognitionType = RecognitionTypes.TemplateMatch,
            TemplateImageMat = GameTaskManager.LoadAssetImage("AutoPick", key + ".png"),
            RegionOfInterest = new Rect(
                (int)(CaptureRect.Width * 0.25), // X: 从25%位置开始
                (int)(CaptureRect.Height * 0.25), // Y: 从25%位置开始
                (int)(CaptureRect.Width * 0.5), // Width: 宽度为50%
                (int)(CaptureRect.Height * 0.5) // Height: 高度为50%
            ),
            DrawOnWindow = false
        }.InitTemplate();
    }

    /// <summary>
    /// 将按键名称转换为虚拟键码
    /// </summary>
    private static int ToVirtualKeyCode(string key)
    {
        return key.ToUpper() switch
        {
            "F" => 0x46,
            "E" => 0x45,
            "G" => 0x47,
            "T" => 0x54,
            _ => 0x46 // 默认F键
        };
    }
}