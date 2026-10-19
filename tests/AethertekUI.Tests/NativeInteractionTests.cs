using System.Numerics;
using System.Runtime.InteropServices;
using System.Reflection;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Bindings.ImGui;
using Xunit;

namespace AethertekUI.Tests;

public unsafe class NativeInteractionTests
{
    [Fact]
    public void XaSlaveShortcutPreservesExporterAndOnlyOpensLoadedProviderOnExplicitClick()
    {
        using var host = new NativeTestContext();
        var theme = new MaterialTheme(new Vector3(.3f, .7f, .4f));
        var support = new Dalamud.MaterialSupportLog();
        var pluginInterface = DispatchProxy.Create<IDalamudPluginInterface, ShortcutServicesProxy>();
        var commands = DispatchProxy.Create<ICommandManager, ShortcutServicesProxy>();
        var commandProxy = (ShortcutServicesProxy)commands;
        commandProxy.Results["ProcessCommand"] = true;
        var pluginType = typeof(IDalamudPluginInterface).GetProperty("InstalledPlugins")!
            .PropertyType.GetGenericArguments().Single();
        var provider = DispatchProxy.Create(pluginType, typeof(ShortcutServicesProxy));
        var providerProxy = (ShortcutServicesProxy)provider;
        providerProxy.Results["get_InternalName"] = "XASlave";
        providerProxy.Results["get_IsLoaded"] = false;
        var providers = Array.CreateInstance(pluginType, 1);
        providers.SetValue(provider, 0);
        var interfaceProxy = (ShortcutServicesProxy)pluginInterface;
        interfaceProxy.Results["get_InstalledPlugins"] = Array.CreateInstance(pluginType, 0);
        var includeShortcut = false;
        var folderActions = 0;
        var errors = 0;
        Vector2 lastCenter = default;
        uint lastId = 0;

        void Draw()
        {
            using var scope = MaterialTheme.Push(theme);
            ImGui.SetNextWindowPos(new(0, 0));
            ImGui.SetNextWindowSize(new(650, 400));
            ImGui.Begin("Support actions", ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoTitleBar);
            if (includeShortcut)
                support.Draw(pluginInterface, key => key, _ => folderActions++, _ => errors++, commands);
            else support.Draw(pluginInterface, key => key, _ => folderActions++, _ => errors++);
            lastId = ImGui.GetCurrentContext().LastItemData.ID;
            lastCenter = (ImGui.GetItemRectMin() + ImGui.GetItemRectMax()) * .5f;
            ImGui.End();
        }

        host.Frame(Draw); host.Frame(Draw);
        var exporterId = lastId;
        includeShortcut = true;
        host.Frame(Draw);
        Assert.Equal(exporterId, lastId); // A missing provider keeps the original exporter.
        interfaceProxy.Results["get_InstalledPlugins"] = providers;
        host.Frame(Draw);
        Assert.Equal(exporterId, lastId); // An unloaded provider adds no replacement control.
        providerProxy.Results["get_IsLoaded"] = true;
        host.Frame(Draw); host.Frame(Draw);
        Assert.NotEqual(exporterId, lastId);
        var shortcutCenter = lastCenter;
        Assert.Empty(commandProxy.Commands);
        host.Click(shortcutCenter, Draw);
        Assert.Equal(new[] { "/xa mods" }, commandProxy.Commands);
        commandProxy.Results["ProcessCommand"] = false;
        host.Click(shortcutCenter, Draw);
        Assert.Equal(new[] { "/xa mods", "/xa mods" }, commandProxy.Commands);
        host.Frame(Draw); host.Frame(Draw);
        Assert.Equal(2, commandProxy.Commands.Count); // Rejection never dispatches another command.
        providerProxy.Results["get_IsLoaded"] = false;
        host.Frame(Draw);
        Assert.Equal(exporterId, lastId);
        Assert.Equal(0, folderActions);
        Assert.Equal(0, errors);
    }

