using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace Flashlight;

[MinimumApiVersion(126)]
public class Flashlight : BasePlugin
{
    public override string ModuleAuthor => "creazy.eth";
    public override string ModuleName => "Flashlight";
    public override string ModuleDescription => "Flashlight for Counter-Strike 2";
    public override string ModuleVersion => "0.0.5";

    private static string ModuleDisplayName => "Flashlight";
    
    // TODO: Change crouch-tracking to a more elegant solution
    // TODO: Add config and make light entity values configurable
    // TODO: Maybe replace light_omni2 with light_rect or something else

    public static Flashlight? Instance { get; private set; }
    
    private readonly List<CCSPlayerController> _connectedPlayers = new();
    private readonly Dictionary<CCSPlayerController, bool> _playerUsingFlashlight = new();
    private readonly Dictionary<CCSPlayerController, bool> _playerIsCrouching = new();
    private readonly Dictionary<CCSPlayerController, bool> _playerCanToggle = new();
    private readonly Dictionary<CCSPlayerController, COmniLight> _playerFlashlight = new();

    public override void Load(bool hotReload)
    {
        Instance = this;
        
        LogHelper.LogToConsole(ConsoleColor.Green, $"{ModuleName} v{ModuleVersion} loading...");
        
        RegisterListener<Listeners.OnTick>(() =>
        {
            foreach (var player in _connectedPlayers.Where(player => player is { IsValid: true, IsBot: false, PawnIsAlive: true }))
            {
                ToggleFlashlight(player);
                
                if (_playerCanToggle[player] == false) continue;
                
                if ((player.Buttons & PlayerButtons.Use) != 0)
                {
                    _playerCanToggle[player] = false;
                    
                    if (_playerUsingFlashlight[player] == false)
                    {
                        _playerUsingFlashlight[player] = true;
                    
                        Instance?.AddTimer(0.25f, () =>
                        {
                            _playerCanToggle[player] = true;
                        });
                    }
                    else
                    {
                        _playerUsingFlashlight[player] = false;
                        
                        Instance?.AddTimer(0.25f, () =>
                        {
                            _playerCanToggle[player] = true;
                        });
                    }
                }
                
                if ((player.Buttons & PlayerButtons.Duck) != 0)
                {
                    _playerIsCrouching[player] = true;
                }
                else
                {
                    _playerIsCrouching[player] = false;
                }
            }
        });

        LogHelper.LogToConsole($"{ModuleDisplayName} v{ModuleVersion} loaded!");
    }
    
    [GameEventHandler]
    public HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
    {
        var player = @event.Userid;
        
        if (!player.IsValid || player.IsBot) return HookResult.Continue;
        
        _connectedPlayers.Add(player);
        _playerUsingFlashlight[player] = false;
        _playerIsCrouching[player] = false;
        _playerCanToggle[player] = true;
        
        LogHelper.LogToConsole(ConsoleColor.Green, $"{player.PlayerName} connected");
        
        return HookResult.Continue;
    }
    
    [GameEventHandler]
    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        var player = @event.Userid;
        
        if (!player.IsValid || player.IsBot) return HookResult.Continue;
        
        _connectedPlayers.Remove(player);
        _playerUsingFlashlight.Remove(player);
        _playerIsCrouching.Remove(player);
        _playerCanToggle.Remove(player);
        
        _playerFlashlight.TryGetValue(player, out var flashlight);
        flashlight?.Remove();
        _playerFlashlight.Remove(player);
        
        LogHelper.LogToConsole(ConsoleColor.Green, $"{player.PlayerName} disconnected");
        
        return HookResult.Continue;
    }
    
    [GameEventHandler]
    public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;
        
        if (!player.IsValid || player.IsBot) return HookResult.Continue;
        
        if (_connectedPlayers.Contains(player) == false)
        {
            _connectedPlayers.Add(player);
            _playerUsingFlashlight[player] = false;
            _playerIsCrouching[player] = false;
            _playerCanToggle[player] = true;
        }
        
        _playerUsingFlashlight[player] = false;
        
        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        var player = @event.Userid;
        
        if (!player.IsValid || player.IsBot) return HookResult.Continue;
        
        _playerUsingFlashlight[player] = false;
        
        return HookResult.Continue;
    }

    public void ToggleFlashlight(CCSPlayerController player)
    {
        if (_playerUsingFlashlight[player] == false)
        {
            if (_playerFlashlight.TryGetValue(player, out var value))
            {
                value.Remove();
                _playerFlashlight.Remove(player);
            }
            
            return;
        }
        
        var entity = _playerFlashlight.TryGetValue(player, out var flashlight) ? flashlight : Utilities.CreateEntityByName<COmniLight>("light_omni2");

        if (entity == null || !entity.IsValid)
        {
            LogHelper.LogToConsole("Failed to create entity!");
            return;
        }
        
        entity.DirectLight = 3;
        
        entity.Teleport(
            new Vector(
                player.PlayerPawn.Value!.AbsOrigin!.X,
                player.PlayerPawn.Value!.AbsOrigin!.Y,
                player.PlayerPawn.Value!.AbsOrigin!.Z + (_playerIsCrouching[player] ? 46.03f : 64.03f)
            ),
            player.PlayerPawn.Value!.EyeAngles,
            player.PlayerPawn.Value!.AbsVelocity
        );
        
        entity.OuterAngle = 45f;
        entity.Enabled = true;
        entity.Color = Color.White;
        entity.ColorTemperature = 6500;
        entity.Brightness = 1f;
        entity.Range = 5000f;
        
        entity.DispatchSpawn();
        _playerFlashlight[player] = entity;
    }

    [ConsoleCommand("css_fl_toggle", "Toggles the flashlight")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void ToggleFlashlight(CCSPlayerController caller, CommandInfo? info)
    {
        if (!caller.IsValid || !caller.PawnIsAlive) return;

        if (_playerCanToggle[caller] == false) return;
        
        _playerUsingFlashlight[caller] = !_playerUsingFlashlight[caller];
        _playerCanToggle[caller] = false;
                        
        Instance?.AddTimer(0.25f, () =>
        {
            _playerCanToggle[caller] = true;
        });
    }
}