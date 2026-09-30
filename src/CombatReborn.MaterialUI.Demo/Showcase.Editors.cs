using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace CombatReborn.MaterialUI.Demo;

internal sealed partial class Showcase
{
    private readonly MaterialOptions<int> recipes = new(CreateRecipeOptions());
    private int recipe, quantity=12;
    private float editorWidth=190, editorDelay=.5f;
    private bool? inherited;
    private ulong jobs=3;
    private string importText="Example list\nMock gathering item x12";
    private string editorMessage="Drag a mock item onto a folder. Right-click an item for actions.";
    private int itemFolder;
    private static readonly MaterialBitOption[] JobOptions =
        [new("mining","Mining",1),new("botany","Botany",2),new("fishing","Fishing",4)];
    private static MaterialOption<int>[] CreateRecipeOptions()
    {
        var result=new MaterialOption<int>[500];
        for(var i=0;i<result.Length;i++) result[i]=new($"recipe-{i}",i,$"Mock recipe {i:000}",i%17==16);
        return result;
    }

    private void Editors()
    {
        MaterialStatus.SectionHeader("LIST EDITOR / INHERITED SETTINGS");
        MaterialSemantic.Feedback(editorMessage);
        var total=ImGui.GetContentRegionAvail().X/MaterialTheme.Metrics.Scale;
        editorWidth=Math.Clamp(editorWidth,120,Math.Max(120,total-260));
        using(var folders=MaterialSurface.Card("folders",new(editorWidth,410)))
        {
            if(folders.Visible)
            {
                MaterialStatus.SectionHeader("FOLDERS");
                for(var folder=0;folder<2;folder++)
                {
                    ImGui.PushID(folder);
                    ImGui.SetNextItemOpen(true,ImGuiCond.Once);
                    using(var tree=MaterialTree.Node("folder",folder==0?"Gathering":"Crafting"))
                    {
                        if(MaterialDragDrop.TryAccept<int>("CR_MOCK_ITEM",out var dropped))
                        { itemFolder=folder; editorMessage=$"Moved mock item {dropped} to folder {folder+1}."; }
                        if(tree.Visible&&itemFolder==folder)
                        {
                            ImGui.Selectable("Mock item###drag-item");
                            var key=42;
                            MaterialDragDrop.Source("CR_MOCK_ITEM",in key,"Move mock item");
                            using var menu=MaterialContextMenu.Begin("item-menu");
                            if(menu.Visible)
                            {
                                if(ImGui.MenuItem("Duplicate")) editorMessage="Duplicate requested (mock).";
                                if(ImGui.MenuItem("Remove",string.Empty,false,ImGui.GetIO().KeyCtrl)) editorMessage="Remove requested (mock, Ctrl held).";
                                MaterialSemantic.Tooltip("Hold Ctrl to enable this destructive action.");
                            }
                        }
                    }
                    ImGui.PopID();
                }
                MaterialStatus.SectionHeader("JOB FILTERS");
                MaterialFlags.Draw("jobs",JobOptions,ref jobs);
            }
        }
        ImGui.SameLine(0,0);
        MaterialSplitPane.Divider("split",ref editorWidth,total,120,260,410);
        ImGui.SameLine(0,0);
        using(var details=MaterialSurface.Card("details",new(0,410)))
        {
            if(details.Visible)
            {
                MaterialStatus.SectionHeader("RECIPE SETTINGS");
                ImGui.TextUnformatted("Recipe / searchable 500-option list");
                MaterialCombo.Draw("recipe",recipes,ref recipe);
                ImGui.TextUnformatted("Quantity");
                MaterialInput.Integer("quantity",ref quantity,1,999,width:160);
                ImGui.TextUnformatted("Delay (seconds)");
                MaterialInput.Number("delay",ref editorDelay,0,10,width:160);
                ImGui.TextUnformatted("Per-recipe override");
                MaterialOverride.Switch("override",ref inherited);
                MaterialSemantic.Feedback(inherited.HasValue?"Explicit recipe setting":"Using the parent configuration");
                ImGui.TextUnformatted("Import / export text");
                MaterialInput.Multiline("import",ref importText,new(-1,90));
                if(MaterialButton.FilledTonalButton("validate","Validate mock text",disabled:importText.Length==0))
                    editorMessage=$"Accepted {importText.Length} characters. No game data was changed.";
            }
        }
    }
}
