using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace AethertekUI;

public static class MaterialStatus
{
    public static void Badge(string label,Vector4? color=null,Vector4? background=null)
        => BadgeCore(label,null,color,background);

    /// <summary>Explicit corner radius in logical units; existing badges remain pill-shaped.</summary>
    public static void Badge(string label,float cornerRadius,Vector4? color=null,Vector4? background=null)
    {
        if(!float.IsFinite(cornerRadius) || cornerRadius < 0) throw new ArgumentOutOfRangeException(nameof(cornerRadius));
        BadgeCore(label,cornerRadius,color,background);
    }

    public static void Badge(string label, MaterialControlAppearance appearance, Vector2 size = default)
        => BadgeCore(label,appearance.CornerRadius,appearance.Foreground,appearance.Fill,appearance,size);

    private static void BadgeCore(string label,float? cornerRadius,Vector4? color,Vector4? background,
        MaterialControlAppearance? appearance = null, Vector2 dimensions = default)
    {
        var m=MaterialTheme.Metrics; var c=MaterialTheme.Current.Colors;
        var fg=color??c.OnSecondaryContainer;
        var bg=background??(color.HasValue?MaterialColor.Alpha(fg,.14f):c.SecondaryContainer);
        var text=MaterialText.Measure(label);
        var size=new Vector2(text.X+2*m.Gap,Math.Max(m.ChipHeight,text.Y+m.Gap));
        if(appearance.HasValue) { appearance.Value.Radius(m.Scale); size=MaterialDraw.Size(dimensions,size.X,size.Y); }
        var p=ImGui.GetCursorScreenPos(); var dl=ImGui.GetWindowDrawList();
        ImGui.Dummy(size);
        if(!ImGui.IsItemVisible()) return;
        var radius=cornerRadius.HasValue ? Math.Min(cornerRadius.Value*m.Scale,Math.Min(size.X,size.Y)*.5f) : size.Y*.5f;
        dl.AddRectFilled(p,p+size,MaterialDraw.Color(bg),radius);
        if(appearance is { } a) dl.AddRect(p,p+size,MaterialDraw.Color(a.Border),radius,ImDrawFlags.RoundCornersAll,m.Border);
        if(appearance.HasValue) dl.PushClipRect(p+new Vector2(2*m.Scale,0),p+size-new Vector2(2*m.Scale,0),true);
        MaterialText.AddText(dl,p+new Vector2(Math.Max(2*m.Scale,(size.X-text.X)*.5f),(size.Y-text.Y)*.5f),MaterialDraw.Color(fg),label);
        if(appearance.HasValue)
        {
            dl.PopClipRect();
            if(text.X>size.X-4*m.Scale && ImGui.IsItemHovered()) MaterialSemantic.DrawTooltip(label);
        }
    }
    public static void LinearProgress(float fraction,float width=-1,float height=0,Vector4? accent=null,Vector4? track=null)
    {
        var m=MaterialTheme.Metrics; var c=MaterialTheme.Current.Colors;
        var size=MaterialDraw.Size(new(width,height),200*m.Scale,m.ProgressHeight);
        var p=ImGui.GetCursorScreenPos(); var dl=ImGui.GetWindowDrawList();
        ImGui.Dummy(size);
        if(!ImGui.IsItemVisible()) return;
        var f=float.IsFinite(fraction)?Math.Clamp(fraction,0,1):0;
        dl.AddRectFilled(p,p+size,MaterialDraw.Color(track??c.SurfaceContainerHighest),size.Y*.5f);
        if(f>0)
        {
            var end=p+new Vector2(size.X*f,size.Y);
            dl.AddRectFilled(p,end,MaterialDraw.Color(accent??c.Primary),Math.Min(size.Y,size.X*f)*.5f);
        }
    }
    public static void SectionHeader(string label)
    {
        var m=MaterialTheme.Metrics;
        ImGui.Dummy(new Vector2(0,m.SectionGap*.5f));
        MaterialText.TextColored(MaterialTheme.Current.Colors.OnSurfaceVariant,label);
        ImGui.Dummy(new Vector2(0,m.Gap*.25f));
    }
}
