using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;

namespace AethertekUI.Demo;

public sealed class Plugin : IDalamudPlugin
{
    private readonly IDalamudPluginInterface pluginInterface;
    private readonly ICommandManager commands;
    private readonly Showcase showcase = new();
    private readonly AethertekUI.Dalamud.MaterialTextHost shapedText;
    private readonly IDalamudTextureWrap sampleImage;
    public Plugin(IDalamudPluginInterface pluginInterface,ICommandManager commands,ITextureProvider textures)
    {
        this.pluginInterface=pluginInterface;
        this.commands=commands;
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
    }
    private void OnCommand(string command,string arguments)
    {
        if(arguments.Trim().Equals("hindi",StringComparison.OrdinalIgnoreCase)) showcase.HindiPreviewOpen=true;
        else Open();
    }
    private void Open() => showcase.IsOpen=true;
    private void OpenSettings() { showcase.Page=4; Open(); }
    private void Draw() => showcase.Draw(global::Dalamud.Interface.Utility.ImGuiHelpers.GlobalScale);
    public void Dispose()
    {
        pluginInterface.UiBuilder.Draw-=Draw;
        pluginInterface.UiBuilder.OpenMainUi-=Open;
        pluginInterface.UiBuilder.OpenConfigUi-=OpenSettings;
        commands.RemoveHandler("/aethertekui");
        showcase.HindiRenderer=null;
        shapedText.Dispose();
        showcase.BorrowedImage = default;
        sampleImage.Dispose();
    }
}
