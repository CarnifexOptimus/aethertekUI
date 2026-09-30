using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace CombatReborn.MaterialUI.Demo;

public sealed class Plugin : IDalamudPlugin
{
    private readonly IDalamudPluginInterface pluginInterface;
    private readonly ICommandManager commands;
    private readonly Showcase showcase = new();
    public Plugin(IDalamudPluginInterface pluginInterface,ICommandManager commands)
    {
        this.pluginInterface=pluginInterface;
        this.commands=commands;
        commands.AddHandler("/crmaterial",new CommandInfo(OnCommand) { HelpMessage="Open the standalone MaterialUI showcase (mock data only)." });
        pluginInterface.UiBuilder.Draw+=Draw;
        pluginInterface.UiBuilder.OpenMainUi+=Open;
        pluginInterface.UiBuilder.OpenConfigUi+=OpenSettings;
    }
    private void OnCommand(string command,string arguments) => Open();
    private void Open() => showcase.IsOpen=true;
    private void OpenSettings() { showcase.Page=4; Open(); }
    private void Draw() => showcase.Draw(Dalamud.Interface.Utility.ImGuiHelpers.GlobalScale);
    public void Dispose()
    {
        pluginInterface.UiBuilder.Draw-=Draw;
        pluginInterface.UiBuilder.OpenMainUi-=Open;
        pluginInterface.UiBuilder.OpenConfigUi-=OpenSettings;
        commands.RemoveHandler("/crmaterial");
    }
}
