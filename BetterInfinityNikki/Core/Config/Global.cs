using System.IO;
using System.Reflection;

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
