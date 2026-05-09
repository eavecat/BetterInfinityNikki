using BetterInfinityNikki.ViewModel.Pages;
using System.Windows.Controls;

namespace BetterInfinityNikki.View.Pages;

public partial class HomePage : Page
{
    public HomePageViewModel ViewModel { get; }

    public HomePage(HomePageViewModel viewModel)
    {
        DataContext = ViewModel = viewModel;
        InitializeComponent();
    }
}
