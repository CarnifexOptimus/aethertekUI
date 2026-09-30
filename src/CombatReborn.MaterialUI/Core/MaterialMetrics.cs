using System.Numerics;

namespace CombatReborn.MaterialUI;

public enum MaterialDensity { Compact, Standard }

/// <summary>Pixel metrics for the current scale. Caller sizes use logical units.</summary>
public readonly record struct MaterialMetrics(MaterialDensity Density, float Scale)
{
    private float D(float compact, float standard) => (Density == MaterialDensity.Compact ? compact : standard)*Scale;
    public float ButtonHeight => D(28,36);
    public float ChipHeight => D(24,30);
    public float IconSize => D(16,20);
    public float Gap => D(6,8);
    public float Padding => D(12,16);
    public float SectionGap => D(14,20);
    public float Radius => D(12,16);
    public float OuterRadius => D(18,22);
    public float Border => Scale;
    public float RailWidth => D(156,184);
    public float CompactRailWidth => D(56,64);
    public float TileHeight => D(86,104);
    public Vector2 CellPadding => new(8*Scale, D(3,5));
    public Vector2 ItemSpacing => new(Gap,Gap);
    public Vector2 FramePadding => new(Padding, D(4,7));
    public Vector2 SwitchSize => new(D(40,48), D(22,26));
    public float ProgressHeight => D(6,8);
    public Vector2 Scaled(Vector2 size) => new(MaterialGeometry.ScaleDimension(size.X,Scale), MaterialGeometry.ScaleDimension(size.Y,Scale));
}

public static class MaterialGeometry
{
    public static float ScaleDimension(float value, float scale) => value > 0 ? value*scale : value;
    public static float PillRadius(float height) => Math.Max(height,0)*.5f;
    public static (float Start, float Width) SegmentBounds(float width, int count, int index)
    {
        if (count < 2 || index < 0 || index >= count || !float.IsFinite(width) || width < 0) throw new ArgumentOutOfRangeException(nameof(count));
        var start = width * index / count;
        return (start, width * (index+1) / count - start);
    }
}
