using System.Numerics;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.BaseTypes;
using OmenTools.Interop.Game.Helpers;

namespace DailyRoutines.Common.KamiToolKit.Addons;

public abstract unsafe class AttachedAddon : NativeAddon
{
    protected virtual AttachedAddonPosition AttachPosition =>
        AttachedAddonPosition.LeftTop;

    protected virtual Vector2 PositionOffset =>
        Vector2.Zero;

    protected virtual bool CanOpenAddon =>
        true;

    // 宿主被隐藏（IsVisible=false）时仍保持附着与定位。
    // 适用于隐藏宿主的替代聊天框场景（如 ChatTwo 隐藏原生 ChatLog 后快捷面板仍需显示）。
    protected virtual bool IgnoreHostVisibility =>
        false;

    // 自定义附着位置提供器；返回非 null 时取代 AttachPosition 的宿主相对计算。
    // 参数为当前面板 addon 指针，返回值直接作为窗口位置（仍叠加 PositionOffset）。
    protected virtual Vector2? CalculateCustomPosition(AtkUnitBase* addon) =>
        null;

    protected AtkUnitBase* HostAddon =>
        AddonHelper.GetByName(hostAddonName);

    private readonly string hostAddonName;
    private readonly bool   runSetupForCurrentHostAddon;

    private bool isClosingAddonOnly;

    // IgnoreHostVisibility 时只要求节点就绪（IsFullyLoaded），不再要求 IsVisible
    private bool IsHostReady() =>
        IgnoreHostVisibility
            ? HostAddon != null && HostAddon->IsFullyLoaded()
            : HostAddon->IsAddonAndNodesReady();

    protected AttachedAddon(string hostAddon, params AddonEvent[] hostAddonEvents)
    {
        hostAddonName               = hostAddon;
        runSetupForCurrentHostAddon = hostAddonEvents.Contains(AddonEvent.PostSetup);

        foreach (var eventType in new[] { AddonEvent.PostDraw, AddonEvent.PreFinalize }.Concat(hostAddonEvents).Distinct())
            DService.Instance().AddonLifecycle.RegisterListener(eventType, hostAddon, OnHostAddonLifecycle);

        DService.Instance().Framework.RunOnFrameworkThread
        (() =>
            {
                if (!IsHostReady())
                    return;

                if (runSetupForCurrentHostAddon)
                    OnHostAddon(AddonEvent.PostSetup, null);

                if (CanOpenAddon)
                    OpenAddon();
            }
        );
    }

    public override void Dispose()
    {
        DService.Instance().AddonLifecycle.UnregisterListener(OnHostAddonLifecycle);

        isClosingAddonOnly = true;
        base.Dispose();
    }

    protected virtual void OnHostAddon(AddonEvent type, AddonArgs? args) { }

    protected virtual void OnAttachedAddonUpdate(AtkUnitBase* addon, AtkUnitBase* hostAddon) { }

    protected virtual void OnAttachedAddonFinalize(AtkUnitBase* addon) { }

    protected virtual bool CanCloseHostAddon(AtkUnitBase* hostAddon) =>
        hostAddon != null && hostAddon->IsVisible;

    protected sealed override void OnUpdate(AtkUnitBase* addon)
    {
        var hostAddon = HostAddon;

        if (!IsHostReady())
        {
            CloseAddonOnly();
            return;
        }

        var hostPosition = new Vector2(hostAddon->RootNode->ScreenX,    hostAddon->RootNode->ScreenY);
        var hostSize     = new Vector2(hostAddon->GetScaledWidth(true), hostAddon->GetScaledHeight(true));
        var addonSize    = new Vector2(addon->GetScaledWidth(true),     addon->GetScaledHeight(true));

        // 自定义附着位置（忽略宿主坐标的场景，如 ChatTwo 隐藏宿主后用窗口位置跟随）
        if (CalculateCustomPosition(addon) is { } customPosition)
        {
            SetWindowPosition(customPosition + PositionOffset);
            OnAttachedAddonUpdate(addon, hostAddon);
            return;
        }

        var position = AttachPosition switch
        {
            AttachedAddonPosition.LeftTop      => new(hostPosition.X - addonSize.X, hostPosition.Y),
            AttachedAddonPosition.LeftCenter   => new(hostPosition.X - addonSize.X, hostPosition.Y              + (hostSize.Y - addonSize.Y) / 2f),
            AttachedAddonPosition.LeftBottom   => new(hostPosition.X - addonSize.X, hostPosition.Y + hostSize.Y - addonSize.Y),
            AttachedAddonPosition.TopLeft      => hostPosition with { Y = hostPosition.Y - addonSize.Y },
            AttachedAddonPosition.TopCenter    => new(hostPosition.X              + (hostSize.X - addonSize.X) / 2f, hostPosition.Y - addonSize.Y),
            AttachedAddonPosition.TopRight     => new(hostPosition.X + hostSize.X - addonSize.X, hostPosition.Y                     - addonSize.Y),
            AttachedAddonPosition.RightTop     => new(hostPosition.X              + hostSize.X, hostPosition.Y),
            AttachedAddonPosition.RightCenter  => new(hostPosition.X              + hostSize.X, hostPosition.Y              + (hostSize.Y - addonSize.Y) / 2f),
            AttachedAddonPosition.RightBottom  => new(hostPosition.X              + hostSize.X, hostPosition.Y + hostSize.Y - addonSize.Y),
            AttachedAddonPosition.BottomLeft   => hostPosition with { Y = hostPosition.Y + hostSize.Y },
            AttachedAddonPosition.BottomCenter => new(hostPosition.X              + (hostSize.X - addonSize.X) / 2f, hostPosition.Y + hostSize.Y),
            AttachedAddonPosition.BottomRight  => new(hostPosition.X + hostSize.X - addonSize.X, hostPosition.Y                     + hostSize.Y),
            _                                  => hostPosition
        };

        SetWindowPosition(position + PositionOffset);
        OnAttachedAddonUpdate(addon, hostAddon);
    }

    protected sealed override void OnFinalize(AtkUnitBase* addon)
    {
        OnAttachedAddonFinalize(addon);

        if (isClosingAddonOnly)
        {
            isClosingAddonOnly = false;
            return;
        }

        var hostAddon = HostAddon;
        if (!CanCloseHostAddon(hostAddon)) return;

        hostAddon->Close(true);
    }

    private void OnHostAddonLifecycle(AddonEvent type, AddonArgs? args)
    {
        OnHostAddon(type, args);

        switch (type)
        {
            case AddonEvent.PostDraw when CanOpenAddon:
                OpenAddon();
                break;
            case AddonEvent.PreFinalize:
                CloseAddonOnly();
                break;
        }
    }

    protected void CloseAddonOnly()
    {
        if (!IsOpen) return;

        isClosingAddonOnly = true;
        Close();
    }

    private void OpenAddon()
    {
        if (IsOpen || !IsHostReady()) return;

        Open();
    }
    
    public enum AttachedAddonPosition
    {
        LeftTop,
        LeftCenter,
        LeftBottom,
        TopLeft,
        TopCenter,
        TopRight,
        RightTop,
        RightCenter,
        RightBottom,
        BottomLeft,
        BottomCenter,
        BottomRight
    }
}
