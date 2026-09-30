using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace CombatReborn.MaterialUI.Demo;

internal sealed partial class Showcase
{
    private bool notifications=true,dependency,dependentSetting,reduceMotion;
    private int behavior,route,delay=250;
    private string preset="Evening route";
    private static readonly string[] Routes=["Nearest node","Lowest cost","Preferred location"];
    private void Settings()
    {
        MaterialStatus.SectionHeader("PREFERENCES");
        using(var appearance=MaterialSurface.Card("appearance",new(-1,165)))
        {
            if(appearance.Visible)
            {
                ImGui.TextUnformatted("Appearance");
                Appearance();
                if(MaterialSwitch.Draw("motion","Reduce motion",ref reduceMotion)) theme.Motion.Enabled=!reduceMotion;
            }
        }
        using(var automation=MaterialSurface.Card("automationSettings",new(-1,230)))
        {
            if(automation.Visible)
            {
                ImGui.TextUnformatted("Automation preferences");
                MaterialSwitch.Draw("notify","Notify when complete",ref notifications);
                MaterialSegmented.Draw<int>("behavior",Modes,ref behavior);
                ImGui.SetNextItemWidth(230*MaterialTheme.Metrics.Scale);
                if(ImGui.BeginCombo("Routing",Routes[route]))
                {
                    for(var i=0;i<Routes.Length;i++) if(ImGui.Selectable(Routes[i],route==i)) route=i;
                    ImGui.EndCombo();
                }
                ImGui.SetNextItemWidth(230*MaterialTheme.Metrics.Scale);
                ImGui.SliderInt("Delay (ms)",ref delay,0,1000);
                MaterialTextField.TextField("preset",ref preset,"Preset name",leading:MaterialIcon.Edit);
            }
        }
        using(var integrations=MaterialSurface.Card("integrations",new(-1,165)))
        {
            if(integrations.Visible)
            {
                ImGui.TextUnformatted("Optional integration / mock");
                MaterialSwitch.Draw("dependency","Dependency available",ref dependency);
                using var nested=MaterialSurface.Begin("nested",new(-1,68),MaterialElevation.High,padding:8,radius:12);
                if(nested.Visible)
                {
                    MaterialSwitch.Draw("dependent","Use integration",ref dependentSetting,!dependency);
                    ImGui.TextDisabled(dependency?"The mock dependency is available.":"Enable the mock dependency to change this setting.");
                }
            }
        }
    }
}
