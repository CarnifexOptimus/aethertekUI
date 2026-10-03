using System.Numerics;

namespace AethertekUI;

/// <summary>Consumer-owned colors and logical corner radius. Geometry still comes from control metrics.</summary>
public readonly record struct MaterialControlAppearance(Vector4 Fill, Vector4 Foreground, Vector4 Border, float CornerRadius = 4)
{
    internal float Radius(float scale)
        => float.IsFinite(CornerRadius) && CornerRadius >= 0
            ? CornerRadius * scale : throw new ArgumentOutOfRangeException(nameof(CornerRadius));
}
