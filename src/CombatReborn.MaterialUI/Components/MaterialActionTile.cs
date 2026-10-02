using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace CombatReborn.MaterialUI;

public enum MaterialTileSizing { Content, Fixed }

public static class MaterialActionTile
{
    public static bool Draw(string id, MaterialIcon icon, string label, string? secondary = null,
        Vector2 size = default, bool active = false, bool disabled = false)
        => Draw(id,icon,label,secondary,size,MaterialTileSizing.Content,active,disabled);

    /// <summary>Content sizing treats requested height as a minimum; Fixed explicitly permits clipping.</summary>
    public static bool Draw(string id, MaterialIcon icon, string label, string? secondary, Vector2 size,
        MaterialTileSizing sizing, bool active = false, bool disabled = false)
    {
        var m = MaterialTheme.Metrics;
        var c = MaterialTheme.Current.Colors;
        var layout = MeasureContent(label, secondary, size, sizing);
        ImGui.PushID(id);
        var clicked = MaterialDraw.Hit("##tile",layout.Size,disabled,out var state);
        MaterialDraw.Shape(state,active ? c.PrimaryContainer : c.SurfaceContainer,c.OnSurface,m.OuterRadius,true);
        if (state.Visible)
        {
            var dl = ImGui.GetWindowDrawList();
            dl.PushClipRect(state.Min,state.Max,true);
            MaterialIcons.Draw(icon,state.Min+new Vector2((layout.Size.X-layout.Icon)*.5f,layout.Padding),
                layout.Icon,c.Primary,state.Alpha);
            var y = layout.Padding+layout.Icon+layout.Gap;
            DrawText(label,layout.Label,y,c.OnSurface);
            if (!string.IsNullOrEmpty(secondary))
                DrawText(secondary,layout.Secondary,y+layout.Label.Y+layout.Gap*.5f,c.OnSurfaceVariant);
            dl.PopClipRect();

            void DrawText(string text, Vector2 measured, float top, Vector4 color)
            {
                var x = Math.Max(layout.Padding,(layout.Size.X-measured.X)*.5f);
                dl.AddText(ImGui.GetFont(),ImGui.GetFontSize(),state.Min+new Vector2(x,top),
                    MaterialDraw.Color(color,state.Alpha),text,layout.WrapWidth);
            }
        }
        ImGui.PopID();
        return clicked;
    }

    /// <summary>Pixel size for the current font/scale and available width. Suitable for arranging equal-height grids.</summary>
    public static Vector2 Measure(string label, string? secondary = null, Vector2 size = default,
        MaterialTileSizing sizing = MaterialTileSizing.Content) => MeasureContent(label,secondary,size,sizing).Size;

    private readonly record struct Layout(Vector2 Size, Vector2 Label, Vector2 Secondary,
        float Icon, float Padding, float Gap, float WrapWidth);

    private static Layout MeasureContent(string label, string? secondary, Vector2 size, MaterialTileSizing sizing)
    {
        if (!Enum.IsDefined(sizing)) throw new ArgumentOutOfRangeException(nameof(sizing));
        var m = MaterialTheme.Metrics;
        var controls = MaterialControls.Metrics;
        var width = MaterialLayout.ResolveWidth(size.X,140*m.Scale,ImGui.GetContentRegionAvail().X,m.Scale);
        var padding = Math.Min(m.Padding,Math.Max(0,(width-1)*.25f));
        var wrap = Math.Max(1,width-2*padding);
        var labelSize = ImGui.CalcTextSize(label,false,wrap);
        var detailSize = string.IsNullOrEmpty(secondary) ? Vector2.Zero : ImGui.CalcTextSize(secondary,false,wrap);
        var icon = Math.Min(wrap,Math.Max(24*m.Scale,controls.IconSize));
        var contentHeight = 2*padding+icon+controls.Gap+labelSize.Y
            + (detailSize.Y > 0 ? controls.Gap*.5f+detailSize.Y : 0);
        var height = size.Y > 0 ? size.Y*m.Scale : m.TileHeight;
        if (sizing == MaterialTileSizing.Content) height = Math.Max(height,contentHeight);
        return new(new(width,height),labelSize,detailSize,icon,padding,controls.Gap,wrap);
    }
}
