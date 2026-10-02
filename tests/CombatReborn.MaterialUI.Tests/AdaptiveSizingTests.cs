using System.Numerics;
using Dalamud.Bindings.ImGui;
using Xunit;

namespace CombatReborn.MaterialUI.Tests;

public class AdaptiveSizingTests
{
    public static IEnumerable<object[]> Contexts()
    {
        foreach (var density in Enum.GetValues<MaterialDensity>())
        foreach (var context in Enum.GetValues<MaterialControlContext>())
        foreach (var scale in new[] { 1f, 1.25f, 1.5f })
        foreach (var fontScale in new[] { 1f, 2f })
            yield return new object[] { density, context, scale, fontScale };
    }

    [Theory, MemberData(nameof(Contexts))]
    public void NativeAndCustomControlsShareHeightAcrossContextsFontsAndScales(
        MaterialDensity density, MaterialControlContext context, float scale, float fontScale)
    {
        using var host = new NativeTestContext(1600,1500,fontScale);
        var theme = new MaterialTheme(new(.3f,.7f,.4f),density);
        var text = ""; var number = 10; var selected = 0; var enabled = true;
        var options = new MaterialOptions<int>([new("one",0,"One"),new("two",1,"Two")]);
        host.Frame(() =>
        {
            using var scope = MaterialTheme.Push(theme,scale);
            ImGui.SetNextWindowSize(new(1200,1400));
            ImGui.Begin("Sizing",ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoTitleBar);
            using var controls = MaterialControls.Push(context);
            var height = MaterialControls.Metrics.Height;
            Assert.True(height >= ImGui.GetTextLineHeight());
            void Check(string name) => Assert.True(Math.Abs(height-ImGui.GetItemRectSize().Y) < .1f,
                $"{name}: expected {height}, got {ImGui.GetItemRectSize().Y}");
            ImGui.Button("Native"); Check("Native button");
            ImGui.SetNextItemWidth(220); ImGui.InputText("Native input",ref text,128); Check("Native input");
            MaterialButton.FilledButton("material","Material"); Check("Material button");
            MaterialButton.IconButton("icon",MaterialIcon.Edit); Check("Icon button");
            MaterialCanvas.GlyphButton("glyph","W",ImGui.GetFont()); Check("Borrowed glyph");
            MaterialInput.Integer("number",ref number,width:220); Check("Number");
            MaterialCombo.Draw("combo",options,ref selected,width:220); Check("Combo");
            MaterialTextField.SearchField("search",ref text,width:220); Check("Search");
            MaterialSwitch.Draw("switch","Enabled",ref enabled); Check("Switch");
            MaterialSelection.Checkbox("checkbox","Selected",ref enabled); Check("Checkbox");
            ImGui.End();
        });
    }

    [Fact]
    public void CompatibilityThemePreservesHostLayoutAndNestedScopesRestoreIt()
    {
        using var host = new NativeTestContext();
        var theme = new MaterialTheme(new(.3f,.7f,.4f));
        var style = ImGui.GetStyle();
        style.FramePadding=new(3,2); style.WindowPadding=new(7,9);
        style.CellPadding=new(2,1); style.ItemSpacing=new(5,4);
        var frame=style.FramePadding; var window=style.WindowPadding; var cell=style.CellPadding; var spacing=style.ItemSpacing;
        host.Frame(() =>
        {
            using (var compatible = MaterialTheme.Push(theme))
            {
                Assert.Equal(frame,style.FramePadding); Assert.Equal(window,style.WindowPadding);
                Assert.Equal(cell,style.CellPadding); Assert.Equal(spacing,style.ItemSpacing);
                using (var dense = MaterialTable.PushControls())
                {
                    var densePadding=style.FramePadding;
                    Assert.Equal(MaterialControlContext.Dense,MaterialControls.Context);
                    using (var regular = MaterialControls.Push(MaterialControlContext.Regular))
                        Assert.True(style.FramePadding.Y > densePadding.Y);
                    Assert.Equal(densePadding,style.FramePadding);
                    using (var nested = MaterialTheme.Push(theme,1.5f,MaterialStyleMode.Material))
                        Assert.Equal(MaterialControlContext.Regular,MaterialControls.Context);
                    Assert.Equal(MaterialControlContext.Dense,MaterialControls.Context);
                    Assert.Equal(densePadding,style.FramePadding);
                }
                Assert.Equal(MaterialControlContext.Regular,MaterialControls.Context);
                Assert.Equal(frame,style.FramePadding);
            }
            Assert.Equal(frame,style.FramePadding); Assert.Equal(window,style.WindowPadding);
            Assert.Equal(cell,style.CellPadding); Assert.Equal(spacing,style.ItemSpacing);
        });
    }

