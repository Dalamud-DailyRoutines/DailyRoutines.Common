using System;
using System.Drawing;
using System.Numerics;
using Dalamud.Utility.Numerics;
using OmenTools.Extensions;

namespace DailyRoutines.Common.Interface.Widgets;

public abstract class CardComponentBase
{
    private const int BACKGROUND_CHANNEL = 0;
    private const int DECORATION_CHANNEL = 1;
    private const int CONTENT_CHANNEL    = 2;

    private const float HOVER_STIFFNESS = 240f;
    private const float HOVER_DAMPING   = 20f;

    private const float PRESS_STIFFNESS = 320f;
    private const float PRESS_DAMPING   = 22f;

    private static readonly Vector4 RestingBorder = new(1f, 1f, 1f, 0.06f);
    private static readonly Vector4 HoveredBorder = KnownColor.DodgerBlue.ToVector4().WithW(0.8f);

    protected Vector2 CurrentSize => isFirstDraw && currentSize == Vector2.Zero ? InitialSize : currentSize;
    
    protected static float Rounding => 8f * GlobalUIScale;
    
    protected virtual Vector2 InitialSize => Vector2.Zero;

    protected virtual bool DisableContent   => false;
    protected virtual bool DrawDisabledMask => false;
    
    private Vector2 currentSize;
    private Vector2 sizeVelocity;
    private bool    isFirstDraw = true;

    private float   hoverProgress;
    private float   hoverVelocity;

    private float   pressProgress;
    private float   pressVelocity;

    public void Draw()
    {
        var context   = CreateContext();
        var frameMin  = context.FrameMin;
        var drawList  = ImGui.GetWindowDrawList();
        var frameSize = CurrentSize;
        var isHovered = frameSize is { X: > 0f, Y: > 0f } &&
                        ImGui.IsMouseHoveringRect(frameMin, frameMin + frameSize);

        UpdateStates(isHovered);

        // 核心微动效：悬停时轻微向上浮起，点击时具有 spongy click 回弹下沉触感
        var hoverOffset = -2.5f * GlobalUIScale * hoverProgress;
        var pressOffset = 1.5f * GlobalUIScale * pressProgress;
        var visualOffset = new Vector2(0f, hoverOffset + pressOffset);

        var visualMin = frameMin + visualOffset;

        drawList.ChannelsSplit(3);
        drawList.ChannelsSetCurrent(CONTENT_CHANNEL);

        // 使内容绘制也跟随微动效偏移
        ImGui.SetCursorScreenPos(context.ContentStartPos + visualOffset);

        using (ImRaii.Disabled(DisableContent))
        using (ImRaii.Group())
            DrawContent(context, isHovered);

        var contentRectSize = ImGui.GetItemRectSize();
        var targetSize      = GetTargetSize(context, contentRectSize);

        UpdateCurrentSize(targetSize);

        var visualMax = visualMin + currentSize;

        drawList.ChannelsSetCurrent(BACKGROUND_CHANNEL);

        var baseColor = ImGui.GetColorU32(ImGuiCol.ChildBg).ToVector4();
        var restingBg = baseColor + new Vector4(0.02f, 0.02f, 0.02f, -0.1f);
        var hoveredBg = restingBg - new Vector4(0.03f, 0.03f, 0.03f, 0f);
        var bgColor = Vector4.Lerp(restingBg, hoveredBg, hoverProgress);

        var borderColor = Vector4.Lerp(RestingBorder, HoveredBorder, hoverProgress);

        // 双层高阶发光阴影效果
        if (hoverProgress > 0.01f)
        {
            // 第一层：环境阴影 (Soft Ambient Drop Shadow - 黑色超宽范围，产生物理离地感)
            var shadowColor = new Vector4(0f, 0f, 0f, 0.18f * hoverProgress);
            var shadowSize = 12f * GlobalUIScale * hoverProgress;
            ImGuiOm.AddGlowRect(drawList, visualMin, visualMax, shadowColor.ToUInt(), Rounding, shadowSize, 10, 0.20f);

            // 第二层：霓虹发光 (Vibrant Accent Glow - 紧贴边框的深天空蓝发光)
            var glowColor = HoveredBorder;
            glowColor.W = 0.16f * hoverProgress;
            var glowSize = 6f * GlobalUIScale * hoverProgress;
            ImGuiOm.AddGlowRect(drawList, visualMin, visualMax, glowColor.ToUInt(), Rounding, glowSize, 10, 0.25f);
        }

        // 绘制卡片背景与精细 1px 细边框
        drawList.AddRectFilled(visualMin, visualMax, bgColor.ToUInt(), Rounding);
        drawList.AddRect(visualMin, visualMax, borderColor.ToUInt(), Rounding, ImDrawFlags.None, 1f);

        // 绘制顶部高光亮点线 (Fluent Design 顶光反射质感细节)
        var topHighlightColor = new Vector4(1f, 1f, 1f, 0.08f + 0.12f * hoverProgress);
        drawList.AddLine(
            new Vector2(visualMin.X + Rounding, visualMin.Y + 0.5f),
            new Vector2(visualMin.X + currentSize.X - Rounding, visualMin.Y + 0.5f),
            topHighlightColor.ToUInt(),
            1f
        );

        drawList.ChannelsMerge();

        DrawAfterFrame(context, visualMax, isHovered);

        // 恢复原有布局排版流，完全不影响相邻元素的物理位置排布
        ImGui.SetCursorScreenPos(frameMin + new Vector2(0f, currentSize.Y + ImGui.GetStyle().ItemSpacing.Y));

        if (DrawDisabledMask)
            drawList.AddRectFilled(visualMin, visualMax, KnownColor.Black.ToVector4().WithW(0.2f).ToUInt(), Rounding);
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
        const float DAMPING   = 28f;

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

    private void UpdateStates(bool isHovered)
    {
        var dt = Math.Min(ImGui.GetIO().DeltaTime, 0.05f);

        // 悬浮状态更新 (刚度 240f，阻尼 20f)
        var targetHover = isHovered ? 1f : 0f;
        var hoverDisplacement = hoverProgress - targetHover;
        var hoverForce = -HOVER_STIFFNESS * hoverDisplacement - HOVER_DAMPING * hoverVelocity;
        hoverVelocity += hoverForce * dt;
        hoverProgress += hoverVelocity * dt;

        if (Math.Abs(hoverDisplacement) < 0.001f && Math.Abs(hoverVelocity) < 0.001f)
        {
            hoverProgress = targetHover;
            hoverVelocity = 0f;
        }
        hoverProgress = Math.Clamp(hoverProgress, 0f, 1f);

        // 按压状态更新 (刚度 320f，阻尼 24f)
        var isPressed = isHovered && ImGui.IsMouseDown(ImGuiMouseButton.Left);
        var targetPress = isPressed ? 1f : 0f;
        var pressDisplacement = pressProgress - targetPress;
        var pressForce = -PRESS_STIFFNESS * pressDisplacement - PRESS_DAMPING * pressVelocity;
        pressVelocity += pressForce * dt;
        pressProgress += pressVelocity * dt;

        if (Math.Abs(pressDisplacement) < 0.001f && Math.Abs(pressVelocity) < 0.001f)
        {
            pressProgress = targetPress;
            pressVelocity = 0f;
        }
        pressProgress = Math.Clamp(pressProgress, 0f, 1f);
    }
    
    protected readonly record struct CardDrawContext
    (
        Vector2 FrameMin,
        Vector2 ContentStartPos,
        Vector2 Padding,
        float   FrameWidth = 0f
    );
}
