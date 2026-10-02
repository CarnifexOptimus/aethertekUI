using System.Numerics;

namespace AethertekUI;

public interface IPaletteGenerator
{
    MaterialPalette Generate(Vector3 seed);
}

/// <summary>Cached tones, indexed by OKLab lightness from 0 through 100.</summary>
public sealed class TonalPalette
{
    private readonly Vector4[] tones = new Vector4[101];
    public TonalPalette(float hue, float chroma)
    {
        for (var i = 0; i <= 100; i++) tones[i] = new(MaterialColor.GamutMap(i/100f, chroma, hue), 1);
    }
    public Vector4 this[int tone] => tones[Math.Clamp(tone,0,100)];
}

public sealed record MaterialPalette(TonalPalette Primary, TonalPalette Secondary, TonalPalette Tertiary,
    TonalPalette Neutral, TonalPalette NeutralVariant, TonalPalette Error);

public sealed class OklchPaletteGenerator : IPaletteGenerator
{
    public MaterialPalette Generate(Vector3 seed)
    {
        if (!float.IsFinite(seed.X) || !float.IsFinite(seed.Y) || !float.IsFinite(seed.Z))
            throw new ArgumentOutOfRangeException(nameof(seed));
        var lch = MaterialColor.LabToLch(MaterialColor.SrgbToOklab(Vector3.Clamp(seed, Vector3.Zero, Vector3.One)));
        var hue = lch.Y < .001f ? 145f * MathF.PI / 180f : lch.Z;
        return new(new(hue, Math.Clamp(lch.Y,.08f,.20f)), new(hue,.055f),
            new(hue+MathF.PI/3,.10f), new(hue,.008f), new(hue,.022f), new(25*MathF.PI/180,.18f));
    }
}
