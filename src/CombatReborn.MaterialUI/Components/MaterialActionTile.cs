using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace CombatReborn.MaterialUI;

public static class MaterialActionTile
{
    public static bool Draw(string id,MaterialIcon icon,string label,string? secondary=null,Vector2 size=default,bool active=false,bool disabled=false)
    {
        var m=MaterialTheme.Metrics; var c=MaterialTheme.Current.Colors;
        var actual=MaterialDraw.Size(size,140*m.Scale,m.TileHeight);
        ImGui.PushID(id);
        var clicked=MaterialDraw.Hit("##tile",actual,disabled,out var state);
        MaterialDraw.Shape(state,active?c.PrimaryContainer:c.SurfaceContainer,c.OnSurface,m.OuterRadius,true);
        if(state.Visible)
        {
            var dl=ImGui.GetWindowDrawList();
            dl.PushClipRect(state.Min+new Vector2(m.Gap),state.Max-new Vector2(m.Gap),true);
            MaterialIcons.Draw(icon,state.Min+new Vector2((actual.X-m.IconSize)*.5f,m.Padding),m.IconSize,c.Primary,state.Alpha);
            var text=ImGui.CalcTextSize(label);
            var y=m.Padding+m.IconSize+m.Gap;
            dl.AddText(state.Min+new Vector2((actual.X-text.X)*.5f,y),MaterialDraw.Color(c.OnSurface,state.Alpha),label);
            if(secondary is not null)
            {
                var sub=ImGui.CalcTextSize(secondary);
                dl.AddText(state.Min+new Vector2((actual.X-sub.X)*.5f,y+text.Y+m.Gap*.5f),MaterialDraw.Color(c.OnSurfaceVariant,state.Alpha),secondary);
            }
            dl.PopClipRect();
        }
        ImGui.PopID();
        return clicked;
    }
}
