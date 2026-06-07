using BetterInfinityNikki.Core.Config;
using BetterInfinityNikki.Service.Interface;
using BetterInfinityNikki.ViewModel;

namespace BetterInfinityNikki.ViewModel.Pages;

public partial class CommonSettingsPageViewModel : ViewModel
{
    public CommonSettingsPageViewModel(IConfigService configService)
    {
        Config = configService.Get();
    }

    public AllConfig Config { get; set; }
}
