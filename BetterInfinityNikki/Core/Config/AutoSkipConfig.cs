using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterInfinityNikki.Core.Config;

/// <summary>
/// 自动剧情配置
/// </summary>
public partial class AutoSkipConfig : ObservableObject
{
    /// <summary>
    /// 触发器是否启用
    /// </summary>
    [ObservableProperty]
    private bool _enabled = true;

    /// <summary>
    /// 自动点击选项
    /// </summary>
    [ObservableProperty]
    private bool _clickOptionEnabled = true;
}
