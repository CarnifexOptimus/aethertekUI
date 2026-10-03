using System.Numerics;
using Dalamud.Bindings.ImGui;
using Xunit;

namespace AethertekUI.Tests;

public class IntegrationNativeTests
{
    [Theory] [InlineData(false)] [InlineData(true)]
    public void SearchableComboFiltersCachedOptionsUsingNativeTextEntry(bool appearance)
    {
        using var context=new NativeTestContext();
        var entries=new MaterialOption<int>[500];
        for(var i=0;i<entries.Length;i++) entries[i]=new($"key-{i}",i,$"Recipe {i:000}");
        var options=new MaterialOptions<int>(entries);
        var selected=0;Vector2 combo=default;
        void Draw()=>Window(()=>
        {
            if(appearance) MaterialCombo.Draw("recipes",options,ref selected,new MaterialControlAppearance(theme.Colors.Surface,theme.Colors.OnSurface,theme.Colors.Outline),width:300);
            else MaterialCombo.Draw("recipes",options,ref selected,width:300);
            if(options.Query.Length==0) combo=(ImGui.GetItemRectMin()+ImGui.GetItemRectMax())*.5f;
        });
        context.Frame(Draw);context.Frame(Draw);context.Click(combo,Draw);
        context.Frame(Draw);context.Frame(Draw);
        foreach(var ch in "499") ImGui.GetIO().AddInputCharacter(ch);
        context.Frame(Draw);
        Assert.Equal("499",options.Query);
        Assert.Equal(1,options.Count);
        Assert.Equal(499,options[0].Value);
    }
    [Fact]
    public void SplitterMovesAndBorrowedIconHelpersRender()
    {
        using var context=new NativeTestContext();
        var width=180f;Vector2 divider=default;
        void Draw()=>Window(()=>
        {
            ImGui.Dummy(new(width,80));ImGui.SameLine(0,0);
            MaterialSplitPane.Divider("split",ref width,600,100,100,80);
            divider=(ImGui.GetItemRectMin()+ImGui.GetItemRectMax())*.5f;
            MaterialCanvas.GlyphButton("glyph","+",ImGui.GetFont());
            MaterialCanvas.ImageButton("texture",new ImTextureID((nint)1),new(32));
            MaterialCanvas.Image(new ImTextureID((nint)1),new(32));
        });
        context.Frame(Draw);context.Frame(Draw);
        ImGui.GetIO().AddMousePosEvent(divider.X,divider.Y);context.Frame(Draw);
        ImGui.GetIO().AddMouseButtonEvent(0,true);context.Frame(Draw);
        ImGui.GetIO().AddMousePosEvent(divider.X+40,divider.Y);context.Frame(Draw);
        ImGui.GetIO().AddMouseButtonEvent(0,false);context.Frame(Draw);
        Assert.InRange(width,219,221);
    }
    private readonly MaterialTheme theme=new(new Vector3(.3f,.7f,.4f));
    private void Window(Action content, bool applyMetrics=true)
    {
        using var scope=MaterialTheme.Push(theme,1,applyMetrics);
        ImGui.SetNextWindowPos(Vector2.Zero);ImGui.SetNextWindowSize(new(700,650));
        ImGui.Begin("Integration tests",ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoTitleBar);
        content();
        ImGui.End();
    }
    [Fact]
    public void ColorsOnlyScopePreservesHostMetricsAndSemanticOverridesRestore()
    {
        using var context=new NativeTestContext();
        context.Frame(()=>
        {
            var padding=ImGui.GetStyle().WindowPadding;
            var rounding=ImGui.GetStyle().FrameRounding;
            var text=ImGui.GetStyle().Colors[(int)ImGuiCol.Text];
            using(var scope=MaterialTheme.Push(theme,1.5f,false))
            {
                Assert.Equal(padding,ImGui.GetStyle().WindowPadding);
                Assert.Equal(rounding,ImGui.GetStyle().FrameRounding);
                var domain=new Vector4(1,.2f,.1f,1);
                using(var semantic=MaterialSemantic.Text(domain))
                    Assert.Equal(domain,ImGui.GetStyle().Colors[(int)ImGuiCol.Text]);
                Assert.Equal(theme.Colors.OnSurface,ImGui.GetStyle().Colors[(int)ImGuiCol.Text]);
            }
            Assert.Equal(text,ImGui.GetStyle().Colors[(int)ImGuiCol.Text]);
        });
    }
    [Fact]
    public void SearchClearRestoresTypingFocus()
    {
        using var context=new NativeTestContext();
        var query="ore";var state=new MaterialSearchState();Vector2 clear=default;
        void Draw()=>Window(()=>
        {
            MaterialTextField.SearchField("search",ref query,width:300,state:state);
            clear=new(ImGui.GetItemRectMax().X-14,(ImGui.GetItemRectMin().Y+ImGui.GetItemRectMax().Y)*.5f);
        });
        context.Frame(Draw);context.Frame(Draw);
        context.Click(clear,Draw);
        Assert.Empty(query);
        context.Frame(Draw);context.Frame(Draw);
        ImGui.GetIO().AddInputCharacter('x');context.Frame(Draw);
        Assert.Equal("x",query);
    }
    [Fact]
    public void IntegerClampsAndDisabledEditorDoesNotChange()
    {
        using var context=new NativeTestContext();
        var count=3;var disabled=false;Vector2 plus=default;
        void Draw()=>Window(()=>
        {
            MaterialInput.Integer("quantity",ref count,1,4,width:200,disabled:disabled);
            plus=new(ImGui.GetItemRectMax().X-8,(ImGui.GetItemRectMin().Y+ImGui.GetItemRectMax().Y)*.5f);
        });
        context.Frame(Draw);context.Frame(Draw);
        context.Click(plus,Draw);Assert.Equal(4,count);
        context.Click(plus,Draw);Assert.Equal(4,count);
        count=2;disabled=true;context.Click(plus,Draw);Assert.Equal(2,count);
    }
    [Fact]
    public void NativeClipperSubmitsVisibleRowsAndRequestedSelectionOnly()
    {
        using var context=new NativeTestContext();
        var submitted=0;var requested=false;
        void Draw()=>Window(()=>
        {
            using var surface=MaterialSurface.Card("rows",new(300,200));
            if(!surface.Visible)return;
            submitted=0;requested=false;
            using var clipper=new MaterialClipper(10_000,ImGui.GetTextLineHeightWithSpacing());
            clipper.Include(9000);
            while(clipper.Step())
                for(var i=clipper.Start;i<clipper.End;i++)
                { submitted++;requested|=i==9000;ImGui.TextUnformatted("Uniform row"); }
        });
        context.Frame(Draw);context.Frame(Draw);
        Assert.InRange(submitted,1,40);Assert.True(requested);
    }
    [Fact]
    public void ExtendedWidgetsSupportNestedScopesAndReadOnlyText()
    {
        using var context=new NativeTestContext();
        var text="Keep this text";var number=1f;ulong flags=1;bool? inherited=null;
        var options=new MaterialOptions<int>([new("a",0,"First"),new("b",1,"Second")]);var selected=0;
        void Draw()=>Window(()=>
        {
            MaterialCombo.Draw("combo",options,ref selected);
            ImGui.SetNextItemOpen(true);
            using(var tree=MaterialTree.Node("tree","Folder"))
                if(tree.Visible)
                {
                    MaterialInput.Multiline("notes",ref text,new(300,80),flags:ImGuiInputTextFlags.ReadOnly);
                    MaterialInput.Number("number",ref number,0,10,width:200);
                    MaterialInput.Slider("slider",ref number,0,10,width:200);
                    MaterialOverride.Switch("override",ref inherited,300);
                    MaterialFlags.Draw("flags",[new("a","One",1),new("b","Two",2)],ref flags);
                    using var menu=MaterialContextMenu.Begin("context");
                    if(menu.Visible) ImGui.TextUnformatted("Menu");
                }
        });
        context.Frame(Draw);context.Frame(Draw);
        Assert.Equal("Keep this text",text);
        Assert.Null(inherited);
    }
    [Fact]
    public void DragDropCopiesTypedPayloadAndDeliversOnlyOnRelease()
    {
        using var context=new NativeTestContext();
        Vector2 source=default,target=default;var result=-1;
        void Draw()=>Window(()=>
        {
            ImGui.Button("Source",new(120,30));
            source=(ImGui.GetItemRectMin()+ImGui.GetItemRectMax())*.5f;
            var key=42;MaterialDragDrop.Source("CR_TEST",in key,"Moving");
            ImGui.Button("Target",new(120,30));
            target=(ImGui.GetItemRectMin()+ImGui.GetItemRectMax())*.5f;
            if(MaterialDragDrop.TryAccept<int>("CR_TEST",out var received)) result=received;
        });
        context.Frame(Draw);context.Frame(Draw);
        ImGui.GetIO().AddMousePosEvent(source.X,source.Y);context.Frame(Draw);
        ImGui.GetIO().AddMouseButtonEvent(0,true);context.Frame(Draw);
        ImGui.GetIO().AddMousePosEvent(source.X+15,source.Y);context.Frame(Draw);
        ImGui.GetIO().AddMousePosEvent(target.X,target.Y);context.Frame(Draw);context.Frame(Draw);
        Assert.Equal(-1,result);
        ImGui.GetIO().AddMouseButtonEvent(0,false);context.Frame(Draw);context.Frame(Draw);
        Assert.Equal(42,result);
    }
}
