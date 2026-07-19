using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
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
        CheckUpdateWindow win = new(option, latest)
        {
            Owner = Application.Current.MainWindow,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Title = $"发现新版本 {latest.Version}",
            UserInteraction = async (sender, button) =>
            {
                switch (button)
                {
                    case CheckUpdateWindow.CheckUpdateWindowButton.OtherUpdate:
                        var downloadUrl = !string.IsNullOrEmpty(latest.DownloadPageUrl)
                            ? latest.DownloadPageUrl
                            : "https://www.bettergi.com/download.html";
                        Process.Start(new ProcessStartInfo(downloadUrl) { UseShellExecute = true });
                        break;

                    case CheckUpdateWindow.CheckUpdateWindowButton.Ignore:
                        Config.NotShowNewVersionNoticeEndVersion = latest.Version;
                        break;

                    case CheckUpdateWindow.CheckUpdateWindowButton.Cancel:
                    default:
                        break;
                }
            }
        };

        if (!string.IsNullOrEmpty(latest.ReleaseNotes))
        {
            win.NavigateToHtml(BuildReleaseMarkdownHtml(latest.Version, latest.ReleaseNotes));
        }
        else
        {
            win.NavigateToHtml(await GetReleaseMarkdownHtmlAsync());
        }

        win.ShowDialog();
    }

    private string BuildReleaseMarkdownHtml(string version, string releaseNotes)
    {
        string md = $"# v{version}{new string('\n', 2)}{releaseNotes}";
        md = WebUtility.HtmlEncode(md);
        string md2html = File.ReadAllText(Global.Absolute(@"Assets\Strings\md2html.html"), Encoding.UTF8);
        return md2html.Replace("{{content}}", md);
    }

    private async Task<string> GetReleaseMarkdownHtmlAsync()
    {
        try
        {
            using HttpClient httpClient = new();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
            string jsonString =
                await httpClient.GetStringAsync(
                    "https://api.github.com/repos/babalae/better-genshin-impact/releases/latest");
            var jsonDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);

            if (jsonDict != null)
            {
                string? name = jsonDict["name"] as string;
                string? body = jsonDict["body"] as string;
                string md = $"# {name}{new string('\n', 2)}{body}";

                md = WebUtility.HtmlEncode(md);
                string md2html = File.ReadAllText(Global.Absolute(@"Assets\Strings\md2html.html"), Encoding.UTF8);
                var html = md2html.Replace("{{content}}", md);

                return html;
            }
        }
        catch (Exception e)
        {
            _logger.LogDebug(e, "获取 GitHub Release 更新说明失败");
        }

        return GetReleaseMarkdownHtmlFallback();
    }

    private string GetReleaseMarkdownHtmlFallback()
    {
        return
            """
            <!DOCTYPE html>
            <html lang="zh">
            <head>
                <meta charset="UTF-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>更新日志</title>
                <style>
                    body {
                        background-color: #212121;
                        color: white;
                        font-family: Arial, sans-serif;
                        display: flex;
                        justify-content: center;
                        align-items: center;
                        height: 100vh;
                        margin: 0;
                    }
                    .message {
                        text-align: center;
                        font-size: 20px;
                    }
                </style>
            </head>
            <body>
                <div class="message">
                    获取更新日志失败，请自行选择是否更新！
                </div>
            </body>
            </html>
            """;
    }
}
