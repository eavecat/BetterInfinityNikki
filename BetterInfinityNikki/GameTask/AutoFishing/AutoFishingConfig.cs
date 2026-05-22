using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterInfinityNikki.GameTask.AutoFishing;

/// <summary>
/// 自动钓鱼配置
/// </summary>
public partial class AutoFishingConfig : ObservableObject
{
    /// <summary>
    /// 触发器是否启用
    /// 启用后：
    /// 1. 自动判断是否进入钓鱼状态
    /// 2. 自动提杆
    /// 3. 自动拉条
    /// </summary>
    [ObservableProperty] 
    private bool _enabled = false;

    /// <summary>
    /// 鱼儿上钩文字识别区域
    /// 暂时无用
    /// </summary>
    //[ObservableProperty] 
    //private Rect _fishHookedRecognitionArea = Rect.Empty;
}
