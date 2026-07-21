using System.Numerics;

namespace Flashlight;

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

    public static bool ShouldCreateLight(bool isOn, bool hasValidLight)
    {
        return isOn && !hasValidLight;
    }

    public static bool ShouldDestroyLight(bool isOn, bool hasValidLight)
    {
        return !isOn && hasValidLight;
    }
}
