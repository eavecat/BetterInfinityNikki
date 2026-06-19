using System;
using System.Collections.Generic;
using System.Linq;
using BetterInfinityNikki.GameTask.Common.BgiVision;
using BetterInfinityNikki.Helpers;
using OpenCvSharp;

namespace BetterInfinityNikki.Core.Recognition.OpenCv.FeatureMatch;

public static class Feature2DExtensions
{
    private static readonly Dictionary<DescriptorMatcherType, DescriptorMatcher> MatcherFactory = new()
    {
        { DescriptorMatcherType.BruteForce, DescriptorMatcher.Create(DescriptorMatcherType.BruteForce.ToString()) },
        { DescriptorMatcherType.FlannBased, DescriptorMatcher.Create(DescriptorMatcherType.FlannBased.ToString()) }
    };

    private static DescriptorMatcher GetMatcher(DescriptorMatcherType type)
    {
        return MatcherFactory[type];
    }

    #region 生成并保存特征

    public static void SaveFeatures(this Feature2D feature2D, string trainImagePath, string trainKeyPointsPath, string trainDescriptorsPath)
    {
        Mat trainDescriptors = new();
        var img = Bv.ImRead(trainImagePath, ImreadModes.Grayscale);

        feature2D.DetectAndCompute(img, null, out var trainKeyPoints, trainDescriptors);

        FeatureStorageHelper.SaveKeyPointArray(trainKeyPoints, trainKeyPointsPath);
        FeatureStorageHelper.SaveDescMat(trainDescriptors, trainDescriptorsPath);
    }

    #endregion

    #region 普通匹配

    /// <summary>
    /// 用于从训练图像和查询图像中找到匹配的特征点，计算透视变换并确定查询图像的中心点在训练图像中的位置。
    /// </summary>
    /// <param name="feature2D"></param>
    /// <param name="trainKeyPoints">训练图像中的关键点集合。</param>
    /// <param name="trainDescriptors">训练图像的特征描述子。</param>
    /// <param name="queryMat">查询图像的 Mat 对象。</param>
    /// <param name="queryMatMask">查询图像的 Mask，用于限定检测特征的区域。</param>
    /// <param name="matcherType">描述符匹配器的类型，默认为 DescriptorMatcherType.FlannBased</param>
    /// <returns></returns>
    public static Point2f Match(this Feature2D feature2D, KeyPoint[] trainKeyPoints, Mat trainDescriptors, Mat queryMat, Mat? queryMatMask = null,
        DescriptorMatcherType matcherType = DescriptorMatcherType.FlannBased)
    {
        SpeedTimer speedTimer = new();

        using var queryDescriptors = new Mat();
#pragma warning disable CS8604 // 引用类型参数可能为 null。
        feature2D.DetectAndCompute(queryMat, queryMatMask, out var queryKeyPoints, queryDescriptors);
#pragma warning restore CS8604 // 引用类型参数可能为 null。
        speedTimer.Record("模板生成KeyPoint");

        var matches = GetMatcher(matcherType).Match(queryDescriptors, trainDescriptors);
        //Finding the Minimum and Maximum Distance
        double minDistance = 1000; //Backward approximation
        double maxDistance = 0;
        for (int i = 0; i < queryDescriptors.Rows; i++)
        {
            double distance = matches[i].Distance;
            if (distance > maxDistance)
            {
                maxDistance = distance;
            }

            if (distance < minDistance)
            {
                minDistance = distance;
            }
        }

        // Debug.WriteLine($"max distance : {maxDistance}");
        // Debug.WriteLine($"min distance : {minDistance}");

        var pointsQuery = new List<Point2f>();
        var pointsTrain = new List<Point2f>();
        //Screening better matching points
        // var goodMatches = new List<DMatch>();
        for (int i = 0; i < queryDescriptors.Rows; i++)
        {
            double distance = matches[i].Distance;
            if (distance < Math.Max(minDistance * 2, 0.02))
            {
                pointsQuery.Add(queryKeyPoints[matches[i].QueryIdx].Pt);
                pointsTrain.Add(trainKeyPoints[matches[i].TrainIdx].Pt);
                //Compression of new ones with distances less than ranges DMatch
                // goodMatches.Add(matches[i]);
            }
        }

        speedTimer.Record("FlannMatch");

        // var outMat = new Mat();

        // algorithm RANSAC Filter the matched results
        var pQuery = pointsQuery.ToPoint2d();
        var pTrain = pointsTrain.ToPoint2d();
        var outMask = new Mat();
        // If the original matching result is null, Skip the filtering step
        if (pQuery.Count > 0 && pTrain.Count > 0)
        {
            var hMat = Cv2.FindHomography(pQuery, pTrain, HomographyMethods.Ransac, mask: outMask);
            speedTimer.Record("FindHomography");

            // 1. 计算查询图像的中心点
            var queryCenterPoint = new Point2f(queryMat.Cols / 2f, queryMat.Rows / 2f);

            // 2. 使用单应矩阵进行透视变换
            Point2f[] queryCenterPoints = [queryCenterPoint];
            Point2f[] transformedCenterPoints = Cv2.PerspectiveTransform(queryCenterPoints, hMat);

            // 3. 获取变换后的中心点
            var trainCenterPoint = transformedCenterPoints[0];
            speedTimer.Record("PerspectiveTransform");
            speedTimer.DebugPrint();
            return trainCenterPoint;
        }

        speedTimer.DebugPrint();
        return new Point2f();
    }

