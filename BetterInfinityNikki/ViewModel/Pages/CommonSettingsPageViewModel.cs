using System.Threading.Tasks;
using BetterInfinityNikki.Core.Config;
using BetterInfinityNikki.Model;
using BetterInfinityNikki.Service.Interface;
using BetterInfinityNikki.View.Windows;
using BetterInfinityNikki.ViewModel;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxResult = System.Windows.MessageBoxResult;

namespace BetterInfinityNikki.ViewModel.Pages;

public partial class CommonSettingsPageViewModel : ViewModel
{
    private readonly IUpdateService _updateService;

    public CommonSettingsPageViewModel(IConfigService configService, IUpdateService updateService)
    {
        _updateService = updateService;
        Config = configService.Get();
    }

    public AllConfig Config { get; set; }

    [RelayCommand]
    private async Task CheckUpdateAsync()
    {
        await _updateService.CheckUpdateAsync(new UpdateOption
        {
            Trigger = UpdateTrigger.Manual,
            Channel = UpdateChannel.Stable
        });
    }

    [RelayCommand]
    private async Task CheckUpdateAlphaAsync()
    {
        var result = await ThemedMessageBox.ShowAsync(
            "测试版本非常不稳定！\n测试版本非常不稳定！\n测试版本非常不稳定！\n\n是否继续检查更新？",
            "警告",
            System.Windows.MessageBoxButton.YesNo,
            ThemedMessageBox.MessageBoxIcon.Warning);

        if (result != System.Windows.MessageBoxResult.Yes)
        {
            return;
        }

        await _updateService.CheckUpdateAsync(new UpdateOption
        {
            Trigger = UpdateTrigger.Manual,
            Channel = UpdateChannel.Alpha
        });
    }
}
