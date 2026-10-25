using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace AethertekUI.Demo;

internal sealed partial class Showcase
{
    private readonly MaterialOptions<int> sizingOptions = new(
        [new("a",0,"A short choice"),new("b",1,"A longer translated option label"),new("c",2,"Third choice")]);
    private int sizingChoice;
    private int sizingNumber=12;
    private float sizingRange=50;
    private bool sizingEnabled=true;
    private string sizingText="";
    private Vector3 sizingAccent=new(28/255f,201/255f,230/255f);
    private string sizingLanguage="en";
    private int sizingNativeLanguage;
    private readonly MaterialOptions<string> sizingLanguages=new([new("en","en","English"),new("de","de","Deutsch")]);
    private static readonly MaterialControlContext[] SizingContexts =
        [MaterialControlContext.Regular,MaterialControlContext.Toolbar,MaterialControlContext.Dense];
    private readonly int[] sizingTableChoices = [0,1,2,0,1];
    private readonly int[] sizingTableCounts = [1,2,3,4,5];

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
        MaterialStatus.SectionHeader("TIGHT EDITOR ROWS");
        ImGui.TextWrapped("Adjacent rows, one-pixel column insets. Padding stays inside the editors.");
        using(var tableControls=MaterialControls.Push())
        using(var tableStyle=MaterialTable.PushTightRows())
        {
            if(ImGui.BeginTable("sizing-table",3,ImGuiTableFlags.Resizable|ImGuiTableFlags.RowBg|ImGuiTableFlags.Borders))
            {
                ImGui.TableSetupColumn("Action",ImGuiTableColumnFlags.WidthFixed,70*MaterialTheme.Metrics.Scale);
                ImGui.TableSetupColumn("Choice"); ImGui.TableSetupColumn("Quantity");
                ImGui.TableHeadersRow();
                for(var row=0;row<sizingTableCounts.Length;row++)
                {
                    ImGui.PushID(row);
                    ImGui.TableNextRow(); ImGui.TableNextColumn();
                    MaterialButton.IconButton("delete",MaterialIcon.Delete);
                    ImGui.TableNextColumn(); MaterialCombo.Draw("table-choice",sizingOptions,ref sizingTableChoices[row]);
                    ImGui.TableNextColumn(); MaterialInput.Integer("table-count",ref sizingTableCounts[row]);
                    ImGui.PopID();
                }
                ImGui.EndTable();
            }
        }
        MaterialStatus.SectionHeader("BOUNDED SUMMARY TEXT");
        MaterialText.SingleLine("Fixture account");
        MaterialText.SingleLine("A fixture character with a longer display name@ExampleWorld");
        MaterialText.SingleLine("Waiting for the current activity to finish before preparing the next assignment.");
        MaterialStatus.SectionHeader("CONTENT-SIZED ACTION");
        MaterialActionTile.Draw("measured-action",MaterialIcon.Play,"Start a longer named action",
            "This status wraps and the tile grows to fit.",new Vector2(240,72));
        ImGui.TextWrapped("Popup width is independent of field width. No plugin-specific geometry is needed.");
        MaterialStatus.SectionHeader("READABLE NATIVE ROWS");
        ImGui.Button("A longer translated action");
        ImGui.SameLine();
        var editorMinimum=ImGui.CalcTextSize("00000").X+2*ImGui.GetStyle().FramePadding.X;
        ImGui.SetNextItemWidth(MaterialLayout.FitNextItemWidth(0,editorMinimum));
        ImGui.InputInt("##readable-native-number",ref sizingNumber,0);
        ImGui.SetNextItemWidth(120 * MaterialTheme.Metrics.Scale);
        var captionList = ImGui.GetWindowDrawList();
        var captionFirst = captionList.VtxBuffer.Size;
        var captionFont = ImGui.GetFont();
        var captionOrigin = ImGui.GetCursorScreenPos();
        var captionWidth = ImGui.CalcItemWidth();
        ImGui.InputInt("Count##retained-caption-demo", ref sizingNumber);
        const string translatedCount = "Translated count";
        var captionMin = captionOrigin + new Vector2(captionWidth + ImGui.GetStyle().ItemInnerSpacing.X, 0);
        var captionMax = new Vector2(captionMin.X + MaterialText.Measure(translatedCount).X, ImGui.GetItemRectMax().Y);
        MaterialText.ReplaceNativeCaption(captionList, captionFirst, captionFont, "Count##retained-caption-demo",
            captionMin, captionMax, captionMin + new Vector2(0, ImGui.GetStyle().FramePadding.Y),
            translatedCount, ImGui.GetColorU32(ImGuiCol.Text));
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
        MaterialStatus.SectionHeader("WINDOW MOTION");
        if(MaterialButton.FilledTonalButton("show-window-motion","Preview minimize and restore"))windowMotionOpen=true;
        if(MaterialButton.FilledTonalButton("show-appearance-titlebar","Preview appearance titlebar"))appearanceWindow.IsOpen=true;
    }

    private void RetainedSettingsExamples()
    {
        var scale = MaterialTheme.Metrics.Scale;
        MaterialStatus.SectionHeader("REBUILT SETTINGS / RETAINED NATIVE IDS");
        ImGui.TextWrapped("Rows bind directly to caller values. Wrapping captions and moving an editor do not change its original native identity.");
        MaterialSettings.Columns("retained-settings-layout", 280 * scale, () =>
        {
            var width = ImGui.GetContentRegionAvail().X;
            MaterialSettings.Section("NOTIFICATIONS & AVAILABILITY", width);
            MaterialSelection.Checkbox("Existing notification setting", "Notify when a longer background activity completes, including while this window is closed",
                ref notifications, width);
            MaterialSelection.Checkbox("Existing enabled setting", "Enable the selected configuration without changing another profile's saved values",
                ref sizingEnabled, width);
            MaterialSwitch.Row("Existing completion switch", "Show the completion notification for a longer running activity", ref notifications,
                "ON", "OFF", width, "The consumer owns this value and its change handler.");
        }, () =>
        {
            var width = ImGui.GetContentRegionAvail().X;
            MaterialSettings.Section("NATIVE CHOICES & VALUES", width);
            var radioWidth = (width - 2 * ImGui.GetStyle().ItemSpacing.X) / 3;
            MaterialSelection.Radio("Existing automatic choice", "Auto", 0, ref sizingChoice, radioWidth); ImGui.SameLine();
            MaterialSelection.Radio("Existing manual choice", "Manual", 1, ref sizingChoice, radioWidth); ImGui.SameLine();
            MaterialSelection.Radio("Existing disabled choice", "Off", 2, ref sizingChoice, radioWidth);
            MaterialSettings.Field("Existing quantity", "Quantity", 112 * scale,
                label => ImGui.InputInt(label, ref sizingNumber, 1), width);
            MaterialSettings.Field("Existing delay", "Delay before starting (ms)", 140 * scale,
                label => ImGui.SliderInt(label, ref delay, 0, 1000), width);
            MaterialText.Text("Native linear track");
            MaterialInput.SliderTrack("retained-linear-track", ref sizingRange, 0, 100, width / scale);
        }, divider: false);
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
            ImGui.SameLine(); MaterialButton.IconButton("person-example",MaterialIcon.Person);
            ImGui.SameLine(); MaterialButton.IconButton("shield-example",MaterialIcon.Shield);
            ImGui.SameLine(); MaterialButton.IconButton("monitor-example",MaterialIcon.Monitor);
            ImGui.SameLine(); MaterialButton.IconButton("location-example",MaterialIcon.Location);
            ImGui.SameLine(); MaterialButton.IconButton("send-example",MaterialIcon.Send);
            MaterialButton.IconButton("save-example",MaterialIcon.Save);
            ImGui.SameLine(); MaterialButton.IconButton("copy-example",MaterialIcon.Copy);
            ImGui.SameLine(); MaterialButton.IconButton("heart-example",MaterialIcon.Heart);
            ImGui.SameLine(); MaterialButton.IconButton("list-example",MaterialIcon.List);
            ImGui.SameLine(); MaterialButton.IconButton("sword-example",MaterialIcon.Sword);
            ImGui.SameLine(); MaterialButton.IconButton("star-example",MaterialIcon.Star);
            ImGui.SameLine(); MaterialButton.IconButton("folder-example",MaterialIcon.Folder);
            ImGui.SameLine(); MaterialButton.IconButton("egg-example",MaterialIcon.Egg);
            foreach(var icon in new[] { MaterialIcon.Wrench, MaterialIcon.DoorExit, MaterialIcon.Cube, MaterialIcon.Chest,
                MaterialIcon.Cart, MaterialIcon.Chat, MaterialIcon.Group, MaterialIcon.Warning, MaterialIcon.Plus,
                MaterialIcon.Pulse, MaterialIcon.Utensils, MaterialIcon.Crystal, MaterialIcon.Calendar, MaterialIcon.Crown,
                MaterialIcon.Book, MaterialIcon.Sparkle, MaterialIcon.Bell, MaterialIcon.Terminal, MaterialIcon.Power, MaterialIcon.Target,
                MaterialIcon.MapPin, MaterialIcon.HeartOutline, MaterialIcon.Discord, MaterialIcon.KoFi, MaterialIcon.Backpack })
            {
                if(icon != MaterialIcon.Wrench && ImGui.GetItemRectMax().X + ImGui.GetStyle().ItemSpacing.X + 40 * MaterialTheme.Metrics.Scale
                    <= ImGui.GetWindowPos().X + ImGui.GetWindowContentRegionMax().X) ImGui.SameLine();
                MaterialButton.IconButton($"consumer-art-{icon}",icon);
            }
            MaterialAppearanceSelector.Draw("preferences",ref sizingAccent,ref sizingLanguage,sizingLanguages,
                new("Color","Language","Teal","Blue","Pink","Custom RGB"));
            MaterialAppearanceSelector.DrawAccent("separated-preferences",ref sizingAccent,
                new("Color","Language","Teal","Blue","Pink","Custom RGB"),37);
            ImGui.SameLine(); MaterialButton.Draw("between-preferences","Consumer action");
            ImGui.SameLine(); MaterialAppearanceSelector.DrawLanguage("separated-preferences",ref sizingLanguage,sizingLanguages);
            if(ImGui.GetItemRectMax().X+ImGui.GetStyle().ItemSpacing.X+144*MaterialTheme.Metrics.Scale
                <=ImGui.GetWindowPos().X+ImGui.GetWindowContentRegionMax().X) ImGui.SameLine();
            using(MaterialControls.Push(MaterialControls.Metrics with { IconSize=32*MaterialTheme.Metrics.Scale }))
                MaterialAppearanceSelector.DrawAccent("rectangular-preferences",ref sizingAccent,
                    new("Color","Language","Teal","Blue","Pink","Custom RGB"),new Vector2(144,49));
            MaterialAppearanceSelector.Draw("native-preferences",ref sizingAccent,
                new("Color","Language","Teal","Blue","Pink","Custom RGB"),DrawNativeLanguage);
        }
        var origin=ImGui.GetCursorScreenPos();
        MaterialCanvas.Surface(origin,origin+new Vector2(280,36)*MaterialTheme.Metrics.Scale,c.SurfaceContainerHigh,c.Background,4*MaterialTheme.Metrics.Scale);
        ImGui.Dummy(new Vector2(280,36)*MaterialTheme.Metrics.Scale);
    }
    private bool DrawNativeLanguage()
    {
        // A retained consumer Combo keeps its native label and index/name option IDs.
        ImGui.SetNextItemWidth(112*MaterialTheme.Metrics.Scale);
        return ImGui.Combo("Language###retained-language",ref sizingNativeLanguage,["English","Deutsch"],2);
    }
}
