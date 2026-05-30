using System;
using System.IO;
using BetterInfinityNikki.Core.Recognition.OpenCv.FeatureMatch;

namespace BetterInfinityNikki.GameTask.Common.Map.Maps;

/// <summary>
/// 世界地图特征数据生成脚本
/// </summary>
public static class WorldMapFeatureBuilder
{
    /// <summary>
    /// 构建世界地图特征数据
    /// </summary>
    public static void BuildWorldMapFeatures()
    {
        Console.WriteLine("========================================");
        Console.WriteLine("无限暖暖世界地图特征数据构建工具");
        Console.WriteLine("========================================\n");
        
        try
        {
            // 配置路径
            string tilesDirectory = @"D:\code\test\images";
            string outputBaseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Map", "NikkiWorld");
            string fullMapPath = Path.Combine(outputBaseDir, "full_map.png");
            string featuresOutputDir = Path.Combine(outputBaseDir, "Features");
            
            // 确保输出目录存在
            Directory.CreateDirectory(outputBaseDir);
            
            // 步骤 1: 分析切片
            Console.WriteLine("【步骤 1】分析切片目录结构...");
            MapTileMerger.AnalyzeTiles(tilesDirectory);
            
            // 步骤 2: 合并切片
            Console.WriteLine("\n【步骤 2】合并地图切片...");
            MapTileMerger.MergeTiles(
                tilesDirectory: tilesDirectory,
                outputPath: fullMapPath,
                rows: 64,
                cols: 64,
                fileNamePattern: "6-{row}-{col}.webp"
            );
            
            // 步骤 3: 生成 SIFT 特征数据
            Console.WriteLine("\n【步骤 3】生成 SIFT 特征数据...");
            MapFeatureGenerator.GenerateFeatures(
                mapImagePath: fullMapPath,
                outputDirectory: featuresOutputDir,
                blockSize: 256
            );
            
            // 步骤 4: 验证特征数据
            Console.WriteLine("\n【步骤 4】验证特征数据...");
            string validationOutputDir = Path.Combine(outputBaseDir, "Validation");
            FeatureDataValidator.ValidateAndVisualize(
                featuresDirectory: featuresOutputDir,
                fullMapPath: fullMapPath,
                outputDirectory: validationOutputDir
            );
            
            Console.WriteLine("\n========================================");
            Console.WriteLine("✅ 所有步骤完成！");
            Console.WriteLine($"完整地图: {fullMapPath}");
            Console.WriteLine($"特征数据: {featuresOutputDir}");
            Console.WriteLine("========================================");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"\n❌ 错误: {ex.Message}");
            Console.Error.WriteLine($"堆栈跟踪:\n{ex.StackTrace}");
            throw;
        }
    }
}
