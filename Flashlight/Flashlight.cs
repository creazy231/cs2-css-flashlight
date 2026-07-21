using System.Drawing;
using System.Numerics;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

namespace Flashlight;

[MinimumApiVersion(371)]
public class FlashlightPlugin : BasePlugin, IPluginConfig<FlashlightConfig>
{
    public override string ModuleAuthor => "creazy.eth";
    public override string ModuleName => "Flashlight";
    public override string ModuleDescription => "Flashlight for Counter-Strike 2";
    public override string ModuleVersion => "0.1.1";

    public FlashlightConfig Config { get; set; } = new();

    private readonly Dictionary<int, PlayerFlashlightState> _playerStates = new();

    public void OnConfigParsed(FlashlightConfig config)
    {
        config.Clamp();
        Config = config;
    }

    public override void Load(bool hotReload)
    {
        Logger.LogInformation("{Name} v{Version} loading...", ModuleName, ModuleVersion);

        RegisterListener<Listeners.OnTick>(OnTick);

        if (hotReload)
        {
            foreach (var player in Utilities.GetPlayers().Where(IsEligiblePlayer))
            {
                EnsureState(player);
            }
        }

        Logger.LogInformation("{Name} v{Version} loaded!", ModuleName, ModuleVersion);
    }

    public override void Unload(bool hotReload)
    {
        foreach (var slot in _playerStates.Keys.ToList())
        {
            DestroyLight(slot);
        }

        _playerStates.Clear();
    }

    private void OnTick()
    {
        if (!Config.Enabled || !Config.AllowUseKey)
        {
            return;
        }

        foreach (var player in Utilities.GetPlayers())
        {
            if (!IsEligiblePlayer(player) || !player.PawnIsAlive)
            {
                continue;
            }

            var state = EnsureState(player);
            var usePressed = (player.Buttons & PlayerButtons.Use) != 0;

            if (FlashlightLogic.IsUsePressedEdge(usePressed, state.WasUsePressed))
            {
                TryToggleFlashlight(player, state);
            }

            state.WasUsePressed = usePressed;
        }
    }

