using System.Numerics;

namespace AethertekUI;

public enum MaterialColorMode { Auto = 0, Picker = 1, Lcars = 2, Glassmorphism = 3, Spatial = 4, Cyberpunk = 5 }

/// <summary>Consumer-owned appearance choices. Auto retains the supplied automatic scheme unchanged.</summary>
public sealed record MaterialColorSettings
{
    public const uint DefaultGlassWave1Rgb = 0x65C7D2;
    public static readonly uint DefaultGlassWave2Rgb = DefaultGlassWaveRgb(1.2f);
    public static readonly uint DefaultGlassWave3Rgb = DefaultGlassWaveRgb(2.35f);
    public const uint DefaultLcarsBackgroundRgb = 0x07090D;
    public const uint DefaultLcarsBar1Rgb = 0xFFAA66;
    public const uint DefaultLcarsBar2Rgb = 0xCC99CC;
    public const uint DefaultLcarsBar3Rgb = 0x9999FF;

    public MaterialColorMode Mode { get; init; }
    public uint BackgroundRgb { get; init; } = 0x132334;
    public uint ControlsRgb { get; init; } = 0x365C84;
    public uint TextRgb { get; init; } = 0xE5F2FF;
    public uint GlassBackgroundRgb { get; init; } = 0x10232D;
    public uint GlassControlsRgb { get; init; } = DefaultGlassWave1Rgb;
    public uint GlassTextRgb { get; init; } = 0xEDF7FA;
    public uint SpatialBackgroundRgb { get; init; } = 0xE8EDF4;
    public uint SpatialControlsRgb { get; init; } = 0x6678A8;
    public uint SpatialTextRgb { get; init; } = 0x1C2538;
    public uint CyberpunkBackgroundRgb { get; init; } = 0x0B0D13;
    public uint CyberpunkControlsRgb { get; init; } = 0x19DCE8;
    public uint CyberpunkTextRgb { get; init; } = 0xEAF8FF;
    public uint LcarsBackgroundRgb { get; init; } = DefaultLcarsBackgroundRgb;
    public uint LcarsBar1Rgb { get; init; } = DefaultLcarsBar1Rgb;
    public uint LcarsBar2Rgb { get; init; } = DefaultLcarsBar2Rgb;
    public uint LcarsBar3Rgb { get; init; } = DefaultLcarsBar3Rgb;
    public bool GlassAnimation { get; init; } = true;
    private float glassAnimationSpeed = 1;
    public float GlassAnimationSpeed
    {
        get => glassAnimationSpeed;
        init => glassAnimationSpeed = NormalizeGlassAnimationSpeed(value);
    }
    public uint GlassWave1Rgb { get; init; } = DefaultGlassWave1Rgb;
    public uint GlassWave2Rgb { get; init; } = DefaultGlassWave2Rgb;
    public uint GlassWave3Rgb { get; init; } = DefaultGlassWave3Rgb;

    /// <summary>The independently saved three-colour palette. Auto and LCARS have different role contracts.</summary>
    public (uint BackgroundRgb, uint ControlsRgb, uint TextRgb) GetPalette(MaterialColorMode mode) => mode switch
    {
        MaterialColorMode.Picker => (BackgroundRgb, ControlsRgb, TextRgb),
        MaterialColorMode.Glassmorphism => (GlassBackgroundRgb, GlassControlsRgb, GlassTextRgb),
        MaterialColorMode.Spatial => (SpatialBackgroundRgb, SpatialControlsRgb, SpatialTextRgb),
        MaterialColorMode.Cyberpunk => (CyberpunkBackgroundRgb, CyberpunkControlsRgb, CyberpunkTextRgb),
        _ => throw new ArgumentOutOfRangeException(nameof(mode)),
    };

    /// <summary>Changes only the named palette, retaining the selected mode and all other choices.</summary>
    public MaterialColorSettings WithPalette(MaterialColorMode mode, uint backgroundRgb, uint controlsRgb, uint textRgb)
    {
        ValidateRgb(backgroundRgb, nameof(backgroundRgb));
        ValidateRgb(controlsRgb, nameof(controlsRgb));
        ValidateRgb(textRgb, nameof(textRgb));
        return mode switch
        {
            MaterialColorMode.Picker => this with { BackgroundRgb = backgroundRgb, ControlsRgb = controlsRgb, TextRgb = textRgb },
            MaterialColorMode.Glassmorphism => this with { GlassBackgroundRgb = backgroundRgb, GlassControlsRgb = controlsRgb, GlassTextRgb = textRgb },
            MaterialColorMode.Spatial => this with { SpatialBackgroundRgb = backgroundRgb, SpatialControlsRgb = controlsRgb, SpatialTextRgb = textRgb },
            MaterialColorMode.Cyberpunk => this with { CyberpunkBackgroundRgb = backgroundRgb, CyberpunkControlsRgb = controlsRgb, CyberpunkTextRgb = textRgb },
            _ => throw new ArgumentOutOfRangeException(nameof(mode)),
        };
    }

