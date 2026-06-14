using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using OpenCvSharp;
using OpenCvSharp.Features2D;
using OpenCvSharp.Internal.Vectors;

namespace BetterInfinityNikki.Core.Recognition.OpenCv.FeatureMatch;

/// <summary>
/// 地图特征数据生成器 - 从完整地图图像生成 SIFT 特征数据
/// 按 BGI LargeSiftExtractor 风格，输出单个合并的特征文件
/// </summary>
public static class MapFeatureGenerator
{
    private const int BlockSize = 1024;
    private const int OverlapSize = BlockSize * 3;
    private const int MaxParallelism = 24;

    /// <summary>
    /// 从完整地图图像生成 SIFT 特征数据（合并输出为单个 .kp.bin + .mat.png）
    /// </summary>
    /// <param name="mapImagePath">完整地图图像路径（灰度或彩色均可）</param>
    /// <param name="outputDirectory">特征数据输出目录</param>
    /// <param name="outputName">输出文件名前缀（不含扩展名）</param>
    public static void GenerateFeatures(
        string mapImagePath,
        string outputDirectory,
        string outputName = "NikkiWorld_0")
    {
        Console.WriteLine($"开始提取图像的SIFT特征: {mapImagePath}");

        if (!File.Exists(mapImagePath))
        {
            throw new FileNotFoundException($"找不到地图图像文件: {mapImagePath}");
        }

        Console.WriteLine($"文件大小: {new FileInfo(mapImagePath).Length / 1024 / 1024}MB");
        Console.WriteLine("正在读取图像...");

        Environment.SetEnvironmentVariable("OPENCV_IO_MAX_IMAGE_PIXELS", Math.Pow(2, 40).ToString("F0"));

        var img = Cv2.ImRead(mapImagePath, ImreadModes.Grayscale);
        if (img.Empty())
        {
            throw new InvalidOperationException($"无法读取地图图像: {mapImagePath}");
        }

        Console.WriteLine($"图像读取完成: {img.Width} x {img.Height}");
        Console.WriteLine($"图像被分成 {img.Height / BlockSize} 行 {img.Width / BlockSize} 列的块。");

        int rows = (int)Math.Ceiling(img.Height / (double)BlockSize);
        int cols = (int)Math.Ceiling(img.Width / (double)BlockSize);
        int totalBlocks = rows * cols;

        var blockResults = new BlockProcessResult[totalBlocks];

        Parallel.For(0, totalBlocks,
            new ParallelOptions { MaxDegreeOfParallelism = MaxParallelism },
            () => SIFT.Create(
                nFeatures: 0,
                nOctaveLayers: 3,
                contrastThreshold: 0.03,
                edgeThreshold: 12,
                sigma: 1.5),
            (index, _, sift) =>
            {
                int row = index / cols;
                int col = index % cols;
                Console.WriteLine($"处理第 {row} 行，第 {col} 列的块");
                var (keypoints, descriptors) = ProcessBlock(img, row, col, sift);
                blockResults[index] = new BlockProcessResult(row, col, keypoints, descriptors);
                return sift;
            },
            sift => sift.Dispose());

        var allKeypoints = new List<KeyPoint>();
        var allDescriptors = new List<Mat>();

        for (int index = 0; index < totalBlocks; index++)
        {
            var blockResult = blockResults[index];
            var keypoints = blockResult.Keypoints;
            for (int i = 0; i < keypoints.Length; i++)
            {
                var kp = keypoints[i];
                kp.Pt.X += blockResult.Col * BlockSize;
                kp.Pt.Y += blockResult.Row * BlockSize;
                keypoints[i] = kp;
            }

            allKeypoints.AddRange(keypoints);
            allDescriptors.Add(blockResult.Descriptors);
        }

        var finalDescriptors = new Mat();
        Cv2.VConcat(allDescriptors.ToArray(), finalDescriptors);

        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        SaveFeatures(allKeypoints.ToArray(), finalDescriptors, outputDirectory, outputName);

        foreach (var desc in allDescriptors)
        {
            desc.Dispose();
        }

        Console.WriteLine($"SIFT特征提取和保存完成。");
    }

