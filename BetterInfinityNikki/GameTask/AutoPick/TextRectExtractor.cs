using OpenCvSharp;

namespace BetterInfinityNikki.GameTask.AutoPick;

public static class TextRectExtractor
{
    /// <summary>
    /// 从图片中提取文字范围（假定文字从最左边贴边开始，向右连续）
    /// 结果矩形固定 x=0,y=0,h=原图高度，只计算连续文字宽度。
    /// </summary>
    /// <param name="textMat">文字图片</param>
    /// <param name="min">二值化阈值</param>
    /// <param name="max">二值化阈值</param>
    /// <returns></returns>
    public static Rect GetTextBoundingRect(Mat textMat, double min = 160, double max = 255)
    {
        // 转换为灰度图
        Mat gray;
        if (textMat.Channels() == 3)
        {
            gray = new Mat();
            Cv2.CvtColor(textMat, gray, ColorConversionCodes.BGR2GRAY);
        }
        else
        {
            gray = textMat.Clone();
        }

        // 使用阈值160进行二值化处理
        using var bin = new Mat();
        Cv2.Threshold(gray, bin, min, max, ThresholdTypes.Binary);

        // 形态学操作：先腐蚀后膨胀，去除噪点并保持文字完整
        Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3));
        Cv2.Erode(bin, bin, kernel, iterations: 1);
        Cv2.Dilate(bin, bin, kernel, iterations: 2);
        kernel.Dispose();

        // 查找非零区域
        Rect boundingRect = Cv2.BoundingRect(bin);
        
        bin.Dispose();
        gray.Dispose();
        
        return boundingRect;
    }
}
