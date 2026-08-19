using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.BaseTypes;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using Lumina.Text.ReadOnly;

namespace DailyRoutines.Common.KamiToolKit.Addons.SelectYesno;

public sealed unsafe class DRSelectYesno : NativeAddon
{
    public static Task Open
    (
        DRSelectYesnoOptions options
    )
    {
        ArgumentNullException.ThrowIfNull(options);
        ValidateOptions(options);

        return DService.Instance().Framework.RunOnFrameworkThread
        (() => { OpenOnFrameworkThread(options); }
        );
    }

    public static Task CloseCurrent()
        => DService.Instance().Framework.RunOnFrameworkThread(() => currentAddon?.Close());

    public override void Dispose()
    {
        if (ReferenceEquals(currentAddon, this))
            currentAddon = null;

        openAfterFinalize    = false;
        suppressHideCallback = true;

        base.Dispose();
    }

    protected override void OnSetup
    (
        AtkUnitBase*   addon,
        Span<AtkValue> atkValueSpan
    )
    {
        hasResult            = false;
        suppressHideCallback = false;

        if (WindowNode is WindowNode window)
        {
            window.ShowCloseButton               = true;
            window.HeaderContainerNode.IsVisible = true;
            window.HeaderCollisionNode.IsVisible = true;
            window.TitleNode.IsVisible           = false;
            window.SubtitleNode.IsVisible        = false;
            window.DividingLineNode.IsVisible    = false;
        }

        PromptNode = new TextNode
        {
            Size             = new Vector2(344.0f, 0.0f),
            TextColor        = ColorHelper.GetColor(8),
            TextOutlineColor = ColorHelper.GetColor(7),
            FontSize         = 14,
            FontType         = FontType.Axis,
            LineSpacing      = 18,
            AlignmentType    = options.PromptAlignment
        };
        PromptNode.AddTextFlags(TextFlags.WordWrap, TextFlags.MultiLine);
        PromptNode.AttachNode(this);

        PrimaryButton = new TextButtonNode
        {
            Size    = new Vector2(100.0f, 28.0f),
            OnClick = () => Select(DRSelectYesnoResult.Yes)
        };
        PrimaryButton.AttachNode(this);

        SecondaryButton = new TextButtonNode
        {
            Size    = new Vector2(100.0f, 28.0f),
            OnClick = () => Select(DRSelectYesnoResult.No)
        };
        SecondaryButton.AttachNode(this);

        ApplyOptions(options);
        SetInitialPosition();
    }

    protected override void OnHide
    (
        AtkUnitBase* addon
    )
    {
        if (suppressHideCallback || hasResult)
            return;

        hasResult = true;
        QueueCallback(options.Callback, DRSelectYesnoResult.Closed);
    }

    protected override void OnFinalize
    (
        AtkUnitBase* addon
    )
    {
        base.OnFinalize(addon);

        PromptNode           = null;
        PrimaryButton        = null;
        SecondaryButton      = null;
        suppressHideCallback = false;
    }

    private DRSelectYesno
    (
        DRSelectYesnoOptions options
    )
        => this.options = options;

    private static void OpenOnFrameworkThread
    (
        DRSelectYesnoOptions options
    )
    {
        if (currentAddon is null)
        {
            currentAddon = new DRSelectYesno(options)
            {
                InternalName              = "DRSelectYesno",
                Title                     = string.Empty,
                Size                      = new Vector2(400.0f, 96.0f),
                OpenWindowSoundEffectId   = options.OpenSoundEffectID,
                RespectCloseAll           = options.RespectCloseAll,
                DisableClamping           = false,
                EnableContextMenu         = false,
                DisableScaleContextOption = true,
                RememberClosePosition     = false,
                CreateWindowNode = () =>
                {
                    var window = new WindowNode();
                    window.ShowCloseButton               = true;
                    window.ShowConfigButton              = false;
                    window.ShowHelpButton                = false;
                    window.HeaderContainerNode.IsVisible = true;
                    window.HeaderCollisionNode.IsVisible = true;
                    window.TitleNode.IsVisible           = false;
                    window.SubtitleNode.IsVisible        = false;
                    window.DividingLineNode.IsVisible    = false;
                    return window;
                }
            };

            currentAddon.Open();
            return;
        }

        if (!currentAddon.IsOpen && currentAddon.InternalAddon is not null)
        {
            currentAddon.suppressHideCallback = true;
            currentAddon.openAfterFinalize    = true;
            DService.Instance().Framework.RunOnTick(currentAddon.ReopenAfterFinalize);
        }

        var previousCallback = currentAddon.ReplaceOptions(options);
        previousCallback?.Invoke(DRSelectYesnoResult.Replaced);

        if (!currentAddon.IsOpen && currentAddon.InternalAddon is null)
            currentAddon.Open();
    }

    private Action<DRSelectYesnoResult>? ReplaceOptions
    (
        DRSelectYesnoOptions nextOptions
    )
    {
        var previousCallback = hasResult ?
                                   null :
                                   options.Callback;

        options                 = nextOptions;
        hasResult               = false;
        RespectCloseAll         = nextOptions.RespectCloseAll;
        OpenWindowSoundEffectId = nextOptions.OpenSoundEffectID;

        if (PromptNode is not null)
        {
            ApplyOptions(nextOptions);
            SetInitialPosition();
        }

        return previousCallback;
    }

