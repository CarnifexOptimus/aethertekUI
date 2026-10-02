using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace AethertekUI;

public static class MaterialChip
{
    public static bool FilterChip(string id,string label,ref bool selected,bool disabled=false,MaterialIcon icon=MaterialIcon.None)
    {
        var clicked=Draw(id,label,selected,disabled,selected?MaterialIcon.Check:icon);
        if(clicked) selected=!selected;
        return clicked;
    }
    public static bool ActionChip(string id,string label,MaterialIcon icon=MaterialIcon.None,bool disabled=false)
        => Draw(id,label,false,disabled,icon);
    private static bool Draw(string id,string label,bool selected,bool disabled,MaterialIcon icon)
    {
        var m=MaterialTheme.Metrics; var c=MaterialTheme.Current.Colors;
        var controls=MaterialControls.Metrics;
        var size=new Vector2(MaterialLayout.ResolveWidth(0,ImGui.CalcTextSize(label).X+2*controls.Padding.X+(icon==MaterialIcon.None?0:controls.IconSize+controls.Gap),
            ImGui.GetContentRegionAvail().X,m.Scale),Math.Max(Math.Min(m.ChipHeight,controls.Height),ImGui.GetTextLineHeight()+2*m.Scale));
        ImGui.PushID(id);
        var clicked=MaterialDraw.Hit("##chip",size,disabled,out var state);
        MaterialDraw.Shape(state,selected?c.SecondaryContainer:Vector4.Zero,c.OnSurface,size.Y*.5f,!selected);
        MaterialDraw.Content(state,label,selected?c.OnSecondaryContainer:c.OnSurface,icon,MaterialIcon.None);
        ImGui.PopID();
        return clicked;
    }
}
