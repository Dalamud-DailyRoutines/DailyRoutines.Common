using System;
using System.IO;
using Newtonsoft.Json;
using OmenTools.Dalamud;
using OmenTools.OmenService;
using OmenTools.Threading;

namespace DailyRoutines.Common.DataUploader.Abstractions;

public abstract class DataUploaderBase
{
    public string ConfigFilePath
    {
        get
        {
            var directory = Path.Join(DService.Instance().PI.GetPluginConfigDirectory(), "DataUploader");
            Directory.CreateDirectory(directory);
            return Path.Join(directory, $"{GetType().Name}.json");
        }
    }
    
    public void PublicInit()
    {
        if (IsDisposed || IsInitialized)
            return;

        try
        {
            Init();
        }
        catch (Exception ex)
        {
            DLog.Error("初始化数据上传器失败", ex);
            PublicUninit();
        }
        finally
        {
            IsInitialized = true;
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
            DLog.Error("卸载数据上传器失败", ex);
        }
        finally
        {
            IsDisposed = true;
        }
    }

    #region 生命周期控制

    public bool IsDisposed { get; private set; }

    public bool IsInitialized { get; private set; }

    #endregion

    #region 继承
    
    protected virtual void Init() { }

    protected virtual void Uninit() { }

    #endregion

    #region 配置

    internal T? LoadConfig<T>() where T : DataUploaderConfig
    {
        try
        {
            if (!File.Exists(ConfigFilePath)) return null;

            var jsonString = File.ReadAllText(ConfigFilePath);
            return JsonConvert.DeserializeObject<T>(jsonString, JsonSerializerSettings.GetShared());
        }
        catch (Exception ex)
        {
            DLog.Error($"为数据上传器加载配置失败: {GetType().Name}", ex);
            return null;
        }
    }

    internal void SaveConfig<T>(T config) where T : DataUploaderConfig
    {
        try
        {
            ArgumentNullException.ThrowIfNull(config);
            
            var jsonString = JsonConvert.SerializeObject(config, Formatting.Indented, JsonSerializerSettings.GetShared());
            SecureSaveHelper.Instance().WriteAllText(ConfigFilePath, jsonString);
        }
        catch (Exception ex)
        {
            DLog.Error($"为数据上传器加载配置失败: {GetType().Name}", ex);
        }
    }

    #endregion
    
    protected static readonly Throttler<string> Throttler = new();
    
    protected static bool IsPlayerReady() =>
        GameState.IsLoggedIn                                &&
        DService.Instance().ObjectTable.LocalPlayer != null &&
        !DService.Instance().Condition.IsBoundByDuty;
}
