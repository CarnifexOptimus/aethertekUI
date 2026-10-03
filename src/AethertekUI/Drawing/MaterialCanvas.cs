using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace AethertekUI;

/// <summary>Surface coordinates are pixels. Image/button size arguments are logical units, scaled exactly once.</summary>
public static class MaterialCanvas
{
    public static uint Color(Vector4 color) => MaterialDraw.Color(color);
    public static void Surface(Vector2 min, Vector2 max, Vector4 color, float radius)
        => ImGui.GetWindowDrawList().AddRectFilled(min,max,Color(color),Math.Max(0,radius));

    /// <summary>Rounded vertical gradient; coordinates and radius remain pixels.</summary>
    public static void Surface(Vector2 min, Vector2 max, Vector4 top, Vector4 bottom, float radius)
    {
        var dl=ImGui.GetWindowDrawList();
        var start=dl.VtxBuffer.Size;
        dl.AddRectFilled(min,max,Color(Vector4.One),Math.Max(0,radius));
        for(var i=start;i<dl.VtxBuffer.Size;i++)
        {
            var vertex=dl.VtxBuffer[i];
            var t=Math.Clamp((vertex.Pos.Y-min.Y)/Math.Max(1,max.Y-min.Y),0,1);
            // Preserve the mesh's antialias fringe coverage.
            var color=Vector4.Lerp(top,bottom,t);
            color.W *= ((vertex.Col>>24)&255)/255f;
            vertex.Col=ImGui.ColorConvertFloat4ToU32(color);
            dl.VtxBuffer[i]=vertex;
        }
    }

    /// <summary>Borrowed texture; the caller must keep it alive for the entire rendered frame.</summary>
    public static void Image(ImTextureID texture, Vector2 size, Vector4? tint = null)
        => ImGui.Image(texture,MaterialTheme.Metrics.Scaled(size),Vector2.Zero,Vector2.One,tint ?? Vector4.One);

    /// <summary>Borrowed icon font. No font ownership or FontAwesome dependency.</summary>
    public static bool GlyphButton(string id, string glyph, ImFontPtr font, bool disabled = false, float size = 0)
    {
        var m=MaterialTheme.Metrics; var c=MaterialTheme.Current.Colors;
        var controls=MaterialControls.Metrics;
        var pixels=size>0?size*m.Scale:controls.Height;
        ImGui.PushID(id);
        var clicked=MaterialDraw.Hit("##glyph",new Vector2(pixels),disabled,out var state);
        MaterialDraw.Shape(state,c.SecondaryContainer,c.OnSecondaryContainer,pixels*.5f);
        ImGui.PushFont(font);
        try
        {
            if(state.Visible)
            {
                var measured=ImGui.CalcTextSize(glyph);
                var available=Math.Min(controls.IconSize,Math.Max(1,pixels-2*m.Scale));
                var factor=Math.Min(1,available/Math.Max(1,Math.Max(measured.X,measured.Y)));
                var dl=ImGui.GetWindowDrawList();
                dl.PushClipRect(state.Min,state.Max,true);
                dl.AddText(font,ImGui.GetFontSize()*factor,state.Min+(new Vector2(pixels)-measured*factor)*.5f,
                    MaterialDraw.Color(c.OnSecondaryContainer,state.Alpha),glyph);
                dl.PopClipRect();
            }
        }
        finally { ImGui.PopFont(); ImGui.PopID(); }
        return clicked;
    }

    public static bool ImageButton(string id, ImTextureID texture, Vector2 size, bool selected = false, bool disabled = false,
        Vector4? tint = null)
    {
        var m=MaterialTheme.Metrics; var c=MaterialTheme.Current.Colors;
        var controls=MaterialControls.Metrics;
        var pixels=MaterialDraw.Size(size,controls.Height,controls.Height);
        ImGui.PushID(id);
        var clicked=MaterialDraw.Hit("##image",pixels,disabled,out var state);
        MaterialDraw.Shape(state,selected?c.PrimaryContainer:c.SurfaceContainer,c.OnSurface,m.Radius,selected);
        if(state.Visible)
        {
            var inset=Math.Min(controls.Gap,Math.Max(0,(Math.Min(pixels.X,pixels.Y)-1)*.5f));
            ImGui.GetWindowDrawList().AddImage(texture,state.Min+new Vector2(inset),state.Max-new Vector2(inset),
                Vector2.Zero,Vector2.One,MaterialDraw.Color(tint ?? Vector4.One,state.Alpha));
        }
        ImGui.PopID();
        return clicked;
    }
}
