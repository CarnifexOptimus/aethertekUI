using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace AethertekUI.Demo;

internal sealed partial class Showcase
{
    private bool notifications=true,dependency,dependentSetting,reduceMotion;
    private int behavior,route,delay=250;
    private string preset="Evening route";
    private bool appearanceCompact=true,showCompact,showTransparency,showLanguage=true,appearanceTransparency=true,appearanceFade=true;
    private int appearanceOpacity=100,appearanceFadedOpacity=50;
    private float appearanceFadeDelay=10;
    private string appearanceLanguage="en";
    private static readonly string[] Routes=["Nearest node","Lowest cost","Preferred location"];
    private void RebuildTheme()
    {
        theme = new(seed, colorChoices, (MaterialDensity)density) { SurfaceOpacity = opacity };
        theme.Motion.Enabled = !reduceMotion;
        appliedColorChoices = colorChoices;
    }

    private void DrawColourEditor(string id)
    {
        if (MaterialAppearanceSelector.DrawColors(id, ref colorChoices, ref seed,
            new("Colour", "Language", "Teal", "Blue", "Pink", "Custom RGB"),
            new("Auto", "Picker", "DEFAULT", "Background, controls and text follow your theme.", "Theme colour",
                "Choose each colour independently.", "Background", "Controls", "Text", "Aa  Preview text", "Control",
                "Switch to Picker to set each colour yourself.", "Switch back to Auto to use the automatic theme."))) RebuildTheme();
    }

    private void Settings()
    {
        MaterialText.Text("Window appearance");
        MaterialAppearanceTab.Draw("groupedAppearance",new("THEME & LANGUAGE","MAIN WINDOW CONTROLS","WINDOW OPACITY","WHEN UNFOCUSED"),
            360*MaterialTheme.Metrics.Scale,() =>
        {
            DrawColourEditor("appearance");
            MaterialText.Text("Language");ImGui.SameLine();
            MaterialAppearanceSelector.DrawLanguage("appearance",ref appearanceLanguage,sizingLanguages);
            ImGui.Checkbox("Compact mode",ref appearanceCompact);
            if(MaterialSwitch.Draw("motion","Reduce motion",ref reduceMotion)) theme.Motion.Enabled=!reduceMotion;
        },() =>
        {
            ImGui.Checkbox("Compact visible on main window",ref showCompact);
            ImGui.Checkbox("Transparency visible on main window",ref showTransparency);
            ImGui.Checkbox("Language visible on main window",ref showLanguage);
        },() =>
        {
            ImGui.Checkbox("Transparency",ref appearanceTransparency);
            ImGui.BeginDisabled(!appearanceTransparency);
            try { MaterialText.Text("Opacity");ImGui.SameLine();ImGui.SetNextItemWidth(150*MaterialTheme.Metrics.Scale);ImGui.SliderInt("##appearanceOpacity",ref appearanceOpacity,10,100,"%d%%"); }
            finally { ImGui.EndDisabled(); }
        },() =>
        {
            ImGui.Checkbox("Auto-fade when unfocused",ref appearanceFade);
            ImGui.BeginDisabled(!appearanceTransparency);
            try
            {
                MaterialText.Text("Unfocused opacity");ImGui.SameLine();ImGui.SetNextItemWidth(150*MaterialTheme.Metrics.Scale);ImGui.SliderInt("##appearanceFaded",ref appearanceFadedOpacity,10,100,"%d%%");
                ImGui.BeginDisabled(!appearanceFade);
                try { MaterialText.Text("Unfocused delay");ImGui.SameLine();ImGui.SetNextItemWidth(90*MaterialTheme.Metrics.Scale);ImGui.InputFloat("##appearanceDelay",ref appearanceFadeDelay); }
                finally { ImGui.EndDisabled(); }
            }
            finally { ImGui.EndDisabled(); }
        });
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
