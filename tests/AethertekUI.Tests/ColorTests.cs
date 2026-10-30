using System.Numerics;
using Xunit;

namespace AethertekUI.Tests;

public class ColorTests
{
    [Fact]
    public void GlassBackgroundChoicesRoundTripWithoutChangingWaveColoursOrBlurDefaults()
    {
        var legacy = System.Text.Json.JsonSerializer.Deserialize<MaterialColorSettings>("{}")!;
        Assert.Equal(MaterialGlassBackground.Waves, legacy.GlassBackground);
        Assert.Equal(MaterialGlassLights.AmbientGlows, legacy.GlassLights);
        Assert.Equal("", legacy.GlassImagePath);
        var selected = legacy with
        {
            GlassBackground = MaterialGlassBackground.Image,
            GlassLights = MaterialGlassLights.SweepingBeams,
            GlassImagePath = @"C:\fixture\glass.png",
            GlassLight1Rgb = 0x123456,
        };
        var restored = System.Text.Json.JsonSerializer.Deserialize<MaterialColorSettings>(
            System.Text.Json.JsonSerializer.Serialize(selected))!;
        Assert.Equal(selected, restored);
        Assert.Equal(legacy.GlassWave1Rgb, restored.GlassWave1Rgb);
        Assert.NotEqual(restored.GlassWave1Rgb, restored.GlassLight1Rgb);
        Assert.True(restored.GlassBackgroundBlur);
        Assert.Equal(75, restored.BackgroundBlurPercent);
        var invalid = restored with { GlassBackground = (MaterialGlassBackground)99, GlassLights = (MaterialGlassLights)(-1) };
        Assert.Equal(MaterialGlassBackground.Waves, invalid.GlassBackground);
        Assert.Equal(MaterialGlassLights.AmbientGlows, invalid.GlassLights);
    }

    [Fact]
    public void BlurDefaultsAndManualChoicesSurviveStyleChangesAndSerialization()
    {
        var legacy = System.Text.Json.JsonSerializer.Deserialize<MaterialColorSettings>("{}")!;
        Assert.Equal(75, legacy.BackgroundBlurPercent);
        foreach (var mode in Enum.GetValues<MaterialColorMode>())
            Assert.Equal(mode == MaterialColorMode.Glassmorphism, legacy.HasBackgroundBlur(mode));
        var saved = legacy.WithBackgroundBlur(MaterialColorMode.Glassmorphism, false)
            .WithBackgroundBlur(MaterialColorMode.Spatial, true) with { BackgroundBlurPercent = 42 };
        var restored = System.Text.Json.JsonSerializer.Deserialize<MaterialColorSettings>(
            System.Text.Json.JsonSerializer.Serialize(saved))!;
        Assert.Equal(saved, restored);
        Assert.False(restored.HasBackgroundBlur(MaterialColorMode.Glassmorphism));
        Assert.True(restored.HasBackgroundBlur(MaterialColorMode.Spatial));
        Assert.False(restored.HasBackgroundBlur(MaterialColorMode.Cyberpunk));
        Assert.Equal(0, (saved with { BackgroundBlurPercent = -1 }).BackgroundBlurPercent);
        Assert.Equal(100, (saved with { BackgroundBlurPercent = 101 }).BackgroundBlurPercent);
    }

    [Theory]
    [InlineData(0f)] [InlineData(.003f)] [InlineData(.04f)] [InlineData(.5f)] [InlineData(1f)]
    public void SrgbRoundTrip(float value) => Assert.InRange(Math.Abs(MaterialColor.LinearToSrgb(MaterialColor.SrgbToLinear(value))-value),0,1e-6f);

