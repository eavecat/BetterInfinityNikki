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
}
