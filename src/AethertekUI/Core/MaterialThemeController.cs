using System.Numerics;

namespace AethertekUI;

/// <summary>Serializable host-owned settings. Persistence is deliberately left to the plugin.</summary>
public sealed record MaterialThemeSettings
{
    public float SeedRed { get; init; } = .36f;
    public float SeedGreen { get; init; } = .72f;
    public float SeedBlue { get; init; } = .46f;
    public float SurfaceOpacity { get; init; } = .94f;
    public MaterialDensity Density { get; init; } = MaterialDensity.Compact;
    public bool ReducedMotion { get; init; }

    public void Validate()
    {
        if (!float.IsFinite(SeedRed) || !float.IsFinite(SeedGreen) || !float.IsFinite(SeedBlue)
            || SeedRed is < 0 or > 1 || SeedGreen is < 0 or > 1 || SeedBlue is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(SeedRed), "Seed channels must be finite sRGB values in [0,1].");
        if (!float.IsFinite(SurfaceOpacity) || SurfaceOpacity is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(SurfaceOpacity));
        if (!Enum.IsDefined(Density)) throw new ArgumentOutOfRangeException(nameof(Density));
    }
}

/// <summary>One instance per UI/context. Apply settings on change; unchanged values do not regenerate palettes.</summary>
public sealed class MaterialThemeController
{
    private readonly IPaletteGenerator? generator;
    public MaterialThemeSettings Settings { get; private set; }
    public MaterialTheme Theme { get; private set; }

    public MaterialThemeController(MaterialThemeSettings? settings = null, IPaletteGenerator? generator = null)
    {
        Settings = settings ?? new();
        Settings.Validate();
        this.generator = generator;
        Theme = Create(Settings);
    }

    public bool Apply(MaterialThemeSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();
        if (settings == Settings) return false;
        if (settings.SeedRed != Settings.SeedRed || settings.SeedGreen != Settings.SeedGreen || settings.SeedBlue != Settings.SeedBlue)
            Theme = Create(settings);
        else
        {
            Theme.Density = settings.Density;
            Theme.SurfaceOpacity = settings.SurfaceOpacity;
            Theme.Motion.Enabled = !settings.ReducedMotion;
        }
        Settings = settings;
        return true;
    }

    private MaterialTheme Create(MaterialThemeSettings settings)
    {
        var result = new MaterialTheme(new Vector3(settings.SeedRed, settings.SeedGreen, settings.SeedBlue), settings.Density, generator)
        { SurfaceOpacity = settings.SurfaceOpacity };
        result.Motion.Enabled = !settings.ReducedMotion;
        return result;
    }
}
