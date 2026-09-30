using System.Numerics;
using System.Text.Json;
using Xunit;

namespace CombatReborn.MaterialUI.Tests;

public class IntegrationLogicTests
{
    private sealed class CountingGenerator : IPaletteGenerator
    {
        public int Calls;
        public MaterialPalette Generate(Vector3 seed) { Calls++; return new OklchPaletteGenerator().Generate(seed); }
    }
    [Fact]
    public void SettingsRoundTripAndOnlySeedChangeRegeneratesPalette()
    {
        var generator=new CountingGenerator();
        var host=new MaterialThemeController(generator:generator);
        var original=host.Theme;
        Assert.False(host.Apply(host.Settings with {}));
        Assert.True(host.Apply(host.Settings with {Density=MaterialDensity.Standard,SurfaceOpacity=.5f,ReducedMotion=true}));
        Assert.Same(original,host.Theme);
        Assert.Equal(1,generator.Calls);
        Assert.False(host.Theme.Motion.Enabled);
        var json=JsonSerializer.Serialize(host.Settings);
        var restored=JsonSerializer.Deserialize<MaterialThemeSettings>(json)!;
        Assert.Equal(host.Settings,restored);
        host.Apply(restored with {SeedRed=.8f});
        Assert.NotSame(original,host.Theme);
        Assert.Equal(2,generator.Calls);
    }
    [Theory]
    [InlineData(float.NaN)] [InlineData(float.PositiveInfinity)] [InlineData(-.1f)] [InlineData(1.1f)]
    public void InvalidSettingsAreRejectedWithoutMutatingTheme(float value)
    {
        var host=new MaterialThemeController();
        var before=host.Theme;
        Assert.Throws<ArgumentOutOfRangeException>(()=>host.Apply(host.Settings with {SurfaceOpacity=value}));
        Assert.Same(before,host.Theme);
    }
    [Fact]
    public void FilteredOptionsRetainValueIdentityAcrossReplacementAndRejectDuplicateKeys()
    {
        var options=new MaterialOptions<int>([new("a",1,"Ore"),new("b",2,"Herb"),new("c",3,"Dark ore",true)]);
        Assert.True(options.SetQuery("ORE"));
        Assert.False(options.SetQuery("ORE"));
        Assert.Equal(2,options.Count);
        Assert.True(options[1].Disabled);
        options.Replace([new("c",3,"Dark ore"),new("a",1,"Ore")]);
        Assert.Equal(3,options[0].Value);
        Assert.Equal("Ore",options.LabelFor(1));
        Assert.Throws<ArgumentException>(()=>options.Replace([new("same",1,"A"),new("same",2,"B")]));
        Assert.Equal(2,options.Count);
        options.SetQuery("nothing");
        Assert.Equal(0,options.Count);
        Assert.Equal("Unknown",options.LabelFor(99,"Unknown"));
    }
    [Fact]
    public void CachedDataViewAndKeySelectionSurviveSortFilterAndThrowingFilter()
    {
        var view=new MaterialDataView<int>();
        var selection=new MaterialSelectionSet<int>();
        selection.SelectOnly(42);selection.Toggle(7);
        view.Rebuild([3,42,7,9],v=>v!=7,Comparer<int>.Create((a,b)=>b.CompareTo(a)));
        Assert.Equal(new[]{42,9,3},view.Rows);
        Assert.True(selection.Contains(7));
        Assert.True(selection.Contains(42));
        Assert.Equal(1,view.Revision);
        Assert.Throws<InvalidOperationException>(()=>view.Rebuild([99],_=>throw new InvalidOperationException()));
        Assert.Equal(new[]{42,9,3},view.Rows);
        selection.Prune(new HashSet<int>{42});
        Assert.Equal(1,selection.Count);
        Assert.True(selection.Contains(42));
    }
    [Fact]
    public void TraversalSkipsDisabledAndHonorsWrap()
    {
        bool[] enabled=[true,false,true,false];
        Assert.Equal(2,MaterialNavigationMath.NextEnabled(enabled,0,1));
        Assert.Equal(0,MaterialNavigationMath.NextEnabled(enabled,2,1));
        Assert.Equal(2,MaterialNavigationMath.NextEnabled(enabled,2,1,false));
        Assert.Equal(2,MaterialNavigationMath.NextEnabled(enabled,0,-1));
        Assert.Equal(-1,MaterialNavigationMath.NextEnabled([],0,1));
        Assert.Equal(1,MaterialNavigationMath.NextEnabled([false,false],1,1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>MaterialNavigationMath.NextEnabled(enabled,0,0));
    }
    [Fact]
    public void FlagsPreserveUnknownBitsAndSupportCombinedMasks()
    {
        var value=MaterialFlags.Set(0x80,3,true);
        Assert.Equal(0x83UL,value);
        Assert.True(MaterialFlags.IsSet(value,3));
        Assert.Equal(0x80UL,MaterialFlags.Set(value,3,false));
        Assert.False(MaterialFlags.IsSet(value,0));
    }
}
