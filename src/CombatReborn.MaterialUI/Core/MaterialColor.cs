using System.Numerics;

namespace CombatReborn.MaterialUI;

/// <summary>Pure sRGB, linear RGB and OKLab conversions. OKLCH hue is in radians.</summary>
public static class MaterialColor
{
    public static float SrgbToLinear(float v) => v <= 0.04045f ? v / 12.92f : MathF.Pow((v + 0.055f) / 1.055f, 2.4f);
    public static float LinearToSrgb(float v) => v <= 0.0031308f ? 12.92f * v : 1.055f * MathF.Pow(v, 1 / 2.4f) - 0.055f;
    public static Vector3 ToLinear(Vector3 rgb) => new(SrgbToLinear(rgb.X), SrgbToLinear(rgb.Y), SrgbToLinear(rgb.Z));
    public static Vector3 ToSrgb(Vector3 rgb) => new(LinearToSrgb(rgb.X), LinearToSrgb(rgb.Y), LinearToSrgb(rgb.Z));
    public static Vector3 SrgbToOklab(Vector3 rgb)
    {
        var c = ToLinear(rgb);
        var l = MathF.Cbrt(.4122214708f * c.X + .5363325363f * c.Y + .0514459929f * c.Z);
        var m = MathF.Cbrt(.2119034982f * c.X + .6806995451f * c.Y + .1073969566f * c.Z);
        var s = MathF.Cbrt(.0883024619f * c.X + .2817188376f * c.Y + .6299787005f * c.Z);
        return new(.2104542553f*l + .7936177850f*m - .0040720468f*s,
            1.9779984951f*l - 2.4285922050f*m + .4505937099f*s,
            .0259040371f*l + .7827717662f*m - .8086757660f*s);
    }
    public static Vector3 OklabToLinear(Vector3 lab)
    {
        var l = lab.X + .3963377774f*lab.Y + .2158037573f*lab.Z;
        var m = lab.X - .1055613458f*lab.Y - .0638541728f*lab.Z;
        var s = lab.X - .0894841775f*lab.Y - 1.2914855480f*lab.Z;
        l *= l*l; m *= m*m; s *= s*s;
        return new(4.0767416621f*l - 3.3077115913f*m + .2309699292f*s,
            -1.2684380046f*l + 2.6097574011f*m - .3413193965f*s,
            -.0041960863f*l - .7034186147f*m + 1.7076147010f*s);
    }
    public static Vector3 OklabToSrgb(Vector3 lab) => ToSrgb(OklabToLinear(lab));
    public static Vector3 LabToLch(Vector3 lab) => new(lab.X, MathF.Sqrt(lab.Y*lab.Y + lab.Z*lab.Z), MathF.Atan2(lab.Z, lab.Y));
    public static Vector3 LchToLab(Vector3 lch) => new(lch.X, lch.Y*MathF.Cos(lch.Z), lch.Y*MathF.Sin(lch.Z));
    public static bool InGamut(Vector3 linear) => linear.X >= 0 && linear.Y >= 0 && linear.Z >= 0 && linear.X <= 1 && linear.Y <= 1 && linear.Z <= 1;
    public static Vector3 GamutMap(float lightness, float chroma, float hue)
    {
        if (!float.IsFinite(lightness) || !float.IsFinite(chroma) || !float.IsFinite(hue))
            throw new ArgumentOutOfRangeException(nameof(lightness), "Color coordinates must be finite.");
        var l = Math.Clamp(lightness, 0, 1);
        if (l == 0 || l == 1) return new Vector3(l);
        var low = 0f;
        var high = Math.Clamp(chroma, 0, 1);
        var result = OklabToLinear(LchToLab(new(l, high, hue)));
        if (!InGamut(result))
        {
            for (var i = 0; i < 24; i++)
            {
                var mid = (low + high) * .5f;
                if (InGamut(OklabToLinear(LchToLab(new(l, mid, hue))))) low = mid;
                else high = mid;
            }
            result = OklabToLinear(LchToLab(new(l, low, hue)));
        }
        return Vector3.Clamp(ToSrgb(result), Vector3.Zero, Vector3.One);
    }
    public static float Luminance(Vector4 color)
    {
        var c = ToLinear(new(color.X, color.Y, color.Z));
        return .2126f*c.X + .7152f*c.Y + .0722f*c.Z;
    }
    public static float Contrast(Vector4 a, Vector4 b)
    {
        var x = Luminance(a); var y = Luminance(b);
        return (Math.Max(x,y)+.05f) / (Math.Min(x,y)+.05f);
    }
    public static Vector4 Alpha(Vector4 color, float alpha) => new(color.X, color.Y, color.Z, Math.Clamp(alpha,0,1));
    public static Vector4 Layer(Vector4 background, Vector4 foreground, float amount)
    {
        var mixed = Vector4.Lerp(background, foreground, Math.Clamp(amount,0,1));
        return new(mixed.X, mixed.Y, mixed.Z, Math.Max(background.W, amount));
    }
}
