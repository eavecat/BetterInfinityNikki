using System;

namespace BetterInfinityNikki.View;

/// <summary>
/// 遮罩窗口占位类
/// TODO: 后续根据需求实现完整的遮罩窗口功能
/// </summary>
public class MaskWindow
{
    private static MaskWindow? _instance;

    public static MaskWindow Instance()
    {
        if (_instance == null)
        {
            // 返回一个空实例，避免抛出异常
            _instance = new MaskWindow();
        }

        return _instance;
    }

    /// <summary>
    /// 刷新窗口显示（占位方法）
    /// </summary>
    public void Refresh()
    {
        // TODO: 实现实际的刷新逻辑
        // 目前仅作为占位，避免编译错误
    }

    /// <summary>
    /// 显示窗口
    /// </summary>
    public void Show()
    {
        // TODO: 实现显示逻辑
    }

    /// <summary>
    /// 隐藏窗口
    /// </summary>
    public void Hide()
    {
        // TODO: 实现隐藏逻辑
    }
}
