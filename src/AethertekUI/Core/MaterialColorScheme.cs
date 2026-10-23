using System.Numerics;

namespace AethertekUI;

/// <summary>Immutable opaque RGB roles. Apply surface opacity at rendering time.</summary>
public sealed class MaterialColorScheme
{
    internal bool IsLcars { get; init; }
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

    /// <summary>Three explicit colour families; semantic error colours retain their original meaning.</summary>
    internal MaterialColorScheme(MaterialColorScheme automatic, Vector3 background, Vector3 controls, Vector3 text)
    {
        var backdrop = new Vector4(background, 1);
        var control = new Vector4(controls, 1);
        var foreground = new Vector4(text, 1);
        var reference = MaterialColor.SrgbToOklab(new(automatic.Background.X, automatic.Background.Y, automatic.Background.Z)).X;
        var selected = MaterialColor.LabToLch(MaterialColor.SrgbToOklab(background));
        Vector4 SurfaceTone(Vector4 role)
        {
            var lightness = MaterialColor.SrgbToOklab(new(role.X, role.Y, role.Z)).X;
            var offset = lightness - reference;
            // Keep a visible surface hierarchy for light backgrounds too. The chosen base stays exact.
            if (selected.X > .7f) offset = -offset;
            return new(MaterialColor.GamutMap(selected.X + offset, selected.Y, selected.Z), 1);
        }

        Background = backdrop;
        OnBackground = OnSurface = OnSurfaceVariant = foreground;
        Surface = SurfaceTone(automatic.Surface);
        SurfaceContainerLowest = SurfaceTone(automatic.SurfaceContainerLowest);
        SurfaceContainerLow = SurfaceTone(automatic.SurfaceContainerLow);
        SurfaceContainer = SurfaceTone(automatic.SurfaceContainer);
        SurfaceContainerHigh = SurfaceTone(automatic.SurfaceContainerHigh);
        SurfaceContainerHighest = SurfaceTone(automatic.SurfaceContainerHighest);
        SurfaceVariant = SurfaceTone(automatic.SurfaceVariant);
        Primary = Secondary = Tertiary = control;
        SecondaryContainer = control;
        PrimaryContainer = MaterialColor.Layer(backdrop, control, .65f);
        TertiaryContainer = MaterialColor.Layer(backdrop, control, .45f);
        OnPrimary = OnSecondary = OnTertiary = foreground;
        OnPrimaryContainer = OnSecondaryContainer = OnTertiaryContainer = foreground;
        Outline = MaterialColor.Layer(backdrop, control, .8f);
        OutlineVariant = MaterialColor.Layer(backdrop, control, .5f);
        InverseSurface = foreground;
        InverseOnSurface = backdrop;
        InversePrimary = control;
        Error = automatic.Error;
        OnError = automatic.OnError;
        ErrorContainer = automatic.ErrorContainer;
        OnErrorContainer = automatic.OnErrorContainer;
        Scrim = automatic.Scrim;
    }

    internal MaterialColorScheme(MaterialColorScheme automatic)
        : this(automatic, MaterialColorSettings.Rgb(0x07090D), MaterialColorSettings.Rgb(0xFFAA66), MaterialColorSettings.Rgb(0xF4F1EA))
    {
        IsLcars = true;
        Surface = SurfaceContainerLowest = SurfaceContainerLow = Background;
        SurfaceContainer = new(.045f, .047f, .063f, 1);
        SurfaceContainerHigh = new(.07f, .07f, .10f, 1);
        SurfaceContainerHighest = SurfaceVariant = new(.105f, .105f, .15f, 1);
        OnSurfaceVariant = new(.77f, .77f, .82f, 1);
        Primary = MaterialLcars.Orange;
        Secondary = SecondaryContainer = MaterialLcars.Lilac;
        Tertiary = MaterialLcars.Periwinkle;
        PrimaryContainer = new(.27f, .16f, .12f, 1);
        TertiaryContainer = new(.16f, .16f, .29f, 1);
        OnPrimary = OnSecondary = OnSecondaryContainer = OnTertiary = Background;
        OnPrimaryContainer = OnTertiaryContainer = OnSurface;
        Outline = MaterialLcars.Lilac;
        OutlineVariant = new(.29f, .25f, .34f, 1);
        InversePrimary = Primary;
    }

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
