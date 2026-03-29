using System;
using System.IO;
using Newtonsoft.Json;
using OmenTools.Dalamud;
using OmenTools.OmenService;

namespace DailyRoutines.Common.Manager.Abstractions;

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
    
    public void PublicInit()
    {
        if (IsInitialized || IsDisposed) 
            return;
        
        try
        {
            Init();
            
            IsInitialized = true;
        }
        catch (Exception ex)
        {
            DLog.Error($"在初始化管理器时发生错误: {GetType().Name}", ex);
            PublicUninit();
        }
    }

    public void PublicUninit()
    {
        if (IsDisposed)
            return;
        
        try
        {
            Uninit();
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
    
    #region 生命周期

    public bool IsInitialized { get; private set; }
    
    public bool IsDisposed { get; private set; }

    #endregion

    #region 继承

    protected virtual void Init() { }

    protected virtual void Uninit() { }

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
