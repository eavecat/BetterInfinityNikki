using BetterInfinityNikki.Core.Config;

namespace BetterInfinityNikki.Service.Interface;

public interface IConfigService
{
    AllConfig Get();
    void Save();
}
