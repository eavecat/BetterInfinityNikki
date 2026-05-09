using BetterInfinityNikki.View;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;
using Wpf.Ui;

namespace BetterInfinityNikki.Service;

/// <summary>
/// 应用程序宿主服务
/// </summary>
public class ApplicationHostService(IServiceProvider serviceProvider) : IHostedService
{
    private INavigationWindow? _navigationWindow;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await HandleActivationAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }

    private async Task HandleActivationAsync()
    {
        if (System.Windows.Application.Current.Windows.OfType<MainWindow>().Any())
        {
            return;
        }

        _navigationWindow = (serviceProvider.GetService(typeof(INavigationWindow)) as INavigationWindow)!;
        _navigationWindow!.ShowWindow();

        // 导航到主页
        _ = _navigationWindow.Navigate(typeof(View.Pages.HomePage));

        await Task.CompletedTask;
    }
}
