using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Text.ReadOnly;

namespace DailyRoutines.Common.KamiToolKit.Addons.SelectYesno;

public sealed record DRSelectYesnoOptions
{
    public required ReadOnlySeString Prompt { get; init; }

    public DRSelectYesnoButtons Buttons { get; init; } = DRSelectYesnoButtons.Both;

    public ReadOnlySeString? YesButtonText { get; init; }

    public ReadOnlySeString? NoButtonText { get; init; }

    public AlignmentType PromptAlignment { get; init; } = AlignmentType.Center;

    public float Width { get; init; } = 360.0f;

    public float MinPromptHeight { get; init; } = 28.0f;

    public float MaxPromptHeight { get; init; } = 300.0f;

    public bool RespectCloseAll { get; init; } = true;

    public Vector2? Position { get; init; }

    public int OpenSoundEffectID { get; init; } = 23;

    public Action<DRSelectYesnoResult>? Callback { get; init; }
}
