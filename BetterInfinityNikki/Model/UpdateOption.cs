namespace BetterInfinityNikki.Model;

public sealed class UpdateOption
{
    /// <summary>
    /// 触发方式：启动自动检查 / 用户手动点击
    /// </summary>
    public UpdateTrigger Trigger { get; set; } = UpdateTrigger.Auto;

    /// <summary>
    /// 更新通道：稳定 / 测试
    /// </summary>
    public UpdateChannel Channel { get; set; } = UpdateChannel.Stable;
}

public enum UpdateTrigger
{
    Auto,
    Manual,
}

public enum UpdateChannel
{
    Stable,
    Alpha,
}
