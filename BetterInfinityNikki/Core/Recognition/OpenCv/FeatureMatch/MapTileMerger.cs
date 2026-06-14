using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace BetterInfinityNikki.Core.Recognition.OpenCv.FeatureMatch;

public static class MapTileMerger
{
    public static void MergeTiles(
        string tilesDirectory,
        string outputPath,
        int startX = 0,
        int startY = 0,
        int endX = 63,
        int endY = 63,
        Action<string>? log = null)
    {
        void Log(string msg) { log?.Invoke(msg); Console.WriteLine(msg); }
        Log($"开始合并地图切片...");
        Log($"切片目录: {tilesDirectory}");

        var tileFiles = Directory.GetFiles(tilesDirectory, "*.webp")
            .Select(f => Path.GetFileNameWithoutExtension(f))
            .ToHashSet();

        int actualCols = endX - startX + 1;
        int actualRows = endY - startY + 1;

        Log($"范围: X[{startX}-{endX}] Y[{startY}-{endY}]");
        Log($"网格: {actualCols} 列 x {actualRows} 行");

        if (actualRows <= 0 || actualCols <= 0)
        {
            throw new Exception("起始行列必须小于结束行列");
        }

        var firstTilePath = Path.Combine(tilesDirectory, $"6-{startX}-{startY}.webp");
        using var firstTile = Cv2.ImRead(firstTilePath, ImreadModes.Color);
        if (firstTile.Empty())
        {
            throw new Exception($"无法读取切片文件: {firstTilePath}");
        }

        int tileHeight = firstTile.Rows;
        int tileWidth = firstTile.Cols;

        Log($"单个切片尺寸: {tileWidth} x {tileHeight}");

        int fullHeight = tileHeight * actualRows;
        int fullWidth = tileWidth * actualCols;

        Log($"完整地图尺寸: {fullWidth} x {fullHeight}");

        using var fullMap = new Mat(fullHeight, fullWidth, MatType.CV_8UC3);

        int successCount = 0;
        int failCount = 0;

        for (int x = startX; x <= endX; x++)
        {
            for (int y = startY; y <= endY; y++)
            {
                int mapCol = x - startX;
                int mapRow = y - startY;

                var tileFileName = $"6-{x}-{y}";
                if (!tileFiles.Contains(tileFileName))
                {
                    failCount++;
                    continue;
                }

                var tilePath = Path.Combine(tilesDirectory, $"{tileFileName}.webp");
                using var tile = Cv2.ImRead(tilePath, ImreadModes.Color);

                if (tile.Empty())
                {
                    failCount++;
                    continue;
                }

                var roi = new Rect(mapCol * tileWidth, mapRow * tileHeight, tileWidth, tileHeight);
                using var roiMat = fullMap.SubMat(roi);
                tile.CopyTo(roiMat);
                successCount++;

                if (successCount % 50 == 0)
                {
                    Log($"进度: {successCount}/{actualCols * actualRows} 个切片");
                }
            }
        }

        Log($"成功合并: {successCount} 个切片");
        Log($"失败跳过: {failCount} 个切片");

        var outputDir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        Cv2.ImWrite(outputPath, fullMap);
        Log($"完整地图已保存到: {outputPath}");
        Log($"地图尺寸: {fullWidth} x {fullHeight}");
    }
}