    [Fact]
    public void OklabRoundTripAcrossRgbCube()
    {
        for (var r=0; r<=10; r++) for(var g=0;g<=10;g++) for(var b=0;b<=10;b++)
        {
            var input=new Vector3(r/10f,g/10f,b/10f);
            var lab=MaterialColor.SrgbToOklab(input);
            Assert.InRange(Vector3.Distance(input,MaterialColor.OklabToSrgb(lab)),0,5e-5f);
            Assert.InRange(Vector3.Distance(lab,MaterialColor.LchToLab(MaterialColor.LabToLch(lab))),0,1e-6f);
        }
    }
    [Fact]
    public void KnownWhiteAndRedCoordinates()
    {
        Assert.InRange(Vector3.Distance(MaterialColor.SrgbToOklab(Vector3.One),new(1,0,0)),0,1e-6f);
        Assert.InRange(Vector3.Distance(MaterialColor.SrgbToOklab(Vector3.UnitX),new(.627955f,.224863f,.125846f)),0,1e-5f);
    }
    [Fact]
    public void PalettesAreDeterministicAndHuePreserving()
    {
        var generator=new OklchPaletteGenerator();
        foreach (var seed in new[]{new Vector3(.3f,.7f,.4f),Vector3.UnitX,Vector3.UnitY,Vector3.UnitZ,new Vector3(.8f,.3f,.7f)})
        {
            var a=generator.Generate(seed); var b=generator.Generate(seed);
            var seedHue=MaterialColor.LabToLch(MaterialColor.SrgbToOklab(seed)).Z;
            foreach(var tone in new[]{30,50,80})
            {
                Assert.Equal(a.Primary[tone],b.Primary[tone]);
                var color=a.Primary[tone];
                var hue=MaterialColor.LabToLch(MaterialColor.SrgbToOklab(new(color.X,color.Y,color.Z))).Z;
                var delta=MathF.Abs(MathF.Atan2(MathF.Sin(hue-seedHue),MathF.Cos(hue-seedHue)));
                Assert.InRange(delta,0,.001f);
            }
        }
    }
    [Fact]
    public void EveryPaletteToneIsFiniteInGamutAndOrdered()
    {
        var p=new OklchPaletteGenerator().Generate(new(.1f,.9f,.5f));
        foreach(var palette in new[]{p.Primary,p.Secondary,p.Tertiary,p.Neutral,p.NeutralVariant,p.Error})
        {
            var previous=-1f;
            for(var tone=0;tone<=100;tone++)
            {
                var c=palette[tone];
                Valid(c);
                var lightness=MaterialColor.SrgbToOklab(new(c.X,c.Y,c.Z)).X;
                Assert.InRange(lightness,tone/100f-1e-5f,tone/100f+1e-5f);
                Assert.True(lightness>previous);
                previous=lightness;
            }
        }
        for(var hue=0;hue<360;hue+=15)
        {
            var mapped=MaterialColor.GamutMap(.7f,2,hue*MathF.PI/180);
            Valid(new(mapped,1));
            Assert.InRange(MaterialColor.LabToLch(MaterialColor.SrgbToOklab(mapped)).Y,0,.5f);
        }
    }
    [Fact]
    public void RequiredRolesAndContrast()
    {
        var c=new MaterialTheme(new Vector3(.3f,.7f,.4f)).Colors;
        var properties=typeof(MaterialColorScheme).GetProperties();
        Assert.Equal(33,properties.Length);
        foreach(var property in properties) Valid((Vector4)property.GetValue(c)!);
        var surfaces=new[]{c.SurfaceContainerLowest,c.SurfaceContainerLow,c.SurfaceContainer,c.SurfaceContainerHigh,c.SurfaceContainerHighest};
        for(var i=1;i<surfaces.Length;i++) Assert.True(MaterialColor.Luminance(surfaces[i])>MaterialColor.Luminance(surfaces[i-1]));
        foreach(var pair in new[]{(c.Primary,c.OnPrimary),(c.PrimaryContainer,c.OnPrimaryContainer),(c.Secondary,c.OnSecondary),
            (c.SecondaryContainer,c.OnSecondaryContainer),(c.Tertiary,c.OnTertiary),(c.Error,c.OnError),
            (c.Surface,c.OnSurface),(c.SurfaceVariant,c.OnSurfaceVariant),(c.Background,c.OnBackground),
            (c.InverseSurface,c.InverseOnSurface)})
            Assert.True(MaterialColor.Contrast(pair.Item1,pair.Item2)>=4.5f);
    }
    [Fact]
    public void OpacityDoesNotChangeRgbOrScheme()
    {
        var t=new MaterialTheme(new Vector3(.4f,.7f,.3f)); var c=t.Colors;
        t.SurfaceOpacity=.5f;
        Assert.Same(c,t.Colors);
        Assert.Equal(new Vector4(c.Surface.X,c.Surface.Y,c.Surface.Z,.5f),t.SurfaceColor(c.Surface));
        Assert.Throws<ArgumentOutOfRangeException>(()=>t.SurfaceOpacity=float.NaN);
        Assert.Throws<ArgumentOutOfRangeException>(()=>new OklchPaletteGenerator().Generate(new(float.NaN,0,0)));
    }

