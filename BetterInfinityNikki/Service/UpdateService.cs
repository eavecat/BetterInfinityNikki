using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;
using BetterInfinityNikki.Core.Config;
using BetterInfinityNikki.Helpers;
using BetterInfinityNikki.Model;
using BetterInfinityNikki.Service.Interface;
using BetterInfinityNikki.Service.Model;
using BetterInfinityNikki.View.Windows;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Wpf.Ui.Violeta.Controls;

namespace BetterInfinityNikki.Service;

public class UpdateService : IUpdateService
{
    private readonly ILogger<UpdateService> _logger;
    private readonly IConfigService _configService;

    /// <summary>
    /// OTA 服务端版本检查接口。
    /// TODO: 部署正式地址后替换。
    /// </summary>
    private const string CheckUrl = "http://localhost:5000/api/betterin/check";

    public AllConfig Config { get; set; }

    public UpdateService(IConfigService configService)
    {
        _logger = App.GetLogger<UpdateService>();
        _configService = configService;
        Config = _configService.Get();
    }

    public async Task CheckUpdateAsync(UpdateOption option)
    {
        try
        {
            var latest = await GetLatestVersionAsync(option);
            if (latest == null || string.IsNullOrWhiteSpace(latest.Version))
            {
                return;
            }

            if (!Global.IsNewVersion(latest.Version))
            {
                if (option.Trigger == UpdateTrigger.Manual)
                {
                    await ThemedMessageBox.InformationAsync("当前已是最新版本！");
                }
                return;
            }

            // 自动触发时，若用户曾选择"不再提示"该版本则跳过
            if (option.Trigger == UpdateTrigger.Auto
                && !string.IsNullOrEmpty(Config.NotShowNewVersionNoticeEndVersion)
                && !Global.IsNewVersion(Config.NotShowNewVersionNoticeEndVersion, latest.Version))
            {
                return;
            }

            await OpenCheckUpdateWindowAsync(option, latest);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "获取 BetterIN 最新版本信息失败");
            if (option.Trigger == UpdateTrigger.Manual)
            {
                await ThemedMessageBox.WarningAsync("检查更新失败，请稍后再试。");
            }
        }
    }

    private async Task<CheckResponseData?> GetLatestVersionAsync(UpdateOption option)
    {
        try
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

            var channel = option.Channel == UpdateChannel.Alpha ? "alpha" : "stable";
            var url = $"{CheckUrl}?current_version={Uri.EscapeDataString(Global.Version)}"
                      + $"&device_id={Uri.EscapeDataString(DeviceIdHelper.DeviceId)}"
                      + $"&channel={channel}";

            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<CheckResponse>();
            if (result is { Code: 0 })
            {
                return result.Data;
            }
        }
        catch (Exception e)
        {
            _logger.LogDebug(e, "版本检查接口调用失败");
        }

        return null;
    }

    private async Task OpenCheckUpdateWindowAsync(UpdateOption option, CheckResponseData latest)
    {
        var win = new CheckUpdateWindow(option, latest)
        {
            Owner = Application.Current.MainWindow,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Title = $"发现新版本 {latest.Version}"
        };

        win.OnUserAction += async button =>
        {
            switch (button)
            {
                case CheckUpdateAction.Update:
                    await RunUpdaterAsync();
                    break;

                case CheckUpdateAction.ManualDownload:
                    if (!string.IsNullOrEmpty(latest.DownloadPageUrl))
                    {
                        Process.Start(new ProcessStartInfo(latest.DownloadPageUrl) { UseShellExecute = true });
                    }
                    break;

                case CheckUpdateAction.Ignore:
                    Config.NotShowNewVersionNoticeEndVersion = latest.Version;
                    break;

                case CheckUpdateAction.Cancel:
                default:
                    break;
            }
        };

        await Task.Run(() => Application.Current.Dispatcher.Invoke(() => win.ShowDialog()));
    }

    private async Task RunUpdaterAsync()
    {
        var updaterExePath = Global.Absolute("BetterIN.update.exe");
        if (!File.Exists(updaterExePath))
        {
            await ThemedMessageBox.ErrorAsync("更新程序不存在（BetterIN.update.exe），请选择手动下载方式！");
            return;
        }

        Process.Start(new ProcessStartInfo(updaterExePath, "-I") { UseShellExecute = true });
        Application.Current.Shutdown();
    }
}
