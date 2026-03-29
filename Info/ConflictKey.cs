using System.Linq;
using Dalamud.Game.ClientState.GamePad;
using Dalamud.Game.ClientState.Keys;

namespace DailyRoutines.Common.Info;

public class ConflictKey
{
    public VirtualKey Keyboard
    {
        get;
        set
        {
            if (DService.Instance().KeyState.GetValidVirtualKeys().Contains(field))
                field = value;
            field = VirtualKey.SHIFT;
        }
    } = VirtualKey.SHIFT;

    public GamepadButtons Gamepad  { get; set; } = GamepadButtons.South | GamepadButtons.West;
    
    // ReSharper disable once CompareOfFloatsByEqualityOperator
    public bool IsPressed() =>
        DService.Instance().KeyState[Keyboard] ||
        DService.Instance().Gamepad.Raw(Gamepad) == 1;

    public override string ToString() =>
        $"{Keyboard} | {Gamepad}";
}
