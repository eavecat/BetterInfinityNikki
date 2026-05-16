using BetterInfinityNikki.ViewModel.Pages;

namespace BetterInfinityNikki.View.Pages;

public partial class TriggerSettingsPage
{
    public TriggerSettingsPageViewModel ViewModel { get; }

    public TriggerSettingsPage(TriggerSettingsPageViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = ViewModel;  // 修复：设置为 ViewModel 而不是 this
        InitializeComponent();
    }
}
