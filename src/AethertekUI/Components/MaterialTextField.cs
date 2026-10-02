using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace AethertekUI;

/// <summary>Optional host-owned focus request, also restores editing focus after clearing.</summary>
public sealed class MaterialSearchState
{
    public bool FocusRequested { get; set; }
}

public static class MaterialTextField
{
    public static bool SearchField(string id,ref string value,string hint="Search...",float width=-1,bool disabled=false,MaterialSearchState? state=null,int maxLength=1024)
        => Field(id,ref value,hint,width,disabled,false,MaterialIcon.Search,MaterialIcon.None,true,ImGuiInputTextFlags.None,maxLength,state);
    public static bool TextField(string id,ref string value,string hint="",float width=-1,bool disabled=false,bool error=false,
        MaterialIcon leading=MaterialIcon.None,MaterialIcon trailing=MaterialIcon.None,ImGuiInputTextFlags flags=ImGuiInputTextFlags.None,int maxLength=1024)
        => Field(id,ref value,hint,width,disabled,error,leading,trailing,false,flags,maxLength);

    private static bool Field(string id,ref string value,string hint,float width,bool disabled,bool error,
        MaterialIcon leading,MaterialIcon trailing,bool clear,ImGuiInputTextFlags flags,int maxLength,MaterialSearchState? searchState=null)
    {
        if(maxLength<1) throw new ArgumentOutOfRangeException(nameof(maxLength));
        var m=MaterialTheme.Metrics; var c=MaterialTheme.Current.Colors;
        var controls=MaterialControls.Metrics;
        var height=controls.Height;
        var size=new Vector2(MaterialLayout.ResolveWidth(width,220*m.Scale,ImGui.GetContentRegionAvail().X,m.Scale),height);
        // Suppress decoration when a narrow panel cannot fit an editable area.
        if (size.X < height*3) leading=MaterialIcon.None;
        if (size.X < height*2) { clear=false; trailing=MaterialIcon.None; }
        var inset=Math.Min(controls.Gap,Math.Max(0,(size.X-1)*.25f));
        var origin=ImGui.GetCursorScreenPos();
        ImGui.PushID(id); ImGui.BeginGroup(); ImGui.BeginDisabled(disabled);
        var dl=ImGui.GetWindowDrawList();
        dl.AddRectFilled(origin,origin+size,MaterialDraw.Color(c.SurfaceContainerHighest),controls.Rounding);
        var leadingWidth=leading==MaterialIcon.None?0:height-controls.Gap;
        var trailingWidth=clear||trailing!=MaterialIcon.None?height:0;
        var style=new MaterialStyleScope();
        style.Color(ImGuiCol.FrameBg,Vector4.Zero);
        style.Color(ImGuiCol.FrameBgHovered,Vector4.Zero);
        style.Color(ImGuiCol.FrameBgActive,Vector4.Zero);
        style.Style(ImGuiStyleVar.FramePadding,new Vector2(inset,(height-ImGui.GetTextLineHeight())*.5f));
        style.Style(ImGuiStyleVar.FrameBorderSize,0);
        ImGui.SetCursorScreenPos(origin+new Vector2(leadingWidth+inset,0));
        ImGui.SetNextItemWidth(Math.Max(1,size.X-leadingWidth-trailingWidth-2*inset));
        if (!disabled && searchState is { FocusRequested: true })
        {
            ImGui.SetKeyboardFocusHere();
            searchState.FocusRequested=false;
        }
        var changed=ImGui.InputTextWithHint("##input",hint,ref value,maxLength,flags);
        var focused=ImGui.IsItemActive()||ImGui.IsItemFocused();
        style.Dispose();
        if(leading!=MaterialIcon.None) MaterialIcons.Draw(leading,origin+new Vector2(controls.Gap,(height-controls.IconSize)*.5f),controls.IconSize,c.OnSurfaceVariant);
        if(clear)
        {
            ImGui.SetCursorScreenPos(origin+new Vector2(Math.Max(0,size.X-height),0));
            if(MaterialButton.IconButton("clear",MaterialIcon.Close,disabled:disabled||value.Length==0,size:height/m.Scale))
            { value=string.Empty; changed=true; if(searchState is not null) searchState.FocusRequested=true; }
        }
        else if(trailing!=MaterialIcon.None)
            MaterialIcons.Draw(trailing,origin+new Vector2(size.X-height+(height-controls.IconSize)*.5f,(height-controls.IconSize)*.5f),controls.IconSize,c.OnSurfaceVariant);
        var hovered=!disabled&&ImGui.IsWindowHovered()&&ImGui.IsMouseHoveringRect(origin,origin+size);
        dl.AddRect(origin,origin+size,MaterialDraw.Color(error?c.Error:focused?c.Primary:hovered?c.Outline:c.OutlineVariant),
            controls.Rounding,ImDrawFlags.RoundCornersAll,focused||error?m.Border*2:m.Border);
        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(size);
        ImGui.EndDisabled(); ImGui.EndGroup(); ImGui.PopID();
        return changed;
    }
}