    [Theory]
    [InlineData(0x000000u, 0x000000u, 0x000000u)]
    [InlineData(0xFFFFFFu, 0xFFFFFFu, 0xFFFFFFu)]
    [InlineData(0x132334u, 0x365C84u, 0xE5F2FFu)]
    public void PickerKeepsChosenRgbExactAndFamiliesIndependent(uint background, uint controls, uint text)
    {
        var automatic = new MaterialTheme(new Vector3(.3f, .7f, .4f)).Colors;
        var settings = new MaterialColorSettings { Mode = MaterialColorMode.Picker,
            BackgroundRgb = background, ControlsRgb = controls, TextRgb = text };
        Assert.Same(automatic, (settings with { Mode = MaterialColorMode.Auto }).Apply(automatic));
        var chosen = settings.Apply(automatic);
        Assert.Equal(new Vector4(MaterialColorSettings.Rgb(background), 1), chosen.Background);
        Assert.Equal(new Vector4(MaterialColorSettings.Rgb(controls), 1), chosen.Primary);
        Assert.Equal(chosen.Primary, chosen.SecondaryContainer);
        Assert.Equal(new Vector4(MaterialColorSettings.Rgb(text), 1), chosen.OnSurface);
        Assert.Equal(chosen.OnSurface, chosen.OnPrimary);
        Assert.Equal(automatic.Error, chosen.Error); Assert.Equal(automatic.OnError, chosen.OnError);
        Assert.Equal(automatic.ErrorContainer, chosen.ErrorContainer); Assert.Equal(automatic.OnErrorContainer, chosen.OnErrorContainer);
        Assert.Equal(MaterialColorMode.Picker, new MaterialTheme(chosen).ColorMode);
        var newBackground = (settings with { BackgroundRgb = 0xABCDE0 }).Apply(automatic);
        Assert.Equal(chosen.Primary, newBackground.Primary); Assert.Equal(chosen.OnSurface, newBackground.OnSurface);
        var newControls = (settings with { ControlsRgb = 0xABCDE0 }).Apply(automatic);
        Assert.Equal(chosen.Background, newControls.Background); Assert.Equal(chosen.Surface, newControls.Surface);
        Assert.Equal(chosen.OnSurface, newControls.OnSurface);
        var newText = (settings with { TextRgb = 0xABCDE0 }).Apply(automatic);
        Assert.Equal(chosen.Background, newText.Background); Assert.Equal(chosen.Primary, newText.Primary);
        Assert.Equal(chosen.SurfaceContainerHigh, newText.SurfaceContainerHigh);
        foreach (var property in typeof(MaterialColorScheme).GetProperties()) Valid((Vector4)property.GetValue(chosen)!);
    }

    [Fact]
    public void PickerSettingsRoundTripInBothHostSerializersAndLegacySettingsStayAutomatic()
    {
        var settings = new MaterialThemeSettings { Colors = new() { Mode = MaterialColorMode.Picker,
            BackgroundRgb = 0x010203, ControlsRgb = 0x040506, TextRgb = 0x070809 } };
        Assert.Equal(settings, System.Text.Json.JsonSerializer.Deserialize<MaterialThemeSettings>(System.Text.Json.JsonSerializer.Serialize(settings)));
        Assert.Equal(settings, Newtonsoft.Json.JsonConvert.DeserializeObject<MaterialThemeSettings>(Newtonsoft.Json.JsonConvert.SerializeObject(settings)));
        Assert.Equal(MaterialColorMode.Auto, System.Text.Json.JsonSerializer.Deserialize<MaterialThemeSettings>("{}")!.Colors.Mode);
        Assert.Equal(MaterialColorMode.Auto, Newtonsoft.Json.JsonConvert.DeserializeObject<MaterialThemeSettings>("{}")!.Colors.Mode);
        Assert.Throws<ArgumentOutOfRangeException>(() => (settings.Colors with { Mode = (MaterialColorMode)9 }).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => (settings.Colors with { TextRgb = 0x1000000 }).Validate());
    }

