using BetterInfinityNikki.Core.Config;
using BetterInfinityNikki.GameTask;
using BetterInfinityNikki.GameTask.AutoPick;
using BetterInfinityNikki.Service.Interface;
using BetterInfinityNikki.View.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using TextBox = Wpf.Ui.Controls.TextBox;

namespace BetterInfinityNikki.ViewModel.Pages;

public partial class TriggerSettingsPageViewModel : ViewModel
{
    [ObservableProperty] private string[] _pickOcrEngineNames = [PickOcrEngineEnum.Paddle.ToString(), PickOcrEngineEnum.Yap.ToString()];

    [ObservableProperty] private bool _isUpdatingMapPointCache;

    public bool CanUpdateMapPointCache => !IsUpdatingMapPointCache;

    public AllConfig Config { get; set; }

    private readonly IMaskMapPointService _mapPointService;

    public TriggerSettingsPageViewModel(IConfigService configService, IMaskMapPointService mapPointService)
    {
        Config = configService.Get();
        _mapPointService = mapPointService;
        
        // 监听璨花捕影和芳间巡游的互斥变化
        Config.AutoPickConfig.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(Config.AutoPickConfig.CanHuaBuYingEnabled))
            {
                // 如果开启了璨花捕影，关闭芳间巡游
                if (Config.AutoPickConfig.CanHuaBuYingEnabled && Config.AutoPickConfig.FangJianXunYouEnabled)
                {
                    Config.AutoPickConfig.FangJianXunYouEnabled = false;
                }
            }
            else if (e.PropertyName == nameof(Config.AutoPickConfig.FangJianXunYouEnabled))
            {
                // 如果开启了芳间巡游，关闭璨花捕影
                if (Config.AutoPickConfig.FangJianXunYouEnabled && Config.AutoPickConfig.CanHuaBuYingEnabled)
                {
                    Config.AutoPickConfig.CanHuaBuYingEnabled = false;
                }
            }
        };
    }

    partial void OnIsUpdatingMapPointCacheChanged(bool value)
    {
        OnPropertyChanged(nameof(CanUpdateMapPointCache));
    }

    [RelayCommand]
    private async Task RefreshMapPointCacheAsync()
    {
        IsUpdatingMapPointCache = true;
        try
        {
            await _mapPointService.UpdateCacheAsync();
            await ThemedMessageBox.SuccessAsync("点位缓存数据已更新完成");
        }
        catch (Exception ex)
        {
            await ThemedMessageBox.ErrorAsync($"更新点位缓存失败：{ex.Message}");
        }
        finally
        {
            IsUpdatingMapPointCache = false;
        }
    }

    [RelayCommand]
    private void OnRefreshTriggers()
    {
        // 刷新触发器配置
        GameTaskManager.RefreshTriggerConfigs();
    }

    [RelayCommand]
    private void OnEditBlacklist()
    {
        // 读取精确匹配黑名单
        var exactPath = @"User\pick_black_lists.txt";
        var exactText = Global.ReadAllTextIfExist(exactPath) ?? string.Empty;

        // 读取模糊匹配黑名单
        var fuzzyPath = @"User\pick_fuzzy_black_lists.txt";
        var fuzzyText = Global.ReadAllTextIfExist(fuzzyPath) ?? string.Empty;

        // 创建精确匹配黑名单输入框
        var exactTextBox = new TextBox
        {
            Height = 150,
            Width = 400,
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = true,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            PlaceholderText = "每行一条记录",
            Text = exactText
        };

        var fuzzyTextBox = new TextBox
        {
            Height = 150,
            Width = 400,
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = true,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            PlaceholderText = "每行一条记录",
            Text = fuzzyText
        };

        var stackPanel = new StackPanel();

        var exactLabel = new Wpf.Ui.Controls.TextBlock
        {
            Text = "精确匹配黑名单：",
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 5)
        };

        var fuzzyLabel = new Wpf.Ui.Controls.TextBlock
        {
            Text = "模糊匹配黑名单：",
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 10, 0, 5)
        };

        stackPanel.Children.Add(exactLabel);
        stackPanel.Children.Add(exactTextBox);
        stackPanel.Children.Add(fuzzyLabel);
        stackPanel.Children.Add(fuzzyTextBox);

        var p = new PromptDialog(
            "黑名单配置\n" +
            "每行一条记录。\n" +
            "示例：\n" +
            "精致的宝箱\n" +
            "史莱姆凝液\n" +
            "牢固的箭簇",
            "黑名单配置",
            stackPanel,
            null);
        p.Height = 600;
        p.Width = 500;
        p.ShowDialog();

        if (p.DialogResult == true)
        {
            Global.WriteAllText(exactPath, exactTextBox.Text);
            Global.WriteAllText(fuzzyPath, fuzzyTextBox.Text);
            GameTaskManager.RefreshTriggerConfigs();
        }
    }

    [RelayCommand]
    private void OnEditWhitelist()
    {
        var path = @"User\pick_white_lists.txt";
        var text = Global.ReadAllTextIfExist(path);
        if (string.IsNullOrEmpty(text))
        {
            text = "";
        }

        var multilineTextBox = new TextBox
        {
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = true,
            VerticalAlignment = VerticalAlignment.Stretch,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            PlaceholderText = "请在此输入白名单配置，每行一条记录。\n" +
                              "示例：\n" +
                              "调查\n" +
                              "合成\n" +
                              "启动"
        };
        var p = new PromptDialog(
            "白名单配置，每行一条记录\n" +
            "示例：\n" +
            "调查\n" +
            "合成\n" +
            "启动",
            "白名单配置",
            multilineTextBox,
            text);
        p.Height = 500;
        p.ShowDialog();
        if (p.DialogResult == true)
        {
            File.WriteAllText(Global.Absolute(path), multilineTextBox.Text);
            GameTaskManager.RefreshTriggerConfigs();
        }
    }
}