    [GameEventHandler]
    public HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player is null || !IsEligiblePlayer(player))
        {
            return HookResult.Continue;
        }

        EnsureState(player);
        Logger.LogInformation("{Player} connected", player.PlayerName);
        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player is null || !player.IsValid)
        {
            return HookResult.Continue;
        }

        var slot = player.Slot;
        DestroyLight(slot);
        _playerStates.Remove(slot);
        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player is null || !IsEligiblePlayer(player))
        {
            return HookResult.Continue;
        }

        var state = EnsureState(player);
        state.IsOn = false;
        DestroyLight(player.Slot);
        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player is null || !IsEligiblePlayer(player))
        {
            return HookResult.Continue;
        }

        var state = EnsureState(player);
        state.IsOn = false;
        DestroyLight(player.Slot);
        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerTeam(EventPlayerTeam @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player is null || !IsEligiblePlayer(player))
        {
            return HookResult.Continue;
        }

        var state = EnsureState(player);
        state.IsOn = false;
        DestroyLight(player.Slot);
        return HookResult.Continue;
    }

    [ConsoleCommand("css_fl_toggle", "Toggles the flashlight")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnToggleCommand(CCSPlayerController? caller, CommandInfo info)
    {
        if (!Config.Enabled || caller is null || !IsEligiblePlayer(caller) || !caller.PawnIsAlive)
        {
            return;
        }

        TryToggleFlashlight(caller, EnsureState(caller));
    }

    private void TryToggleFlashlight(CCSPlayerController player, PlayerFlashlightState state)
    {
        // Turning on is restricted by AllowedTeam; turning off is always allowed.
        if (!state.IsOn && !FlashlightLogic.IsTeamAllowed(Config.AllowedTeam, (byte)player.Team))
        {
            return;
        }

        var isOn = state.IsOn;
        var canToggle = state.CanToggle;

        if (!FlashlightLogic.TryToggle(ref isOn, ref canToggle))
        {
            return;
        }

        state.IsOn = isOn;
        state.CanToggle = canToggle;

        if (state.IsOn)
        {
            CreateAndParentLight(player, state);
        }
        else
        {
            DestroyLight(player.Slot);
        }

        var slot = player.Slot;
        AddTimer(Config.ToggleCooldownSeconds, () =>
        {
            if (_playerStates.TryGetValue(slot, out var current))
            {
                current.CanToggle = true;
            }
        });
    }

    private void CreateAndParentLight(CCSPlayerController player, PlayerFlashlightState state)
    {
        DestroyLight(player.Slot);

        var pawn = player.PlayerPawn.Value;
        if (pawn is null || !pawn.IsValid || pawn.AbsOrigin is null || pawn.V_angle is null)
        {
            Logger.LogWarning("Failed to get pawn data for {Player}", player.PlayerName);
            state.IsOn = false;
            return;
        }

        var light = Utilities.CreateEntityByName<CBarnLight>("light_barn");
        if (light is null || !light.IsValid)
        {
            Logger.LogWarning("Failed to create light_barn for {Player}", player.PlayerName);
            state.IsOn = false;
            return;
        }

        var isCrouching = (player.Buttons & PlayerButtons.Duck) != 0;
        var eyeOffsetZ = FlashlightLogic.GetEyeOffsetZ(
            isCrouching,
            Config.StandEyeOffsetZ,
            Config.CrouchEyeOffsetZ);

        var angles = pawn.V_angle;
        var forward = FlashlightLogic.ForwardFromAnglesDegrees(angles.X, angles.Y);
        var origin = FlashlightLogic.CalculateLightOrigin(
            new Vector3(pawn.AbsOrigin.X, pawn.AbsOrigin.Y, pawn.AbsOrigin.Z),
            forward,
            eyeOffsetZ,
            Config.ForwardDistance);

        light.Enabled = true;
        light.Color = Color.FromArgb(255, Config.ColorR, Config.ColorG, Config.ColorB);
        light.ColorTemperature = Config.ColorTemperature;
        light.Brightness = Config.Brightness;
        light.Range = Config.Range;
        light.SoftX = Config.SoftX;
        light.SoftY = Config.SoftY;
        light.Skirt = Config.Skirt;
        light.SkirtNear = Config.SkirtNear;
        light.CastShadows = Config.CastShadows ? 1 : 0;
        light.DirectLight = 3;
        light.SizeParams.X = Config.SizeX;
        light.SizeParams.Y = Config.SizeY;
        light.SizeParams.Z = Config.SizeZ;

        light.Teleport(
            origin,
            new Vector3(angles.X, angles.Y, angles.Z),
            null);

        using (var keyValues = new CEntityKeyValues())
        {
            keyValues.SetString("lightcookie", Config.LightCookie);
            light.DispatchSpawn(keyValues);
        }

        light.AcceptInput("SetParent", pawn, light, "!activator");
        light.AcceptInput("SetParentAttachmentMaintainOffset", null, null, Config.AttachmentName);

        state.Light = light;
        state.IsOn = true;
    }

    private void DestroyLight(int slot)
    {
        if (!_playerStates.TryGetValue(slot, out var state))
        {
            return;
        }

        var light = state.Light;
        state.Light = null;

        if (light is not null && light.IsValid)
        {
            light.Remove();
        }
    }

    private PlayerFlashlightState EnsureState(CCSPlayerController player)
    {
        if (_playerStates.TryGetValue(player.Slot, out var state))
        {
            return state;
        }

        state = new PlayerFlashlightState();
        _playerStates[player.Slot] = state;
        return state;
    }

    private static bool IsEligiblePlayer(CCSPlayerController? player)
    {
        return player is { IsValid: true, IsBot: false };
    }
}
