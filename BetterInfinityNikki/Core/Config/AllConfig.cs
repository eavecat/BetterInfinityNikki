using System.Text.Json.Serialization;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using Fischless.GameCapture;

namespace BetterInfinityNikki.Core.Config;

/// <summary>
/// 所有配置的根类
/// </summary>
[Serializable]
public partial class AllConfig : ObservableObject
{
    /// <summary>
    /// 通用配置
    /// </summary>
    public CommonConfig CommonConfig { get; set; } = new();

    /// <summary>
    ///     原神启动配置
    /// </summary>
    public GameStartConfig GameStartConfig { get; set; } = new();

    /// <summary>
    /// 原神按键绑定配置
    /// </summary>
    public KeyBindingsConfig KeyBindingsConfig { get; set; } = new();

    /// <summary>
    /// 其他配置
    /// </summary>
    public OtherConfig OtherConfig { get; set; } = new();
    
    
    /// <summary>
    /// 硬件加速设置
    /// </summary>
    public HardwareAccelerationConfig HardwareAccelerationConfig { get; set; } = new();
    
    /// <summary>
    /// 窗口捕获的方式
    /// </summary>
    [ObservableProperty]
    private string _captureMode = CaptureModes.WindowsGraphicsCapture.ToString();
    
    /// <summary>
    /// 触发器触发频率(ms)
    /// </summary>
    [ObservableProperty]
    private int _triggerInterval = 50;

    [JsonIgnore]
    public Action? OnAnyChangedAction { get; set; }

    public void InitEvent()
    {
        PropertyChanged += OnAnyPropertyChanged;
        CommonConfig.PropertyChanged += OnAnyPropertyChanged;
        GameStartConfig.PropertyChanged += OnAnyPropertyChanged;
        KeyBindingsConfig.PropertyChanged += OnAnyPropertyChanged;
        OtherConfig.PropertyChanged += OnAnyPropertyChanged;
        HardwareAccelerationConfig.PropertyChanged += OnAnyPropertyChanged;
    }

    public void OnAnyPropertyChanged(object? sender, EventArgs args)
    {
        OnAnyChangedAction?.Invoke();
    }
}

/// <summary>
/// 通用配置
/// </summary>
public partial class CommonConfig : ObservableObject
{
    /// <summary>
    /// 是否首次运行
    /// </summary>
    [ObservableProperty]
    private bool _isFirstRun = true;

    /// <summary>
    /// 退出到托盘
    /// </summary>
    [ObservableProperty]
    private bool _exitToTray = false;

    /// <summary>
    /// 当前主题类型
    /// </summary>
    [ObservableProperty]
    private ThemeType _currentThemeType = ThemeType.DarkAcrylic;

    /// <summary>
    /// 曾经运行过的设备ID列表
    /// </summary>
    public List<string> OnceHadRunDeviceIdList { get; set; } = [];

    /// <summary>
    /// 运行过的版本
    /// </summary>
    public string? RunForVersion { get; set; }
}

/// <summary>
/// 主题类型枚举
/// </summary>
public enum ThemeType
{
    DarkNone,
    DarkMica,
    DarkAcrylic,
    LightNone,
    LightMica,
    LightAcrylic
}
