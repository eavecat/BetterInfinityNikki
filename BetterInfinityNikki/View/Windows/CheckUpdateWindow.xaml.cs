using System;
using System.Threading.Tasks;
using System.Windows;
using BetterInfinityNikki.Core.Config;
using BetterInfinityNikki.Helpers.Ui;
using BetterInfinityNikki.Model;
using BetterInfinityNikki.Service.Model;
using Wpf.Ui.Controls;

namespace BetterInfinityNikki.View.Windows;

public partial class CheckUpdateWindow : FluentWindow
{
    private readonly UpdateOption _option;
    private readonly CheckResponseData _latest;

    public Func<CheckUpdateAction, Task>? OnUserAction { get; set; }

    public CheckUpdateWindow(UpdateOption option, CheckResponseData latest)
    {
        _option = option ?? throw new ArgumentNullException(nameof(option));
        _latest = latest ?? throw new ArgumentNullException(nameof(latest));

        InitializeComponent();
        SourceInitialized += (s, e) => WindowHelper.TryApplySystemBackdrop(this);
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        VersionTitleText.Text = $"发现新版本：v{_latest.Version}";
        CurrentVersionText.Text = $"当前版本：v{Global.Version}";
        ReleaseNotesText.Text = string.IsNullOrWhiteSpace(_latest.ReleaseNotes)
            ? "（暂无更新说明）"
            : _latest.ReleaseNotes;

        if (_latest.Force)
        {
            IgnoreButton.Visibility = Visibility.Collapsed;
        }

        if (string.IsNullOrEmpty(_latest.DownloadPageUrl))
        {
            ManualDownloadButton.Visibility = Visibility.Collapsed;
        }
    }

    private async void UpdateButton_Click(object sender, RoutedEventArgs e)
    {
        DisableAllButtons();
        if (OnUserAction != null)
        {
            await OnUserAction.Invoke(CheckUpdateAction.Update);
        }
        Close();
    }

    private async void ManualDownloadButton_Click(object sender, RoutedEventArgs e)
    {
        if (OnUserAction != null)
        {
            await OnUserAction.Invoke(CheckUpdateAction.ManualDownload);
        }
        Close();
    }

    private async void IgnoreButton_Click(object sender, RoutedEventArgs e)
    {
        if (OnUserAction != null)
        {
            await OnUserAction.Invoke(CheckUpdateAction.Ignore);
        }
        Close();
    }

    private async void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        if (OnUserAction != null)
        {
            await OnUserAction.Invoke(CheckUpdateAction.Cancel);
        }
        Close();
    }

    private void DisableAllButtons()
    {
        UpdateButton.IsEnabled = false;
        ManualDownloadButton.IsEnabled = false;
        IgnoreButton.IsEnabled = false;
    }
}

public enum CheckUpdateAction
{
    Update,
    ManualDownload,
    Ignore,
    Cancel,
}
