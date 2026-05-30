using System;
using System.IO;
using OpenCvSharp;
using OpenCvSharp.XFeatures2D;

namespace BetterInfinityNikki.Core.Recognition.OpenCv.FeatureMatch;

/// <summary>
/// 地图特征数据生成器 - 从完整地图图像生成 SIFT 特征数据
/// </summary>
public static class MapFeatureGenerator
{
    /// <summary>
    /// 从完整地图图像生成 SIFT 特征数据
    /// </summary>
    /// <param name="mapImagePath">完整地图图像路径</param>
    /// <param name="outputDirectory">特征数据输出目录</param>
    /// <param name="blockSize">分块大小（默认256）</param>
    public static void GenerateFeatures(
        string mapImagePath,
        string outputDirectory,
        int blockSize = 256)
    {
        Console.WriteLine($"\n开始生成 SIFT 特征数据...");
        Console.WriteLine($"地图图像: {mapImagePath}");
        Console.WriteLine($"输出目录: {outputDirectory}");
        
        // 加载地图图像
        if (!File.Exists(mapImagePath))
        {
            throw new FileNotFoundException($"找不到地图图像文件: {mapImagePath}");
        }
        
        using var mapImage = Cv2.ImRead(mapImagePath, ImreadModes.Color);
        Console.WriteLine($"地图尺寸: {mapImage.Width} x {mapImage.Height}");
        
        // 转换为灰度图
        using var grayMap = new Mat();
        Cv2.CvtColor(mapImage, grayMap, ColorConversionCodes.BGR2GRAY);
        
        // 获取 SIFT 检测器（使用工厂单例）
        var sift = Feature2DFactory.Get(Feature2DType.SIFT);
        
        Console.WriteLine($"SIFT 参数: blockSize={blockSize}");
        
        // 计算分块数量
        int rows = (grayMap.Height + blockSize - 1) / blockSize;
        int cols = (grayMap.Width + blockSize - 1) / blockSize;
        
        Console.WriteLine($"分块网格: {rows} x {cols}");
        
        // 创建输出目录
        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }
        
        // 提取并保存每个分块的特征
        int totalKeypoints = 0;
        
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                // 计算当前分块的 ROI
                int x = col * blockSize;
                int y = row * blockSize;
                int width = Math.Min(blockSize, grayMap.Width - x);
                int height = Math.Min(blockSize, grayMap.Height - y);
                
                if (width <= 0 || height <= 0)
                    continue;
                
                var roi = new Rect(x, y, width, height);
                using var blockMat = new Mat(grayMap, roi);
                
                // 提取 SIFT 特征
                using var descriptors = new Mat();
                sift.DetectAndCompute(blockMat, null, out var keypoints, descriptors);
                
                if (keypoints.Length > 0)
                {
                    totalKeypoints += keypoints.Length;
                    
                    // 生成分块文件名
                    var blockName = $"Teyvat_{row}_{col}";
                    
                    // 保存关键点（使用统一的格式）
                    var kpPath = Path.Combine(outputDirectory, $"{blockName}_SIFT.kp.bin");
                    FeatureStorageHelper.SaveKeyPointArray(keypoints, kpPath);
                    
                    // 保存描述子矩阵
                    var matPath = Path.Combine(outputDirectory, $"{blockName}_SIFT.mat.png");
                    FeatureStorageHelper.SaveDescriptors(descriptors, matPath);
                    
                    Console.WriteLine($"  分块 [{row},{col}]: {keypoints.Length} 个特征点");
                }
            }
            
            // 每处理10行输出进度
            if (row % 10 == 0)
            {
                Console.WriteLine($"进度: {row + 1}/{rows} 行");
            }
        }
        
        Console.WriteLine($"\n特征生成完成！");
        Console.WriteLine($"总特征点数: {totalKeypoints}");
        Console.WriteLine($"输出目录: {outputDirectory}");
    }
}
