using System.Threading.Tasks;
using System.Windows;
using BetterInfinityNikki.Model;
using BetterInfinityNikki.Service.Interface;

namespace BetterInfinityNikki.ViewModel;

public partial class NotifyIconViewModel : ObservableObject
{
    [RelayCommand]
    public void Exit()
    {
        App.GetService<IConfigService>()?.Save();
        Application.Current.Shutdown();
    }

    [RelayCommand]
    public void ShowOrHide()
    {
        var mainWindow = Application.Current.MainWindow;
        if (mainWindow != null)
        {
            if (mainWindow.Visibility == Visibility.Visible)
            {
                mainWindow.Hide();
            }
            else
            {
                mainWindow.Show();
                mainWindow.Activate();
                mainWindow.Focus();
            }
        }
    }

    [RelayCommand]
    public async Task CheckUpdateAsync()
    {
        var updateService = App.GetService<IUpdateService>();
        if (updateService != null)
        {
            await updateService.CheckUpdateAsync(new UpdateOption
            {
                Trigger = UpdateTrigger.Manual
            });
        }
    }
}
