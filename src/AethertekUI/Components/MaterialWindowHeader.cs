using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace AethertekUI;

public readonly record struct MaterialHeaderAction(string Id,MaterialIcon Icon,string? Tooltip=null);

public static class MaterialWindowHeader
{
    /// <returns>Index of the activated action, or -1.</returns>
    public static int Draw(string title,string? subtitle,ReadOnlySpan<MaterialHeaderAction> actions,bool compact=false)
    {
        var m=MaterialTheme.Metrics; var c=MaterialTheme.Current.Colors;
        var origin=ImGui.GetCursorScreenPos();
        var width=ImGui.GetContentRegionAvail().X;
        var height=compact||subtitle is null?m.ButtonHeight:Math.Max(m.ButtonHeight,ImGui.GetTextLineHeight()*2+m.Gap);
        var actionWidth=actions.Length*(m.ButtonHeight+m.Gap);
        var dl=ImGui.GetWindowDrawList();
        dl.PushClipRect(origin,origin+new Vector2(Math.Max(0,width-actionWidth),height),true);
        dl.AddText(origin,MaterialDraw.Color(c.OnSurface),title);
        if(!compact&&subtitle is not null) dl.AddText(origin+new Vector2(0,ImGui.GetTextLineHeight()+m.Gap),MaterialDraw.Color(c.OnSurfaceVariant),subtitle);
        dl.PopClipRect();
        ImGui.BeginGroup();
        var result=-1;
        for(var i=0;i<actions.Length;i++)
        {
            ImGui.SetCursorScreenPos(origin+new Vector2(width-actionWidth+i*(m.ButtonHeight+m.Gap),0));
            if(MaterialButton.IconButton(actions[i].Id,actions[i].Icon)) result=i;
            if(actions[i].Tooltip is not null&&ImGui.IsItemHovered()) ImGui.SetTooltip(actions[i].Tooltip!);
        }
        ImGui.SetCursorScreenPos(origin); ImGui.Dummy(new(width,height)); ImGui.EndGroup();
        return result;
    }
}
