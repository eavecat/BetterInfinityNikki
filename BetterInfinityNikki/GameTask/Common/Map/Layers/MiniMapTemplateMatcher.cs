using System;
using System.IO;
using System.Threading;
using BetterInfinityNikki.GameTask.Common.Map.Maps;
using Microsoft.Extensions.Logging;
using OpenCvSharp;

namespace BetterInfinityNikki.GameTask.Common.Map.Layers;

public class MiniMapTemplateMatcher
{
    private static readonly object Lock = new();
    private static MiniMapTemplateMatcher? _instance;

    private readonly ILogger<MiniMapTemplateMatcher> _logger;
    private readonly NikkiWorldMap _worldMap;

    private Mat? _fullMapGrey;
    private Mat? _fullMapGreyRough;
    private Mat? _fullMapEdges;
    private Mat? _fullMapEdgesRough;
    private bool _isLoaded;
    private int _loading;

    private const int RoughDownscale = 8;
    private const int RoughSearchRadius = 200;
    private const int ExactSearchRadius = 120;
    private const int FineSearchRadius = 60;
    private const double RoughConfidenceThreshold = 0.35;
    private const double EdgeConfidenceThreshold = 0.30;

    private const int CannyLow = 40;
    private const int CannyHigh = 120;

    private Point _prevRoughPos;
    private bool _hasPrevPos;

    private MiniMapTemplateMatcher(NikkiWorldMap worldMap)
    {
        _logger = App.GetLogger<MiniMapTemplateMatcher>();
        _worldMap = worldMap;
    }

    public static MiniMapTemplateMatcher GetInstance(NikkiWorldMap worldMap)
    {
        if (_instance == null)
        {
            lock (Lock)
            {
                _instance ??= new MiniMapTemplateMatcher(worldMap);
            }
        }
        return _instance;
    }

    private void EnsureLoaded()
    {
        if (_isLoaded) return;

        if (Interlocked.Exchange(ref _loading, 1) == 0)
        {
            try
            {
                var fullMapPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Assets", "Map", "NikkiWorld", "full_map.png");

                if (!File.Exists(fullMapPath))
                {
                    _logger.LogWarning("full_map.png 不存在: {Path}", fullMapPath);
                    return;
                }

                _fullMapGrey = Cv2.ImRead(fullMapPath, ImreadModes.Grayscale);
                if (_fullMapGrey.Empty())
                {
                    _logger.LogWarning("full_map.png 加载失败");
                    return;
                }

                _fullMapGreyRough = new Mat();
                Cv2.Resize(_fullMapGrey, _fullMapGreyRough, new Size(),
                    1.0 / RoughDownscale, 1.0 / RoughDownscale, InterpolationFlags.Area);

                _fullMapEdges = new Mat();
                Cv2.Canny(_fullMapGrey, _fullMapEdges, CannyLow, CannyHigh);

                _fullMapEdgesRough = new Mat();
                Cv2.Resize(_fullMapEdges, _fullMapEdgesRough, new Size(),
                    1.0 / RoughDownscale, 1.0 / RoughDownscale, InterpolationFlags.Area);
                Cv2.Threshold(_fullMapEdgesRough, _fullMapEdgesRough, 1, 255, ThresholdTypes.Binary);

                _isLoaded = true;
            }
            finally
            {
                Interlocked.Exchange(ref _loading, 0);
            }
        }
        else
        {
            while (!_isLoaded) Thread.Sleep(10);
        }
    }

