using DailyRoutines.Common.Runtime.Hosts;
using Newtonsoft.Json;
using OmenTools.Dalamud;
using OmenTools.OmenService;

namespace DailyRoutines.Common.Manager.Abstractions;

public abstract class ManagerBase<T> : ManagerBase where T : ManagerBase<T>
{
    public static T Instance() =>
        ManagerHost.Current.Get<T>() is { IsInitialized: true, IsDisposed: false } manager
            ? manager
            : throw new InvalidOperationException($"管理器 {typeof(T).Name} 尚未注册或初始化");
}

public abstract class ManagerBase
{
    public string ConfigFilePath
    {
        get
        {
            var directory = Path.Join(DService.Instance().PI.GetPluginConfigDirectory(), "Manager");
            Directory.CreateDirectory(directory);
            return Path.Join(directory, $"{GetType().Name}.json");
        }
    }
    
    public async Task PublicInitAsync()
    {
        if (IsInitialized || IsDisposed) 
            return;
        
        try
        {
            await Init();
            
            IsInitialized = true;
        }
        catch (Exception ex)
        {
            DLog.Error($"在初始化管理器时发生错误: {GetType().Name}", ex);
            await PublicUninitAsync();
        }
    }

    public async Task PublicUninitAsync()
    {
        if (IsDisposed)
            return;
        
        try
        {
            await Uninit();
        }
        catch (Exception ex)
        {
            DLog.Error($"在卸载管理器时发生错误: {GetType().Name}", ex);
        }
        finally
        {
            IsDisposed = true;
        }
    }

    public async Task PublicPostInitAsync()
    {
        if (IsDisposed)
            return;

        try
        {
            await PostInit();
        }
        catch (Exception ex)
        {
            DLog.Error($"在管理器后初始化时发生错误: {GetType().Name}", ex);
        }
    }
    
    #region 生命周期

    public bool IsInitialized { get; private set; }
    
    public bool IsDisposed { get; private set; }

    #endregion

    #region 继承

    protected virtual Task Init() => Task.CompletedTask;

    protected virtual Task PostInit() => Task.CompletedTask;

    protected virtual Task Uninit() => Task.CompletedTask;

    #endregion

    #region 配置

    internal T? LoadConfig<T>() where T : ManagerConfig
    {
        try
        {
            if (!File.Exists(ConfigFilePath)) return null;
            
            var jsonString = File.ReadAllText(ConfigFilePath);
            return JsonConvert.DeserializeObject<T>(jsonString, JsonSerializerSettings.GetShared());
        }
        catch (Exception ex)
        {
            DLog.Error($"为管理器加载配置失败: {GetType().Name}", ex);
            return null;
        }
    }

    internal void SaveConfig<T>(T config) where T : ManagerConfig
    {
        try
        {
            ArgumentNullException.ThrowIfNull(config);
            
            var jsonString = JsonConvert.SerializeObject(config, Formatting.Indented, JsonSerializerSettings.GetShared());
            SecureSaveHelper.Instance().WriteAllText(ConfigFilePath, jsonString);
        }
        catch (Exception ex)
        {
            DLog.Error($"为管理器保存配置失败: {GetType().Name}", ex);
        }
    }

    #endregion
}
