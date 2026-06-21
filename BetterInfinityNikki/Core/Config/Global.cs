using System.IO;
using System.Reflection;
using Semver;

namespace BetterInfinityNikki.Core.Config;

public class Global
{
    public static string Version { get; } = Assembly.GetEntryAssembly()?
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
        .InformationalVersion ?? "0.1.0";

    public static string StartUpPath { get; set; } = AppContext.BaseDirectory;

    public static string Absolute(string relativePath)
    {
        return Path.Combine(StartUpPath, relativePath);
    }

    /// <summary>
    ///     新获取到的版本号与当前版本号比较，判断是否为新版本
    /// </summary>
    public static bool IsNewVersion(string currentVersion)
    {
        return IsNewVersion(Version, currentVersion);
    }

    /// <summary>
    ///     两个版本号比较，判断 currentVersion 是否比 oldVersion 更新
    /// </summary>
    public static bool IsNewVersion(string oldVersion, string currentVersion)
    {
        try
        {
            var oldVersionX = SemVersion.Parse(oldVersion, SemVersionStyles.Strict);
            var currentVersionX = SemVersion.Parse(currentVersion, SemVersionStyles.Strict);
            return currentVersionX.CompareSortOrderTo(oldVersionX) > 0;
        }
        catch
        {
            return false;
        }
    }

    public static string ReadAllTextIfExist(string relativePath)
    {
        var path = Absolute(relativePath);
        if (File.Exists(path))
        {
            return File.ReadAllText(path);
        }
        return string.Empty;
    }

    public static void WriteAllText(string relativePath, string content)
    {
        var path = Absolute(relativePath);
        // 确保目录存在
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        File.WriteAllText(path, content);
    }
}