    /// <summary>
    /// 通过模板匹配定位小地图中心在世界地图上的位置
    /// 使用 Canny 边缘 + CCoeffNormed 进行匹配，以弥合小地图样式与全图灰度之间的视觉差异
    /// </summary>
    /// <param name="greyMinimap">灰度小地图图像（已裁剪）</param>
    /// <returns>世界地图坐标系中的中心点，失败返回 default</returns>
    public Point2f Match(Mat greyMinimap)
    {
        EnsureLoaded();

        if (_fullMapGrey == null || _fullMapGreyRough == null ||
            _fullMapEdges == null || _fullMapEdgesRough == null)
        {
            return default;
        }

        try
        {
            using var croppedMinimap = CropInscribedSquare(greyMinimap);
            if (croppedMinimap.Cols < 20 || croppedMinimap.Rows < 20)
            {
                return default;
            }

            // 提取小地图边缘
            using var minimapEdges = new Mat();
            Cv2.Canny(croppedMinimap, minimapEdges, CannyLow, CannyHigh);

            // 粗匹配：8x 缩小后在边缘图上全图/局部搜索
            using var templateRough = new Mat();
            Cv2.Resize(minimapEdges, templateRough, new Size(),
                1.0 / RoughDownscale, 1.0 / RoughDownscale, InterpolationFlags.Area);
            Cv2.Threshold(templateRough, templateRough, 1, 255, ThresholdTypes.Binary);

            Point roughPos;
            double roughConf;

            if (_hasPrevPos)
            {
                var searchRoi = GetSearchRoi(
                    _fullMapEdgesRough.Cols, _fullMapEdgesRough.Rows,
                    _prevRoughPos.X, _prevRoughPos.Y,
                    RoughSearchRadius + templateRough.Cols);

                if (searchRoi.Width > templateRough.Cols && searchRoi.Height > templateRough.Rows)
                {
                    using var searchRegion = new Mat(_fullMapEdgesRough, searchRoi);
                    var (localPos, localConf) = MatchTemplate(searchRegion, templateRough);
                    if (localPos.X >= 0 && localConf >= RoughConfidenceThreshold)
                    {
                        roughPos = new Point(
                            searchRoi.X + localPos.X + templateRough.Cols / 2,
                            searchRoi.Y + localPos.Y + templateRough.Rows / 2);
                        roughConf = localConf;
                    }
                    else
                    {
                        (roughPos, roughConf) = MatchTemplateGlobalWithConfidence(_fullMapEdgesRough, templateRough);
                    }
                }
                else
                {
                    (roughPos, roughConf) = MatchTemplateGlobalWithConfidence(_fullMapEdgesRough, templateRough);
                }
            }
            else
            {
                (roughPos, roughConf) = MatchTemplateGlobalWithConfidence(_fullMapEdgesRough, templateRough);
            }

            if (roughPos.X < 0)
                return default;

            _prevRoughPos = roughPos;
            _hasPrevPos = true;

            // 精匹配：在原始分辨率的边缘图上局部搜索
            var exactCenterX = roughPos.X * RoughDownscale;
            var exactCenterY = roughPos.Y * RoughDownscale;
            var exactRadius = ExactSearchRadius + croppedMinimap.Cols;

            var exactRoi = GetSearchRoi(
                _fullMapEdges.Cols, _fullMapEdges.Rows,
                exactCenterX, exactCenterY, exactRadius);

            if (exactRoi.Width <= minimapEdges.Cols || exactRoi.Height <= minimapEdges.Rows)
            {
                return ConvertImageToMap(exactCenterX, exactCenterY);
            }

            using var exactRegion = new Mat(_fullMapEdges, exactRoi);
            var (exactPos, exactConf) = MatchTemplate(exactRegion, minimapEdges);

            int finalX, finalY;
            double fineConf = exactConf;
            if (exactPos.X >= 0 && exactConf >= EdgeConfidenceThreshold)
            {
                finalX = exactRoi.X + exactPos.X + minimapEdges.Cols / 2;
                finalY = exactRoi.Y + exactPos.Y + minimapEdges.Rows / 2;
            }
            else
            {
                finalX = exactCenterX;
                finalY = exactCenterY;
            }

            // 精修：在更小范围内再次边缘匹配，用于微调
            if (finalX > 0 && finalY > 0)
            {
                var fineRoi = GetSearchRoi(
                    _fullMapEdges.Cols, _fullMapEdges.Rows,
                    finalX, finalY, FineSearchRadius + minimapEdges.Cols);

                if (fineRoi.Width > minimapEdges.Cols && fineRoi.Height > minimapEdges.Rows)
                {
                    using var fineRegion = new Mat(_fullMapEdges, fineRoi);
                    var (finePos, fConf) = MatchTemplate(fineRegion, minimapEdges);
                    if (finePos.X >= 0 && fConf > fineConf)
                    {
                        finalX = fineRoi.X + finePos.X + minimapEdges.Cols / 2;
                        finalY = fineRoi.Y + finePos.Y + minimapEdges.Rows / 2;
                        fineConf = fConf;
                    }
                }
            }

            return ConvertImageToMap(finalX, finalY);
        }
        catch (Exception)
        {
            return default;
        }
    }

