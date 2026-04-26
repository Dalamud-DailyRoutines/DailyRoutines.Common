using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit;

namespace DailyRoutines.Common.KamiToolKit.Addons;

public abstract unsafe class AttachedAddon : NativeAddon
{
    private bool skipHostAddonCloseOnce;

    protected abstract AtkUnitBase* HostAddon { get; }

    protected virtual bool RequireHostAddonReady => true;

    protected virtual Vector2 PositionOffset => Vector2.Zero;

    protected void CloseWithoutClosingHostAddon()
    {
        if (!IsOpen) return;

        skipHostAddonCloseOnce = true;
        Close();
    }

    protected sealed override void OnUpdate(AtkUnitBase* addon)
    {
        var hostAddon = HostAddon;
        if (!IsHostAddonReady(hostAddon))
        {
            CloseWithoutClosingHostAddon();
            return;
        }

        SetWindowPosition(GetAttachedWindowPosition(addon, hostAddon));
        OnHostAddonUpdate(addon, hostAddon);
    }

    protected sealed override void OnFinalize(AtkUnitBase* addon)
    {
        OnAttachedAddonFinalize(addon);

        if (skipHostAddonCloseOnce)
        {
            skipHostAddonCloseOnce = false;
            return;
        }

        var hostAddon = HostAddon;
        if (hostAddon == null || !ShouldCloseHostAddon(hostAddon)) return;

        hostAddon->Close(true);
    }

    protected virtual bool IsHostAddonReady(AtkUnitBase* hostAddon) => 
        hostAddon != null && hostAddon->RootNode != null && (!RequireHostAddonReady || hostAddon->IsAddonAndNodesReady());

    protected virtual Vector2 GetAttachedWindowPosition(AtkUnitBase* addon, AtkUnitBase* hostAddon) => 
        GetHostAddonAnchor(hostAddon) - GetCurrentAddonAnchor(addon) + PositionOffset;

    protected virtual Vector2 GetHostAddonAnchor(AtkUnitBase* hostAddon) => 
        new(hostAddon->RootNode->ScreenX, hostAddon->RootNode->ScreenY);

    protected virtual Vector2 GetCurrentAddonAnchor(AtkUnitBase* addon) => 
        new(addon->GetScaledWidth(true), 0f);

    protected virtual void OnHostAddonUpdate(AtkUnitBase* addon, AtkUnitBase* hostAddon) { }

    protected virtual void OnAttachedAddonFinalize(AtkUnitBase* addon) { }

    protected virtual bool ShouldCloseHostAddon(AtkUnitBase* hostAddon) => true;
}
