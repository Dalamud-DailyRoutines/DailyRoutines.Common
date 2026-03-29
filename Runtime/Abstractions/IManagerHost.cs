using DailyRoutines.Common.Module.Abstractions;
using Dalamud.Interface.Windowing;

namespace DailyRoutines.Common.Runtime.Abstractions;

public interface IManagerHost
{
    Task LoadAsync(ModuleBase module, bool affectConfig);

    Task UnloadAsync(ModuleBase module, bool affectConfig, bool forceUnload);

    bool AddWindow(Window window);

    bool RemoveWindow(Window window);

    string GetLoc(string key, params object[] args);
}
