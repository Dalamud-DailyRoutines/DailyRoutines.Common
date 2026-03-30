using System.Numerics;
using Dalamud.Utility.Numerics;

namespace DailyRoutines.Common.Interface.Widgets;

public abstract class CardComponentBase
{
    private const int BACKGROUND_CHANNEL = 0;
    private const int DECORATION_CHANNEL = 1;
    private const int CONTENT_CHANNEL    = 2;

    protected Vector2 CurrentSize => isFirstDraw && currentSize == Vector2.Zero ? InitialSize : currentSize;
    
    protected static float Rounding => 6f * GlobalUIScale;
    
    protected virtual Vector2 InitialSize => Vector2.Zero;

    protected virtual bool DisableContent   => false;
    protected virtual bool DrawDisabledMask => false;
    
    private Vector2 currentSize;
    private Vector2 sizeVelocity;
    private bool    isFirstDraw = true;

    public void Draw()
    {
        var context   = CreateContext();
        var frameMin  = context.FrameMin;
        var drawList  = ImGui.GetWindowDrawList();
        var frameSize = CurrentSize;
        var isHovered = frameSize is { X: > 0f, Y: > 0f } &&
                        ImGui.IsMouseHoveringRect(frameMin, frameMin + frameSize);

        drawList.ChannelsSplit(3);
        drawList.ChannelsSetCurrent(CONTENT_CHANNEL);

        ImGui.SetCursorScreenPos(context.ContentStartPos);

        using (ImRaii.Disabled(DisableContent))
        using (ImRaii.Group())
            DrawContent(context, isHovered);

        var contentRectSize = ImGui.GetItemRectSize();
        var targetSize      = GetTargetSize(context, contentRectSize);

        UpdateCurrentSize(targetSize);

        var frameMax = frameMin + currentSize;

        drawList.ChannelsSetCurrent(BACKGROUND_CHANNEL);

        var bgColor = ImGui.GetColorU32(ImGuiCol.ChildBg).ToVector4();
        bgColor += new Vector4(0.02f, 0.02f, 0.02f, -0.1f);
        if (isHovered)
            bgColor -= new Vector4(0.03f, 0.03f, 0.03f, 0f);

        drawList.AddRectFilled(frameMin, frameMax, bgColor.ToUInt(), Rounding);

        if (isHovered)
            drawList.AddRect(frameMin, frameMax, ImGui.GetColorU32(ImGuiCol.ButtonActive, 0.5f), Rounding, ImDrawFlags.None, 2f);

        drawList.ChannelsMerge();

        DrawAfterFrame(context, frameMax, isHovered);

        ImGui.SetCursorScreenPos(frameMin + new Vector2(0f, currentSize.Y + ImGui.GetStyle().ItemSpacing.Y));

        if (DrawDisabledMask)
            drawList.AddRectFilled(frameMin, frameMax, KnownColor.Black.ToVector4().WithW(0.2f).ToUInt(), Rounding);
    }

    protected static int GetDecorationChannel() =>
        DECORATION_CHANNEL;

    protected static int GetContentChannel() =>
        CONTENT_CHANNEL;

    protected virtual void DrawAfterFrame(CardDrawContext context, Vector2 frameMax, bool isHovered) { }

    protected abstract CardDrawContext CreateContext();

    protected abstract void DrawContent(CardDrawContext context, bool isHovered);

    protected abstract Vector2 GetTargetSize(CardDrawContext context, Vector2 contentRectSize);

    private void UpdateCurrentSize(Vector2 targetSize)
    {
        var dt = Math.Min(ImGui.GetIO().DeltaTime, 0.05f);

        if (isFirstDraw)
        {
            currentSize  = targetSize;
            sizeVelocity = Vector2.Zero;
            isFirstDraw  = false;
            return;
        }

        const float STIFFNESS = 180f;
        const float DAMPING   = 12f;

        var displacement = currentSize               - targetSize;
        var force        = -STIFFNESS * displacement - DAMPING * sizeVelocity;

        sizeVelocity += force        * dt;
        currentSize  += sizeVelocity * dt;

        if (displacement.Length() < 0.1f && sizeVelocity.Length() < 0.1f)
        {
            currentSize  = targetSize;
            sizeVelocity = Vector2.Zero;
        }
    }
    
    protected readonly record struct CardDrawContext
    (
        Vector2 FrameMin,
        Vector2 ContentStartPos,
        Vector2 Padding,
        float   FrameWidth = 0f
    );
}
