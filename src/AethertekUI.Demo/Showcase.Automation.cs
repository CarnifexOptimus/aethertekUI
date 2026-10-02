using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace AethertekUI.Demo;

internal sealed partial class Showcase
{
    private bool overlayOpen;
    private int automationMode;
    private static readonly MaterialHeaderAction[] OverlayActions=[new("settings",MaterialIcon.Settings,"Show settings"),new("close",MaterialIcon.Close,"Close overlay")];
    private void AutomationPage()
    {
        MaterialStatus.SectionHeader("COMPACT AUTOMATION");
        ImGui.TextWrapped("A separate translucent overlay shares state with this preview. No game commands are sent.");
        using(var card=MaterialSurface.Card("preview",new(-1,170),true))
        {
            if(card.Visible) AutomationContent(false);
        }
        if(MaterialButton.FilledButton("openOverlay",overlayOpen?"Hide floating overlay":"Show floating overlay",leading:MaterialIcon.Play))
            overlayOpen=!overlayOpen;
        ImGui.Spacing();
        ImGui.TextWrapped("Change Auto / Manual / Off, open settings, or close the floating window. Its surface uses the shared accent and opacity.");
    }
    private void AutomationContent(bool floating)
    {
        var action=MaterialWindowHeader.Draw("Automation","Demo controller",OverlayActions);
        if(action==0) { Page=4; IsOpen=true; }
        if(action==1) { if(floating) overlayOpen=false; else notice="Preview close clicked. The floating overlay can be reopened below."; }
        ImGui.Spacing();
        ImGui.TextColored(MaterialTheme.Current.Colors.Primary,automationMode==0?"Auto":automationMode==1?"Manual":"Off");
        MaterialSegmented.Draw<int>("mode",Modes,ref automationMode);
        MaterialStatus.Badge(automationMode==2?"Paused":"Ready",automationMode==2?Upcoming:Available);
    }
    private void DrawOverlay()
    {
        var scale=MaterialTheme.Metrics.Scale;
        ImGui.SetNextWindowSize(new Vector2(380,190)*scale,ImGuiCond.Always);
        if(ImGui.Begin("Automation###crmaterial-overlay",ref overlayOpen,
            ImGuiWindowFlags.NoTitleBar|ImGuiWindowFlags.NoResize|ImGuiWindowFlags.NoSavedSettings))
            AutomationContent(true);
        ImGui.End();
    }
}
