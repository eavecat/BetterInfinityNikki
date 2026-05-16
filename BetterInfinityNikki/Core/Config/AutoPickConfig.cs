using BetterInfinityNikki.GameTask.AutoPick;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterInfinityNikki.Core.Config;

/// <summary>
/// 自动拾取配置
/// </summary>
public partial class AutoPickConfig : ObservableObject
{
    /// <summary>
    /// 触发器是否启用
    /// </summary>
    [ObservableProperty]
    private bool _enabled = true;

    /// <summary>
    /// 1080p下拾取文字左边的起始偏移
    /// </summary>
    [ObservableProperty] 
    private int _itemIconLeftOffset = 60;

    /// <summary>
    /// 1080p下拾取文字的起始偏移
    /// </summary>
    [ObservableProperty] 
    private int _itemTextLeftOffset = 115;

    /// <summary>
    /// 1080p下拾取文字的终止偏移
    /// </summary>
    [ObservableProperty] 
    private int _itemTextRightOffset = 400;

    /// <summary>
    /// 文字识别引擎
    /// - Paddle
    /// - Yap
    /// </summary>
    [ObservableProperty]
    private string _ocrEngine = nameof(PickOcrEngineEnum.Paddle);

    /// <summary>
    /// 自定义按键拾取
    /// </summary>
    [ObservableProperty] 
    private string _pickKey = "F";

    /// <summary>
    /// 黑名单启用状态
    /// </summary>
    [ObservableProperty]
    private bool _blackListEnabled = true;

    /// <summary>
    /// 白名单启用状态
    /// </summary>
    [ObservableProperty]
    private bool _whiteListEnabled = false;

    /// <summary>
    /// 芳间巡游启用状态（范围采集）
    /// </summary>
    [ObservableProperty]
    private bool _FangJianXunYouEnabled = false;
}
