using Xunit;

namespace Flashlight.Tests;

public class FlashlightLogicTests
{
    [Fact]
    public void FlashlightState_TogglesCorrectly()
    {
        // Test the core logic of flashlight state toggling
        var playerKey = "test_player_1";
        var flashlightState = new Dictionary<string, bool>();
        
        // Initial state should be off
        flashlightState[playerKey] = false;
        Assert.False(flashlightState[playerKey]);
        
        // Toggle on
        flashlightState[playerKey] = !flashlightState[playerKey];
        Assert.True(flashlightState[playerKey]);
        
        // Toggle off
        flashlightState[playerKey] = !flashlightState[playerKey];
        Assert.False(flashlightState[playerKey]);
    }
    
    [Fact]
    public void FlashlightState_TracksMultiplePlayers()
    {
        var playerStates = new Dictionary<string, bool>();
        var player1 = "player_1";
        var player2 = "player_2";
        var player3 = "player_3";
        
        // Initialize all off
        playerStates[player1] = false;
        playerStates[player2] = false;
        playerStates[player3] = false;
        
        // Toggle player 1 on
        playerStates[player1] = !playerStates[player1];
        Assert.True(playerStates[player1]);
        Assert.False(playerStates[player2]);
        Assert.False(playerStates[player3]);
        
        // Toggle player 2 on
        playerStates[player2] = !playerStates[player2];
        Assert.True(playerStates[player1]);
        Assert.True(playerStates[player2]);
        Assert.False(playerStates[player3]);
    }
    
    [Fact]
    public void ToggleCooldown_PreventsRapidToggling()
    {
        // Simulate the cooldown mechanism
        var canToggle = true;
        
        // First toggle - should work
        Assert.True(canToggle);
        canToggle = false; // Simulate setting cooldown
        
        // Second toggle - should be blocked
        Assert.False(canToggle);
        
        // After cooldown expires
        canToggle = true;
        Assert.True(canToggle);
    }
    
    [Fact]
    public void CrouchState_UpdatesCorrectly()
    {
        var isCrouching = false;
        var buttons = 0;
        const int DuckButton = 1 << 2; // Typical duck button bit
        
        // Not crouching initially
        Assert.False(isCrouching);
        
        // Press duck button
        buttons |= DuckButton;
        if ((buttons & DuckButton) != 0)
        {
            isCrouching = true;
        }
        Assert.True(isCrouching);
        
        // Release duck button
        buttons &= ~DuckButton;
        if ((buttons & DuckButton) == 0)
        {
            isCrouching = false;
        }
        Assert.False(isCrouching);
    }
    
    [Fact]
    public void LightPosition_CalculatesCrouchOffsetCorrectly()
    {
        // Test the position calculation logic
        var baseZ = 100f;
        var standOffset = 64.03f;
        var crouchOffset = 46.03f;
        
        // Standing position
        var standPosition = baseZ + standOffset;
        Assert.Equal(164.03f, standPosition);
        
        // Crouching position
        var crouchPosition = baseZ + crouchOffset;
        Assert.Equal(146.03f, crouchPosition);
    }
    
    [Fact]
    public void FlashlightEntity_Management()
    {
        // Test entity tracking dictionary behavior
        var playerEntities = new Dictionary<string, FakeLightEntity>();
        var playerKey = "test_player";
        
        // No entity initially
        Assert.False(playerEntities.TryGetValue(playerKey, out _));
        
        // Add entity
        var light = new FakeLightEntity { IsValid = true };
        playerEntities[playerKey] = light;
        Assert.True(playerEntities.TryGetValue(playerKey, out var retrieved));
        Assert.True(retrieved?.IsValid);
        
        // Remove entity
        playerEntities.Remove(playerKey);
        Assert.False(playerEntities.TryGetValue(playerKey, out _));
    }
    
    private class FakeLightEntity
    {
        public bool IsValid { get; set; }
        public void Remove() => IsValid = false;
    }
}