    #endregion 普通匹配

    #region Knn匹配

    /// <summary>
    /// https://github.com/tignioj/minimap/blob/main/matchmap/sifttest/sifttest5.py
    /// Copilot 生成
    /// </summary>
    /// <returns></returns>
    public static Point2f KnnMatch(this Feature2D feature2D, KeyPoint[] trainKeyPoints, Mat trainDescriptors, Mat queryMat, Mat? queryMatMask = null,
        DescriptorMatcherType matcherType = DescriptorMatcherType.FlannBased)
    {
        SpeedTimer speedTimer = new();
        using var queryDescriptors = new Mat();
#pragma warning disable CS8604 // 引用类型参数可能为 null。
        feature2D.DetectAndCompute(queryMat, queryMatMask, out var queryKeyPoints, queryDescriptors);
#pragma warning restore CS8604 // 引用类型参数可能为 null。
        speedTimer.Record("模板生成KeyPoint");

        DMatch[][] matches;
        try
        {
            matches = GetMatcher(matcherType).KnnMatch(queryDescriptors, trainDescriptors, k: 2);
        }
        catch (Exception)
        {
            matches = GetMatcher(DescriptorMatcherType.BruteForce).KnnMatch(queryDescriptors, trainDescriptors, k: 2);
        }
        speedTimer.Record("FlannMatch");

        // 应用比例测试来过滤匹配点
        List<DMatch> goodMatches = [];
        foreach (var match in matches)
        {
            if (match.Length == 2 && match[0].Distance < 0.75 * match[1].Distance)
            {
                goodMatches.Add(match[0]);
            }
        }

        if (goodMatches.Count < 7)
        {
            return new Point2f();
        }

        // 获取匹配点的坐标
        var srcPts = goodMatches.Select(m => queryKeyPoints[m.QueryIdx].Pt).ToArray();
        var dstPts = goodMatches.Select(m => trainKeyPoints[m.TrainIdx].Pt).ToArray();

        speedTimer.Record("GetGoodMatchPoints");

        // 使用RANSAC找到变换矩阵
        var mask = new Mat();
        var hMat = Cv2.FindHomography(srcPts.ToList().ToPoint2d(), dstPts.ToList().ToPoint2d(), HomographyMethods.Ransac, 3.0, mask);
        if (hMat.Empty())
        {
            return new Point2f();
        }

        speedTimer.Record("FindHomography");

        // 计算小地图的中心点
        var h = queryMat.Rows;
        var w = queryMat.Cols;
        var centerPoint = new Point2f(w / 2f, h / 2f);
        Point2f[] centerPoints = [centerPoint];
        Point2f[] transformedCenter = Cv2.PerspectiveTransform(centerPoints, hMat);

        speedTimer.Record("PerspectiveTransform");
        speedTimer.DebugPrint();
        // 返回小地图在大地图中的中心坐标
        return transformedCenter[0];
    }

