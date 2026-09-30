using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace CombatReborn.MaterialUI;

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
        var size=new Vector2(ImGui.CalcTextSize(label).X+2*m.Padding+(icon==MaterialIcon.None?0:m.IconSize+m.Gap),
            Math.Max(m.ChipHeight,ImGui.GetTextLineHeight()+m.Gap));
        ImGui.PushID(id);
        var clicked=MaterialDraw.Hit("##chip",size,disabled,out var state);
        MaterialDraw.Shape(state,selected?c.SecondaryContainer:Vector4.Zero,c.OnSurface,size.Y*.5f,!selected);
        MaterialDraw.Content(state,label,selected?c.OnSecondaryContainer:c.OnSurface,icon,MaterialIcon.None);
        ImGui.PopID();
        return clicked;
    }
}
