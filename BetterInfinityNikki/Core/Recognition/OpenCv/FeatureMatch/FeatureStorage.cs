using System;
using System.IO;
using BetterInfinityNikki.Core.Config;
using OpenCvSharp;

namespace BetterInfinityNikki.Core.Recognition.OpenCv.FeatureMatch;

/// <summary>
/// 特征存储类
/// 管理单个地图的特征数据（关键点和描述子）
/// </summary>
public class FeatureStorage
{
    private readonly string _rootPath;
    private readonly string _name;

    /// <summary>
    /// 特征类型名称（如 "SIFT"）
    /// </summary>
    public string TypeName { get; set; } = "UNKNOWN";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="name">地图名称</param>
    public FeatureStorage(string name)
    {
        _name = name;
        _rootPath = Global.Absolute(@"Assets\Map\");
    }

    /// <summary>
    /// 构造函数（自定义根路径）
    /// </summary>
    /// <param name="name">地图名称</param>
    /// <param name="rootPath">根路径</param>
    public FeatureStorage(string name, string rootPath)
    {
        _name = name;
        _rootPath = rootPath;
    }

    /// <summary>
    /// 设置特征类型
    /// </summary>
    /// <param name="type">特征类型</param>
    public void SetType(Feature2DType type)
    {
        TypeName = type.ToString();
    }

    /// <summary>
    /// 加载关键点数组
    /// </summary>
    /// <returns>关键点数组，如果文件不存在则返回 null</returns>
    public unsafe KeyPoint[]? LoadKeyPointArray()
    {
        CreateFolder();
        var kpPath = GetKeyPointPath();
        return FeatureStorageHelper.LoadKeyPointArray(kpPath);
    }

    /// <summary>
    /// 保存关键点数组
    /// </summary>
    /// <param name="kpArray">关键点数组</param>
    public unsafe void SaveKeyPointArray(KeyPoint[] kpArray)
    {
        CreateFolder();
        var kpPath = GetKeyPointPath();
        FeatureStorageHelper.SaveKeyPointArray(kpArray, kpPath);
    }

    /// <summary>
    /// 加载描述子矩阵
    /// </summary>
    /// <returns>描述子矩阵，如果文件不存在则返回 null</returns>
    public Mat? LoadDescriptors()
    {
        CreateFolder();
        var descPath = GetDescriptorPath();
        return FeatureStorageHelper.LoadDescriptors(descPath);
    }

    /// <summary>
    /// 保存描述子矩阵
    /// </summary>
    /// <param name="descriptors">描述子矩阵</param>
    public void SaveDescriptors(Mat descriptors)
    {
        CreateFolder();
        var descPath = GetDescriptorPath();
        FeatureStorageHelper.SaveDescriptors(descriptors, descPath);
    }

    /// <summary>
    /// 检查特征文件是否存在
    /// </summary>
    /// <returns>是否所有特征文件都存在</returns>
    public bool Exists()
    {
        var basePath = GetBasePath();
        return FeatureStorageHelper.FeatureFilesExist(basePath, TypeName);
    }

    /// <summary>
    /// 获取关键点文件路径
    /// </summary>
    /// <returns>关键点文件完整路径</returns>
    private string GetKeyPointPath()
    {
        var basePath = GetBasePath();
        FeatureStorageHelper.GetFeatureFilePaths(basePath, TypeName, out var kpPath, out _);
        return kpPath;
    }

    /// <summary>
    /// 获取描述子文件路径
    /// </summary>
    /// <returns>描述子文件完整路径</returns>
    private string GetDescriptorPath()
    {
        var basePath = GetBasePath();
        FeatureStorageHelper.GetFeatureFilePaths(basePath, TypeName, out _, out var descPath);
        return descPath;
    }

    /// <summary>
    /// 获取基础路径
    /// </summary>
    /// <returns>基础路径（不含扩展名）</returns>
    private string GetBasePath()
    {
        return Path.Combine(_rootPath, _name);
    }

    /// <summary>
    /// 创建文件夹（如果不存在）
    /// </summary>
    private void CreateFolder()
    {
        var folder = Path.GetDirectoryName(GetKeyPointPath())!;
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }
    }

    /// <summary>
    /// 删除特征文件
    /// </summary>
    public void Delete()
    {
        var kpPath = GetKeyPointPath();
        var descPath = GetDescriptorPath();

        if (File.Exists(kpPath))
        {
            File.Delete(kpPath);
        }

        if (File.Exists(descPath))
        {
            File.Delete(descPath);
        }
    }
}
