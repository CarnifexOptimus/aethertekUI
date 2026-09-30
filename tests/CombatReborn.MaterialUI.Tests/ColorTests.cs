using System.Numerics;
using Xunit;

namespace CombatReborn.MaterialUI.Tests;

public class ColorTests
{
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
        var c=new MaterialTheme(new(.3f,.7f,.4f)).Colors;
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
        var t=new MaterialTheme(new(.4f,.7f,.3f)); var c=t.Colors;
        t.SurfaceOpacity=.5f;
        Assert.Same(c,t.Colors);
        Assert.Equal(new Vector4(c.Surface.X,c.Surface.Y,c.Surface.Z,.5f),t.SurfaceColor(c.Surface));
        Assert.Throws<ArgumentOutOfRangeException>(()=>t.SurfaceOpacity=float.NaN);
        Assert.Throws<ArgumentOutOfRangeException>(()=>new OklchPaletteGenerator().Generate(new(float.NaN,0,0)));
    }
    private static void Valid(Vector4 c)
    {
        foreach(var v in new[]{c.X,c.Y,c.Z,c.W}) { Assert.True(float.IsFinite(v)); Assert.InRange(v,0,1); }
    }
}