    [Fact]
    public void LegacyModeNumbersAndMissingAppearanceDefaultsSurviveBothSerializers()
    {
        Assert.Equal(new[] { 0, 1, 2, 3, 4, 5 }, Enum.GetValues<MaterialColorMode>().Select(mode => (int)mode));
        foreach (var mode in new[] { MaterialColorMode.Auto, MaterialColorMode.Picker, MaterialColorMode.Lcars })
        {
            var json = $"{{\"Mode\":{(int)mode},\"BackgroundRgb\":66051,\"ControlsRgb\":263430,\"TextRgb\":460809}}";
            var expected = new MaterialColorSettings { Mode = mode, BackgroundRgb = 0x010203, ControlsRgb = 0x040506, TextRgb = 0x070809 };
            Assert.Equal(expected, System.Text.Json.JsonSerializer.Deserialize<MaterialColorSettings>(json));
            Assert.Equal(expected, Newtonsoft.Json.JsonConvert.DeserializeObject<MaterialColorSettings>(json));
        }
        var defaults = new MaterialColorSettings();
        Assert.Equal((0x10232Du, 0x65C7D2u, 0xEDF7FAu), defaults.GetPalette(MaterialColorMode.Glassmorphism));
        Assert.Equal((0xE8EDF4u, 0x6678A8u, 0x1C2538u), defaults.GetPalette(MaterialColorMode.Spatial));
        Assert.Equal((0x0B0D13u, 0x19DCE8u, 0xEAF8FFu), defaults.GetPalette(MaterialColorMode.Cyberpunk));
        Assert.Equal((0x07090Du, 0xFFAA66u, 0xCC99CCu, 0x9999FFu),
            (defaults.LcarsBackgroundRgb, defaults.LcarsBar1Rgb, defaults.LcarsBar2Rgb, defaults.LcarsBar3Rgb));
        Assert.True(defaults.GlassAnimation);
        Assert.Equal(1, defaults.GlassAnimationSpeed);
        var glass = new MaterialTheme(Vector3.UnitY, defaults with { Mode = MaterialColorMode.Glassmorphism }).Colors;
        Assert.Equal(0x65C7D2u, defaults.GlassWave1Rgb);
        Assert.Equal(MaterialColorSettings.ToRgb(new(glass.Secondary.X, glass.Secondary.Y, glass.Secondary.Z)), defaults.GlassWave2Rgb);
        Assert.Equal(MaterialColorSettings.ToRgb(new(glass.Tertiary.X, glass.Tertiary.Y, glass.Tertiary.Z)), defaults.GlassWave3Rgb);
        Assert.NotEqual(defaults.GlassWave1Rgb, defaults.GlassWave2Rgb);
        Assert.NotEqual(defaults.GlassWave2Rgb, defaults.GlassWave3Rgb);
    }

    [Theory]
    [InlineData(MaterialColorMode.Picker)]
    [InlineData(MaterialColorMode.Glassmorphism)]
    [InlineData(MaterialColorMode.Spatial)]
    [InlineData(MaterialColorMode.Cyberpunk)]
    public void PaletteEditsAffectOnlyTheNamedSavedPalette(MaterialColorMode selected)
    {
        var initial = new MaterialColorSettings { Mode = MaterialColorMode.Lcars, GlassAnimation = false,
            GlassAnimationSpeed = 2.75f, GlassWave1Rgb = 0x112233, GlassWave2Rgb = 0x445566, GlassWave3Rgb = 0x778899,
            LcarsBackgroundRgb = 0x121314, LcarsBar1Rgb = 0x212223, LcarsBar2Rgb = 0x313233, LcarsBar3Rgb = 0x414243 };
        var edited = initial.WithPalette(selected, 0x010203, 0x040506, 0x070809);
        Assert.Equal(MaterialColorMode.Lcars, edited.Mode);
        foreach (var mode in new[] { MaterialColorMode.Picker, MaterialColorMode.Glassmorphism, MaterialColorMode.Spatial, MaterialColorMode.Cyberpunk })
            Assert.Equal(mode == selected ? (0x010203u, 0x040506u, 0x070809u) : initial.GetPalette(mode), edited.GetPalette(mode));
        Assert.Equal(initial, edited.WithPalette(selected, MaterialColorSettings.DefaultPalette(selected).BackgroundRgb,
            MaterialColorSettings.DefaultPalette(selected).ControlsRgb, MaterialColorSettings.DefaultPalette(selected).TextRgb));
        Assert.Equal(edited, System.Text.Json.JsonSerializer.Deserialize<MaterialColorSettings>(System.Text.Json.JsonSerializer.Serialize(edited)));
        Assert.Equal(edited, Newtonsoft.Json.JsonConvert.DeserializeObject<MaterialColorSettings>(Newtonsoft.Json.JsonConvert.SerializeObject(edited)));
    }

