using System.Numerics;
using Dalamud.Bindings.ImGui;
using Xunit;

namespace CombatReborn.MaterialUI.Tests;

public unsafe class NativeInteractionTests
{
    [Theory] [InlineData(false)] [InlineData(true)]
    public void MouseActivatesSwitchAndSegmentButDisabledControlsDoNotChange(bool disabled)
    {
        using var host=new NativeTestContext();
        var theme=new MaterialTheme(new(.3f,.7f,.4f));
        var enabled=false; var selected=0;
        MaterialSegment<int>[] options=[new(0,"Auto"),new(1,"Manual"),new(2,"Off")];
        Vector2 switchCenter=default,segmentCenter=default;
        void Draw()
        {
            using var scope=MaterialTheme.Push(theme);
            ImGui.SetNextWindowPos(new(0,0)); ImGui.SetNextWindowSize(new(600,400));
            ImGui.Begin("Interactions",ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoTitleBar);
            MaterialSwitch.Draw("switch","Enabled",ref enabled,disabled);
            switchCenter=(ImGui.GetItemRectMin()+ImGui.GetItemRectMax())*.5f;
            MaterialSegmented.Draw<int>("segments",options,ref selected,300,disabled);
            segmentCenter=(ImGui.GetItemRectMin()+ImGui.GetItemRectMax())*.5f;
            ImGui.End();
        }
        host.Frame(Draw); host.Frame(Draw);
        host.Click(switchCenter,Draw);
        Assert.Equal(!disabled,enabled);
        host.Click(segmentCenter,Draw);
        Assert.Equal(disabled?0:1,selected);
    }
    [Fact]
    public void SearchEditingAndInlineClearUseNativeInput()
    {
        using var host=new NativeTestContext();
        var theme=new MaterialTheme(new(.3f,.7f,.4f));
        var query=""; Vector2 min=default,max=default;
        void Draw()
        {
            using var scope=MaterialTheme.Push(theme);
            ImGui.SetNextWindowPos(new(0,0)); ImGui.SetNextWindowSize(new(600,400));
            ImGui.Begin("Search",ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoTitleBar);
            MaterialTextField.SearchField("search",ref query,width:300);
            min=ImGui.GetItemRectMin(); max=ImGui.GetItemRectMax();
            ImGui.End();
        }
        host.Frame(Draw);host.Frame(Draw);
        host.Click(min+new Vector2(80,14),Draw);
        foreach(var character in "ore") ImGui.GetIO().AddInputCharacter(character);
        host.Frame(Draw);
        Assert.Equal("ore",query);
        host.Click(new Vector2(max.X-14,(min.Y+max.Y)*.5f),Draw);
        Assert.Equal("",query);
    }
    [Fact]
    public void StableIdSurvivesLabelChangeAndKeyboardActivation()
    {
        using var host=new NativeTestContext();
        ImGui.GetIO().ConfigFlags|=ImGuiConfigFlags.NavEnableKeyboard;
        var theme=new MaterialTheme(new(.3f,.7f,.4f));
        var label="Start";var activated=false; var requestFocus=true; uint initialId=0;
        void Draw()
        {
            using var scope=MaterialTheme.Push(theme);
            ImGui.SetNextWindowPos(new(0,0));ImGui.SetNextWindowSize(new(600,400));
            ImGui.Begin("Keyboard",ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoTitleBar);
            ImGui.PushID("stable");
            var id=ImGui.GetID("##button");
            if(initialId==0) initialId=id; else Assert.Equal(initialId,id);
            ImGui.PopID();
            if(requestFocus) ImGui.SetKeyboardFocusHere();
            activated|=MaterialButton.FilledButton("stable",label);
            ImGui.End();
        }
        host.Frame(Draw);host.Frame(Draw);requestFocus=false;
        label="Stop";
        ImGui.GetIO().AddKeyEvent(ImGuiKey.Space,true);host.Frame(Draw);
        ImGui.GetIO().AddKeyEvent(ImGuiKey.Space,false);host.Frame(Draw);
        Assert.True(activated);
    }
}

internal sealed unsafe class NativeTestContext : IDisposable
{
    private readonly ImGuiContextPtr context;
    public byte* Pixels { get; private set; }
    public int AtlasWidth { get; private set; }
    public int AtlasHeight { get; private set; }
    public NativeTestContext(int width=1100,int height=850,float scale=1)
    {
        context=ImGui.CreateContext();
        var io=ImGui.GetIO();io.IniFilename=null;io.LogFilename=null;
        io.DisplaySize=new(width,height);io.DeltaTime=1f/60;io.FontGlobalScale=scale;
        io.Fonts.AddFontDefault();
        byte* pixels=null;int aw=0,ah=0;
        io.Fonts.GetTexDataAsRGBA32(0,&pixels,&aw,&ah);io.Fonts.SetTexID(0,new ImTextureID((nint)1));
        Pixels=pixels;AtlasWidth=aw;AtlasHeight=ah;
    }
    public void RefreshAtlas()
    {
        byte* pixels=null;int width=0,height=0;
        ImGui.GetIO().Fonts.GetTexDataAsRGBA32(0,&pixels,&width,&height);
        Pixels=pixels;AtlasWidth=width;AtlasHeight=height;
    }
    public void Frame(Action draw)
    {
        ImGui.NewFrame();
        draw();
        ImGui.Render();
    }
    public void Click(Vector2 point,Action draw)
    {
        ImGui.GetIO().AddMousePosEvent(point.X,point.Y);Frame(draw);
        ImGui.GetIO().AddMouseButtonEvent(0,true);Frame(draw);
        ImGui.GetIO().AddMouseButtonEvent(0,false);Frame(draw);
    }
    public void Dispose() => ImGui.DestroyContext(context);
}
