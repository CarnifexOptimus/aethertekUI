using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace CombatReborn.MaterialUI.Demo;

internal sealed partial class Showcase
{
    private int runState;
    private float runProgress;
    private string destination="Home";
    private void Dashboard()
    {
        if(runState==1) runProgress=Math.Min(1,runProgress+ImGui.GetIO().DeltaTime*.035f);
        if(runProgress>=1) runState=3;
        MaterialStatus.SectionHeader("AUTOMATION");
        var tileWidth=(ImGui.GetContentRegionAvail().X-2*MaterialTheme.Metrics.Gap)/3/MaterialTheme.Metrics.Scale;
        var tileSize=new Vector2(tileWidth,0);
        if(MaterialActionTile.Draw("start",MaterialIcon.Play,"Start","Mock gathering",tileSize,runState==1))
        { if(runProgress>=1) runProgress=0; runState=1; }
        ImGui.SameLine();
        if(MaterialActionTile.Draw("pause",MaterialIcon.Pause,"Pause","Keep progress",tileSize,runState==2,runState==0)) runState=2;
        ImGui.SameLine();
        if(MaterialActionTile.Draw("stop",MaterialIcon.Stop,"Stop","Reset queue",tileSize,disabled:runState==0))
        { runState=0; runProgress=0; }
        MaterialStatus.SectionHeader("NAVIGATION");
        if(MaterialActionTile.Draw("move",MaterialIcon.ArrowRight,"Move","To mock node",tileSize,destination=="Node")) destination="Node";
        ImGui.SameLine();
        if(MaterialActionTile.Draw("return",MaterialIcon.Return,"Return","To home",tileSize,destination=="Home")) destination="Home";
        ImGui.SameLine();
        if(MaterialActionTile.Draw("repair",MaterialIcon.Settings,"Repair","Mock workshop",tileSize,destination=="Workshop")) destination="Workshop";
        MaterialStatus.SectionHeader("STATUS");
        using var card=MaterialSurface.Card("status",new(-1,150));
        if(card.Visible)
        {
            MaterialStatus.Badge(runState switch {1=>"Running",2=>"Paused",3=>"Complete",_=>"Ready"},runState==2?Upcoming:Available);
            ImGui.SameLine(); MaterialStatus.Badge(destination);
            ImGui.Spacing();
            ImGui.TextUnformatted("Queue progress");
            MaterialStatus.LinearProgress(runProgress);
            ImGui.TextWrapped("Simulated progress only. No gathering, navigation, repair or game commands are executed.");
        }
    }
}