    [Fact]
    public void WidthResolutionSeparatesMeasuredPixelsFromLogicalRequests()
    {
        Assert.Equal(80,MaterialLayout.ResolveWidth(-1,30,80,2));
        Assert.Equal(50,MaterialLayout.ResolveWidth(0,50,80,2));
        Assert.Equal(80,MaterialLayout.ResolveWidth(100,50,80,2));
        Assert.Equal(200,MaterialLayout.ResolveWidth(100,50,80,2,false));
        Assert.Equal(1,MaterialLayout.ResolveWidth(-1,50,-5,2));
        Assert.Throws<ArgumentOutOfRangeException>(()=>MaterialLayout.ResolveWidth(float.NaN,20,80,1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>MaterialControlMetrics.Measure(new(MaterialDensity.Compact,1),float.NaN));
    }

    [Fact]
    public void ActionTileMeasuresWrappedContentAndHonorsExplicitFixedMode()
    {
        using var host = new NativeTestContext(1200,1200,2);
        var theme = new MaterialTheme(new(.3f,.7f,.4f));
        host.Frame(() =>
        {
            using var scope=MaterialTheme.Push(theme);
            ImGui.SetNextWindowSize(new(1100,1100));
            ImGui.Begin("Tile sizing",ImGuiWindowFlags.NoSavedSettings);
            const string label="A longer translated action label";
            const string detail="Status text needs room below the action.";
            var requested=new Vector2(240,72);
            var measured=MaterialActionTile.Measure(label,detail,requested);
            Assert.True(measured.Y>requested.Y);
            var narrow=MaterialActionTile.Measure(label,detail,new(120,72));
            Assert.True(narrow.Y>measured.Y);
            MaterialActionTile.Draw("tile",MaterialIcon.Play,label,detail,requested);
            Assert.Equal(measured,ImGui.GetItemRectSize());
            Assert.Equal(72,MaterialActionTile.Measure(label,detail,requested,MaterialTileSizing.Fixed).Y);
            ImGui.End();
        });
    }

    [Fact]
    public void NarrowFieldCanOpenWiderSearchablePopup()
    {
        using var host=new NativeTestContext();
        var theme=new MaterialTheme(new(.3f,.7f,.4f));
        var options=new MaterialOptions<int>([new("a",0,"First long option"),new("b",1,"Second long option")]);
        var value=0; Vector2 point=default;
        void Draw()
        {
            using var scope=MaterialTheme.Push(theme);
            ImGui.SetNextWindowPos(new(20,20)); ImGui.SetNextWindowSize(new(240,500));
            ImGui.Begin("Narrow plugin",ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoTitleBar);
            var available=ImGui.GetContentRegionAvail().X;
            MaterialCombo.Draw("combo",options,ref value,new MaterialComboLayout(360,4),width:800);
            if(options.Query.Length==0) point=(ImGui.GetItemRectMin()+ImGui.GetItemRectMax())*.5f;
            Assert.InRange(ImGui.GetItemRectSize().X,1,available+.01f);
            ImGui.End();
        }
        host.Frame(Draw); host.Frame(Draw); host.Click(point,Draw); host.Frame(Draw);
        foreach(var ch in "Second") ImGui.GetIO().AddInputCharacter(ch);
        host.Frame(Draw); host.Frame(Draw);
        Assert.Equal("Second",options.Query);
        Assert.Equal(1,options.Count);
        if(Environment.GetEnvironmentVariable("MATERIALUI_PREVIEW_DIR") is { Length:>0 } directory)
            SoftwarePreview.Save(Path.Combine(directory,"narrow-field-wide-popup.png"),host);
    }

    [Fact]
    public void NarrowNumberKeepsEditorWhileWideNumberRetainsStepButtons()
    {
        using var host=new NativeTestContext(1200,800,1.5f);
        var theme=new MaterialTheme(new(.3f,.7f,.4f),MaterialDensity.Standard);
        var value=12; var narrow=true; Vector2 right=default;
        void Draw()
        {
            using var scope=MaterialTheme.Push(theme,1.5f);
            ImGui.SetNextWindowPos(new(20,20)); ImGui.SetNextWindowSize(new(900,600));
            ImGui.Begin("Steppers",ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoTitleBar);
            MaterialInput.Integer("value",ref value,width:narrow?95:240);
            right=ImGui.GetItemRectMax()-new Vector2(5,ImGui.GetItemRectSize().Y*.5f);
            ImGui.End();
        }
        host.Frame(Draw); host.Frame(Draw);
        host.Click(right,Draw); Assert.Equal(12,value);
        narrow=false; host.Frame(Draw); host.Frame(Draw);
        host.Click(right,Draw); Assert.Equal(13,value);
    }

    [Fact]
    public void TinyExplicitGlyphAndImageButtonsKeepTheirBoundsAndActivate()
    {
        using var host=new NativeTestContext();
        var theme=new MaterialTheme(new(.3f,.7f,.4f));
        Vector2 glyph=default; var clicked=false;
        void Draw()
        {
            using var scope=MaterialTheme.Push(theme);
            ImGui.SetNextWindowPos(new(20,20)); ImGui.SetNextWindowSize(new(600,400));
            ImGui.Begin("Small icons",ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoTitleBar);
            clicked|=MaterialCanvas.GlyphButton("glyph","W",ImGui.GetFont(),size:8);
            Assert.Equal(new Vector2(8),ImGui.GetItemRectSize());
            glyph=(ImGui.GetItemRectMin()+ImGui.GetItemRectMax())*.5f;
            MaterialButton.IconButton("vector",MaterialIcon.Edit,size:8);
            Assert.Equal(new Vector2(8),ImGui.GetItemRectSize());
            MaterialCanvas.ImageButton("image",new ImTextureID((nint)1),new(8));
            Assert.Equal(new Vector2(8),ImGui.GetItemRectSize());
            var value="";
            MaterialTextField.SearchField("tiny",ref value,width:4);
            Assert.InRange(ImGui.GetItemRectSize().X,1,4.1f);
            ImGui.End();
        }
        host.Frame(Draw); host.Frame(Draw); host.Click(glyph,Draw);
        Assert.True(clicked);
    }

    [Fact]
    public void InvalidScopeAndComboArgumentsDoNotDisturbCurrentContext()
    {
        using var host=new NativeTestContext(); var theme=new MaterialTheme(new(.3f,.7f,.4f));
        host.Frame(() =>
        {
            using var scope=MaterialTheme.Push(theme);
            using var dense=MaterialControls.Push(MaterialControlContext.Dense);
            var before=ImGui.GetStyle().FramePadding;
            Assert.Throws<ArgumentOutOfRangeException>(()=> { using var invalid=MaterialControls.Push((MaterialControlContext)99); });
            Assert.Throws<ArgumentOutOfRangeException>(()=> { using var invalid=MaterialTheme.Push(theme,1,(MaterialStyleMode)99); });
            Assert.Equal(before,ImGui.GetStyle().FramePadding);
            Assert.Equal(MaterialControlContext.Dense,MaterialControls.Context);
        });
    }
}
