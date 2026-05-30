using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenCvSharp;

namespace BetterInfinityNikki.Core.Recognition.OpenCv.FeatureMatch;

/// <summary>
/// 地图特征数据验证工具
/// </summary>
public static class FeatureDataValidator
{
    /// <summary>
    /// 验证特征数据并生成可视化报告
    /// </summary>
    public static void ValidateAndVisualize(
        string featuresDirectory,
        string fullMapPath,
        string outputDirectory)
    {
        Console.WriteLine("\n========================================");
        Console.WriteLine("🔍 开始验证特征数据...");
        Console.WriteLine("========================================\n");
        
        // 步骤 1: 基础验证
        Console.WriteLine("=== 步骤 1: 基础验证 ===");
        ValidateFileStructure(featuresDirectory);
        
        // 步骤 2: 统计特征分布
        Console.WriteLine("\n=== 步骤 2: 特征分布统计 ===");
        var stats = AnalyzeFeatureDistribution(featuresDirectory);
        
        // 步骤 3: 可视化验证（在地图上绘制特征点）
        Console.WriteLine("\n=== 步骤 3: 可视化验证 ===");
        VisualizeFeaturesOnMap(fullMapPath, featuresDirectory, outputDirectory);
        
        Console.WriteLine("\n========================================");
        Console.WriteLine("✅ 验证完成！");
        Console.WriteLine("========================================\n");
    }
    
    /// <summary>
    /// 验证文件结构
    /// </summary>
    private static void ValidateFileStructure(string featuresDirectory)
    {
        if (!Directory.Exists(featuresDirectory))
        {
            Console.WriteLine($"❌ 错误: 目录不存在 - {featuresDirectory}");
            return;
        }
        
        var kpFiles = Directory.GetFiles(featuresDirectory, "*_SIFT.kp.bin");
        var matFiles = Directory.GetFiles(featuresDirectory, "*_SIFT.mat.png");
        
        Console.WriteLine($"关键点文件 (.kp.bin): {kpFiles.Length} 个");
        Console.WriteLine($"描述子文件 (.mat.png): {matFiles.Length} 个");
        
        if (kpFiles.Length != matFiles.Length)
        {
            Console.WriteLine($"⚠️  警告: 文件数量不匹配！");
        }
        else
        {
            Console.WriteLine($"✅ 文件配对正常");
        }
        
        // 验证文件配对
        var kpNames = kpFiles.Select(f => Path.GetFileNameWithoutExtension(f).Replace("_SIFT.kp", "")).ToHashSet();
        var matNames = matFiles.Select(f => Path.GetFileNameWithoutExtension(f).Replace("_SIFT.mat", "")).ToHashSet();
        
        var missingInMat = kpNames.Except(matNames).ToList();
        var missingInKp = matNames.Except(kpNames).ToList();
        
        if (missingInMat.Any())
        {
            Console.WriteLine($"⚠️  缺少描述子文件: {string.Join(", ", missingInMat.Take(5))}...");
        }
        
        if (missingInKp.Any())
        {
            Console.WriteLine($"️  缺少关键点文件: {string.Join(", ", missingInKp.Take(5))}...");
        }
        
        if (!missingInMat.Any() && !missingInKp.Any())
        {
            Console.WriteLine($"✅ 所有文件正确配对");
        }
    }
    
    /// <summary>
    /// 分析特征分布
    /// </summary>
    private static FeatureStats AnalyzeFeatureDistribution(string featuresDirectory)
    {
        var stats = new FeatureStats();
        var kpFiles = Directory.GetFiles(featuresDirectory, "*_SIFT.kp.bin");
        
        int totalKeypoints = 0;
        int blocksWithFeatures = 0;
        var featureCounts = new List<int>();
        
        foreach (var kpFile in kpFiles)
        {
            var keypoints = FeatureStorageHelper.LoadKeyPointArray(kpFile);
            int count = keypoints?.Length ?? 0;
            totalKeypoints += count;
            featureCounts.Add(count);
            
            if (count > 0)
            {
                blocksWithFeatures++;
            }
        }
        
        stats.TotalBlocks = kpFiles.Length;
        stats.BlocksWithFeatures = blocksWithFeatures;
        stats.TotalKeypoints = totalKeypoints;
        stats.AveragePerBlock = blocksWithFeatures > 0 ? totalKeypoints / blocksWithFeatures : 0;
        stats.MaxPerBlock = featureCounts.Any() ? featureCounts.Max() : 0;
        stats.MinPerBlock = featureCounts.Any() ? featureCounts.Min() : 0;
        stats.EmptyBlocks = kpFiles.Length - blocksWithFeatures;
        
        Console.WriteLine($"总块数: {stats.TotalBlocks}");
        Console.WriteLine($"有特征的块: {stats.BlocksWithFeatures}");
        Console.WriteLine($"空块数: {stats.EmptyBlocks}");
        Console.WriteLine($"总特征点数: {stats.TotalKeypoints}");
        Console.WriteLine($"平均每块: {stats.AveragePerBlock} 个特征点");
        Console.WriteLine($"最多: {stats.MaxPerBlock} 个特征点");
        Console.WriteLine($"最少: {stats.MinPerBlock} 个特征点");
        Console.WriteLine($"✅ 覆盖率: {(double)stats.BlocksWithFeatures / stats.TotalBlocks * 100:F1}%");
        
        return stats;
    }
    
