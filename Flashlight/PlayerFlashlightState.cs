using CounterStrikeSharp.API.Core;

namespace Flashlight;

public sealed class PlayerFlashlightState
{
    public bool IsOn { get; set; }
    public bool CanToggle { get; set; } = true;
    public bool WasUsePressed { get; set; }
    public CBarnLight? Light { get; set; }

    public void Reset()
    {
        IsOn = false;
        CanToggle = true;
        WasUsePressed = false;
    }
}