    private static (KeyPoint[] keypoints, Mat descriptors) ProcessBlock(Mat img, int row, int col, Feature2D sift)
    {
        int startY = row * BlockSize;
        int startX = col * BlockSize;

        bool isEdgeRow = row == 0 || row == img.Height / BlockSize;
        bool isEdgeCol = col == 0 || col == img.Width / BlockSize;

        if (!isEdgeRow && !isEdgeCol)
        {
            int overlapStartY = Math.Max(0, startY - BlockSize);
            int overlapStartX = Math.Max(0, startX - BlockSize);

            using var blockRegion = new Mat(img,
                new Rect(overlapStartX, overlapStartY,
                    Math.Min(OverlapSize, img.Width - overlapStartX),
                    Math.Min(OverlapSize, img.Height - overlapStartY)));

            Mat desc = new();
            sift.DetectAndCompute(blockRegion, null, out var kps, desc);

            var centralKeypointIndices = new List<int>();
            var centralKeypoints = new List<KeyPoint>();

            for (int i = 0; i < kps.Length; i++)
            {
                if (kps[i].Pt.X >= BlockSize && kps[i].Pt.X < BlockSize * 2 &&
                    kps[i].Pt.Y >= BlockSize && kps[i].Pt.Y < BlockSize * 2)
                {
                    centralKeypointIndices.Add(i);
                    var kp = kps[i];
                    kp.Pt.X -= BlockSize;
                    kp.Pt.Y -= BlockSize;
                    centralKeypoints.Add(kp);
                }
            }

            var centralDesc = new Mat(centralKeypointIndices.Count, desc.Cols, desc.Type());
            for (int i = 0; i < centralKeypointIndices.Count; i++)
            {
                var rowIndex = centralKeypointIndices[i];
                var row2 = desc.Row(rowIndex);
                row2.CopyTo(centralDesc.Row(i));
                row2.Dispose();
            }

            Console.WriteLine($"中心区域处理了 {centralKeypoints.Count} 个关键点。");
            return (centralKeypoints.ToArray(), centralDesc);
        }
        else
        {
            using var blockRegion = new Mat(img,
                new Rect(startX, startY,
                    Math.Min(BlockSize, img.Width - startX),
                    Math.Min(BlockSize, img.Height - startY)));

            var desc = new Mat();
            sift.DetectAndCompute(blockRegion, null, out var kps, desc);

            Console.WriteLine($"边缘区域处理了 {kps.Length} 个关键点。");
            return (kps, desc);
        }
    }

    private static void SaveFeatures(KeyPoint[] keypoints, Mat descriptors, string outputPath, string outputName)
    {
        Console.WriteLine($"保存 {keypoints.Length} 个关键点和描述符到 {outputPath}");

        var kpPath = Path.Combine(outputPath, $"{outputName}_SIFT.kp.bin");
        var matPath = Path.Combine(outputPath, $"{outputName}_SIFT.mat.png");

        SaveKeyPointArray(keypoints, kpPath);
        descriptors.SaveImage(matPath);

        Console.WriteLine("特征保存成功。");
    }

    private static unsafe void SaveKeyPointArray(KeyPoint[] kpArray, string outputPath)
    {
        var kpVector = new VectorOfKeyPoint(kpArray);
        var sizeOfKeyPoint = Marshal.SizeOf<KeyPoint>();
        var kpSpan = new ReadOnlySpan<byte>((byte*)kpVector.ElemPtr, kpArray.Length * sizeOfKeyPoint);
        using var fs = new FileStream(outputPath, FileMode.Create);
        fs.Write(kpSpan);
    }

    private readonly record struct BlockProcessResult(int Row, int Col, KeyPoint[] Keypoints, Mat Descriptors);
}
