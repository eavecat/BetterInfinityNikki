using System;
using System.IO;
using System.Runtime.InteropServices;
using OpenCvSharp;
using OpenCvSharp.Internal.Vectors;

namespace BetterInfinityNikki.Core.Recognition.OpenCv.FeatureMatch;

/// <summary>
/// 特征存储助手类
/// 提供关键点和描述子的文件读写功能
/// </summary>
public static class FeatureStorageHelper
{
    /// <summary>
    /// 从文件加载关键点数组
    /// </summary>
    /// <param name="kpPath">关键点文件路径（.kp.bin）</param>
    /// <returns>关键点数组，如果文件不存在则返回 null</returns>
    /// <exception cref="FileFormatException">文件格式不正确</exception>
    public static unsafe KeyPoint[]? LoadKeyPointArray(string kpPath)
    {
        if (!File.Exists(kpPath))
        {
            return null;
        }

        using var fs = File.Open(kpPath, FileMode.Open);
        var sizeOfKeyPoint = Marshal.SizeOf<KeyPoint>();
        
        // 验证文件大小是否正确
        if (fs.Length % sizeOfKeyPoint != 0)
        {
            throw new FileFormatException("无法识别的 KeyPoint 格式");
        }

        // 创建向量并读取数据
        using var kpVector = new VectorOfKeyPoint((nuint)(fs.Length / sizeOfKeyPoint));
        using var ms = new UnmanagedMemoryStream(
            (byte*)kpVector.ElemPtr, 
            fs.Length, 
            fs.Length, 
            FileAccess.Write);
        
        fs.CopyTo(ms);
        return kpVector.ToArray();
    }

    /// <summary>
    /// 保存关键点数组到文件
    /// </summary>
    /// <param name="kpArray">关键点数组</param>
    /// <param name="kpPath">输出文件路径（.kp.bin）</param>
    public static unsafe void SaveKeyPointArray(KeyPoint[] kpArray, string kpPath)
    {
        var kpVector = new VectorOfKeyPoint(kpArray);
        var sizeOfKeyPoint = Marshal.SizeOf<KeyPoint>();
        var kpSpan = new ReadOnlySpan<byte>(
            (byte*)kpVector.ElemPtr, 
            kpArray.Length * sizeOfKeyPoint);
        
        using var fs = new FileStream(kpPath, FileMode.Create);
        fs.Write(kpSpan);
    }

    /// <summary>
    /// 从文件加载描述子矩阵
    /// </summary>
    /// <param name="descPath">描述子文件路径（.mat.png 或 .desc.bin）</param>
    /// <returns>描述子矩阵，如果文件不存在则返回 null</returns>
    public static Mat? LoadDescriptors(string descPath)
    {
        if (!File.Exists(descPath))
        {
            return null;
        }

        // 如果是 .mat.png 格式，使用 OpenCV 加载
        if (descPath.EndsWith(".mat.png", StringComparison.OrdinalIgnoreCase))
        {
            return Cv2.ImRead(descPath, ImreadModes.Unchanged);
        }

        // 如果是 .desc.bin 格式，直接加载二进制数据
        return new Mat(descPath);
    }

    /// <summary>
    /// 保存描述子矩阵到文件
    /// </summary>
    /// <param name="descriptors">描述子矩阵</param>
    /// <param name="descPath">输出文件路径</param>
    public static void SaveDescriptors(Mat descriptors, string descPath)
    {
        // 统一保存为 PNG 格式（压缩率高，适合大矩阵）
        Cv2.ImWrite(descPath, descriptors);
    }

    /// <summary>
    /// 检查特征文件是否存在
    /// </summary>
    /// <param name="basePath">基础路径（不含扩展名）</param>
    /// <param name="featureType">特征类型名称（如 "SIFT"）</param>
    /// <returns>是否所有特征文件都存在</returns>
    public static bool FeatureFilesExist(string basePath, string featureType)
    {
        var folder = Path.GetDirectoryName(basePath)!;
        var fileName = Path.GetFileNameWithoutExtension(basePath);

        var kpPath = Path.Combine(folder, $"{fileName}_{featureType}.kp.bin");
        var descPath = Path.Combine(folder, $"{fileName}_{featureType}.mat.png");

        return File.Exists(kpPath) && File.Exists(descPath);
    }

    /// <summary>
    /// 获取特征文件路径
    /// </summary>
    /// <param name="basePath">基础路径（不含扩展名）</param>
    /// <param name="featureType">特征类型名称（如 "SIFT"）</param>
    /// <param name="kpPath">输出的关键点文件路径</param>
    /// <param name="descPath">输出的描述子文件路径</param>
    public static void GetFeatureFilePaths(
        string basePath, 
        string featureType, 
        out string kpPath, 
        out string descPath)
    {
        var folder = Path.GetDirectoryName(basePath)!;
        var fileName = Path.GetFileNameWithoutExtension(basePath);

        kpPath = Path.Combine(folder, $"{fileName}_{featureType}.kp.bin");
        descPath = Path.Combine(folder, $"{fileName}_{featureType}.mat.png");
    }
}
