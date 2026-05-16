using BetterInfinityNikki.Helpers.Ui;
using BetterInfinityNikki.ViewModel;
using BetterInfinityNikki.Service.Interface;
using System;
using System.Windows;
using System.Threading.Tasks;
using BetterInfinityNikki.Helpers.DpiAwareness;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Controls;
using Wpf.Ui.Tray.Controls;

namespace BetterInfinityNikki.View;

public partial class MainWindow : FluentWindow, INavigationWindow
{
    private readonly ILogger<MainWindow> _logger = App.GetLogger<MainWindow>();

    public MainWindowViewModel ViewModel { get; }

    public MainWindow(MainWindowViewModel viewModel, INavigationService navigationService, ISnackbarService snackbarService)
    {
        _logger.LogDebug("主窗体实例化");
        DataContext = ViewModel = viewModel;

        InitializeComponent();
        this.InitializeDpiAwareness();

        snackbarService.SetSnackbarPresenter(SnackbarPresenter);
        navigationService.SetNavigationControl(RootNavigation);

        Application.Current.MainWindow = this;

        Loaded += OnMainWindowLoaded;
    }

    private void OnMainWindowLoaded(object sender, RoutedEventArgs e)
    {
        Activate();
        
        // 不再在这里初始化遮罩窗口
        // 遮罩窗口会在游戏启动并初始化 TaskContext 后由 HomePageViewModel 创建
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        WindowHelper.TryApplySystemBackdrop(this);
    }

    protected override void OnClosed(EventArgs e)
    {
        _logger.LogDebug("主窗体退出");
        base.OnClosed(e);
        App.GetService<NotifyIconViewModel>()?.Exit();
    }

    private void OnNotifyIconLeftDoubleClick(NotifyIcon sender, RoutedEventArgs e)
    {
        App.GetService<NotifyIconViewModel>()?.ShowOrHide();
    }

    public INavigationView GetNavigation() => RootNavigation;

    public bool Navigate(Type pageType) => RootNavigation.Navigate(pageType);

    public void SetServiceProvider(IServiceProvider serviceProvider)
    {
        throw new NotImplementedException();
    }

    public void SetPageService(INavigationViewPageProvider navigationViewPageProvider) =>
        RootNavigation.SetPageProviderService(navigationViewPageProvider);

    public void ShowWindow() => Show();

    public void CloseWindow() => Close();
}