    public static (uint BackgroundRgb, uint ControlsRgb, uint TextRgb) DefaultPalette(MaterialColorMode mode) => mode switch
    {
        MaterialColorMode.Picker => (0x132334, 0x365C84, 0xE5F2FF),
        MaterialColorMode.Glassmorphism => (0x10232D, DefaultGlassWave1Rgb, 0xEDF7FA),
        MaterialColorMode.Spatial => (0xE8EDF4, 0x6678A8, 0x1C2538),
        MaterialColorMode.Cyberpunk => (0x0B0D13, 0x19DCE8, 0xEAF8FF),
        _ => throw new ArgumentOutOfRangeException(nameof(mode)),
    };

    public static float NormalizeGlassAnimationSpeed(float value) => float.IsFinite(value) ? Math.Clamp(value, .1f, 10f) : 1;

    public void Validate()
    {
        if (!Enum.IsDefined(Mode)) throw new ArgumentOutOfRangeException(nameof(Mode));
        ValidateRgb(BackgroundRgb, nameof(BackgroundRgb));
        ValidateRgb(ControlsRgb, nameof(ControlsRgb));
        ValidateRgb(TextRgb, nameof(TextRgb));
        ValidateRgb(GlassBackgroundRgb, nameof(GlassBackgroundRgb));
        ValidateRgb(GlassControlsRgb, nameof(GlassControlsRgb));
        ValidateRgb(GlassTextRgb, nameof(GlassTextRgb));
        ValidateRgb(SpatialBackgroundRgb, nameof(SpatialBackgroundRgb));
        ValidateRgb(SpatialControlsRgb, nameof(SpatialControlsRgb));
        ValidateRgb(SpatialTextRgb, nameof(SpatialTextRgb));
        ValidateRgb(CyberpunkBackgroundRgb, nameof(CyberpunkBackgroundRgb));
        ValidateRgb(CyberpunkControlsRgb, nameof(CyberpunkControlsRgb));
        ValidateRgb(CyberpunkTextRgb, nameof(CyberpunkTextRgb));
        ValidateRgb(LcarsBackgroundRgb, nameof(LcarsBackgroundRgb));
        ValidateRgb(LcarsBar1Rgb, nameof(LcarsBar1Rgb));
        ValidateRgb(LcarsBar2Rgb, nameof(LcarsBar2Rgb));
        ValidateRgb(LcarsBar3Rgb, nameof(LcarsBar3Rgb));
        ValidateRgb(GlassWave1Rgb, nameof(GlassWave1Rgb));
        ValidateRgb(GlassWave2Rgb, nameof(GlassWave2Rgb));
        ValidateRgb(GlassWave3Rgb, nameof(GlassWave3Rgb));
    }

    public MaterialColorScheme Apply(MaterialColorScheme automatic)
    {
        ArgumentNullException.ThrowIfNull(automatic);
        Validate();
        return Mode switch
        {
            MaterialColorMode.Auto => automatic,
            MaterialColorMode.Picker => new MaterialColorScheme(automatic, Rgb(BackgroundRgb), Rgb(ControlsRgb), Rgb(TextRgb))
                { AppearanceSettings = this },
            _ => new MaterialColorScheme(automatic, this),
        };
    }

    private static void ValidateRgb(uint rgb, string name)
    {
        if (rgb > 0xFFFFFF) throw new ArgumentOutOfRangeException(name);
    }

    private static uint DefaultGlassWaveRgb(float angle)
    {
        var lch = MaterialColor.LabToLch(MaterialColor.SrgbToOklab(Rgb(DefaultGlassWave1Rgb)));
        return ToRgb(MaterialColor.GamutMap(lch.X, lch.Y, lch.Z + angle));
    }

    public static Vector3 Rgb(uint rgb) => new(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f);

    public static uint ToRgb(Vector3 rgb) => ((uint)Math.Clamp((int)MathF.Round(rgb.X * 255), 0, 255) << 16)
        | ((uint)Math.Clamp((int)MathF.Round(rgb.Y * 255), 0, 255) << 8)
        | (uint)Math.Clamp((int)MathF.Round(rgb.Z * 255), 0, 255);
}