    public static Point2f[] KnnMatchCorners(this Feature2D feature2D, KeyPoint[] trainKeyPoints, Mat trainDescriptors, Mat queryMat, Mat? queryMatMask = null,
        DescriptorMatcherType matcherType = DescriptorMatcherType.FlannBased)
    {
        SpeedTimer speedTimer = new();
        using var queryDescriptors = new Mat();
#pragma warning disable CS8604 // 引用类型参数可能为 null。
        feature2D.DetectAndCompute(queryMat, queryMatMask, out var queryKeyPoints, queryDescriptors);
#pragma warning restore CS8604 // 引用类型参数可能为 null。
        
        // 检查是否检测到特征点
        if (queryDescriptors.Empty() || queryDescriptors.Rows == 0)
        {
            return [];
        }
        
        // 确保查询描述子是 CV_32F 类型（FLANN 匹配器要求）
        using var queryDescriptorsFloat = new Mat();
        if (queryDescriptors.Type() != MatType.CV_32FC1)
        {
            queryDescriptors.ConvertTo(queryDescriptorsFloat, MatType.CV_32FC1);
        }
        else
        {
            queryDescriptors.CopyTo(queryDescriptorsFloat);
        }
        
        // 确保训练描述子也是 CV_32F 类型
        using var trainDescriptorsFloat = new Mat();
        if (trainDescriptors.Type() != MatType.CV_32FC1)
        {
            trainDescriptors.ConvertTo(trainDescriptorsFloat, MatType.CV_32FC1);
        }
        else
        {
            trainDescriptors.CopyTo(trainDescriptorsFloat);
        }
        
        speedTimer.Record("模板生成KeyPoint");

        DMatch[][] matches;
        try
        {
            matches = GetMatcher(matcherType).KnnMatch(queryDescriptorsFloat, trainDescriptorsFloat, k: 2);
        }
        catch (Exception)
        {
            matches = GetMatcher(DescriptorMatcherType.BruteForce).KnnMatch(queryDescriptorsFloat, trainDescriptorsFloat, k: 2);
        }
        speedTimer.Record("FlannMatch");

        // 应用比例测试来过滤匹配点
        List<DMatch> goodMatches = [];
        foreach (var match in matches)
        {
            if (match.Length == 2 && match[0].Distance < 0.75 * match[1].Distance)
            {
                goodMatches.Add(match[0]);
            }
        }

        if (goodMatches.Count < 7)
        {
            return [];
        }

        // 获取匹配点的坐标
        var srcPts = goodMatches.Select(m => queryKeyPoints[m.QueryIdx].Pt).ToArray();
        var dstPts = goodMatches.Select(m => trainKeyPoints[m.TrainIdx].Pt).ToArray();

        speedTimer.Record("GetGoodMatchPoints");

        // 使用RANSAC找到变换矩阵
        var mask = new Mat();
        var hMat = Cv2.FindHomography(srcPts.ToList().ToPoint2d(), dstPts.ToList().ToPoint2d(), HomographyMethods.Ransac, 3.0, mask);
        if (hMat.Empty())
        {
            return [];
        }

        speedTimer.Record("FindHomography");

        // 返回四个角点
        var objCorners = new Point2f[4];
        objCorners[0] = new Point2f(0, 0);
        objCorners[1] = new Point2f(0, queryMat.Rows);
        objCorners[2] = new Point2f(queryMat.Cols, queryMat.Rows);
        objCorners[3] = new Point2f(queryMat.Cols, 0);

        var sceneCorners = Cv2.PerspectiveTransform(objCorners, hMat);
        speedTimer.Record("PerspectiveTransform");
        speedTimer.DebugPrint();

        var dbgRect = Cv2.BoundingRect(sceneCorners);
        Serilog.Log.Information(
            "[KnnMatchCorners] queryMat={Cols}x{Rows}, " +
            "sceneCorners=[{Corners}], rect=({RX},{RY},{RW},{RH})",
            queryMat.Cols, queryMat.Rows,
            string.Join(",", sceneCorners.Select(c => $"({c.X:F1},{c.Y:F1})")),
            dbgRect.X, dbgRect.Y, dbgRect.Width, dbgRect.Height);

        return sceneCorners;
    }

    public static Rect KnnMatchRect(this Feature2D feature2D, KeyPoint[] trainKeyPoints, Mat trainDescriptors, Mat queryMat, Mat? queryMatMask = null)
    {
        var corners = KnnMatchCorners(feature2D, trainKeyPoints, trainDescriptors, queryMat, queryMatMask);
        if (corners.Length == 0)
        {
            return default;
        }

        var rect = Cv2.BoundingRect(corners);
        
        // 调试日志：输出坐标范围
        System.Diagnostics.Debug.WriteLine($"[KnnMatchRect] 角点坐标: [{string.Join(", ", corners.Select(c => $"({c.X:F0},{c.Y:F0})"))}]");
        System.Diagnostics.Debug.WriteLine($"[KnnMatchRect] 边界矩形: {rect}, queryMat尺寸: {queryMat.Cols}x{queryMat.Rows}");
        
        // 裁剪到地图边界内（避免负坐标或超出范围）
        // 注意：trainKeyPoints 的坐标空间是训练地图的全尺寸（16384x16384）
        if (rect.X < 0) rect = new Rect(0, rect.Y, rect.Width + rect.X, rect.Height);
        if (rect.Y < 0) rect = new Rect(rect.X, 0, rect.Width, rect.Height + rect.Y);
        
        return rect;
    }

