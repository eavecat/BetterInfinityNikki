using System.Threading;
using BetterInfinityNikki.Core.Simulator;
using BetterInfinityNikki.GameTask.InsectCatching.Assets;
using OpenCvSharp;
using OpenCvRect = OpenCvSharp.Rect;

namespace BetterInfinityNikki.GameTask.InsectCatching;

/// <summary>
/// 璨花捕影触发器（自动捕虫）
/// </summary>
public partial class InsectCatchingTrigger : ITaskTrigger
{
    private readonly ILogger<InsectCatchingTrigger> _logger = App.GetLogger<InsectCatchingTrigger>();
    private readonly InsectCatchingAssets _insectCatchingAssets;

    private DateTime _prevExecute = DateTime.MinValue;
    private const int ExecuteInterval = 1200;
    private const int GoldenPixelThreshold = 300; // 金色像素阈值常量

    public int Priority => 30;
    public bool IsExclusive => false;
    public string Name => "InsectCatching";
    public bool IsEnabled { get; set; }
    
    public InsectCatchingTrigger()
    {
        _insectCatchingAssets = InsectCatchingAssets.Instance();
    }

    public void Init()
    {
        var config = TaskContext.Instance().Config.AutoPickConfig;
        IsEnabled = config.CanHuaBuYingEnabled;
        _logger.LogInformation("璨花捕影 初始化: IsEnabled={Enabled}", IsEnabled);
        _prevExecute = DateTime.MinValue;
    }

    public void OnCapture(CaptureContent content)
    {
        // 检查触发器是否启用
        if (!IsEnabled)
        {
            return;
        }

        var config = TaskContext.Instance().Config.AutoPickConfig;
        if (!config.CanHuaBuYingEnabled)
        {
            return;
        }

        // 执行间隔控制
        if ((DateTime.Now - _prevExecute).TotalMilliseconds < ExecuteInterval)
        {
            return;
        }

        try
        {
            _prevExecute = DateTime.Now;
            TryInsectCatching(content);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "璨花捕影: 处理异常");
        }
    }

    /// <summary>
    /// 尝试璨花捕影（自动捕虫）
    /// 检测逻辑：
    /// 1. 先识别捕虫按钮图标
    /// 2. 在按钮周围区域检测金色像素点数量
    /// 3. 金色像素达到阈值则认为可以触发捕虫
    /// </summary>
    private void TryInsectCatching(CaptureContent content)
    {
        try
        {
            // 1. 识别捕虫按钮图标
            using var catchArea = content.CaptureRectArea.Find(_insectCatchingAssets.InsectCatchingNormalRo);

            if (catchArea.IsEmpty())
            {
                return; // 未找到捕虫按钮，直接返回
            }

            // 2. 在捕虫按钮周围区域检测金色像素点
            var buttonCenterX = catchArea.X + catchArea.Width / 2;
            var buttonCenterY = catchArea.Y + catchArea.Height / 2;

            // 定义检测区域：按钮周围扩大区域（半径约按钮宽度的1.5倍）
            var detectRadius = (int)(catchArea.Width * 1);
            var detectRect = new OpenCvRect(
                Math.Max(0, buttonCenterX - detectRadius),
                Math.Max(0, buttonCenterY - detectRadius),
                Math.Min(content.CaptureRectArea.SrcMat.Width, buttonCenterX + detectRadius) -
                Math.Max(0, buttonCenterX - detectRadius),
                Math.Min(content.CaptureRectArea.SrcMat.Height, buttonCenterY + detectRadius) -
                Math.Max(0, buttonCenterY - detectRadius)
            );

            // 边界检查
            if (detectRect.Width <= 0 || detectRect.Height <= 0)
            {
                return;
            }

            // 3. 检测金色像素点
            using var detectMat = new Mat(content.CaptureRectArea.SrcMat, detectRect);
            var goldenPixelCount = CountGoldenPixels(detectMat);

            // 4. 判断是否达到触发阈值
            if (goldenPixelCount >= GoldenPixelThreshold)
            {
                _logger.LogInformation("璨花捕影: 检测到捕虫光点（金色像素: {goldenPixelCount}），触发右键", goldenPixelCount);

                // 点击右键触发捕虫
                Simulation.SendInput.Mouse.RightButtonDown();
                Thread.Sleep(40);
                Simulation.SendInput.Mouse.RightButtonUp();

                // 等待捕虫动画完成
                Thread.Sleep(500);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "璨花捕影: 处理失败");
        }
    }

    /// <summary>
    /// 统计金色像素点数量
    /// 金色特征：RGB空间中 R高、G中高、B低
    /// 根据图片分析，金色光环的像素值范围约为：
    /// - R: 180-245 (高亮度红色)
    /// - G: 140-220 (中等亮度绿色)
    /// - B: 90-160 (低亮度蓝色)
    /// </summary>
    /// <param name="mat">待检测的图像矩阵</param>
    /// <returns>金色像素点数量</returns>
    private int CountGoldenPixels(Mat mat)
    {
        int goldenCount = 0;
        var channels = mat.Channels();

        for (int y = 0; y < mat.Rows; y++)
        {
            for (int x = 0; x < mat.Cols; x++)
            {
                Vec3b pixel;
                if (channels == 3)
                {
                    pixel = mat.At<Vec3b>(y, x);
                }
                else if (channels == 4)
                {
                    var pixel4 = mat.At<Vec4b>(y, x);
                    pixel = new Vec3b(pixel4.Item0, pixel4.Item1, pixel4.Item2);
                }
                else
                {
                    continue; // 不支持的通道数
                }

                // OpenCV 使用 BGR 顺序
                var b = pixel.Item0;
                var g = pixel.Item1;
                var r = pixel.Item2;

                // 检测金色像素：R高、G中高、B低
                if (r >= 180 && r <= 245 &&
                    g >= 160 && g <= 220 &&
                    b >= 100 && b <= 160)
                {
                    goldenCount++;
                }
            }
        }

        return goldenCount;
    }
}
