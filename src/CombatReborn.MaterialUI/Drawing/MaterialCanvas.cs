using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace CombatReborn.MaterialUI;

/// <summary>Public escape hatch for charts and game-anchored overlays. Coordinates and sizes here are pixels.</summary>
public static class MaterialCanvas
{
    public static uint Color(Vector4 color) => MaterialDraw.Color(color);
    public static void Surface(Vector2 min, Vector2 max, Vector4 color, float radius)
        => ImGui.GetWindowDrawList().AddRectFilled(min,max,Color(color),Math.Max(0,radius));

    /// <summary>Borrowed texture; the caller must keep it alive for the entire rendered frame.</summary>
    public static void Image(ImTextureID texture, Vector2 size, Vector4? tint = null)
        => ImGui.Image(texture,MaterialTheme.Metrics.Scaled(size),Vector2.Zero,Vector2.One,tint ?? Vector4.One);

    /// <summary>Borrowed icon font. No font ownership or FontAwesome dependency.</summary>
    public static bool GlyphButton(string id, string glyph, ImFontPtr font, bool disabled = false, float size = 0)
    {
        var m=MaterialTheme.Metrics; var c=MaterialTheme.Current.Colors;
        var pixels=size>0?size*m.Scale:m.ButtonHeight;
        ImGui.PushID(id);
        var clicked=MaterialDraw.Hit("##glyph",new Vector2(pixels),disabled,out var state);
        MaterialDraw.Shape(state,c.SecondaryContainer,c.OnSecondaryContainer,pixels*.5f);
        ImGui.PushFont(font);
        try { MaterialDraw.Content(state,glyph,c.OnSecondaryContainer,MaterialIcon.None,MaterialIcon.None); }
        finally { ImGui.PopFont(); ImGui.PopID(); }
        return clicked;
    }

    public static bool ImageButton(string id, ImTextureID texture, Vector2 size, bool selected = false, bool disabled = false,
        Vector4? tint = null)
    {
        var m=MaterialTheme.Metrics; var c=MaterialTheme.Current.Colors;
        var pixels=MaterialDraw.Size(size,m.ButtonHeight,m.ButtonHeight);
        ImGui.PushID(id);
        var clicked=MaterialDraw.Hit("##image",pixels,disabled,out var state);
        MaterialDraw.Shape(state,selected?c.PrimaryContainer:c.SurfaceContainer,c.OnSurface,m.Radius,selected);
        if(state.Visible)
            ImGui.GetWindowDrawList().AddImage(texture,state.Min+new Vector2(m.Gap),state.Max-new Vector2(m.Gap),
                Vector2.Zero,Vector2.One,MaterialDraw.Color(tint ?? Vector4.One,state.Alpha));
        ImGui.PopID();
        return clicked;
    }
}
