using System;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace BetterInfinityNikki.Helpers;

public class DeviceIdHelper
{
    private static readonly ILogger _logger = App.GetLogger<DeviceIdHelper>();
    private static readonly Lazy<string> _lazyDeviceId = new(InitializeDeviceId);

    public static string DeviceId => _lazyDeviceId.Value;

    private static string InitializeDeviceId()
    {
        try
        {
            // 使用 Windows 系统生成的 MachineGuid，稳定且不依赖额外包
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
            var guid = key?.GetValue("MachineGuid") as string;
            if (!string.IsNullOrEmpty(guid))
            {
                return guid;
            }

            // 兜底：随机生成并持久化到 User 目录
            var fallbackPath = System.IO.Path.Combine(
                AppContext.BaseDirectory, "User", ".device_id");
            if (System.IO.File.Exists(fallbackPath))
            {
                return System.IO.File.ReadAllText(fallbackPath).Trim();
            }
            var newId = Guid.NewGuid().ToString("N");
            try
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fallbackPath)!);
                System.IO.File.WriteAllText(fallbackPath, newId);
            }
            catch { /* 权限不足时忽略 */ }
            return newId;
        }
        catch (Exception e)
        {
            _logger.LogDebug(e, "获取设备ID失败");
            return string.Empty;
        }
    }
}