    [Theory]
    [InlineData(MaterialColorMode.Auto)]
    [InlineData(MaterialColorMode.Lcars)]
    [InlineData((MaterialColorMode)99)]
    public void ThreeRolePaletteHelpersRejectModesWithDifferentContracts(MaterialColorMode mode)
    {
        var settings = new MaterialColorSettings();
        Assert.Throws<ArgumentOutOfRangeException>(() => settings.GetPalette(mode));
        Assert.Throws<ArgumentOutOfRangeException>(() => settings.WithPalette(mode, 0, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => MaterialColorSettings.DefaultPalette(mode));
    }

    [Theory]
    [InlineData(-1f, .1f)]
    [InlineData(.1f, .1f)]
    [InlineData(3.25f, 3.25f)]
    [InlineData(20f, 10f)]
    [InlineData(float.NaN, 1f)]
    [InlineData(float.PositiveInfinity, 1f)]
    [InlineData(float.NegativeInfinity, 1f)]
    public void GlassSpeedUsesTheApprovedBounds(float speed, float expected)
        => Assert.Equal(expected, new MaterialColorSettings { GlassAnimationSpeed = speed }.GlassAnimationSpeed);

    [Fact]
    public void EverySavedRgbRoleRejectsValuesOutsideRgb()
    {
        foreach (var property in typeof(MaterialColorSettings).GetProperties().Where(property => property.PropertyType == typeof(uint)))
        {
            var settings = System.Text.Json.JsonSerializer.Deserialize<MaterialColorSettings>($"{{\"{property.Name}\":16777216}}")!;
            Assert.Throws<ArgumentOutOfRangeException>(() => settings.Validate());
        }
    }

    [Theory]
    [InlineData(MaterialColorMode.Glassmorphism, 0x1B2D36u, 0x0D1D25u)]
    [InlineData(MaterialColorMode.Spatial, 0xF5F7FAu, 0xDEE2E9u)]
    [InlineData(MaterialColorMode.Cyberpunk, 0x162127u, 0x090B10u)]
    public void ApprovedMaterialPalettesKeepExactBasesRelativeSurfacesAndReadableAccentInk(MaterialColorMode mode, uint surface, uint well)
    {
        var automatic = new MaterialTheme(Vector3.UnitX).Colors;
        var settings = new MaterialColorSettings { Mode = mode };
        var chosen = settings.Apply(automatic);
        var theme = new MaterialTheme(chosen);
        var palette = settings.GetPalette(mode);
        Assert.Equal(mode, theme.ColorMode);
        Assert.Equal(mode == MaterialColorMode.Glassmorphism, theme.IsGlassmorphism);
        Assert.Equal(mode == MaterialColorMode.Spatial, theme.IsSpatial);
        Assert.Equal(mode == MaterialColorMode.Cyberpunk, theme.IsCyberpunk);
        Assert.False(theme.IsLcars);
        Assert.Equal(new Vector4(MaterialColorSettings.Rgb(palette.BackgroundRgb), 1), chosen.Background);
        Assert.Equal(new Vector4(MaterialColorSettings.Rgb(palette.ControlsRgb), 1), chosen.Primary);
        Assert.Equal(new Vector4(MaterialColorSettings.Rgb(palette.TextRgb), 1), chosen.OnSurface);
        Assert.Equal(surface, MaterialColorSettings.ToRgb(new(chosen.Surface.X, chosen.Surface.Y, chosen.Surface.Z)));
        Assert.Equal(well, MaterialColorSettings.ToRgb(new(chosen.SurfaceContainerLowest.X, chosen.SurfaceContainerLowest.Y, chosen.SurfaceContainerLowest.Z)));
        Assert.NotEqual(chosen.Primary, chosen.Secondary);
        Assert.NotEqual(chosen.Secondary, chosen.Tertiary);
        foreach (var property in typeof(MaterialColorScheme).GetProperties()) Valid((Vector4)property.GetValue(chosen)!);
        foreach (var pair in new[] { (chosen.Primary, chosen.OnPrimary), (chosen.Secondary, chosen.OnSecondary), (chosen.Tertiary, chosen.OnTertiary) })
            Assert.True(MaterialColor.Contrast(pair.Item1, pair.Item2) >= 4.5f);
        Assert.Equal(automatic.Error, chosen.Error); Assert.Equal(automatic.OnError, chosen.OnError);
        Assert.Equal(automatic.ErrorContainer, chosen.ErrorContainer); Assert.Equal(automatic.OnErrorContainer, chosen.OnErrorContainer);
        var otherSeed = settings.Apply(new MaterialTheme(Vector3.UnitZ).Colors);
        Assert.Equal(chosen.Surface, otherSeed.Surface); Assert.Equal(chosen.Secondary, otherSeed.Secondary);
        var exact = settings.WithPalette(mode, 0xFFFFFF, 0, 0x808080).Apply(automatic);
        Assert.Equal(Vector4.One, exact.Background);
        Assert.Equal(new Vector4(0, 0, 0, 1), exact.Primary);
        Assert.Equal(new Vector4(MaterialColorSettings.Rgb(0x808080), 1), exact.OnSurface);
        Assert.Same(automatic, (settings with { Mode = MaterialColorMode.Auto }).Apply(automatic));
    }

    [Theory]
    [InlineData(0xFFFFFFu, 0x102030u, 0x405060u, 0x708090u)]
    [InlineData(0x000000u, 0xFEDCBAu, 0xCBADEFu, 0xABCDEFu)]
    public void LcarsUsesAllFourChosenRolesAndChoosesReadableForegrounds(uint background, uint bar1, uint bar2, uint bar3)
    {
        var automatic = new MaterialTheme(Vector3.UnitY).Colors;
        var settings = new MaterialColorSettings { Mode = MaterialColorMode.Lcars,
            LcarsBackgroundRgb = background, LcarsBar1Rgb = bar1, LcarsBar2Rgb = bar2, LcarsBar3Rgb = bar3 };
        var chosen = settings.Apply(automatic);
        Assert.Equal(new Vector4(MaterialColorSettings.Rgb(background), 1), chosen.Background);
        Assert.Equal(new Vector4(MaterialColorSettings.Rgb(bar1), 1), chosen.Primary);
        Assert.Equal(new Vector4(MaterialColorSettings.Rgb(bar2), 1), chosen.Secondary);
        Assert.Equal(new Vector4(MaterialColorSettings.Rgb(bar3), 1), chosen.Tertiary);
        foreach (var pair in new[] { (chosen.Primary, chosen.OnPrimary), (chosen.Secondary, chosen.OnSecondary),
            (chosen.Tertiary, chosen.OnTertiary), (chosen.Background, chosen.OnBackground), (chosen.SecondaryContainer, chosen.OnSecondaryContainer),
            (chosen.TertiaryContainer, chosen.OnTertiaryContainer), (chosen.SurfaceVariant, chosen.OnSurfaceVariant) })
            Assert.True(MaterialColor.Contrast(pair.Item1, pair.Item2) >= 4.5f);
        Assert.Equal(automatic.Error, chosen.Error); Assert.Equal(automatic.OnError, chosen.OnError);
        Assert.Equal(automatic.ErrorContainer, chosen.ErrorContainer); Assert.Equal(automatic.OnErrorContainer, chosen.OnErrorContainer);
        Assert.Equal(MaterialColorMode.Lcars, new MaterialTheme(chosen).ColorMode);
    }

    [Fact]
    public void SchemeConstructionNeverBorrowsTheActiveThemesCustomLcarsRoles()
    {
        using var host = new NativeTestContext();
        var automatic = new MaterialTheme(Vector3.UnitY).Colors;
        var custom = new MaterialColorSettings { Mode = MaterialColorMode.Lcars, LcarsBackgroundRgb = 0xFFFFFF,
            LcarsBar1Rgb = 0x123456, LcarsBar2Rgb = 0x456789, LcarsBar3Rgb = 0x789ABC };
        var active = new MaterialTheme(custom.Apply(automatic));
        var defaults = new MaterialColorSettings { Mode = MaterialColorMode.Lcars };
        var expected = defaults.Apply(automatic);
        host.Frame(() =>
        {
            using var scope = MaterialTheme.Push(active);
            var chosen = defaults.Apply(automatic);
            foreach (var property in typeof(MaterialColorScheme).GetProperties())
                Assert.Equal(property.GetValue(expected), property.GetValue(chosen));
            Assert.Equal(new Vector4(MaterialColorSettings.Rgb(0x123456), 1), active.Colors.Primary);
            Assert.Equal(Vector4.One, custom.Apply(automatic).Background);
        });
    }

    private static void Valid(Vector4 c)
    {
        foreach(var v in new[]{c.X,c.Y,c.Z,c.W}) { Assert.True(float.IsFinite(v)); Assert.InRange(v,0,1); }
    }
}
