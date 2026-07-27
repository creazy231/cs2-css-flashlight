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
    public override string ModuleVersion => "0.1.2";

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
        if (!Config.Enabled)
        {
            return;
        }

        foreach (var player in Utilities.GetPlayers())
        {
            if (!IsEligiblePlayer(player))
            {
                continue;
            }

            if (!player.PawnIsAlive)
            {
                // An un-parented light no longer dies together with the pawn, so sweep it up here
                // in case a death or round-end never reached the event handlers.
                if (_playerStates.TryGetValue(player.Slot, out var deadState) && deadState.IsOn)
                {
                    deadState.IsOn = false;
                    DestroyLight(player.Slot);
                }

                continue;
            }

            var state = EnsureState(player);

            if (Config.AllowUseKey)
            {
                var usePressed = (player.Buttons & PlayerButtons.Use) != 0;

                if (FlashlightLogic.IsUsePressedEdge(usePressed, state.WasUsePressed))
                {
                    TryToggleFlashlight(player, state);
                }

                state.WasUsePressed = usePressed;
            }

            if (state.IsOn)
            {
                UpdateLight(player, state);
            }
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
            CreateLight(player, state);
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

    private void CreateLight(CCSPlayerController player, PlayerFlashlightState state)
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

        ApplyTransform(light, player, pawn);

        using (var keyValues = new CEntityKeyValues())
        {
            keyValues.SetString("lightcookie", Config.LightCookie);
            light.DispatchSpawn(keyValues);
        }

        state.Light = light;
        state.IsOn = true;
    }

    /// <summary>
    /// Keeps the light glued to the player's eye every tick.
    /// </summary>
    /// <remarks>
    /// The light is deliberately not parented to the pawn. Handing it to the engine via
    /// <c>SetParent</c> / <c>SetParentAttachmentMaintainOffset</c> locks its orientation to a model
    /// attachment, and those attachments only carry the body's yaw, so the beam could never follow
    /// the player looking up or down. Driving the transform ourselves keeps pitch and yaw in sync.
    /// </remarks>
    private void UpdateLight(CCSPlayerController player, PlayerFlashlightState state)
    {
        var light = state.Light;

        if (light is null || !light.IsValid)
        {
            // The engine can reap the entity underneath us (round restart, cleanup); rebuild it.
            state.Light = null;
            CreateLight(player, state);
            return;
        }

        var pawn = player.PlayerPawn.Value;
        if (pawn is null || !pawn.IsValid || pawn.AbsOrigin is null || pawn.V_angle is null)
        {
            return;
        }

        ApplyTransform(light, player, pawn);
    }

    private void ApplyTransform(CBarnLight light, CCSPlayerController player, CCSPlayerPawn pawn)
    {
        var isCrouching = (player.Buttons & PlayerButtons.Duck) != 0;
        var eyeOffsetZ = FlashlightLogic.GetEyeOffsetZ(
            isCrouching,
            Config.StandEyeOffsetZ,
            Config.CrouchEyeOffsetZ);

        var origin = pawn.AbsOrigin!;
        var angles = pawn.V_angle;

        var transform = FlashlightLogic.CalculateLightTransform(
            new Vector3(origin.X, origin.Y, origin.Z),
            angles.X,
            angles.Y,
            angles.Z,
            eyeOffsetZ,
            Config.ForwardDistance);

        // Handing the pawn's velocity over lets clients interpolate the light between ticks
        // instead of visibly stepping it.
        var velocity = pawn.AbsVelocity;

        light.Teleport(
            transform.Origin,
            transform.Angles,
            velocity is null ? null : new Vector3(velocity.X, velocity.Y, velocity.Z));
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
