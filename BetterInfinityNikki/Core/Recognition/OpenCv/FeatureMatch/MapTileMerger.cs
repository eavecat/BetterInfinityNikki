using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace BetterInfinityNikki.Core.Recognition.OpenCv.FeatureMatch;

/// <summary>
/// 地图切片合并工具 - 将多个切片拼接成完整地图
/// </summary>
public static class MapTileMerger
{
    /// <summary>
    /// 合并地图切片为完整图像
    /// </summary>
    /// <param name="tilesDirectory">切片文件目录</param>
    /// <param name="outputPath">输出完整地图的路径</param>
    /// <param name="rows">行数（64）</param>
    /// <param name="cols">列数（64）</param>
    /// <param name="fileNamePattern">文件名模式，例如 "6-{row}-{col}.webp"</param>
    public static void MergeTiles(
        string tilesDirectory,
        string outputPath,
        int rows = 64,
        int cols = 64,
        string fileNamePattern = "6-{row}-{col}.webp")
    {
        Console.WriteLine($"开始合并地图切片...");
        Console.WriteLine($"切片目录: {tilesDirectory}");
        
        // 第一步：扫描所有文件，解析行列索引
        var tileFiles = Directory.GetFiles(tilesDirectory, "*.webp")
            .Select(f => new
            {
                Path = f,
                FileName = Path.GetFileNameWithoutExtension(f)
            })
            .ToList();
        
        Console.WriteLine($"找到 {tileFiles.Count} 个切片文件");
        
        // 解析所有行列索引（注意：文件名格式是 6-列-行）
        var rowIndices = new HashSet<int>();
        var colIndices = new HashSet<int>();
        var tileMap = new Dictionary<(int row, int col), string>();
        
        foreach (var file in tileFiles)
        {
            var parts = file.FileName.Split('-');
            if (parts.Length >= 3 && 
                int.TryParse(parts[1], out int col) &&  // parts[1] 是列
                int.TryParse(parts[2], out int row))    // parts[2] 是行
            {
                rowIndices.Add(row);
                colIndices.Add(col);
                tileMap[(row, col)] = file.Path;
            }
        }
        
        int actualRows = rowIndices.Count;
        int actualCols = colIndices.Count;
        int minRow = rowIndices.Min();
        int maxRow = rowIndices.Max();
        int minCol = colIndices.Min();
        int maxCol = colIndices.Max();
        
        Console.WriteLine($"实际行数: {minRow}-{maxRow} (共 {actualRows} 行)");
        Console.WriteLine($"实际列数: {minCol}-{maxCol} (共 {actualCols} 列)");
        Console.WriteLine($"总计: {tileMap.Count} 个切片");
        
        if (actualRows == 0 || actualCols == 0)
        {
            throw new Exception("无法解析切片文件的行列索引");
        }
        
        // 读取第一个切片获取尺寸
        var firstTilePath = tileMap.Values.First();
        using var firstTile = Cv2.ImRead(firstTilePath, ImreadModes.Color);
        if (firstTile.Empty())
        {
            throw new Exception($"无法读取切片文件: {firstTilePath}");
        }
        
        int tileHeight = firstTile.Rows;
        int tileWidth = firstTile.Cols;
        
        Console.WriteLine($"单个切片尺寸: {tileWidth} x {tileHeight}");
        Console.WriteLine($"通道数: {firstTile.Channels()}");
        
        // 计算完整地图尺寸
        int fullHeight = tileHeight * actualRows;
        int fullWidth = tileWidth * actualCols;
        
        Console.WriteLine($"完整地图尺寸: {fullWidth} x {fullHeight}");
        
        // 创建完整地图图像（3通道 BGR）
        using var fullMap = new Mat(fullHeight, fullWidth, MatType.CV_8UC3);
        
        // 第二步：按正确的行列顺序合并
        int successCount = 0;
        int failCount = 0;
        
        for (int row = minRow; row <= maxRow; row++)
        {
            for (int col = minCol; col <= maxCol; col++)
            {
                // 计算在完整地图中的位置（从0开始）
                int mapRow = row - minRow;
                int mapCol = col - minCol;
                
                if (!tileMap.TryGetValue((row, col), out var tilePath))
                {
                    Console.WriteLine($"警告: 找不到切片 ({row}, {col})");
                    failCount++;
                    continue;
                }
                
                // 使用 Color 模式读取（3通道）
                using var tile = Cv2.ImRead(tilePath, ImreadModes.Color);
                
                if (tile.Empty())
                {
                    Console.WriteLine($"警告: 无法读取切片 ({row}, {col})");
                    failCount++;
                    continue;
                }
                
                // 计算目标区域
                var roi = new Rect(mapCol * tileWidth, mapRow * tileHeight, tileWidth, tileHeight);
                
                // 使用 SubMat 确保正确复制
                using var roiMat = fullMap.SubMat(roi);
                tile.CopyTo(roiMat);
                successCount++;
                
                // 每处理 100 个切片输出一次进度
                if (successCount % 100 == 0)
                {
                    Console.WriteLine($"进度: {successCount}/{tileMap.Count} 个切片");
                }
            }
        }
        
        Console.WriteLine($"成功合并: {successCount} 个切片");
        Console.WriteLine($"失败跳过: {failCount} 个切片");
        
        // 保存完整地图
        var outputDir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }
        
        Cv2.ImWrite(outputPath, fullMap);
        Console.WriteLine($"完整地图已保存到: {outputPath}");
        Console.WriteLine($"地图尺寸: {fullWidth} x {fullHeight}");
        
        // 验证保存的文件
        using var verify = Cv2.ImRead(outputPath, ImreadModes.Color);
        if (!verify.Empty())
        {
            var pixel = verify.At<Vec3b>(0, 0);
            Console.WriteLine($"验证: 左上角像素值 ({pixel.Item0}, {pixel.Item1}, {pixel.Item2})");
            
            // 检查是否全黑
            bool isBlack = verify.At<Vec3b>(0, 0).Item0 == 0 && 
                          verify.At<Vec3b>(0, 0).Item1 == 0 && 
                          verify.At<Vec3b>(0, 0).Item2 == 0;
            Console.WriteLine($"左上角是否全黑: {isBlack}");
        }
    }
    
    /// <summary>
    /// 分析切片目录结构
    /// </summary>
    public static void AnalyzeTiles(string tilesDirectory)
    {
        var files = Directory.GetFiles(tilesDirectory, "*.webp")
            .OrderBy(f => f)
            .ToList();
        
        Console.WriteLine($"\n=== 切片目录分析 ===");
        Console.WriteLine($"总文件数: {files.Count}");
        
        // 解析行列索引
        var rowIndices = new HashSet<int>();
        var colIndices = new HashSet<int>();
        
        foreach (var file in files.Take(100)) // 只分析前100个
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            var parts = fileName.Split('-');
            
            if (parts.Length >= 3 && 
                int.TryParse(parts[1], out int row) && 
                int.TryParse(parts[2], out int col))
            {
                rowIndices.Add(row);
                colIndices.Add(col);
            }
        }
        
        Console.WriteLine($"行数范围: {rowIndices.Min()}-{rowIndices.Max()} (共 {rowIndices.Count} 行)");
        Console.WriteLine($"列数范围: {colIndices.Min()}-{colIndices.Max()} (共 {colIndices.Count} 列)");
        Console.WriteLine("==================\n");
    }
}
