using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace AethertekUI.Demo;

internal sealed partial class Showcase
{
    private readonly MaterialOptions<int> sizingOptions = new(
        [new("a",0,"A short choice"),new("b",1,"A longer translated option label"),new("c",2,"Third choice")]);
    private int sizingChoice;
    private int sizingNumber=12;
    private bool sizingEnabled=true;
    private string sizingText="";
    private Vector3 sizingAccent=new(28/255f,201/255f,230/255f);
    private string sizingLanguage="en";
    private readonly MaterialOptions<string> sizingLanguages=new([new("en","en","English"),new("de","de","Deutsch")]);
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
        MaterialStatus.SectionHeader("COMPACT PLUGIN TOOLBAR");
        using(var controls=MaterialControls.Push(MaterialControlContext.Toolbar))
        {
            MaterialButton.Draw("document","Notes",4,MaterialButtonVariant.Outlined,leading:MaterialIcon.Document); ImGui.SameLine();
            MaterialButton.Draw("data","Data",4,MaterialButtonVariant.Tonal,leading:MaterialIcon.Database); ImGui.SameLine();
            MaterialButton.Draw("reload","Reload",4,MaterialButtonVariant.Outlined,leading:MaterialIcon.Refresh); ImGui.SameLine();
            MaterialButton.Draw("status","Status",4,MaterialButtonVariant.Outlined,leading:MaterialIcon.Chart); ImGui.SameLine();
            MaterialButton.Draw("external","Open",4,MaterialButtonVariant.Text,leading:MaterialIcon.ExternalLink); ImGui.SameLine();
            MaterialButton.Draw("download","Download",4,MaterialButtonVariant.Text,leading:MaterialIcon.Download);
        }
        MaterialStatus.Badge("Available",4); ImGui.SameLine();
        MaterialStatus.Badge("Warning",4,new Vector4(1,.86f,.35f,1));
        AppearanceExamples();
    }

    internal void AppearanceExamples()
    {
        MaterialStatus.SectionHeader("EXACT APPEARANCE / CONSUMER PREFERENCES");
        var c=MaterialTheme.Current.Colors;
        var appearance=new MaterialControlAppearance(c.SurfaceContainerLow,c.OnSurface,c.OutlineVariant,4);
        var m=MaterialControlMetrics.Measure(MaterialTheme.Metrics,ImGui.GetTextLineHeight()) with { Height=38*MaterialTheme.Metrics.Scale };
        using(var pixels=MaterialControls.Push(m))
        {
            MaterialButton.Draw("exact","Slate action",appearance,new(180,52));
            MaterialStatus.Badge("Measured badge",appearance,new(160,34));
            MaterialTextField.SearchField("exact-search",ref sizingText,appearance,width:222);
            MaterialCombo.Draw("exact-choice",sizingOptions,ref sizingChoice,appearance,width:213,searchable:false);
            MaterialSwitch.Draw("exact-switch","44 x 26 track",ref sizingEnabled,new Vector2(44,26));
            MaterialButton.IconButton("map-example",MaterialIcon.Map);
            ImGui.SameLine(); MaterialButton.IconButton("clock-example",MaterialIcon.Clock);
            ImGui.SameLine(); MaterialButton.IconButton("link-example",MaterialIcon.Link);
            ImGui.SameLine(); MaterialButton.IconButton("calculator-example",MaterialIcon.Calculator);
            MaterialAppearanceSelector.Draw("preferences",ref sizingAccent,ref sizingLanguage,sizingLanguages,
                new("Color","Language","Teal","Blue","Pink","Custom RGB"));
        }
        var origin=ImGui.GetCursorScreenPos();
        MaterialCanvas.Surface(origin,origin+new Vector2(280,36)*MaterialTheme.Metrics.Scale,c.SurfaceContainerHigh,c.Background,4*MaterialTheme.Metrics.Scale);
        ImGui.Dummy(new Vector2(280,36)*MaterialTheme.Metrics.Scale);
    }
}
