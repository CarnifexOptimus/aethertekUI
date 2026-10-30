using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.ImGuiFileDialog;
using AethertekUI.Dalamud;

namespace AethertekUI.Demo;

public sealed class Plugin : IDalamudPlugin
{
    private readonly IDalamudPluginInterface pluginInterface;
    private readonly ICommandManager commands;
    private readonly Showcase showcase = new();
    private readonly AethertekUI.Dalamud.MaterialTextHost shapedText;
    private readonly IDalamudTextureWrap sampleImage;
    private readonly ITextureProvider textures;
    private readonly MaterialGlassRenderer backgroundGlass;
    private readonly FileDialogManager imagePicker = new();
    public Plugin(IDalamudPluginInterface pluginInterface,ICommandManager commands,ITextureProvider textures,IPluginLog log)
    {
        this.pluginInterface=pluginInterface;
        this.commands=commands;
        this.textures=textures;
        backgroundGlass=new(pluginInterface,log);
        showcase.BackgroundRenderer=backgroundGlass;
        showcase.BrowseBackgroundImage=() => imagePicker.OpenFileDialog("Choose background image",
            "Images{.png,.jpg,.jpeg,.bmp,.gif,.webp,.tif,.tiff,.dds,.tga},.*",
            (accepted,path) => { if(accepted) showcase.SelectBackgroundImage(path); });
        shapedText=new(textures);
        // Diagnostic colour/alpha swatch: the host owns it, while core borrows its handle.
        sampleImage = textures.CreateFromRaw(RawImageSpecification.Rgba32(2, 2),
            new byte[] { 255, 80, 80, 255, 80, 255, 80, 255, 80, 80, 255, 255, 255, 255, 255, 0 },
            "AethertekUI borrowed image demonstration");
        showcase.BorrowedImage = sampleImage.Handle;
        showcase.BorrowedImageSize = new(2, 2);
        showcase.HindiRenderer=shapedText.Renderer;
        commands.AddHandler("/aethertekui",new CommandInfo(OnCommand) { HelpMessage="Open the AethertekUI Showcase; add 'hindi' for the shaped editing preview (mock data only)." });
        pluginInterface.UiBuilder.Draw+=Draw;
        pluginInterface.UiBuilder.OpenMainUi+=Open;
        pluginInterface.UiBuilder.OpenConfigUi+=OpenSettings;
        pluginInterface.UiBuilder.ResizeBuffers+=ReleaseBackground;
    }
    private void OnCommand(string command,string arguments)
    {
        if(arguments.Trim().Equals("hindi",StringComparison.OrdinalIgnoreCase)) showcase.HindiPreviewOpen=true;
        else Open();
    }
    private void Open() => showcase.IsOpen=true;
    private void OpenSettings() { showcase.Page=4; Open(); }
    private void ReleaseBackground() => backgroundGlass.ReleaseResources();
    private void Draw()
    {
        imagePicker.Draw();
        var settings=showcase.BackgroundSettings;
        var image=settings.Mode==MaterialColorMode.Glassmorphism && settings.GlassBackground==MaterialGlassBackground.Image;
        var enabled=showcase.HasOpenWindows && (image || settings.HasBackgroundBlur(settings.Mode));
        backgroundGlass.ConfigureImage(textures,image,settings.GlassImagePath);
        if(enabled) backgroundGlass.BeginFrame();
        var completed=false;
        try { showcase.Draw(global::Dalamud.Interface.Utility.ImGuiHelpers.GlobalScale); completed=true; }
        finally { backgroundGlass.EndFrame(enabled && completed); }
    }
    public void Dispose()
    {
        pluginInterface.UiBuilder.Draw-=Draw;
        pluginInterface.UiBuilder.OpenMainUi-=Open;
        pluginInterface.UiBuilder.OpenConfigUi-=OpenSettings;
        pluginInterface.UiBuilder.ResizeBuffers-=ReleaseBackground;
        imagePicker.Reset();
        backgroundGlass.Dispose();
        showcase.BackgroundRenderer=null;
        showcase.BrowseBackgroundImage=null;
        commands.RemoveHandler("/aethertekui");
        showcase.HindiRenderer=null;
        shapedText.Dispose();
        showcase.BorrowedImage = default;
        sampleImage.Dispose();
    }
}