    /// <summary>
    /// 在地图上可视化特征点
    /// </summary>
    private static void VisualizeFeaturesOnMap(
        string fullMapPath,
        string featuresDirectory,
        string outputDirectory)
    {
        if (!File.Exists(fullMapPath))
        {
            Console.WriteLine($"❌ 错误: 地图文件不存在 - {fullMapPath}");
            return;
        }
        
        // 加载完整地图
        using var mapImage = Cv2.ImRead(fullMapPath, ImreadModes.Color);
        if (mapImage.Empty())
        {
            Console.WriteLine($"❌ 错误: 无法读取地图文件");
            return;
        }
        
        Console.WriteLine($"地图尺寸: {mapImage.Width} x {mapImage.Height}");
        
        // 解析分块参数（从文件名推断）
        // 假设文件名格式: Teyvat_行_列_SIFT.kp.bin
        // 需要推断 blockSize
        var kpFiles = Directory.GetFiles(featuresDirectory, "*_SIFT.kp.bin");
        
        if (!kpFiles.Any())
        {
            Console.WriteLine($"❌ 错误: 找不到关键点文件");
            return;
        }
        
        // 从文件名提取行列信息，推断网格大小
        int maxRow = 0;
        int maxCol = 0;
        
        foreach (var kpFile in kpFiles)
        {
            var fileName = Path.GetFileNameWithoutExtension(kpFile).Replace("_SIFT.kp", "");
            var parts = fileName.Split('_');
            
            if (parts.Length >= 3 && 
                int.TryParse(parts[1], out int row) && 
                int.TryParse(parts[2], out int col))
            {
                maxRow = Math.Max(maxRow, row);
                maxCol = Math.Max(maxCol, col);
            }
        }
        
        int rows = maxRow + 1;
        int cols = maxCol + 1;
        
        // 使用实际的分块大小（从文件名推断的网格大小可能不准确）
        // 实际生成时使用的是 256 像素分块
        int actualBlockSize = 256;
        int expectedRows = mapImage.Height / actualBlockSize;
        int expectedCols = mapImage.Width / actualBlockSize;
        
        Console.WriteLine($"推断网格: {rows} x {cols}");
        Console.WriteLine($"推断 blockSize: {mapImage.Height / rows}");
        Console.WriteLine($"实际分块大小: {actualBlockSize} (预期 {expectedRows} x {expectedCols} 网格)");
        
        // 在地图上绘制特征点
        int totalPoints = 0;
        
        foreach (var kpFile in kpFiles)
        {
            var fileName = Path.GetFileNameWithoutExtension(kpFile).Replace("_SIFT.kp", "");
            var parts = fileName.Split('_');
            
            if (parts.Length < 3 || 
                !int.TryParse(parts[1], out int row) || 
                !int.TryParse(parts[2], out int col))
            {
                continue;
            }
            
            // 计算分块在地图中的位置（使用实际的分块大小）
            int offsetX = col * actualBlockSize;
            int offsetY = row * actualBlockSize;
            
            // 使用统一的加载方法读取关键点
            var keypoints = FeatureStorageHelper.LoadKeyPointArray(kpFile);
            if (keypoints == null || keypoints.Length == 0)
            {
                continue;
            }
            
            // 绘制关键点（绿色圆点）
            foreach (var kp in keypoints)
            {
                // 转换为地图坐标
                int mapX = offsetX + (int)kp.Pt.X;
                int mapY = offsetY + (int)kp.Pt.Y;
                
                // 只在边界内绘制
                if (mapX >= 0 && mapX < mapImage.Width && mapY >= 0 && mapY < mapImage.Height)
                {
                    // 绘制小圆点
                    Cv2.Circle(mapImage, new Point(mapX, mapY), 2, new Scalar(0, 255, 0), -1);
                    totalPoints++;
                }
            }
        }
        
        Console.WriteLine($"✅ 在地图上绘制了 {totalPoints} 个特征点");
        
        // 保存可视化结果
        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }
        
        var outputPath = Path.Combine(outputDirectory, "features_visualization.png");
        Cv2.ImWrite(outputPath, mapImage);
        Console.WriteLine($"✅ 可视化结果已保存: {outputPath}");
        Console.WriteLine($" 请打开该图片查看特征点分布是否合理");
    }
    
    /// <summary>
    /// 特征统计信息
    /// </summary>
    private class FeatureStats
    {
        public int TotalBlocks { get; set; }
        public int BlocksWithFeatures { get; set; }
        public int EmptyBlocks { get; set; }
        public int TotalKeypoints { get; set; }
        public int AveragePerBlock { get; set; }
        public int MaxPerBlock { get; set; }
        public int MinPerBlock { get; set; }
    }
}
