using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace AethertekUI.Demo;

internal sealed partial class Showcase
{
    private bool enabled=true, checkedValue, chip, dialogOpen, cardSelected;
    private int two,three,four,limited,radio;
    private string search="",text="Evening gathering";
    private float progress=.62f;

    private void Gallery()
    {
        MaterialStatus.SectionHeader("APPEARANCE");
        Appearance();
        using var tabs=MaterialTabs.Begin("galleryTabs");
        if(!tabs.Visible) return;
        using(var components=MaterialTabs.Item("Components"))
        {
            if(components.Visible) GalleryComponents();
        }
        using(var states=MaterialTabs.Item("Disabled & focus"))
        {
            if(states.Visible)
            {
                ImGui.TextWrapped("Tab and Shift+Tab navigate controls when host navigation is enabled. Space activates focused buttons. Disabled options cannot activate.");
                MaterialButton.FilledButton("disabledFilled","Filled",disabled:true);
                ImGui.SameLine(); MaterialButton.FilledTonalButton("disabledTonal","Tonal",disabled:true);
                ImGui.SameLine(); MaterialButton.OutlinedButton("disabledOutline","Outlined",disabled:true);
                ImGui.SameLine(); MaterialButton.TextButton("disabledText","Text",disabled:true);
                MaterialSwitch.Draw("disabledSwitch","Disabled switch",ref enabled,true);
                MaterialSegmented.Draw<int>("disabledSegments",Modes,ref three,disabled:true);
                MaterialSegmented.Draw<int>("partDisabled",DisabledOptions,ref limited);
                MaterialTextField.TextField("readOnly",ref text,"Read only",flags:ImGuiInputTextFlags.ReadOnly);
                MaterialTextField.TextField("disabledInput",ref text,"Disabled",disabled:true);
                MaterialButton.IconButton("disabledDelete",MaterialIcon.Delete,disabled:true);
                if(ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) ImGui.SetTooltip("Consumer-owned tooltip explains the disabled state.");
                MaterialSelection.Radio("localRadio","Local",0,ref radio);
                ImGui.SameLine(); MaterialSelection.Radio("sharedRadio","Shared",1,ref radio);
            }
        }
    }
    private void GalleryComponents()
    {
        MaterialStatus.SectionHeader("BUTTONS");
        if(MaterialButton.FilledButton("filled","Create",leading:MaterialIcon.Play)) notice="Filled button activated.";
        ImGui.SameLine();
        if(MaterialButton.FilledTonalButton("tonal","Save",leading:MaterialIcon.Check)) notice="Tonal button activated.";
        ImGui.SameLine();
        if(MaterialButton.OutlinedButton("outlined","Import",trailing:MaterialIcon.ArrowRight)) notice="Outlined button activated.";
        ImGui.SameLine();
        if(MaterialButton.TextButton("textButton","Learn more")) notice="Text button activated.";
        if(MaterialButton.IconButton("settings",MaterialIcon.Settings,true)) Page=4;
        ImGui.SameLine();
        if(MaterialButton.IconButton("edit",MaterialIcon.Edit)) notice="Edit selected.";
        ImGui.SameLine();
        if(MaterialButton.IconButton("delete",MaterialIcon.Delete)) notice="Mock delete selected.";
        ImGui.SameLine();
        if(MaterialButton.IconButton("more",MaterialIcon.More)) notice="More actions selected.";
        ImGui.SameLine(); MaterialStatus.Badge("Latest");
        ImGui.SameLine(); MaterialStatus.Badge("Ready",Available);
        ImGui.TextColored(MaterialTheme.Current.Colors.OnSurfaceVariant,notice);
        MaterialStatus.SectionHeader("SELECTION");
        MaterialSwitch.Draw("enabled","Automation enabled",ref enabled);
        ImGui.SameLine(); MaterialSelection.Checkbox("checkbox","Notify on completion",ref checkedValue);
        MaterialSegmented.Draw<int>("two",TwoOptions,ref two);
        MaterialSegmented.Draw<int>("three",Modes,ref three);
        MaterialSegmented.Draw<int>("four",FourOptions,ref four);
        MaterialChip.FilterChip("filter","Timed nodes",ref chip,icon:MaterialIcon.Info);
        ImGui.SameLine();
        if(MaterialChip.ActionChip("assist","Reset filters",MaterialIcon.Return)) { chip=false; search=""; }
        MaterialStatus.SectionHeader("FIELDS & PROGRESS");
        MaterialTextField.SearchField("search",ref search,"Search the gallery...");
        MaterialTextField.TextField("name",ref text,"Preset name",leading:MaterialIcon.Edit,error:text.Length==0);
        ImGui.SliderFloat("Completion",ref progress,0,1,"%.2f");
        MaterialStatus.LinearProgress(progress);
        MaterialStatus.SectionHeader("SURFACES");
        using(var card=MaterialSurface.Card("tonalCard",new(-1,86)))
        {
            if(card.Visible)
            {
                ImGui.TextUnformatted("A compact tonal card");
                ImGui.TextColored(MaterialTheme.Current.Colors.OnSurfaceVariant,"Content remains ordinary immediate-mode ImGui.");
                MaterialStatus.LinearProgress(.4f,height:4);
            }
        }
        if(MaterialSurface.ClickableCard("interactiveCard","Selectable card","Click to change selected state",new(-1,78),cardSelected))
            cardSelected=!cardSelected;
        if(MaterialButton.FilledTonalButton("openDialog","Open dialog",leading:MaterialIcon.Info))
        { dialogOpen=true; ImGui.OpenPopup("Prototype dialog"); }
        using var dialog=MaterialDialog.Begin("Prototype dialog",ref dialogOpen);
        if(dialog.Visible)
        {
            ImGui.TextUnformatted("Standard ImGui modal lifecycle.");
            ImGui.TextUnformatted("This only updates demo state.");
            if(MaterialButton.FilledButton("confirm","Confirm"))
            { notice="Dialog confirmed."; ImGui.CloseCurrentPopup(); dialogOpen=false; }
            ImGui.SameLine();
            if(MaterialButton.TextButton("cancel","Cancel")) { ImGui.CloseCurrentPopup(); dialogOpen=false; }
        }
    }
}
