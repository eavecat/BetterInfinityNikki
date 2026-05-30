using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BetterInfinityNikki.Core.Recognition.OpenCv.Model;
using OpenCvSharp;

namespace BetterInfinityNikki.Core.Recognition.OpenCv.FeatureMatch;

/// <summary>
/// 关键点特征分块助手类
/// 用于将地图特征点按空间位置分块，加速搜索
/// </summary>
public static class KeyPointFeatureBlockHelper
{
    /// <summary>
    /// 将特征点按网格分块
    /// </summary>
    /// <param name="originalImage">原始图像尺寸</param>
    /// <param name="rows">行数</param>
    /// <param name="cols">列数</param>
    /// <param name="keyPoints">所有关键点</param>
    /// <param name="descriptors">所有描述子</param>
    /// <returns>二维分块数组 [row][col]</returns>
    public static KeyPointFeatureBlock[][] SplitFeatures(
        Size originalImage, 
        int rows, 
        int cols, 
        KeyPoint[] keyPoints, 
        Mat descriptors)
    {
        var matchesCols = descriptors.Cols; // SIFT: 128, SURF: 64
        
        // 计算网格大小
        int cellWidth = originalImage.Width / cols;
        int cellHeight = originalImage.Height / rows;

        // 初始化分块数组
        var splitKeyPoints = new KeyPointFeatureBlock[rows][];
        for (int i = 0; i < rows; i++)
        {
            splitKeyPoints[i] = new KeyPointFeatureBlock[cols];
            for (int j = 0; j < cols; j++)
            {
                splitKeyPoints[i][j] = new KeyPointFeatureBlock();
            }
        }

        // 将每个关键点分配到对应的区块
        for (int i = 0; i < keyPoints.Length; i++)
        {
            int row = (int)(keyPoints[i].Pt.Y / cellHeight);
            int col = (int)(keyPoints[i].Pt.X / cellWidth);

            // 确保索引在范围内
            row = Math.Min(Math.Max(row, 0), rows - 1);
            col = Math.Min(Math.Max(col, 0), cols - 1);

            splitKeyPoints[row][col].KeyPointList.Add(keyPoints[i]);
            splitKeyPoints[row][col].KeyPointIndexList.Add(i);
        }

        // 为每个区块创建描述子矩阵
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                var block = splitKeyPoints[i][j];
                if (block.KeyPointIndexList.Count > 0)
                {
                    block.Descriptor = new Mat(block.KeyPointIndexList.Count, matchesCols, MatType.CV_32FC1);
                    InitBlockMat(block.KeyPointIndexList, block.Descriptor, descriptors);
                }
            }
        }

        return splitKeyPoints;
    }

    /// <summary>
    /// 初始化区块的描述子矩阵
    /// </summary>
    /// <param name="indices">关键点索引列表</param>
    /// <param name="destMat">目标矩阵</param>
    /// <param name="sourceMat">源描述子矩阵</param>
    private static unsafe void InitBlockMat(List<int> indices, Mat destMat, Mat sourceMat)
    {
        int cols = sourceMat.Cols; // 描述子维度（SIFT: 128）
        
        // 使用 unsafe 指针直接拷贝，避免 Matrix<T> 的步长问题
        float* ptrDest = (float*)destMat.DataPointer;
        
        for (int i = 0; i < indices.Count; i++)
        {
            var index = indices[i];
            
            // 边界检查
            if (index < 0 || index >= sourceMat.Rows)
            {
                throw new ArgumentOutOfRangeException(nameof(index), $"索引 {index} 超出范围 [0, {sourceMat.Rows})");
            }
            
            // 获取源矩阵的第 index 行
            float* ptrSrcRow = (float*)sourceMat.Ptr(index);
            
            // 拷贝一行数据（cols 个 float）
            Buffer.MemoryCopy(ptrSrcRow, ptrDest + i * cols, cols * sizeof(float), cols * sizeof(float));
        }
    }

    /// <summary>
    /// 矩阵访问辅助结构
    /// </summary>
    private readonly ref struct Matrix<T>
    {
        private readonly ref T reference;
        private readonly int width;
        private readonly int height;

        public unsafe Matrix(void* pointer, int width, int height)
        {
            reference = ref Unsafe.AsRef<T>(pointer);
            this.width = width;
            this.height = height;
        }

        public Span<T> this[int row]
        {
            get
            {
                if (row < 0 || row >= height)
                {
                    throw new IndexOutOfRangeException();
                }

                return MemoryMarshal.CreateSpan(ref Unsafe.Add(ref reference, row * width), width);
            }
        }
    }

    /// <summary>
    /// 合并指定范围内的区块特征
    /// </summary>
    /// <param name="splitBlocks">分块数组</param>
    /// <param name="allDescriptors">所有描述子（用于获取列数）</param>
    /// <param name="rowStart">起始行</param>
    /// <param name="rowEnd">结束行</param>
    /// <param name="colStart">起始列</param>
    /// <param name="colEnd">结束列</param>
    /// <returns>合并后的特征块</returns>
    public static KeyPointFeatureBlock MergeFeaturesInRange(
        KeyPointFeatureBlock[][] splitBlocks,
        Mat allDescriptors,
        int rowStart,
        int rowEnd,
        int colStart,
        int colEnd)
    {
        var rows = splitBlocks.Length;
        var cols = splitBlocks[0].Length;
        var matchesCols = allDescriptors.Cols;

        // 边界检查
        rowStart = Math.Max(rowStart, 0);
        rowEnd = Math.Min(rowEnd, rows - 1);
        colStart = Math.Max(colStart, 0);
        colEnd = Math.Min(colEnd, cols - 1);

        // 收集范围内的所有特征
        var neighboringKeyPoints = new List<KeyPoint>();
        var neighboringKeyPointIndices = new List<int>();

        for (int i = rowStart; i <= rowEnd; i++)
        {
            for (int j = colStart; j <= colEnd; j++)
            {
                var block = splitBlocks[i][j];
                neighboringKeyPoints.AddRange(block.KeyPointList);
                neighboringKeyPointIndices.AddRange(block.KeyPointIndexList);
            }
        }

        // 创建合并后的特征块
        var mergedBlock = new KeyPointFeatureBlock
        {
            MergedCenterCellCol = (colStart + colEnd) / 2,
            MergedCenterCellRow = (rowStart + rowEnd) / 2,
            KeyPointList = neighboringKeyPoints,
            KeyPointIndexList = neighboringKeyPointIndices
        };

        // 创建描述子矩阵
        if (neighboringKeyPointIndices.Count > 0)
        {
            mergedBlock.Descriptor = new Mat(neighboringKeyPointIndices.Count, matchesCols, MatType.CV_32FC1);
            InitBlockMat(neighboringKeyPointIndices, mergedBlock.Descriptor, allDescriptors);
        }

        return mergedBlock;
    }

    /// <summary>
    /// 根据矩形位置获取对应的区块范围
    /// </summary>
    /// <param name="mapSize">地图尺寸</param>
    /// <param name="rows">总行数</param>
    /// <param name="cols">总列数</param>
    /// <param name="rect">矩形区域</param>
    /// <returns>(rowStart, rowEnd, colStart, colEnd)</returns>
    public static (int rowStart, int rowEnd, int colStart, int colEnd) GetCellRange(
        Size mapSize,
        int rows,
        int cols,
        Rect rect)
    {
        var cellWidth = mapSize.Width / cols;
        var cellHeight = mapSize.Height / rows;

        var colStart = (int)(rect.X / cellWidth);
        var colEnd = (int)((rect.X + rect.Width) / cellWidth);
        var rowStart = (int)(rect.Y / cellHeight);
        var rowEnd = (int)((rect.Y + rect.Height) / cellHeight);

        // 边界检查
        colStart = Math.Max(colStart, 0);
        colEnd = Math.Min(colEnd, cols - 1);
        rowStart = Math.Max(rowStart, 0);
        rowEnd = Math.Min(rowEnd, rows - 1);

        return (rowStart, rowEnd, colStart, colEnd);
    }
}
