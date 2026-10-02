using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace AethertekUI;

public enum MaterialButtonVariant { Filled, Tonal, Outlined, Text }

public static class MaterialButton
{
    public static bool FilledButton(string id, string label, Vector2 size = default, bool disabled = false,
        MaterialIcon leading = MaterialIcon.None, MaterialIcon trailing = MaterialIcon.None)
        => Draw(id,label,MaterialButtonVariant.Filled,size,disabled,leading,trailing);
    public static bool FilledTonalButton(string id, string label, Vector2 size = default, bool disabled = false,
        MaterialIcon leading = MaterialIcon.None, MaterialIcon trailing = MaterialIcon.None)
        => Draw(id,label,MaterialButtonVariant.Tonal,size,disabled,leading,trailing);
    public static bool OutlinedButton(string id, string label, Vector2 size = default, bool disabled = false,
        MaterialIcon leading = MaterialIcon.None, MaterialIcon trailing = MaterialIcon.None)
        => Draw(id,label,MaterialButtonVariant.Outlined,size,disabled,leading,trailing);
    public static bool TextButton(string id, string label, Vector2 size = default, bool disabled = false,
        MaterialIcon leading = MaterialIcon.None, MaterialIcon trailing = MaterialIcon.None)
        => Draw(id,label,MaterialButtonVariant.Text,size,disabled,leading,trailing);

    public static bool Draw(string id, string label, MaterialButtonVariant variant = MaterialButtonVariant.Filled,
        Vector2 size = default, bool disabled = false, MaterialIcon leading = MaterialIcon.None, MaterialIcon trailing = MaterialIcon.None)
    {
        var c=MaterialTheme.Current.Colors; var m=MaterialControls.Metrics;
        var width=ImGui.CalcTextSize(label).X+2*m.Padding.X;
        if(leading!=MaterialIcon.None) width+=m.IconSize+m.Gap;
        if(trailing!=MaterialIcon.None) width+=m.IconSize+m.Gap;
        var actual=MaterialDraw.Size(size,width,m.Height);
        ImGui.PushID(id);
        var clicked=MaterialDraw.Hit("##button",actual,disabled,out var state);
        var bg=variant switch { MaterialButtonVariant.Filled=>c.Primary, MaterialButtonVariant.Tonal=>c.SecondaryContainer, _=>Vector4.Zero };
        var fg=variant switch { MaterialButtonVariant.Filled=>c.OnPrimary, MaterialButtonVariant.Tonal=>c.OnSecondaryContainer, _=>c.Primary };
        if(disabled) { bg=variant is MaterialButtonVariant.Filled or MaterialButtonVariant.Tonal ? c.SurfaceVariant : Vector4.Zero; fg=c.OnSurface; }
        MaterialDraw.Shape(state,bg,fg,actual.Y*.5f,variant==MaterialButtonVariant.Outlined);
        MaterialDraw.Content(state,label,fg,leading,trailing);
        ImGui.PopID();
        return clicked;
    }

    public static bool IconButton(string id, MaterialIcon icon, bool tonal = false, bool disabled = false, float size = 0)
    {
        var m=MaterialTheme.Metrics; var c=MaterialTheme.Current.Colors;
        var controls=MaterialControls.Metrics;
        var side=size>0?size*m.Scale:controls.Height;
        var iconSize=Math.Min(controls.IconSize,Math.Max(1,side-2*m.Scale));
        ImGui.PushID(id);
        var clicked=MaterialDraw.Hit("##icon",new(side),disabled,out var state);
        MaterialDraw.Shape(state,tonal?c.SecondaryContainer:Vector4.Zero,c.OnSurface,side*.5f);
        if(state.Visible) MaterialIcons.Draw(icon,state.Min+new Vector2((side-iconSize)*.5f),iconSize,tonal?c.OnSecondaryContainer:c.OnSurface,state.Alpha);
        ImGui.PopID();
        return clicked;
    }
}
