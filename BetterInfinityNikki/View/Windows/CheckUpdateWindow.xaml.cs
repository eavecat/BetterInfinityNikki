using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using BetterInfinityNikki.Core.Config;
using BetterInfinityNikki.Helpers.Ui;
using BetterInfinityNikki.Model;
using BetterInfinityNikki.Service.Model;
using Wpf.Ui.Controls;
using Wpf.Ui.Violeta.Controls;
using MessageBoxResult = System.Windows.MessageBoxResult;

namespace BetterInfinityNikki.View.Windows;

[ObservableObject]
public partial class CheckUpdateWindow : FluentWindow
{
    public Func<object, CheckUpdateWindowButton, Task>? UserInteraction = null!;

    [ObservableProperty] private bool showOtherUpdateTip = false;

    [ObservableProperty] private string selectedGitSource = "CNB";

    public string GitSourceDescription => SelectedGitSource == "CNB" ? "【国内】直接从 CNB 下载并更新" : "【国外】直接从 Github 下载并更新";

    partial void OnSelectedGitSourceChanged(string value)
    {
        OnPropertyChanged(nameof(GitSourceDescription));
    }

    private readonly UpdateOption _option;
    private readonly CheckResponseData _latest;

    public CheckUpdateWindow(UpdateOption option, CheckResponseData latest)
    {
        _option = option ?? throw new ArgumentNullException(nameof(option));
        _latest = latest ?? throw new ArgumentNullException(nameof(latest));
        DataContext = this;
        InitializeComponent();
        SourceInitialized += (s, e) => WindowHelper.TryApplySystemBackdrop(this);
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_latest.Force)
        {
            IgnoreButton.Visibility = Visibility.Collapsed;
        }

        // 延迟显示气泡提示
        var showTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(0.8)
        };
        showTimer.Tick += (s, e) =>
        {
            showTimer.Stop();
            ShowOtherUpdateTip = true;

            // 5秒后自动消失
            var hideTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(6)
            };
            hideTimer.Tick += (s2, e2) =>
            {
                hideTimer.Stop();
                ShowOtherUpdateTip = false;
            };
            hideTimer.Start();
        };
        showTimer.Start();
    }

    [RelayCommand]
    private async Task UpdateFromGitHostPlatformAsync()
    {
        string source = SelectedGitSource == "CNB" ? "cnb" : "github";

        if (source == "github")
        {
            var result = await ThemedMessageBox.ShowAsync("您已选择「Github」作为更新源。\n请确认：您当前网络可正常访问 Github 文件服务？\n若不确定能否访问，建议切换至其他更新渠道。\n是否继续使用 Github 渠道更新？",
                "警告", System.Windows.MessageBoxButton.OKCancel, ThemedMessageBox.MessageBoxIcon.Warning);
            if (result != MessageBoxResult.OK)
            {
                return;
            }
        }

        await RunUpdaterAsync($"-I --source {source}");
    }

    [RelayCommand]
    private async Task UpdateFromLocalServiceAsync()
    {
        await RunUpdaterAsync("-I --source local");
    }

    [RelayCommand]
    private async Task OtherUpdateAsync()
    {
        if (UserInteraction != null)
        {
            await UserInteraction.Invoke(this, CheckUpdateWindowButton.OtherUpdate);
        }
    }

    [RelayCommand]
    private async Task IgnoreAsync()
    {
        if (UserInteraction != null)
        {
            await UserInteraction.Invoke(this, CheckUpdateWindowButton.Ignore);
        }
        Close();
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        if (UserInteraction != null)
        {
            await UserInteraction.Invoke(this, CheckUpdateWindowButton.Cancel);
        }
        Close();
    }

    private async Task RunUpdaterAsync(string parameters)
    {
        string updaterExePath = Global.Absolute("BetterIN.update.exe");
        if (!File.Exists(updaterExePath))
        {
            await ThemedMessageBox.ErrorAsync("更新程序不存在，请选择其他更新方式！");
            return;
        }

        Process.Start(updaterExePath, parameters);
        Application.Current.Shutdown();
    }

    private void OnCloseTipClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        ShowOtherUpdateTip = false;
    }

    public void NavigateToHtml(string html)
    {
        WebpagePanel?.NavigateToHtml(html);
    }

    public void NavigateToMd(string md)
    {
        WebpagePanel?.NavigateToMd(md);
    }

    public enum CheckUpdateWindowButton
    {
        OtherUpdate,
        Ignore,
        Cancel,
    }
}
