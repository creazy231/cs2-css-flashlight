using System.Numerics;
using Xunit;

namespace Flashlight.Tests;

public class FlashlightLogicTests
{
    [Fact]
    public void TryToggle_TogglesWhenAllowed()
    {
        var isOn = false;
        var canToggle = true;

        Assert.True(FlashlightLogic.TryToggle(ref isOn, ref canToggle));
        Assert.True(isOn);
        Assert.False(canToggle);
    }

    [Fact]
    public void TryToggle_BlockedDuringCooldown()
    {
        var isOn = true;
        var canToggle = false;

        Assert.False(FlashlightLogic.TryToggle(ref isOn, ref canToggle));
        Assert.True(isOn);
        Assert.False(canToggle);
    }

    [Fact]
    public void IsUsePressedEdge_OnlyTrueOnRisingEdge()
    {
        Assert.True(FlashlightLogic.IsUsePressedEdge(true, false));
        Assert.False(FlashlightLogic.IsUsePressedEdge(true, true));
        Assert.False(FlashlightLogic.IsUsePressedEdge(false, true));
        Assert.False(FlashlightLogic.IsUsePressedEdge(false, false));
    }

    [Theory]
    [InlineData(false, 64f, 46f, 64f)]
    [InlineData(true, 64f, 46f, 46f)]
    public void GetEyeOffsetZ_UsesCrouchWhenDucking(
        bool crouching,
        float stand,
        float crouch,
        float expected)
    {
        Assert.Equal(expected, FlashlightLogic.GetEyeOffsetZ(crouching, stand, crouch));
    }

    [Fact]
    public void CalculateLightOrigin_AppliesEyeAndForwardOffsets()
    {
        var origin = FlashlightLogic.CalculateLightOrigin(
            new Vector3(10f, 20f, 30f),
            new Vector3(1f, 0f, 0f),
            eyeOffsetZ: 64f,
            forwardDistance: 54f);

        Assert.Equal(new Vector3(64f, 20f, 94f), origin);
    }

    [Fact]
    public void ForwardFromAnglesDegrees_YawZeroLooksAlongX()
    {
        var forward = FlashlightLogic.ForwardFromAnglesDegrees(0f, 0f);

        Assert.Equal(1f, forward.X, 3);
        Assert.Equal(0f, forward.Y, 3);
        Assert.Equal(0f, forward.Z, 3);
    }

    [Fact]
    public void ShouldCreateAndDestroyLight_Policies()
    {
        Assert.True(FlashlightLogic.ShouldCreateLight(isOn: true, hasValidLight: false));
        Assert.False(FlashlightLogic.ShouldCreateLight(isOn: true, hasValidLight: true));
        Assert.True(FlashlightLogic.ShouldDestroyLight(isOn: false, hasValidLight: true));
        Assert.False(FlashlightLogic.ShouldDestroyLight(isOn: false, hasValidLight: false));
    }

    [Fact]
    public void ConfigClamp_FixesInvalidValues()
    {
        var config = new FlashlightConfig
        {
            ToggleCooldownSeconds = -1f,
            Brightness = -5f,
            Range = 0f,
            ColorTemperature = 50f,
            SoftX = -1f,
            SoftY = -1f,
            Skirt = -1f,
            SkirtNear = -1f,
            SizeX = -1f,
            SizeY = -1f,
            SizeZ = -1f,
            ForwardDistance = -10f,
            StandEyeOffsetZ = -1f,
            CrouchEyeOffsetZ = -1f,
            AttachmentName = " ",
            LightCookie = ""
        };

        config.Clamp();

        Assert.Equal(0f, config.ToggleCooldownSeconds);
        Assert.Equal(0f, config.Brightness);
        Assert.Equal(1f, config.Range);
        Assert.Equal(1000f, config.ColorTemperature);
        Assert.Equal(0f, config.SoftX);
        Assert.Equal(0f, config.SoftY);
        Assert.Equal(0f, config.Skirt);
        Assert.Equal(0f, config.SkirtNear);
        Assert.Equal(0f, config.SizeX);
        Assert.Equal(0f, config.SizeY);
        Assert.Equal(0f, config.SizeZ);
        Assert.Equal(0f, config.ForwardDistance);
        Assert.Equal(0f, config.StandEyeOffsetZ);
        Assert.Equal(0f, config.CrouchEyeOffsetZ);
        Assert.Equal("axis_of_intent", config.AttachmentName);
        Assert.Equal("materials/effects/lightcookies/flashlight.vtex", config.LightCookie);
    }

    [Theory]
    [InlineData("Any", 2, true)]
    [InlineData("Any", 3, true)]
    [InlineData("Any", 1, true)]
    [InlineData("T", 2, true)]
    [InlineData("T", 3, false)]
    [InlineData("CT", 3, true)]
    [InlineData("CT", 2, false)]
    public void IsTeamAllowed_RespectsConfiguredSide(string allowedTeam, byte team, bool expected)
    {
        Assert.Equal(expected, FlashlightLogic.IsTeamAllowed(allowedTeam, team));
    }

    [Theory]
    [InlineData("t", "T")]
    [InlineData("Terrorist", "T")]
    [InlineData("ct", "CT")]
    [InlineData("CounterTerrorist", "CT")]
    [InlineData("any", "Any")]
    [InlineData("something-else", "Any")]
    [InlineData(null, "Any")]
    public void ConfigClamp_NormalizesAllowedTeam(string? input, string expected)
    {
        var config = new FlashlightConfig { AllowedTeam = input! };
        config.Clamp();
        Assert.Equal(expected, config.AllowedTeam);
    }
}
