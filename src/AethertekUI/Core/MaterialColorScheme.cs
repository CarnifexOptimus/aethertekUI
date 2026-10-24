using System.Numerics;

namespace AethertekUI;

/// <summary>Immutable opaque RGB roles. Apply surface opacity at rendering time.</summary>
public sealed class MaterialColorScheme
{
    internal bool IsLcars { get; init; }
    internal MaterialColorSettings? AppearanceSettings { get; init; }
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
        : this(automatic, new MaterialColorSettings { Mode = MaterialColorMode.Lcars }) { }

    /// <summary>Approved material palettes and their relative roles; saved base colours remain exact.</summary>
    internal MaterialColorScheme(MaterialColorScheme automatic, MaterialColorSettings settings)
    {
        AppearanceSettings = settings;
        IsLcars = settings.Mode == MaterialColorMode.Lcars;
        var palette = IsLcars ? default : settings.GetPalette(settings.Mode);
        var background = new Vector4(MaterialColorSettings.Rgb(IsLcars ? settings.LcarsBackgroundRgb : palette.BackgroundRgb), 1);
        var primary = new Vector4(MaterialColorSettings.Rgb(IsLcars ? settings.LcarsBar1Rgb : palette.ControlsRgb), 1);
        var text = IsLcars ? ReadableInk(background) : new Vector4(MaterialColorSettings.Rgb(palette.TextRgb), 1);
        static Vector4 Mix(Vector4 a, Vector4 b, float amount) => Vector4.Lerp(a, b, amount) with { W = 1 };
        Vector4 Rotate(float angle, float chroma = 1)
        {
            var lch = MaterialColor.LabToLch(MaterialColor.SrgbToOklab(new(primary.X, primary.Y, primary.Z)));
            return new(MaterialColor.GamutMap(lch.X, lch.Y * chroma, lch.Z + angle), 1);
        }

        var light = ReadableInk(background).X == 0;
        var surface = IsLcars ? background : Mix(background, Vector4.One, light ? .55f : .045f);
        if (settings.Mode == MaterialColorMode.Cyberpunk) surface = Mix(surface, primary, .045f);
        var well = IsLcars ? background : Mix(background, new(0, 0, 0, 1), light ? .045f : .18f);
        var secondary = IsLcars ? new Vector4(MaterialColorSettings.Rgb(settings.LcarsBar2Rgb), 1)
            : settings.Mode == MaterialColorMode.Cyberpunk ? Rotate(2.35f, 1.5f) : Rotate(1.2f);
        var tertiary = IsLcars ? new Vector4(MaterialColorSettings.Rgb(settings.LcarsBar3Rgb), 1)
            : Rotate(settings.Mode == MaterialColorMode.Cyberpunk ? 1.2f : 2.35f);

        Background = background;
        OnBackground = text;
        Surface = surface;
        OnSurface = text;
        SurfaceContainerLowest = well;
        SurfaceContainerLow = surface;
        SurfaceContainer = IsLcars ? background : Mix(surface, text, .025f);
        SurfaceContainerHigh = Mix(surface, text, .055f);
        SurfaceContainerHighest = Mix(surface, text, .1f);
        SurfaceVariant = Mix(surface, text, .16f);
        OnSurfaceVariant = IsLcars ? ReadableInk(SurfaceVariant) : Mix(text, background, .23f);
        Outline = Mix(background, primary, .5f);
        OutlineVariant = Mix(background, primary, .25f);
        Primary = primary;
        OnPrimary = ReadableInk(primary);
        PrimaryContainer = IsLcars ? background : Mix(surface, primary, .1f);
        OnPrimaryContainer = text;
        Secondary = secondary;
        OnSecondary = ReadableInk(secondary);
        SecondaryContainer = IsLcars ? secondary : Mix(surface, primary, .18f);
        OnSecondaryContainer = IsLcars ? ReadableInk(SecondaryContainer) : text;
        Tertiary = tertiary;
        OnTertiary = ReadableInk(tertiary);
        TertiaryContainer = Mix(surface, tertiary, .12f);
        OnTertiaryContainer = IsLcars ? ReadableInk(TertiaryContainer) : text;
        InverseSurface = text;
        InverseOnSurface = background;
        InversePrimary = primary;
        Error = automatic.Error;
        OnError = automatic.OnError;
        ErrorContainer = automatic.ErrorContainer;
        OnErrorContainer = automatic.OnErrorContainer;
        Scrim = automatic.Scrim;
    }

    private static Vector4 ReadableInk(Vector4 background)
    {
        var black = new Vector4(0, 0, 0, 1);
        return MaterialColor.Contrast(background, black) >= MaterialColor.Contrast(background, Vector4.One) ? black : Vector4.One;
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
