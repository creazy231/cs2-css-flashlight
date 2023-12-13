using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace Flashlight;

public class Flashlight : BasePlugin
{
    public override string ModuleAuthor => "creazy.eth";
    public override string ModuleName => "Flashlight";
    public override string ModuleDescription => "Flashlight for Counter-Strike 2";
    public override string ModuleVersion => "0.0.1";

    private static string ModuleDisplayName => "Flashlight";
    
    // TODO: Add config and make light entity values configurable
    // TODO: Maybe replace light_omni2 with light_rect or something else

    public static Flashlight? Instance { get; private set; }
    
    private readonly List<CCSPlayerController> _connectedPlayers = new();
    private readonly Dictionary<CCSPlayerController, bool> _playerUsingFlashlight = new();
    private readonly Dictionary<CCSPlayerController, bool> _playerCanToggle = new();
    private readonly Dictionary<CCSPlayerController, COmniLight> _playerFlashlight = new();

    public override void Load(bool hotReload)
    {
        LogHelper.LogToConsole(ConsoleColor.Green, $"{ModuleName} version {ModuleVersion} loaded");

        Instance = this;
        
        RegisterListener<Listeners.OnTick>(() =>
        {
            foreach (var player in _connectedPlayers.Where(player => player is { IsValid: true, IsBot: false, PawnIsAlive: true }))
            {
                ToggleFlashlight(player, null);
                
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
            }
        });

        LogHelper.LogToChatAll($"{ModuleDisplayName} v{ModuleVersion} loaded!");
    }
    
    [GameEventHandler]
    public HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
    {
        var player = @event.Userid;
        
        if (!player.IsValid || player.IsBot) return HookResult.Continue;
        
        _connectedPlayers.Add(player);
        _playerUsingFlashlight[player] = false;
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
        _playerCanToggle.Remove(player);
        
        LogHelper.LogToConsole(ConsoleColor.Green, $"{player.PlayerName} disconnected");
        
        return HookResult.Continue;
    }
    
    [GameEventHandler]
    public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;
        
        if (!player.IsValid || player.IsBot) return HookResult.Continue;
        
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

    [ConsoleCommand("fl_toggle", "Toggles the flashlight")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void ToggleFlashlight(CCSPlayerController caller, CommandInfo? info)
    {
        if (_playerUsingFlashlight[caller] == false)
        {
            if (_playerFlashlight.TryGetValue(caller, out var value))
            {
                value.Remove();
                _playerFlashlight.Remove(caller);
            }
            
            return;
        }
        
        var entity = _playerFlashlight.TryGetValue(caller, out var flashlight) ? flashlight : Utilities.CreateEntityByName<COmniLight>("light_omni2");

        if (entity == null || !entity.IsValid)
        {
            LogHelper.LogToConsole("Failed to create entity!");
            return;
        }
        
        entity.DirectLight = 3;
        
        entity.Teleport(
            new Vector(
                caller.PlayerPawn.Value!.AbsOrigin!.X,
                caller.PlayerPawn.Value!.AbsOrigin!.Y,
                caller.PlayerPawn.Value!.AbsOrigin!.Z + 64.03f
            ),
            caller.PlayerPawn.Value!.EyeAngles,
            caller.PlayerPawn.Value!.AbsVelocity
        );
        
        entity.OuterAngle = 45f;
        entity.Enabled = true;
        entity.Color = Color.White;
        entity.ColorTemperature = 6500;
        entity.Brightness = 1f;
        entity.Range = 5000f;
        
        entity.DispatchSpawn();
        _playerFlashlight[caller] = entity;
    }
}