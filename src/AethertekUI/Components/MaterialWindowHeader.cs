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
        var text=MaterialText.Measure(title);
        var detail=subtitle is null?Vector2.Zero:MaterialText.Measure(subtitle);
        var titleHeight=MaterialNativeCaption.IsShaped(title)?text.Y:ImGui.GetTextLineHeight();
        var detailHeight=subtitle is not null && MaterialNativeCaption.IsShaped(subtitle)?detail.Y:ImGui.GetTextLineHeight();
        var height=compact||subtitle is null?m.ButtonHeight:Math.Max(m.ButtonHeight,titleHeight+detailHeight+m.Gap);
        if(MaterialNativeCaption.IsShaped(title)) height=Math.Max(height,text.Y);
        var actionWidth=actions.Length*(m.ButtonHeight+m.Gap);
        var dl=ImGui.GetWindowDrawList();
        dl.PushClipRect(origin,origin+new Vector2(Math.Max(0,width-actionWidth),height),true);
        MaterialText.AddText(dl,origin,MaterialDraw.Color(c.OnSurface),title);
        if(!compact&&subtitle is not null) MaterialText.AddText(dl,origin+new Vector2(0,titleHeight+m.Gap),MaterialDraw.Color(c.OnSurfaceVariant),subtitle);
        dl.PopClipRect();
        ImGui.BeginGroup();
        var result=-1;
        for(var i=0;i<actions.Length;i++)
        {
            ImGui.SetCursorScreenPos(origin+new Vector2(width-actionWidth+i*(m.ButtonHeight+m.Gap),0));
            if(MaterialButton.IconButton(actions[i].Id,actions[i].Icon)) result=i;
            if(actions[i].Tooltip is not null&&ImGui.IsItemHovered()) MaterialSemantic.DrawTooltip(actions[i].Tooltip!);
        }
        ImGui.SetCursorScreenPos(origin); ImGui.Dummy(new(width,height)); ImGui.EndGroup();
        return result;
    }
}
