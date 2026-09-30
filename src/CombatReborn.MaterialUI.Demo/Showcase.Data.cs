using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace CombatReborn.MaterialUI.Demo;

internal sealed partial class Showcase
{
    // Domain colors intentionally never derive from the Material accent.
    private static readonly Vector4 Available=new(.35f,.88f,.63f,1);
    private static readonly Vector4 Upcoming=new(1,.76f,.35f,1);
    private static readonly Vector4 Missing=new(.96f,.45f,.46f,1);
    private sealed record MockItem(int Id,string Name,int Level,string Location,int Status)
    {
        public string LevelLabel { get; }=Level.ToString();
        public string StatusLabel => Status==0?"Available":Status==1?"Upcoming":"Missing";
    }
    private readonly MockItem[] items=[
        new(1,"Darksteel Ore",50,"Coerthas",0),new(2,"Mock Thavnair Herb",90,"Thavnair",1),
        new(3,"Copper Ore",1,"Thanalan",0),new(4,"Mock Moon Crystal",90,"Mare Lamentorum",2),
        new(5,"Cedar Log",51,"Coerthas",0),new(6,"Mock Highland Sage",100,"Urqopacha",1),
        new(7,"Mythril Ore",35,"Thanalan",0),new(8,"Mock Sea Blossom",90,"Elpis",2),
        new(9,"Iron Ore",14,"La Noscea",0),new(10,"Mock Golden Sap",100,"Yak T'el",1),
        new(11,"Silver Ore",25,"Black Shroud",0),new(12,"Mock River Reed",96,"Kozama'uka",0),
        new(13,"Mock Cloud Sprig",80,"Il Mheg",1),new(14,"Mock Volcanic Stone",80,"Amh Araeng",2),
        new(15,"Maple Log",2,"Black Shroud",0),new(16,"Mock Star Sand",90,"Ultima Thule",1),
        new(17,"Mock Desert Cotton",100,"Shaaloani",0),new(18,"Mock Frost Flower",60,"Coerthas",2)];
    private string dataSearch="";
    private bool availableOnly,upcomingOnly;
    private int selectedItem;
    private int sortColumn;
    private bool sortDescending;
    private void DataPage()
    {
        MaterialStatus.SectionHeader("GATHERING / MOCK DATA");
        MaterialTextField.SearchField("itemSearch",ref dataSearch,"Search items or locations...");
        MaterialChip.FilterChip("available","Available",ref availableOnly);
        ImGui.SameLine(); MaterialChip.FilterChip("upcoming","Upcoming",ref upcomingOnly);
        ImGui.SameLine();
        if(MaterialButton.TextButton("clearFilters","Clear")) { dataSearch=""; availableOnly=upcomingOnly=false; }
        ImGui.TextColored(MaterialTheme.Current.Colors.OnSurfaceVariant,"Accent colors style the interface; status colors retain their meaning.");
        using var tableStyle=MaterialTable.PushStyle();
        var flags=ImGuiTableFlags.RowBg|ImGuiTableFlags.Resizable|ImGuiTableFlags.Sortable|ImGuiTableFlags.ScrollY|ImGuiTableFlags.BordersInnerH;
        if(!ImGui.BeginTable("gathering",4,flags,new(0,ImGui.GetContentRegionAvail().Y))) return;
        try
        {
            ImGui.TableSetupScrollFreeze(0,1);
            ImGui.TableSetupColumn("Item",ImGuiTableColumnFlags.WidthStretch|ImGuiTableColumnFlags.DefaultSort,2);
            ImGui.TableSetupColumn("Level",ImGuiTableColumnFlags.WidthFixed,50*MaterialTheme.Metrics.Scale);
            ImGui.TableSetupColumn("Location",ImGuiTableColumnFlags.WidthStretch,1.3f);
            ImGui.TableSetupColumn("Status",ImGuiTableColumnFlags.WidthFixed,100*MaterialTheme.Metrics.Scale);
            ImGui.TableHeadersRow();
            var specs=ImGui.TableGetSortSpecs();
            if(specs.SpecsDirty&&specs.SpecsCount>0)
            {
                sortColumn=specs.Specs.ColumnIndex;
                sortDescending=specs.Specs.SortDirection==ImGuiSortDirection.Descending;
                Array.Sort(items,CompareItems);
                specs.SpecsDirty=false;
            }
            for(var i=0;i<items.Length;i++)
            {
                var item=items[i];
                if(dataSearch.Length>0&&!item.Name.Contains(dataSearch,StringComparison.OrdinalIgnoreCase)&&!item.Location.Contains(dataSearch,StringComparison.OrdinalIgnoreCase)) continue;
                if((availableOnly||upcomingOnly)&&!((availableOnly&&item.Status==0)||(upcomingOnly&&item.Status==1))) continue;
                ImGui.PushID(item.Id);
                ImGui.TableNextRow(); ImGui.TableSetColumnIndex(0);
                if(ImGui.Selectable(item.Name,selectedItem==item.Id,ImGuiSelectableFlags.SpanAllColumns))
                    selectedItem=item.Id;
                MaterialTable.RowState(selectedItem==item.Id,ImGui.IsItemHovered());
                if(ImGui.IsItemHovered()) ImGui.SetTooltip("Mock row. Native selection, resizing and sorting remain available.");
                ImGui.TableSetColumnIndex(1); ImGui.TextUnformatted(item.LevelLabel);
                ImGui.TableSetColumnIndex(2); ImGui.TextUnformatted(item.Location);
                ImGui.TableSetColumnIndex(3); ImGui.TextColored(item.Status==0?Available:item.Status==1?Upcoming:Missing,item.StatusLabel);
                ImGui.PopID();
            }
        }
        finally { ImGui.EndTable(); }
    }
    private int CompareItems(MockItem a,MockItem b)
    {
        var result=sortColumn switch {
            1=>a.Level.CompareTo(b.Level),2=>string.CompareOrdinal(a.Location,b.Location),
            3=>a.Status.CompareTo(b.Status),_=>string.CompareOrdinal(a.Name,b.Name) };
        if(result==0) result=a.Id.CompareTo(b.Id);
        return sortDescending?-result:result;
    }
}
