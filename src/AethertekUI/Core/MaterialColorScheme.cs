using System.Numerics;

namespace AethertekUI;

/// <summary>Immutable opaque RGB roles. Apply surface opacity at rendering time.</summary>
public sealed class MaterialColorScheme
{
    public Vector4 Primary { get; init; }
    public Vector4 OnPrimary { get; init; }
    public Vector4 PrimaryContainer { get; init; }
    public Vector4 OnPrimaryContainer { get; init; }
    public Vector4 Secondary { get; init; }
    public Vector4 OnSecondary { get; init; }
    public Vector4 SecondaryContainer { get; init; }
    public Vector4 OnSecondaryContainer { get; init; }
    public Vector4 Tertiary { get; init; }
    public Vector4 OnTertiary { get; init; }
    public Vector4 TertiaryContainer { get; init; }
    public Vector4 OnTertiaryContainer { get; init; }
    public Vector4 Error { get; init; }
    public Vector4 OnError { get; init; }
    public Vector4 ErrorContainer { get; init; }
    public Vector4 OnErrorContainer { get; init; }
    public Vector4 Background { get; init; }
    public Vector4 OnBackground { get; init; }
    public Vector4 Surface { get; init; }
    public Vector4 OnSurface { get; init; }
    public Vector4 SurfaceVariant { get; init; }
    public Vector4 OnSurfaceVariant { get; init; }
    public Vector4 SurfaceContainerLowest { get; init; }
    public Vector4 SurfaceContainerLow { get; init; }
    public Vector4 SurfaceContainer { get; init; }
    public Vector4 SurfaceContainerHigh { get; init; }
    public Vector4 SurfaceContainerHighest { get; init; }
    public Vector4 Outline { get; init; }
    public Vector4 OutlineVariant { get; init; }
    public Vector4 InverseSurface { get; init; }
    public Vector4 InverseOnSurface { get; init; }
    public Vector4 InversePrimary { get; init; }
    public Vector4 Scrim { get; init; }
    public MaterialColorScheme(MaterialPalette palette)
    {
        Primary = palette.Primary[80];
        OnPrimary = palette.Primary[20];
        PrimaryContainer = palette.Primary[32];
        OnPrimaryContainer = palette.Primary[92];
        Secondary = palette.Secondary[80];
        OnSecondary = palette.Secondary[20];
        SecondaryContainer = palette.Secondary[32];
        OnSecondaryContainer = palette.Secondary[92];
        Tertiary = palette.Tertiary[80];
        OnTertiary = palette.Tertiary[20];
        TertiaryContainer = palette.Tertiary[32];
        OnTertiaryContainer = palette.Tertiary[92];
        Error = palette.Error[80];
        OnError = palette.Error[20];
        ErrorContainer = palette.Error[32];
        OnErrorContainer = palette.Error[92];
        Background = palette.Neutral[14];
        OnBackground = palette.Neutral[93];
        Surface = palette.Neutral[18];
        OnSurface = palette.Neutral[93];
        SurfaceVariant = palette.NeutralVariant[32];
        OnSurfaceVariant = palette.NeutralVariant[78];
        SurfaceContainerLowest = palette.Neutral[12];
        SurfaceContainerLow = palette.Neutral[20];
        SurfaceContainer = palette.Neutral[24];
        SurfaceContainerHigh = palette.Neutral[28];
        SurfaceContainerHighest = palette.Neutral[32];
        Outline = palette.NeutralVariant[57];
        OutlineVariant = palette.NeutralVariant[36];
        InverseSurface = palette.Neutral[90];
        InverseOnSurface = palette.Neutral[20];
        InversePrimary = palette.Primary[40];
        Scrim = palette.Neutral[0];
    }
}
