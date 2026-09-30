using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace CombatReborn.MaterialUI;

public static class MaterialSwitch
{
    public static bool Draw(string id, string label, ref bool value, bool disabled = false)
    {
        var m=MaterialTheme.Metrics; var c=MaterialTheme.Current.Colors;
        var track=m.SwitchSize;
        var height=Math.Max(track.Y,ImGui.GetTextLineHeight()+m.Gap);
        var width=track.X+(label.Length==0?0:m.Gap+ImGui.CalcTextSize(label).X);
        ImGui.PushID(id);
        var changed=MaterialDraw.Hit("##switch",new(width,height),disabled,out var state);
        if(changed) value=!value;
        var t=MaterialDraw.Animate(state.Id,2,value?1:0);
        if(state.Visible)
        {
            var min=state.Min+new Vector2(0,(height-track.Y)*.5f);
            var trackState=state with { Min=min,Max=min+track };
            var bg=Vector4.Lerp(c.SurfaceContainerHighest,c.Primary,t);
            MaterialDraw.Shape(trackState,bg,c.OnSurface,track.Y*.5f,!value);
            var inset=3*m.Scale;
            var radius=(track.Y-2*inset)*.5f;
            var center=min+new Vector2(inset+radius+t*(track.X-2*(inset+radius)),track.Y*.5f);
            var thumb=Vector4.Lerp(c.Outline,c.OnPrimary,t);
            ImGui.GetWindowDrawList().AddCircleFilled(center,radius,MaterialDraw.Color(thumb,state.Alpha),24);
            if(state.Hovered)
                ImGui.GetWindowDrawList().AddCircle(center,radius+inset,MaterialDraw.Color(MaterialColor.Alpha(c.OnSurface,.12f),state.Alpha),24,m.Border*2);
            ImGui.GetWindowDrawList().AddText(state.Min+new Vector2(track.X+m.Gap,(height-ImGui.GetTextLineHeight())*.5f),
                MaterialDraw.Color(c.OnSurface,state.Alpha),label);
        }
        ImGui.PopID();
        return changed;
    }
}

public static class MaterialSelection
{
    public static bool Checkbox(string id, string label, ref bool value, bool disabled = false)
    {
        var m=MaterialTheme.Metrics; var c=MaterialTheme.Current.Colors;
        var side=Math.Max(m.IconSize,ImGui.GetTextLineHeight());
        var size=new Vector2(side+m.Gap+ImGui.CalcTextSize(label).X,side+m.Gap);
        ImGui.PushID(id);
        var changed=MaterialDraw.Hit("##checkbox",size,disabled,out var state);
        if(changed) value=!value;
        var box=state with { Min=state.Min+new Vector2(0,m.Gap*.5f),Max=state.Min+new Vector2(side,side+m.Gap*.5f) };
        MaterialDraw.Shape(box,value?c.Primary:c.SurfaceContainer,c.OnSurface,m.Border*3,!value);
        if(state.Visible)
        {
            if(value) MaterialIcons.Draw(MaterialIcon.Check,box.Min,side,c.OnPrimary,state.Alpha);
            ImGui.GetWindowDrawList().AddText(state.Min+new Vector2(side+m.Gap,(size.Y-ImGui.GetTextLineHeight())*.5f),MaterialDraw.Color(c.OnSurface,state.Alpha),label);
        }
        ImGui.PopID();
        return changed;
    }
    public static bool Radio<T>(string id, string label, T option, ref T value, bool disabled = false)
    {
        var m=MaterialTheme.Metrics; var c=MaterialTheme.Current.Colors;
        var side=Math.Max(m.IconSize,ImGui.GetTextLineHeight());
        var size=new Vector2(side+m.Gap+ImGui.CalcTextSize(label).X,side+m.Gap);
        ImGui.PushID(id);
        var clicked=MaterialDraw.Hit("##radio",size,disabled,out var state);
        var selected=EqualityComparer<T>.Default.Equals(value,option);
        var changed=clicked&&!selected;
        if(changed) value=option;
        if(state.Visible)
        {
            var center=state.Min+new Vector2(side*.5f,size.Y*.5f);
            var dl=ImGui.GetWindowDrawList();
            dl.AddCircle(center,side*.45f,MaterialDraw.Color(selected||changed||state.Focused?c.Primary:c.Outline,state.Alpha),20,m.Border*(state.Focused?2:1));
            if(selected||changed) dl.AddCircleFilled(center,side*.25f,MaterialDraw.Color(c.Primary,state.Alpha),16);
            dl.AddText(state.Min+new Vector2(side+m.Gap,(size.Y-ImGui.GetTextLineHeight())*.5f),MaterialDraw.Color(c.OnSurface,state.Alpha),label);
        }
        ImGui.PopID();
        return changed;
    }
}
