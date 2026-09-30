using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace CombatReborn.MaterialUI.Demo;

internal sealed partial class Showcase
{
    public bool IsOpen;
    public int Page;
    private Vector3 seed = new(.36f,.72f,.46f);
    private MaterialTheme theme = new(new(.36f,.72f,.46f));
    private float opacity=.94f;
    private int density;
    private bool expanded=true;
    private string notice="Ready. All actions use mock state.";
    private static readonly string[] Pages=["Gallery","Automation","Gathering data","Dashboard","Settings"];
    private static readonly MaterialIcon[] PageIcons=[MaterialIcon.Grid,MaterialIcon.Play,MaterialIcon.Table,MaterialIcon.Home,MaterialIcon.Settings];
    private static readonly MaterialHeaderAction[] HeaderActions=[new("density",MaterialIcon.Sliders,"Toggle expanded navigation"),new("close",MaterialIcon.Close,"Close showcase")];
    private static readonly MaterialSegment<int>[] DensityOptions=[new(0,"Compact"),new(1,"Standard")];
    private static readonly MaterialSegment<int>[] Modes=[new(0,"Auto"),new(1,"Manual"),new(2,"Off")];
    private static readonly MaterialSegment<int>[] FourOptions=[new(0,"All"),new(1,"Mining"),new(2,"Botany"),new(3,"Fishing")];
    private static readonly MaterialSegment<int>[] TwoOptions=[new(0,"Local"),new(1,"Shared")];
    private static readonly MaterialSegment<int>[] DisabledOptions=[new(0,"Ready"),new(1,"Unavailable",Disabled:true),new(2,"Off")];

    public void Draw(float globalScale = 1)
    {
        if(!IsOpen&&!overlayOpen) return;
        // The theme scope wraps Begin/End, so it also styles the outer window.
        using var scope=MaterialTheme.Push(theme,globalScale);
        if(IsOpen)
        {
            var scale=MaterialTheme.Metrics.Scale;
            ImGui.SetNextWindowSize(new Vector2(1000,760)*scale,ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(new Vector2(780,500)*scale,new(float.MaxValue));
            if(ImGui.Begin("MaterialUI showcase###crmaterial",ref IsOpen,ImGuiWindowFlags.NoTitleBar|ImGuiWindowFlags.NoSavedSettings))
            {
                var action=MaterialWindowHeader.Draw("MaterialUI / prototype","Compact surfaces. Native behavior. Mock data.",HeaderActions);
                if(action==0) expanded=!expanded;
                if(action==1) IsOpen=false;
                MaterialStatus.SectionHeader("SHOWCASE");
                using(var rail=MaterialNavigation.Rail("navigation",expanded))
                {
                    if(rail.Visible)
                    {
                        for(var i=0;i<Pages.Length;i++)
                        {
                            ImGui.PushID(i);
                            if(MaterialNavigation.Item("page",Pages[i],PageIcons[i],Page==i,expanded)) Page=i;
                            if(!expanded&&ImGui.IsItemHovered()) ImGui.SetTooltip(Pages[i]);
                            ImGui.PopID();
                        }
                        if(expanded)
                        {
                            ImGui.Spacing();
                            MaterialStatus.Badge("API 15 / .NET 10");
                            ImGui.Spacing();
                            ImGui.TextWrapped("Independent library. No game automation.");
                        }
                    }
                }
                ImGui.SameLine();
                using(var page=MaterialSurface.Begin("page",default,MaterialElevation.Lowest,padding:12))
                {
                    if(page.Visible)
                    {
                        switch(Page)
                        {
                            case 0: Gallery(); break;
                            case 1: AutomationPage(); break;
                            case 2: DataPage(); break;
                            case 3: Dashboard(); break;
                            case 4: Settings(); break;
                        }
                    }
                }
            }
            ImGui.End();
        }
        if(overlayOpen) DrawOverlay();
    }
    private void Appearance()
    {
        ImGui.SetNextItemWidth(180*MaterialTheme.Metrics.Scale);
        if(ImGui.ColorEdit3("Accent",ref seed,ImGuiColorEditFlags.NoInputs))
        {
            var motion=theme.Motion.Enabled;
            theme=new(seed,(MaterialDensity)density) { SurfaceOpacity=opacity };
            theme.Motion.Enabled=motion;
            notice="Accent palette regenerated.";
        }
        ImGui.SameLine();
        ImGui.SetNextItemWidth(200*MaterialTheme.Metrics.Scale);
        if(ImGui.SliderFloat("Opacity",ref opacity,.4f,1,"%.2f")) theme.SurfaceOpacity=opacity;
        if(MaterialSegmented.Draw<int>("density",DensityOptions,ref density,300)) theme.Density=(MaterialDensity)density;
    }
}
