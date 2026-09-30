using Dalamud.Bindings.ImGui;

namespace CombatReborn.MaterialUI.Demo;

internal sealed partial class Showcase
{
    private sealed record CatalogRow(int Key,string Label,string Level,string Zone,bool Available);
    private static readonly CatalogRow[] Catalog=CreateCatalog();
    private readonly MaterialDataView<CatalogRow> catalogView=new();
    private readonly MaterialSelectionSet<int> catalogSelection=new();
    private readonly MaterialSearchState catalogSearchState=new();
    private string catalogQuery="";
    private bool catalogAvailable, catalogDirty=true, catalogDescending;
    private int catalogSort;
    internal int SubmittedCatalogRows { get; private set; }
    private static CatalogRow[] CreateCatalog()
    {
        var result=new CatalogRow[10_000];
        for(var i=0;i<result.Length;i++)
            result[i]=new(i,$"Mock resource {i:00000}",(i%100+1).ToString(),i%2==0?"Coerthas":"Thavnair",i%3==0);
        return result;
    }
    private bool MatchesCatalog(CatalogRow row) => (!catalogAvailable||row.Available)&&
        (catalogQuery.Length==0||row.Label.Contains(catalogQuery,StringComparison.OrdinalIgnoreCase)||row.Zone.Contains(catalogQuery,StringComparison.OrdinalIgnoreCase));
    private int CompareCatalog(CatalogRow a,CatalogRow b)
    {
        var result=catalogSort switch
        {
            1=>(a.Key%100).CompareTo(b.Key%100),
            2=>string.CompareOrdinal(a.Zone,b.Zone),
            3=>a.Available.CompareTo(b.Available),
            _=>a.Key.CompareTo(b.Key)
        };
        if(result==0) result=a.Key.CompareTo(b.Key);
        return catalogDescending?-result:result;
    }
    private void LargeData()
    {
        MaterialStatus.SectionHeader("CATALOG / 10,000 MOCK ROWS");
        if(MaterialTextField.SearchField("catalog-search",ref catalogQuery,"Filter resources or zones...",state:catalogSearchState)) catalogDirty=true;
        if(MaterialChip.FilterChip("catalog-available","Available only",ref catalogAvailable)) catalogDirty=true;
        ImGui.SameLine();
        if(MaterialButton.TextButton("clear-selection","Clear selection")) catalogSelection.Clear();
        ImGui.TextUnformatted("Ctrl-click toggles selection. Sorting and filtering preserve selected domain IDs.");
        using var style=MaterialTable.PushStyle();
        if(!ImGui.BeginTable("catalog",4,ImGuiTableFlags.ScrollY|ImGuiTableFlags.Sortable|ImGuiTableFlags.Resizable|
            ImGuiTableFlags.Reorderable|ImGuiTableFlags.Hideable|ImGuiTableFlags.RowBg,new(0,0))) return;
        try
        {
            ImGui.TableSetupScrollFreeze(0,1);
            ImGui.TableSetupColumn("Resource",ImGuiTableColumnFlags.DefaultSort|ImGuiTableColumnFlags.WidthStretch,2);
            ImGui.TableSetupColumn("Level",ImGuiTableColumnFlags.WidthFixed,70*MaterialTheme.Metrics.Scale);
            ImGui.TableSetupColumn("Zone",ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Status",ImGuiTableColumnFlags.WidthFixed,100*MaterialTheme.Metrics.Scale);
            ImGui.TableHeadersRow();
            var specs=ImGui.TableGetSortSpecs();
            if(specs.SpecsDirty&&specs.SpecsCount>0)
            {
                catalogSort=specs.Specs.ColumnIndex;
                catalogDescending=specs.Specs.SortDirection==ImGuiSortDirection.Descending;
                catalogDirty=true; specs.SpecsDirty=false;
            }
            if(catalogDirty)
            {
                catalogView.Rebuild(Catalog,MatchesCatalog,Comparer<CatalogRow>.Create(CompareCatalog));
                catalogDirty=false;
            }
            SubmittedCatalogRows=0;
            using var clipper=new MaterialClipper(catalogView.Count);
            while(clipper.Step())
                for(var i=clipper.Start;i<clipper.End;i++)
                {
                    var row=catalogView[i]; SubmittedCatalogRows++;
                    ImGui.PushID(row.Key);
                    ImGui.TableNextRow(); ImGui.TableSetColumnIndex(0);
                    if(ImGui.Selectable(row.Label,catalogSelection.Contains(row.Key),ImGuiSelectableFlags.SpanAllColumns))
                    {
                        if(ImGui.GetIO().KeyCtrl) catalogSelection.Toggle(row.Key);
                        else catalogSelection.SelectOnly(row.Key);
                    }
                    MaterialTable.RowState(catalogSelection.Contains(row.Key),ImGui.IsItemHovered());
                    using(var menu=MaterialContextMenu.Begin("row"))
                        if(menu.Visible&&ImGui.MenuItem("Select only this row")) catalogSelection.SelectOnly(row.Key);
                    ImGui.TableSetColumnIndex(1);ImGui.TextUnformatted(row.Level);
                    ImGui.TableSetColumnIndex(2);ImGui.TextUnformatted(row.Zone);
                    ImGui.TableSetColumnIndex(3);
                    using(var semantic=MaterialSemantic.Text(row.Available?Available:Upcoming))
                        ImGui.TextUnformatted(row.Available?"Available":"Upcoming");
                    ImGui.PopID();
                }
        }
        finally { ImGui.EndTable(); }
    }
}
