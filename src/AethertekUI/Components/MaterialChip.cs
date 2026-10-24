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

    public static bool FilterChip(string id,string label,ref bool selected,MaterialControlAppearance appearance,
        bool disabled=false,MaterialIcon icon=MaterialIcon.None,bool showSelectionCheck=true)
    {
        appearance.Radius(MaterialTheme.Metrics.Scale);
        var clicked=Draw(id,label,selected,disabled,selected&&showSelectionCheck?MaterialIcon.Check:icon,appearance);
        if(clicked) selected=!selected;
        return clicked;
    }

    private static bool Draw(string id,string label,bool selected,bool disabled,MaterialIcon icon,MaterialControlAppearance? appearance=null)
    {
        var m=MaterialTheme.Metrics; var c=MaterialTheme.Current.Colors;
        var controls=MaterialControls.Metrics;
        var text=MaterialText.Measure(label);
        var lineHeight=MaterialNativeCaption.IsShaped(label)?text.Y:ImGui.GetTextLineHeight();
        var size=new Vector2(MaterialLayout.ResolveWidth(0,text.X+2*controls.Padding.X+(icon==MaterialIcon.None?0:controls.IconSize+controls.Gap),
            ImGui.GetContentRegionAvail().X,m.Scale),Math.Max(Math.Min(m.ChipHeight,controls.Height),lineHeight+2*m.Scale));
        ImGui.PushID(id);
        var clicked=MaterialDraw.Hit("##chip",size,disabled,out var state);
        var foreground=appearance?.Foreground??(selected?c.OnSecondaryContainer:c.OnSurface);
        MaterialDraw.Shape(state,appearance?.Fill??(selected?c.SecondaryContainer:Vector4.Zero),foreground,
            appearance?.Radius(m.Scale)??size.Y*.5f,appearance.HasValue||!selected,border:appearance?.Border,preserveFill:appearance.HasValue);
        MaterialDraw.Content(state,label,foreground,icon,MaterialIcon.None);
        ImGui.PopID();
        return clicked;
    }
}
