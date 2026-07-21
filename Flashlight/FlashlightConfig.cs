using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace Flashlight;

public class FlashlightConfig : BasePluginConfig
{
    [JsonPropertyName("Enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("AllowUseKey")]
    public bool AllowUseKey { get; set; } = true;

    /// <summary>
    /// Which team may use the flashlight: Any, CT, or T.
    /// </summary>
    [JsonPropertyName("AllowedTeam")]
    public string AllowedTeam { get; set; } = "Any";

    [JsonPropertyName("ToggleCooldownSeconds")]
    public float ToggleCooldownSeconds { get; set; } = 0.25f;

    [JsonPropertyName("Brightness")]
    public float Brightness { get; set; } = 1.0f;

    [JsonPropertyName("Range")]
    public float Range { get; set; } = 2048f;

    [JsonPropertyName("ColorR")]
    public byte ColorR { get; set; } = 255;

    [JsonPropertyName("ColorG")]
    public byte ColorG { get; set; } = 255;

    [JsonPropertyName("ColorB")]
    public byte ColorB { get; set; } = 255;

    [JsonPropertyName("ColorTemperature")]
    public float ColorTemperature { get; set; } = 6500f;

    [JsonPropertyName("CastShadows")]
    public bool CastShadows { get; set; } = true;

    [JsonPropertyName("SoftX")]
    public float SoftX { get; set; } = 1.0f;

    [JsonPropertyName("SoftY")]
    public float SoftY { get; set; } = 1.0f;

    [JsonPropertyName("Skirt")]
    public float Skirt { get; set; } = 0.5f;

    [JsonPropertyName("SkirtNear")]
    public float SkirtNear { get; set; } = 1.0f;

    [JsonPropertyName("SizeX")]
    public float SizeX { get; set; } = 45f;

    [JsonPropertyName("SizeY")]
    public float SizeY { get; set; } = 45f;

    [JsonPropertyName("SizeZ")]
    public float SizeZ { get; set; } = 0.03f;

    [JsonPropertyName("ForwardDistance")]
    public float ForwardDistance { get; set; } = 54f;

    [JsonPropertyName("StandEyeOffsetZ")]
    public float StandEyeOffsetZ { get; set; } = 64f;

    [JsonPropertyName("CrouchEyeOffsetZ")]
    public float CrouchEyeOffsetZ { get; set; } = 46f;

    [JsonPropertyName("AttachmentName")]
    public string AttachmentName { get; set; } = "axis_of_intent";

    [JsonPropertyName("LightCookie")]
    public string LightCookie { get; set; } = "materials/effects/lightcookies/flashlight.vtex";

    public void Clamp()
    {
        ToggleCooldownSeconds = Math.Max(0f, ToggleCooldownSeconds);
        Brightness = Math.Max(0f, Brightness);
        Range = Math.Max(1f, Range);
        ColorTemperature = Math.Clamp(ColorTemperature, 1000f, 12000f);
        SoftX = Math.Max(0f, SoftX);
        SoftY = Math.Max(0f, SoftY);
        Skirt = Math.Max(0f, Skirt);
        SkirtNear = Math.Max(0f, SkirtNear);
        SizeX = Math.Max(0f, SizeX);
        SizeY = Math.Max(0f, SizeY);
        SizeZ = Math.Max(0f, SizeZ);
        ForwardDistance = Math.Max(0f, ForwardDistance);
        StandEyeOffsetZ = Math.Max(0f, StandEyeOffsetZ);
        CrouchEyeOffsetZ = Math.Max(0f, CrouchEyeOffsetZ);

        if (string.IsNullOrWhiteSpace(AttachmentName))
        {
            AttachmentName = "axis_of_intent";
        }

        if (string.IsNullOrWhiteSpace(LightCookie))
        {
            LightCookie = "materials/effects/lightcookies/flashlight.vtex";
        }

        AllowedTeam = NormalizeAllowedTeam(AllowedTeam);
    }

    public static string NormalizeAllowedTeam(string? value)
    {
        return value?.Trim().ToUpperInvariant() switch
        {
            "T" or "TERRORIST" or "TERRORISTS" => "T",
            "CT" or "COUNTERTERRORIST" or "COUNTERTERRORISTS" or "COUNTER-TERRORIST" or "COUNTER-TERRORISTS" => "CT",
            _ => "Any"
        };
    }
}
