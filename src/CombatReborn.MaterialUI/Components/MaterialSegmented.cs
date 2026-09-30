using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace CombatReborn.MaterialUI;

public readonly record struct MaterialSegment<T>(T Value, string Label, MaterialIcon Icon = MaterialIcon.None, bool Disabled = false);

public static class MaterialSegmented
{
    public static bool Draw<T>(string id, ReadOnlySpan<MaterialSegment<T>> segments, ref T value, float width = -1, bool disabled = false)
    {
        if(segments.Length<2) throw new ArgumentException("At least two segments are required.",nameof(segments));
        var m=MaterialTheme.Metrics; var c=MaterialTheme.Current.Colors;
        var natural=0f;
        for(var i=0;i<segments.Length;i++)
            natural=Math.Max(natural,ImGui.CalcTextSize(segments[i].Label).X+2*m.Padding+m.IconSize+m.Gap);
        var size=MaterialDraw.Size(new(width,0),natural*segments.Length,Math.Max(m.ButtonHeight,ImGui.GetTextLineHeight()+2*m.Gap));
        var changed=false;
        ImGui.PushID(id);
        ImGui.BeginGroup();
        for(var i=0;i<segments.Length;i++)
        {
            if(i>0) ImGui.SameLine(0,0);
            var bounds=MaterialGeometry.SegmentBounds(size.X,segments.Length,i);
            ImGui.PushID(i);
            if(MaterialDraw.Hit("##segment",new(bounds.Width,size.Y),disabled||segments[i].Disabled,out var state)
                && !EqualityComparer<T>.Default.Equals(value,segments[i].Value))
            {
                value=segments[i].Value; changed=true;
            }
            var selected=EqualityComparer<T>.Default.Equals(value,segments[i].Value);
            var emphasis=MaterialDraw.Animate(state.Id,1,selected?1:0);
            var corners=i==0?ImDrawFlags.RoundCornersLeft:i==segments.Length-1?ImDrawFlags.RoundCornersRight:ImDrawFlags.RoundCornersNone;
            var bg=Vector4.Lerp(c.SurfaceContainerLow,c.SecondaryContainer,emphasis);
            MaterialDraw.Shape(state,bg,c.OnSecondaryContainer,size.Y*.5f,false,corners);
            MaterialDraw.Content(state,segments[i].Label,selected?c.OnSecondaryContainer:c.OnSurface,
                selected?MaterialIcon.Check:segments[i].Icon,MaterialIcon.None);
            if(state.Visible)
            {
                var dl=ImGui.GetWindowDrawList();
                // Straight shared boundaries, rounded outer corners only.
                dl.AddRect(state.Min,state.Max,MaterialDraw.Color(c.OutlineVariant,state.Alpha),size.Y*.5f,corners,m.Border);
            }
            ImGui.PopID();
        }
        ImGui.EndGroup();
        ImGui.PopID();
        return changed;
    }
}
