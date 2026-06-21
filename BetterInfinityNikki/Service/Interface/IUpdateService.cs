using BetterInfinityNikki.Model;
using System.Threading.Tasks;

namespace BetterInfinityNikki.Service.Interface;

public interface IUpdateService
{
    /// <summary>
    /// 检查更新。若发现新版本，弹出更新窗口供用户选择。
    /// 主线程调用。
    /// </summary>
    Task CheckUpdateAsync(UpdateOption option);
}