    /// <summary>
    /// 针对小图像的 KNN 匹配（放宽阈值，上采样）
    /// 适用于小地图等小尺寸图像的 SIFT 匹配
    /// </summary>
    /// <param name="feature2D">特征检测器</param>
    /// <param name="trainKeyPoints">训练关键点</param>
    /// <param name="trainDescriptors">训练描述子</param>
    /// <param name="queryMat">查询图像（小尺寸）</param>
    /// <param name="scaleFactor">上采样倍数（默认2）</param>
    /// <returns>查询图像中心点在训练图像坐标系中的位置，匹配失败返回 default</returns>
    public static Point2f KnnMatchCenterRelaxed(this Feature2D feature2D, KeyPoint[] trainKeyPoints, Mat trainDescriptors, Mat queryMat, double scaleFactor = 2.0)
    {
        using var scaledMat = new Mat();
        if (scaleFactor > 1.0)
        {
            Cv2.Resize(queryMat, scaledMat, new Size(), scaleFactor, scaleFactor, InterpolationFlags.Cubic);
        }
        else
        {
            queryMat.CopyTo(scaledMat);
        }

        using var queryDescriptors = new Mat();
        feature2D.DetectAndCompute(scaledMat, null, out var queryKeyPoints, queryDescriptors);

        if (queryDescriptors.Empty() || queryDescriptors.Rows < 4)
        {
            System.Diagnostics.Debug.WriteLine($"[KnnMatchCenterRelaxed] 特征点不足: {queryDescriptors.Rows}");
            return default;
        }

        using var queryDescFloat = new Mat();
        if (queryDescriptors.Type() != MatType.CV_32FC1)
            queryDescriptors.ConvertTo(queryDescFloat, MatType.CV_32FC1);
        else
            queryDescriptors.CopyTo(queryDescFloat);

        using var trainDescFloat = new Mat();
        if (trainDescriptors.Type() != MatType.CV_32FC1)
            trainDescriptors.ConvertTo(trainDescFloat, MatType.CV_32FC1);
        else
            trainDescriptors.CopyTo(trainDescFloat);

        DMatch[][] matches;
        try
        {
            matches = GetMatcher(DescriptorMatcherType.FlannBased).KnnMatch(queryDescFloat, trainDescFloat, k: 2);
        }
        catch (Exception)
        {
            matches = GetMatcher(DescriptorMatcherType.BruteForce).KnnMatch(queryDescFloat, trainDescFloat, k: 2);
        }

        List<DMatch> goodMatches = [];
        foreach (var match in matches)
        {
            if (match.Length == 2 && match[0].Distance < 0.85 * match[1].Distance)
            {
                goodMatches.Add(match[0]);
            }
        }

        System.Diagnostics.Debug.WriteLine($"[KnnMatchCenterRelaxed] 总匹配: {matches.Length}, 好匹配: {goodMatches.Count}");

        if (goodMatches.Count < 4)
        {
            return default;
        }

        var srcPts = goodMatches.Select(m => queryKeyPoints[m.QueryIdx].Pt).ToArray();
        var dstPts = goodMatches.Select(m => trainKeyPoints[m.TrainIdx].Pt).ToArray();

        var mask = new Mat();
        var hMat = Cv2.FindHomography(srcPts.ToList().ToPoint2d(), dstPts.ToList().ToPoint2d(), HomographyMethods.Ransac, 3.0, mask);
        if (hMat.Empty())
        {
            return default;
        }

        var centerPoint = new Point2f(scaledMat.Cols / 2f, scaledMat.Rows / 2f);
        Point2f[] centerPoints = [centerPoint];
        Point2f[] transformedCenter = Cv2.PerspectiveTransform(centerPoints, hMat);

        return transformedCenter[0];
    }

    #endregion Knn匹配
}