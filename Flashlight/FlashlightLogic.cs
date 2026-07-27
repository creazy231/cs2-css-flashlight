using System.Numerics;

namespace Flashlight;

/// <summary>
/// Position and orientation to apply to the flashlight entity.
/// <paramref name="Angles"/> is a Source QAngle laid out as (pitch, yaw, roll).
/// </summary>
public readonly record struct LightTransform(Vector3 Origin, Vector3 Angles);

public static class FlashlightLogic
{
    public static bool TryToggle(ref bool isOn, ref bool canToggle)
    {
        if (!canToggle)
        {
            return false;
        }

        isOn = !isOn;
        canToggle = false;
        return true;
    }

    public static bool IsUsePressedEdge(bool usePressed, bool wasUsePressed)
    {
        return usePressed && !wasUsePressed;
    }

    public static float GetEyeOffsetZ(bool isCrouching, float standOffset, float crouchOffset)
    {
        return isCrouching ? crouchOffset : standOffset;
    }

    public static Vector3 CalculateLightOrigin(
        Vector3 pawnOrigin,
        Vector3 forward,
        float eyeOffsetZ,
        float forwardDistance)
    {
        return new Vector3(
            pawnOrigin.X + forward.X * forwardDistance,
            pawnOrigin.Y + forward.Y * forwardDistance,
            pawnOrigin.Z + eyeOffsetZ + forward.Z * forwardDistance);
    }

    public static Vector3 ForwardFromAnglesDegrees(float pitch, float yaw)
    {
        var pitchRad = pitch * (MathF.PI / 180f);
        var yawRad = yaw * (MathF.PI / 180f);
        var cosPitch = MathF.Cos(pitchRad);

        return new Vector3(
            cosPitch * MathF.Cos(yawRad),
            cosPitch * MathF.Sin(yawRad),
            -MathF.Sin(pitchRad));
    }

    /// <summary>
    /// Forward vector on the horizontal plane only, ignoring pitch.
    /// </summary>
    public static Vector3 HorizontalForwardFromYawDegrees(float yaw)
    {
        var yawRad = yaw * (MathF.PI / 180f);

        return new Vector3(MathF.Cos(yawRad), MathF.Sin(yawRad), 0f);
    }

    /// <summary>
    /// World transform for the flashlight given the player's current pawn origin and view angles.
    /// </summary>
    /// <remarks>
    /// The angles carry the full view rotation so the beam tracks pitch as well as yaw, while the
    /// origin is only pushed forward on the horizontal plane. Offsetting the origin along the full
    /// pitched forward vector would drop the light through the floor when looking straight down
    /// (a crouched eye height of 46 minus a 54 unit offset ends up below the ground).
    /// </remarks>
    public static LightTransform CalculateLightTransform(
        Vector3 pawnOrigin,
        float pitch,
        float yaw,
        float roll,
        float eyeOffsetZ,
        float forwardDistance)
    {
        var forward = HorizontalForwardFromYawDegrees(yaw);
        var origin = CalculateLightOrigin(pawnOrigin, forward, eyeOffsetZ, forwardDistance);

        return new LightTransform(origin, new Vector3(pitch, yaw, roll));
    }

    public static bool ShouldCreateLight(bool isOn, bool hasValidLight)
    {
        return isOn && !hasValidLight;
    }

    public static bool ShouldDestroyLight(bool isOn, bool hasValidLight)
    {
        return !isOn && hasValidLight;
    }

    /// <summary>
    /// Returns whether <paramref name="team"/> may use the flashlight.
    /// Team values match CS2: 2 = Terrorist, 3 = Counter-Terrorist.
    /// </summary>
    public static bool IsTeamAllowed(string allowedTeam, byte team)
    {
        return allowedTeam switch
        {
            "T" => team == 2,
            "CT" => team == 3,
            _ => true
        };
    }
}
