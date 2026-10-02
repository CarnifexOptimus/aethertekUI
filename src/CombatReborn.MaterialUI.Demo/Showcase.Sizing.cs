using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace CombatReborn.MaterialUI.Demo;

internal sealed partial class Showcase
{
    private readonly MaterialOptions<int> sizingOptions = new(
        [new("a",0,"A short choice"),new("b",1,"A longer translated option label"),new("c",2,"Third choice")]);
    private int sizingChoice;
    private int sizingNumber=12;
    private bool sizingEnabled=true;
    private string sizingText="";
    private static readonly MaterialControlContext[] SizingContexts =
        [MaterialControlContext.Regular,MaterialControlContext.Toolbar,MaterialControlContext.Dense];

    private void SizingLab()
    {
        MaterialStatus.SectionHeader("ADAPTIVE CONTROLS");
        ImGui.TextWrapped("One theme, three layout contexts. Native editors and Material controls share row heights.");
        foreach(var context in SizingContexts)
        {
            ImGui.PushID((int)context);
            ImGui.TextUnformatted(context.ToString());
            using(var controls=MaterialControls.Push(context))
            {
                MaterialButton.FilledTonalButton("button","Action"); ImGui.SameLine();
                MaterialButton.IconButton("edit",MaterialIcon.Edit); ImGui.SameLine();
                MaterialCanvas.GlyphButton("glyph","+",ImGui.GetFont()); ImGui.SameLine();
                MaterialInput.Integer("number",ref sizingNumber,width:95); ImGui.SameLine();
                MaterialSwitch.Draw("enabled","Enabled",ref sizingEnabled);
                MaterialCombo.Draw("choice",sizingOptions,ref sizingChoice,new MaterialComboLayout(340,6),width:175);
                ImGui.SameLine();
                MaterialTextField.SearchField("search",ref sizingText,width:220);
            }
            ImGui.Spacing();
            ImGui.PopID();
        }
        MaterialStatus.SectionHeader("DENSE NATIVE TABLE");
        using(var tableStyle=MaterialTable.PushControls())
        {
            if(ImGui.BeginTable("sizing-table",3,ImGuiTableFlags.Resizable|ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("Action",ImGuiTableColumnFlags.WidthFixed,70*MaterialTheme.Metrics.Scale);
                ImGui.TableSetupColumn("Choice"); ImGui.TableSetupColumn("Quantity");
                ImGui.TableHeadersRow();
                ImGui.TableNextRow(); ImGui.TableNextColumn();
                MaterialButton.IconButton("delete",MaterialIcon.Delete);
                ImGui.TableNextColumn(); MaterialCombo.Draw("table-choice",sizingOptions,ref sizingChoice);
                ImGui.TableNextColumn(); MaterialInput.Integer("table-count",ref sizingNumber);
                ImGui.EndTable();
            }
        }
        MaterialStatus.SectionHeader("CONTENT-SIZED ACTION");
        MaterialActionTile.Draw("measured-action",MaterialIcon.Play,"Start a longer named action",
            "This status wraps and the tile grows to fit.",new Vector2(240,72));
        ImGui.TextWrapped("Popup width is independent of field width. No plugin-specific geometry is needed.");
    }
}
