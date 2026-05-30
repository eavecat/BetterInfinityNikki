using System;
using BetterInfinityNikki.Core.Recognition;
using BetterInfinityNikki.GameTask.Model;
using OpenCvSharp;

namespace BetterInfinityNikki.GameTask.MapMask.Assets;

public class MapMaskAssets : BaseAssets<MapMaskAssets>
{
    private readonly ILogger<MapMaskAssets> _logger = App.GetLogger<MapMaskAssets>();

    /// <summary>
    /// 大地图标题文字（左上角）
    /// </summary>
    public required RecognitionObject MapTitleRo;

    private MapMaskAssets()
    {
        // 加载地图标题模板
        MapTitleRo = new RecognitionObject
        {
            Name = "MapTitle",
            RecognitionType = RecognitionTypes.TemplateMatch,
            TemplateImageMat = GameTaskManager.LoadAssetImage("MapMask", "map_title.png", systemInfo),
            RegionOfInterest = new Rect(0, 0, CaptureRect.Width / 10, CaptureRect.Height / 10), // 左上角区域
            Threshold = 0.8,
            DrawOnWindow = true
        }.InitTemplate();
    }
}