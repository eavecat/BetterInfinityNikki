using System.Windows;
using Wpf.Ui;

namespace BetterInfinityNikki.ViewModel;

public partial class NotifyIconViewModel : ObservableObject
{
    [RelayCommand]
    public void Exit()
    {
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
            }
        }
    }
}