    public class ShortcutServicesProxy : DispatchProxy
    {
        public readonly Dictionary<string, object?> Results = new();
        public readonly List<string> Commands = new();
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method!.Name == "ProcessCommand") Commands.Add((string)args![0]!);
            return Results.TryGetValue(method.Name, out var result) ? result
                : throw new NotSupportedException(method.Name);
        }
    }

    [Theory] [InlineData(false,false)] [InlineData(true,false)] [InlineData(false,true)] [InlineData(true,true)]
    public void MouseActivatesSwitchAndSegmentButDisabledControlsDoNotChange(bool disabled,bool keepIcons)
    {
        using var host=new NativeTestContext();
        var theme=new MaterialTheme(new Vector3(.3f,.7f,.4f));
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
            if(keepIcons) MaterialSegmented.Draw<int>("segments",options,ref selected,300,disabled,false,6);
            else MaterialSegmented.Draw<int>("segments",options,ref selected,300,disabled);
            segmentCenter=(ImGui.GetItemRectMin()+ImGui.GetItemRectMax())*.5f;
            ImGui.End();
        }
        host.Frame(Draw); host.Frame(Draw);
        host.Click(switchCenter,Draw);
        Assert.Equal(!disabled,enabled);
        host.Click(segmentCenter,Draw);
        Assert.Equal(disabled?0:1,selected);
    }
    [Theory] [InlineData(false)] [InlineData(true)]
    public void SearchEditingAndInlineClearUseNativeInput(bool appearance)
    {
        using var host=new NativeTestContext();
        var theme=new MaterialTheme(new Vector3(.3f,.7f,.4f));
        var query=""; Vector2 min=default,max=default;
        void Draw()
        {
            using var scope=MaterialTheme.Push(theme);
            ImGui.SetNextWindowPos(new(0,0)); ImGui.SetNextWindowSize(new(600,400));
            ImGui.Begin("Search",ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoTitleBar);
            if(appearance) MaterialTextField.SearchField("search",ref query,new MaterialControlAppearance(theme.Colors.Surface,theme.Colors.OnSurface,theme.Colors.Outline),width:300);
            else MaterialTextField.SearchField("search",ref query,width:300);
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
    public void SearchHonorsConsumerSuppliedInputLimit()
    {
        using var host=new NativeTestContext();
        var theme=new MaterialTheme(new Vector3(.3f,.7f,.4f));
        var query=""; Vector2 origin=default;
        void Draw()
        {
            using var scope=MaterialTheme.Push(theme);
            ImGui.SetNextWindowPos(new(0,0)); ImGui.SetNextWindowSize(new(600,400));
            ImGui.Begin("Limited search",ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoTitleBar);
            MaterialTextField.SearchField("search",ref query,width:300,maxLength:16);
            origin=ImGui.GetItemRectMin();
            ImGui.End();
        }
        host.Frame(Draw); host.Frame(Draw);
        host.Click(origin+new Vector2(80,14),Draw);
        foreach(var character in new string('x',100)) ImGui.GetIO().AddInputCharacter(character);
        host.Frame(Draw);
        Assert.InRange(query.Length,1,16);
        Assert.All(query,character=>Assert.Equal('x',character));
    }
    [Fact]
    public void StableIdSurvivesLabelChangeAndKeyboardActivation()
    {
        using var host=new NativeTestContext();
        ImGui.GetIO().ConfigFlags|=ImGuiConfigFlags.NavEnableKeyboard;
        var theme=new MaterialTheme(new Vector3(.3f,.7f,.4f));
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
    private readonly Dictionary<ulong, RegisteredTexture> textures = [];
    private nint nextTextureId = 2;
    public int ReleasedTextures { get; private set; }
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
    public SoftwarePreview.Texture Texture(ImTextureID id)
    {
        if(id.Handle==1) return new((nint)Pixels,AtlasWidth,AtlasHeight);
        if(textures.TryGetValue(id.Handle,out var texture)) return texture.Texture;
        throw new InvalidOperationException("Unknown diagnostic texture.");
    }
    public RegisteredTexture RegisterTexture(byte[] pixels, int width, int height)
    {
        if(width<1 || height<1 || pixels.Length!=checked(width*height*4)) throw new ArgumentException("Invalid diagnostic texture.");
        var texture = new RegisteredTexture(this, nextTextureId++, pixels, width, height);
        textures.Add(texture.Id.Handle, texture);
        return texture;
    }
    internal sealed class RegisteredTexture : IDisposable
    {
        private readonly NativeTestContext owner;
        private GCHandle pixels;
        private bool disposed;
        public ImTextureID Id { get; }
        public SoftwarePreview.Texture Texture { get; }
        public RegisteredTexture(NativeTestContext owner,nint id,byte[] bytes,int width,int height)
        {
            this.owner=owner;
            Id=new ImTextureID(id);
            pixels=GCHandle.Alloc(bytes,GCHandleType.Pinned);
            Texture=new SoftwarePreview.Texture(pixels.AddrOfPinnedObject(),width,height);
        }
        public void Dispose()
        {
            if(disposed) return;
            disposed=true;
            owner.textures.Remove(Id.Handle);
            pixels.Free();
            owner.ReleasedTextures++;
        }
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
    public void Dispose()
    {
        foreach(var texture in textures.Values.ToArray()) texture.Dispose();
        ImGui.DestroyContext(context);
    }
}
