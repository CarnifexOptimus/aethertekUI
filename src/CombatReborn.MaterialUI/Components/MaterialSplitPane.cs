using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace CombatReborn.MaterialUI;

/// <summary>Call between two child panels on the same line; width is host-owned logical units.</summary>
public static class MaterialSplitPane
{
    public static bool Divider(string id, ref float firstWidth, float totalWidth, float minimumFirst = 120,
        float minimumSecond = 160, float height = -1)
    {
        if (!float.IsFinite(totalWidth) || totalWidth <= 0 || minimumFirst < 0 || minimumSecond < 0)
            throw new ArgumentOutOfRangeException(nameof(totalWidth));
        var m=MaterialTheme.Metrics;
        var dividerWidth=m.Gap;
        var available=Math.Max(0,totalWidth-dividerWidth/m.Scale);
        var low=Math.Min(minimumFirst,available);
        var high=Math.Max(low,available-minimumSecond);
        firstWidth=Math.Clamp(float.IsFinite(firstWidth)?firstWidth:low,low,high);
        ImGui.PushID(id);
        var origin=ImGui.GetCursorScreenPos();
        var pixels=new Vector2(dividerWidth,height<0?Math.Max(1,ImGui.GetContentRegionAvail().Y):height*m.Scale);
        var clicked=MaterialDraw.Hit("##divider",pixels,false,out var state);
        var old=firstWidth;
        if(state.Pressed) firstWidth=Math.Clamp(firstWidth+ImGui.GetIO().MouseDelta.X/m.Scale,low,high);
        if(state.Hovered||state.Pressed) ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEw);
        var c=MaterialTheme.Current.Colors;
        ImGui.GetWindowDrawList().AddLine(origin+new Vector2(dividerWidth*.5f,0),
            origin+new Vector2(dividerWidth*.5f,pixels.Y),MaterialDraw.Color(state.Hovered||state.Pressed?c.Primary:c.OutlineVariant),m.Border);
        ImGui.PopID();
        return old!=firstWidth || clicked;
    }
}