    private void ApplyOptions
    (
        DRSelectYesnoOptions nextOptions
    )
    {
        if (PromptNode is null || PrimaryButton is null || SecondaryButton is null)
            return;

        PromptNode.AlignmentType = nextOptions.PromptAlignment;
        PromptNode.String        = nextOptions.Prompt;

        SetButtonText(PrimaryButton,   nextOptions.YesButtonText, 3);
        SetButtonText(SecondaryButton, nextOptions.NoButtonText,  4);

        var buttons       = nextOptions.Buttons;
        var showPrimary   = (buttons & DRSelectYesnoButtons.Yes) != 0;
        var showSecondary = (buttons & DRSelectYesnoButtons.No)  != 0;

        PrimaryButton.IsVisible   = showPrimary;
        SecondaryButton.IsVisible = showSecondary;

        PromptNode.Size = new Vector2(344.0f, 0.0f);
        var promptHeight = PromptNode.GetTextDrawSize().Y;

        var buttonCount = (showPrimary ?
                               1 :
                               0) +
                          (showSecondary ?
                               1 :
                               0);
        var buttonHeight = buttonCount == 0 ?
                               0.0f :
                               28.0f;
        var buttonSpacing = buttonCount == 0 ?
                                0.0f :
                                8.0f;
        var height = MathF.Max(96.0f, 20.0f + promptHeight + buttonSpacing + buttonHeight + 18.0f);

        SetWindowSize(400.0f, height);

        PromptNode.Size     = new Vector2(344.0f, promptHeight);
        PromptNode.Position = new Vector2(28.0f,  20.0f);

        var buttonWidth = 100.0f;
        var buttonGap   = 8.0f;
        var totalButtonWidth = buttonCount == 2 ?
                                   (buttonWidth * 2.0f) + buttonGap :
                                   buttonWidth;
        var buttonLeft = (400.0f - totalButtonWidth) / 2.0f;
        var buttonY    = height - buttonHeight - 18.0f;

        var visibleButtons = new List<TextButtonNode>(buttonCount);
        if (showPrimary)
            visibleButtons.Add(PrimaryButton);
        if (showSecondary)
            visibleButtons.Add(SecondaryButton);

        for (var index = 0; index < visibleButtons.Count; index++)
        {
            var button          = visibleButtons[index];
            var navigationIndex = index + 1;
            var previousIndex = index == 0 ?
                                    visibleButtons.Count :
                                    index;
            var nextIndex = index == visibleButtons.Count - 1 ?
                                1 :
                                index + 2;

            button.Position = new Vector2(buttonLeft + (index * (buttonWidth + buttonGap)), buttonY);
            button.NavIndex = navigationIndex;
            button.NavLeft  = previousIndex;
            button.NavRight = nextIndex;
        }

        if (!showPrimary)
            PrimaryButton.NavIndex = 0;
        if (!showSecondary)
            SecondaryButton.NavIndex = 0;

        if (InternalAddon is not null)
            InternalAddon->FocusNode = showPrimary ? PrimaryButton : showSecondary ? SecondaryButton : RootNode;
    }

    private static void SetButtonText
    (
        TextButtonNode    button,
        ReadOnlySeString? text,
        uint              defaultTextID
    )
    {
        button.TextId = 0;
        button.String = text ?? DService.Instance().SeStringEvaluator.EvaluateFromAddon(defaultTextID, []);
    }

    private void SetInitialPosition()
    {
        if (InternalAddon is null)
            return;

        var screenSize  = (Vector2)AtkStage.Instance()->ScreenSize;
        var maxPosition = Vector2.Max(Vector2.Zero, screenSize - Size);
        var position    = options.Position ?? maxPosition / 2.0f;
        SetWindowPosition(Vector2.Clamp(position, Vector2.Zero, maxPosition));
    }

    private void Select
    (
        DRSelectYesnoResult result
    )
    {
        if (hasResult)
            return;

        hasResult = true;
        var callback = options.Callback;
        Close();
        QueueCallback(callback, result);
    }

    private static void QueueCallback
    (
        Action<DRSelectYesnoResult>? callback,
        DRSelectYesnoResult          result
    )
    {
        if (callback is not null)
            DService.Instance().Framework.RunOnTick(() => callback(result));
    }

    private void ReopenAfterFinalize()
    {
        if (!openAfterFinalize || DService.IsDisposed)
        {
            openAfterFinalize = false;
            return;
        }

        if (InternalAddon is null)
        {
            openAfterFinalize = false;
            Open();
            return;
        }

        DService.Instance().Framework.RunOnTick(ReopenAfterFinalize);
    }

    private static void ValidateOptions
    (
        DRSelectYesnoOptions options
    )
    {
        if ((options.Buttons & ~DRSelectYesnoButtons.Both) != 0)
            throw new ArgumentOutOfRangeException(nameof(options.Buttons));

        if (options.Position is { } position && (!float.IsFinite(position.X) || !float.IsFinite(position.Y)))
            throw new ArgumentOutOfRangeException(nameof(options.Position));
    }

    private static DRSelectYesno? currentAddon;

    private DRSelectYesnoOptions options;
    private TextNode?            PromptNode      { get; set; }
    private TextButtonNode?      PrimaryButton   { get; set; }
    private TextButtonNode?      SecondaryButton { get; set; }
    private bool                 hasResult;
    private bool                 openAfterFinalize;
    private bool                 suppressHideCallback;
}