    /// <summary>
    /// 从正方形图像中裁切出最大内接正方形（去掉圆形小地图四角的黑色区域）
    /// 边长取 0.7 * side，保证完全落在圆内同时保留大部分地图内容
    /// </summary>
    private static Mat CropInscribedSquare(Mat square)
    {
        var side = Math.Min(square.Cols, square.Rows);
        var cropSide = (int)(side * 0.7) & ~1;
        if (cropSide < 20) cropSide = Math.Min(side, 20);
        var offset = (side - cropSide) / 2;
        var roi = new OpenCvSharp.Rect(offset, offset, cropSide, cropSide);
        return new Mat(square, roi).Clone();
    }

    private (Point pos, double confidence) MatchTemplate(Mat source, Mat template)
    {
        if (source.Cols <= template.Cols || source.Rows <= template.Rows)
            return (new Point(-1, -1), 0);

        using var result = new Mat();
        Cv2.MatchTemplate(source, template, result, TemplateMatchModes.CCoeffNormed);
        Cv2.MinMaxLoc(result, out _, out _, out _, out var maxLoc);
        var confidence = result.At<float>(maxLoc.Y, maxLoc.X);
        return (maxLoc, confidence);
    }

    private (Point pos, double confidence) MatchTemplateGlobalWithConfidence(Mat source, Mat template)
    {
        if (source.Cols <= template.Cols || source.Rows <= template.Rows)
            return (new Point(-1, -1), 0);

        using var result = new Mat();
        Cv2.MatchTemplate(source, template, result, TemplateMatchModes.CCoeffNormed);
        Cv2.MinMaxLoc(result, out _, out var maxVal, out _, out var maxLoc);

        if (maxVal < RoughConfidenceThreshold)
            return (new Point(-1, -1), maxVal);

        var pos = new Point(
            maxLoc.X + template.Cols / 2,
            maxLoc.Y + template.Rows / 2);
        return (pos, maxVal);
    }

    private Point2f ConvertImageToMap(int imageX, int imageY)
    {
        return new Point2f(imageX, imageY);
    }

    private static OpenCvSharp.Rect GetSearchRoi(int srcW, int srcH, int cx, int cy, int radius)
    {
        var x = Math.Max(0, cx - radius);
        var y = Math.Max(0, cy - radius);
        var w = Math.Min(srcW - x, radius * 2);
        var h = Math.Min(srcH - y, radius * 2);
        return new OpenCvSharp.Rect(x, y, w, h);
    }

    public void ResetTracking()
    {
        _hasPrevPos = false;
        _prevRoughPos = default;
    }

    public void Dispose()
    {
        _fullMapGrey?.Dispose();
        _fullMapGrey = null;
        _fullMapGreyRough?.Dispose();
        _fullMapGreyRough = null;
        _fullMapEdges?.Dispose();
        _fullMapEdges = null;
        _fullMapEdgesRough?.Dispose();
        _fullMapEdgesRough = null;
        _isLoaded = false;

        if (_instance != null)
        {
            lock (Lock) { _instance = null; }
        }
    }
}
