using BetterInfinityNikki.Core.Recognition.OpenCv;
using BetterInfinityNikki.GameTask.AutoPick;
using BetterInfinityNikki.GameTask.AutoSkip;
using BetterInfinityNikki.GameTask.GameLoading;
using BetterInfinityNikki.GameTask.GameLoading.Assets;
using BetterInfinityNikki.GameTask.Model;
using OpenCvSharp;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BetterInfinityNikki.Core.Config;
using BetterInfinityNikki.GameTask.AutoPick.Assets;
using BetterInfinityNikki.GameTask.AutoSkip.Assets;
using BetterInfinityNikki.GameTask.InsectCatching;
using BetterInfinityNikki.GameTask.InsectCatching.Assets;
using BetterInfinityNikki.View.Drawable;

namespace BetterInfinityNikki.GameTask;

/// <summary>
/// 游戏任务管理器
/// </summary>
internal class GameTaskManager
{
    public static ConcurrentDictionary<string, ITaskTrigger>? TriggerDictionary { get; set; }

    /// <summary>
    /// 一定要在任务上下文初始化完毕后使用
    /// </summary>
    /// <returns></returns>
    public static List<ITaskTrigger> LoadInitialTriggers()
    {
        ReloadAssets();
        TriggerDictionary = new ConcurrentDictionary<string, ITaskTrigger>();

        // 添加自动进入游戏触发器
        TriggerDictionary.TryAdd("GameLoading", new GameLoadingTrigger());

        // 添加自动拾取触发器
        TriggerDictionary.TryAdd("AutoPick", new AutoPickTrigger());

        // 添加自动剧情触发器
        TriggerDictionary.TryAdd("AutoSkip", new AutoSkipTrigger());

        // 添加璨花捕影触发器
        TriggerDictionary.TryAdd("InsectCatching", new InsectCatchingTrigger());

        return ConvertToTriggerList();
    }

    public static List<ITaskTrigger> ConvertToTriggerList(bool allEnabled = false)
    {
        if (TriggerDictionary is null)
        {
            return [];
        }

        var loadedTriggers = TriggerDictionary.Values.ToList();

        loadedTriggers.ForEach(i => i.Init());
        if (allEnabled)
        {
            loadedTriggers.ForEach(i => i.IsEnabled = true);
        }

        loadedTriggers = [.. loadedTriggers.OrderByDescending(i => i.Priority)];
        return loadedTriggers;
    }

    public static void ClearTriggers()
    {
        TriggerDictionary?.Clear();
    }

    /// <summary>
    /// 通过名称添加触发器
    /// </summary>
    /// <param name="name"></param>
    /// <param name="externalConfig"></param>
    public static bool AddTrigger(string name, object? externalConfig)
    {
        TriggerDictionary ??= new ConcurrentDictionary<string, ITaskTrigger>();

        ITaskTrigger? trigger = null;
        string? triggerName = null;
        switch (name)
        {
            case "GameLoading":
                triggerName = "GameLoading";
                trigger = new GameLoadingTrigger();
                break;
            case "AutoPick":
                triggerName = "AutoPick";
                trigger = new AutoPickTrigger();
                break;
            case "AutoSkip":
                triggerName = "AutoSkip";
                trigger = new AutoSkipTrigger();
                break;
            case "InsectCatching":
                triggerName = "InsectCatching";
                trigger = new InsectCatchingTrigger();
                break;
        }

        if (triggerName == null || trigger == null)
        {
            return false;
        }
        TriggerDictionary[triggerName] = trigger;
        return true;
    }

    public static void RefreshTriggerConfigs()
    {
        if (TriggerDictionary is { Count: > 0 })
        {
            TriggerDictionary.GetValueOrDefault("GameLoading")?.Init();
            TriggerDictionary.GetValueOrDefault("AutoPick")?.Init();
            TriggerDictionary.GetValueOrDefault("AutoSkip")?.Init();
            TriggerDictionary.GetValueOrDefault("InsectCatching")?.Init();
            // 清理画布
            VisionContext.Instance().DrawContent.ClearAll();
        }

        ReloadAssets();
    }

    public static void ReloadAssets()
    {
        GameLoadingAssets.DestroyInstance();
        AutoPickAssets.DestroyInstance();
        AutoSkipAssets.DestroyInstance();
        InsectCatchingAssets.DestroyInstance();
    }

    /// <summary>
    /// 获取素材图片并缩放
    /// </summary>
    /// <param name="featName">任务名称</param>
    /// <param name="assertName">素材文件名</param>
    /// <param name="flags"></param>
    /// <returns></returns>
    /// <exception cref="FileNotFoundException"></exception>
    public static Mat LoadAssetImage(string featName, string assertName, ImreadModes flags = ImreadModes.Color)
    {
        return LoadAssetImage(featName, assertName, TaskContext.Instance().SystemInfo, flags);
    }

    /// <summary>
    /// 获取素材图片并缩放
    /// </summary>
    /// <returns></returns>
    /// <exception cref="FileNotFoundException"></exception>
    public static Mat LoadAssetImage(string featName, string assertName, ISystemInfo systemInfo, ImreadModes flags = ImreadModes.Color)
    {
        var assetsFolder = Global.Absolute($@"GameTask\{featName}\Assets\{systemInfo.GameScreenSize.Width}x{systemInfo.GameScreenSize.Height}");
        if (!Directory.Exists(assetsFolder))
        {
            assetsFolder = Global.Absolute($@"GameTask\{featName}\Assets\1920x1080");
        }

        if (!Directory.Exists(assetsFolder))
        {
            throw new FileNotFoundException($"未找到{featName}的素材文件夹");
        }

        var filePath = Path.Combine(assetsFolder, assertName);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"未找到{featName}中的{assertName}文件");
        }

        var mat = Mat.FromStream(File.OpenRead(filePath), flags);
        if (systemInfo.GameScreenSize.Width != 1920)
        {
            mat = ResizeHelper.Resize(mat, systemInfo.AssetScale);
        }

        return mat;
    }
}
