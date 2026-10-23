using System.Numerics;

namespace AethertekUI;

public enum MaterialColorMode { Auto, Picker, Lcars }

/// <summary>Consumer-owned appearance choices. Auto retains the supplied automatic scheme unchanged.</summary>
public sealed record MaterialColorSettings
{
    public MaterialColorMode Mode { get; init; }
    public uint BackgroundRgb { get; init; } = 0x132334;
    public uint ControlsRgb { get; init; } = 0x365C84;
    public uint TextRgb { get; init; } = 0xE5F2FF;

    public void Validate()
    {
        if (!Enum.IsDefined(Mode)) throw new ArgumentOutOfRangeException(nameof(Mode));
        if (BackgroundRgb > 0xFFFFFF) throw new ArgumentOutOfRangeException(nameof(BackgroundRgb));
        if (ControlsRgb > 0xFFFFFF) throw new ArgumentOutOfRangeException(nameof(ControlsRgb));
        if (TextRgb > 0xFFFFFF) throw new ArgumentOutOfRangeException(nameof(TextRgb));
    }

    public MaterialColorScheme Apply(MaterialColorScheme automatic)
    {
        ArgumentNullException.ThrowIfNull(automatic);
        Validate();
        return Mode switch
        {
            MaterialColorMode.Auto => automatic,
            MaterialColorMode.Lcars => new MaterialColorScheme(automatic),
            _ => new MaterialColorScheme(automatic, Rgb(BackgroundRgb), Rgb(ControlsRgb), Rgb(TextRgb)),
        };
    }

    public static Vector3 Rgb(uint rgb) => new(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f);

    public static uint ToRgb(Vector3 rgb) => ((uint)Math.Clamp((int)MathF.Round(rgb.X * 255), 0, 255) << 16)
        | ((uint)Math.Clamp((int)MathF.Round(rgb.Y * 255), 0, 255) << 8)
        | (uint)Math.Clamp((int)MathF.Round(rgb.Z * 255), 0, 255);
}
