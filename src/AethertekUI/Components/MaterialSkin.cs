using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace AethertekUI;

/// <summary>Selected materials painted inside the caller's current native window.</summary>
public static class MaterialSkin
{
    internal static bool HasMaterial => MaterialTheme.CurrentOrNull is { } theme
        && (theme.IsGlassmorphism || theme.IsSpatial || theme.IsCyberpunk);

    /// <summary>Wrap the adopting owner's Begin with this scope, then dispose before its content. Theme the whole Begin/End lifetime.</summary>
    public static MaterialStyleScope BackdropStyle(bool popup = false)
    {
        var scope = new MaterialStyleScope();
        if (HasMaterial) scope.Color(popup ? ImGuiCol.PopupBg : ImGuiCol.WindowBg, Vector4.Zero);
        return scope;
    }

    /// <summary>Call first after native Begin and BackdropStyle, before tables or draw-channel splits. This paints colour ribbons, not a game blur.</summary>
    public static unsafe void Backdrop()
    {
        if (MaterialTheme.CurrentOrNull is not { } theme || !HasMaterial) return;
        var window = ImGuiP.GetCurrentWindow();
        if ((window.Flags & ImGuiWindowFlags.NoBackground) != 0) return;
        var min = ImGui.GetWindowPos() + new Vector2(0, ImGuiP.TitleBarHeight(window));
        var max = min + ImGui.GetWindowSize();
        max.Y -= ImGuiP.TitleBarHeight(window);
        var dl = ImGui.GetWindowDrawList();
        if (dl.Splitter.Count > 1)
            throw new InvalidOperationException("Call MaterialSkin.Backdrop first after Begin, before tables or draw-channel splits.");
        var c = theme.Colors;
        var alpha = ImGui.GetStyle().Alpha * theme.SurfaceOpacity;
        var selectedRadius = window.DockIsActive ? 0 : window.ViewportOwned ? ImGui.GetStyle().WindowRounding : window.WindowRounding;
        var radius = Math.Clamp(selectedRadius,
            0, Math.Min(max.X - min.X, max.Y - min.Y) * .5f);
        var roundTop = (window.Flags & ImGuiWindowFlags.NoTitleBar) != 0;
        var corners = roundTop ? ImDrawFlags.RoundCornersAll : ImDrawFlags.RoundCornersBottom;
        // Native chrome is already emitted by Begin. Merge this bounded background before it,
        // retaining the owner's outer clip and all native border/title/scrollbar/resize ink.
        dl.ChannelsSplit(2);
        dl.ChannelsSetCurrent(1);
        dl.PushClipRect(window.OuterRectClipped.Min, window.OuterRectClipped.Max, false);
        try
        {
            if (theme.IsGlassmorphism) GlassBackdrop(dl, min, max, theme, alpha, radius, roundTop);
            else if (theme.IsSpatial)
            {
                var first = dl.VtxBuffer.Size;
                dl.AddRectFilled(min, max, Ink(Vector4.One, alpha), radius, corners);
                for (var i = first; i < dl.VtxBuffer.Size; i++)
                {
                    var vertex = dl.VtxBuffer[i];
                    var u = Math.Clamp((vertex.Pos.X - min.X) / Math.Max(1, max.X - min.X), 0, 1);
                    var v = Math.Clamp((vertex.Pos.Y - min.Y) / Math.Max(1, max.Y - min.Y), 0, 1);
                    var color = u >= v
                        ? c.SurfaceContainerLow * (1 - u) + c.Background * (u - v) + c.SurfaceContainer * v
                        : c.SurfaceContainerLow * (1 - v) + c.Background * (v - u) + c.SurfaceContainer * u;
                    vertex.Col = Ink(color, (vertex.Col >> 24) / 255f);
                    dl.VtxBuffer[i] = vertex;
                }
            }
            else dl.AddRectFilled(min, max, Ink(c.Background, alpha), radius, corners);
        }
        finally
        {
            dl.PopClipRect();
            dl.ChannelsSetCurrent(0);
            var first = dl.Splitter.Channels.Data[0];
            dl.Splitter.Channels.Data[0] = dl.Splitter.Channels.Data[1];
            dl.Splitter.Channels.Data[1] = first;
            dl.Splitter.Current = 1;
            dl.ChannelsMerge();
        }
    }

    private static void GlassBackdrop(ImDrawListPtr dl, Vector2 min, Vector2 max, MaterialTheme theme, float alpha,
        float radius, bool roundTop)
    {
        var size = max - min;
        if (size.X <= 0 || size.Y <= 0) return;
        var c = theme.Colors;
        var settings = theme.AppearanceSettings;
        var wave1 = Rgb(settings?.GlassWave1Rgb ?? MaterialColorSettings.DefaultGlassWave1Rgb);
        var wave2 = Rgb(settings?.GlassWave2Rgb ?? MaterialColorSettings.DefaultGlassWave2Rgb);
        var wave3 = Rgb(settings?.GlassWave3Rgb ?? MaterialColorSettings.DefaultGlassWave3Rgb);
        var time = theme.Motion.Enabled && settings?.GlassAnimation != false
            ? (float)ImGui.GetTime() * .65f * (settings?.GlassAnimationSpeed ?? 1) : 0;
        var scale = MaterialTheme.Metrics.Scale;
        // Bilinear corner colours form curved ribbons. The grid is bounded at 22,528 vertices.
        var columns = Math.Clamp((int)MathF.Ceiling(size.X / (12 * scale)), 48, 88);
        var rows = Math.Clamp((int)MathF.Ceiling(size.Y / (12 * scale)), 32, 64);
        Span<uint> previous = stackalloc uint[89];
        Span<uint> current = stackalloc uint[89];
        Span<float> previousX = stackalloc float[89];
        Span<float> currentX = stackalloc float[89];
        var topRows = radius > 0 && roundTop ? 4 : 0;
        var bottomRows = radius > 0 ? 4 : 0;
        var previousY = min.Y;
        for (var row = 0; row <= rows; row++)
        {
            var y = topRows > 0 && row <= topRows ? radius * row / topRows
                : bottomRows > 0 && row >= rows - bottomRows ? size.Y - radius + radius * (row - rows + bottomRows) / bottomRows
                : (topRows > 0 ? radius : 0) + (size.Y - (topRows > 0 ? radius : 0) - (bottomRows > 0 ? radius : 0))
                    * (row - topRows) / (rows - topRows - bottomRows);
            var v = y / size.Y;
            var cornerY = topRows > 0 && y < radius ? radius - y : y > size.Y - radius ? y - size.Y + radius : 0;
            var inset = radius - MathF.Sqrt(Math.Max(0, radius * radius - cornerY * cornerY));
            var primaryCenter = .22f + .38f * v + .11f * MathF.Sin(v * 5 - time * .9f);
            var secondaryCenter = .84f - .38f * v + .13f * MathF.Sin(v * 5.2f + time * .72f + .8f);
            for (var column = 0; column <= columns; column++)
            {
                currentX[column] = min.X + inset + (size.X - 2 * inset) * column / columns;
                var u = (currentX[column] - min.X) / size.X;
                var crossing = .80f - .68f * u + .14f * MathF.Sin(u * 5.3f - time * .83f + .7f);
                var d1 = u - primaryCenter;
                var d2 = u - secondaryCenter;
                var d3 = v - crossing;
                var band1 = MathF.Exp(-d1 * d1 * 45);
                var band2 = MathF.Exp(-d2 * d2 * 55);
                var band3 = MathF.Exp(-d3 * d3 * 65);
                var color = Vector4.Lerp(c.Background, wave2, .14f + .07f * u + .04f * v);
                color = Vector4.Lerp(color, wave1, band1 * .58f);
                color = Vector4.Lerp(color, wave2, band2 * .50f);
                color = Vector4.Lerp(color, wave3, band3 * .50f);
                var crest = MathF.Max(band1, MathF.Max(band2, band3));
                crest *= crest; crest *= crest; crest *= crest;
                color = Vector4.Lerp(color, c.OnSurface, crest * .10f);
                color.W = 1;
                current[column] = Ink(color, alpha);
            }
            if (row > 0)
            {
                for (var column = 0; column < columns; column++)
                {
                    var first = dl.VtxBuffer.Size;
                    dl.AddRectFilledMultiColor(new(previousX[column], previousY), new(currentX[column + 1], min.Y + y),
                        previous[column], previous[column + 1], current[column + 1], current[column]);
                    if (dl.VtxBuffer.Size - first != 4) continue; // Fully transparent backgrounds emit no cell.
                    var topRight = dl.VtxBuffer[first + 1]; topRight.Pos.X = previousX[column + 1]; dl.VtxBuffer[first + 1] = topRight;
                    var bottomLeft = dl.VtxBuffer[first + 3]; bottomLeft.Pos.X = currentX[column]; dl.VtxBuffer[first + 3] = bottomLeft;
                }
            }
            var swap = previous; previous = current; current = swap;
            var swapX = previousX; previousX = currentX; currentX = swapX;
            previousY = min.Y + y;
        }
    }

    /// <summary>Paint behind existing content. LCARS owners reserve the rail band outside their controls.</summary>
    public static void PanelFrame(Vector2 min, Vector2 max, int accent = 0, bool inset = false)
    {
        if (MaterialTheme.CurrentOrNull is not { } theme || max.X <= min.X || max.Y <= min.Y) return;
        if (theme.IsLcars)
        {
            if (!inset) MaterialLcars.PanelFrame(min, max, accent);
            else
            {
                var dl = ImGui.GetWindowDrawList();
                var alpha = ImGui.GetStyle().Alpha;
                var rounding = 9 * MaterialTheme.Metrics.Scale;
                dl.AddRectFilled(min, max, Ink(MaterialLcars.Background, alpha, theme.SurfaceOpacity), rounding);
                dl.AddRect(min, max, Ink(MaterialLcars.Accent(accent), alpha, .35f), rounding);
            }
            return;
        }
        if (!HasMaterial) return;
        var scale = MaterialTheme.Metrics.Scale;
        var radius = (theme.IsCyberpunk ? 3 : theme.IsSpatial && inset ? 6 : theme.IsSpatial ? 11 : 10) * scale;
        PaintPanel(min, max, inset, inset ? theme.Colors.SurfaceContainerLowest : theme.Colors.SurfaceContainerLow,
            radius, ImGui.GetStyle().Alpha);
    }

    internal static void PaintPanel(Vector2 min, Vector2 max, bool inset, Vector4 fill, float radius, float alpha,
        ImDrawListPtr? drawList = null)
    {
        var theme = MaterialTheme.Current;
        var c = theme.Colors;
        var dl = drawList ?? ImGui.GetWindowDrawList();
        var s = MaterialTheme.Metrics.Scale;
        radius = Math.Clamp(radius, 0, Math.Min(max.X - min.X, max.Y - min.Y) * .5f);
        if (theme.IsGlassmorphism)
        {
            dl.AddRectFilled(min + new Vector2(0, 3 * s), max + new Vector2(0, 3 * s), Ink(new(0, 0, 0, .15f), alpha), radius);
            GlassGlow(dl, min, max, radius, alpha, inset ? .55f : 1);
            dl.AddRectFilled(min, max, Ink(fill, alpha, (inset ? .68f : .42f) * theme.SurfaceOpacity), radius);
            dl.AddRect(min, max, Ink(Vector4.Lerp(c.Primary, c.OnSurface, .35f), alpha, inset ? .5f : .8f), radius);
            TopLight(dl, min, max, radius, c.OnSurface, alpha, .36f);
        }
        else if (theme.IsSpatial)
        {
            if (!inset) ContactShadow(dl, min, max, radius, alpha);
            dl.AddRectFilled(min, max, Ink(fill, alpha, theme.SurfaceOpacity), radius);
            dl.AddRect(min, max, Ink(c.Outline, alpha, inset ? .25f : .22f), radius);
            TopLight(dl, min, max, radius, inset ? c.Outline : Vector4.One, alpha, inset ? .3f : .95f, inset ? 2 : 1);
            if (inset && max.X - min.X > 2 * radius)
                dl.AddLine(new(min.X + radius, max.Y - s), max - new Vector2(radius, s), Ink(Vector4.One, alpha, .8f), s);
        }
        else if (theme.IsCyberpunk)
        {
            if (!inset)
                dl.AddRectFilled(min + new Vector2(0, 3 * s), max + new Vector2(0, 3 * s), Ink(new(0, 0, 0, .22f), alpha), radius);
            dl.AddRectFilled(min, max, Ink(fill, alpha, theme.SurfaceOpacity), radius);
            TopLight(dl, min, max, radius, c.OnSurface, alpha, .06f);
        }
    }

    public static void Heading(string title, int accent = 0)
    {
        if (MaterialTheme.Current.IsLcars) { MaterialLcars.PanelHeading(title, accent); return; }
        if (!MaterialTheme.Current.IsCyberpunk) { MaterialText.Text(title); ImGui.Separator(); return; }
        var origin = ImGui.GetCursorScreenPos();
        var width = Math.Max(1, ImGui.GetContentRegionAvail().X);
        var s = MaterialTheme.Metrics.Scale;
        var c = MaterialTheme.Current.Colors;
        var left = Math.Min((accent == 0 ? 18 : 52) * s, width * .25f);
        var right = Math.Min(24 * s, width * .15f);
        var tabWidth = Math.Min(width, Math.Max(256 * s, MaterialText.Measure(title).X + left + right));
        var textWidth = Math.Max(1, tabWidth - left - right);
        var text = MaterialText.Measure(title, wrapWidth: textWidth);
        var height = Math.Max(28 * s, text.Y + 8 * s);
        var cut = Math.Min(right, height * .7f);
        var lead = accent == 0 ? 0 : Math.Min(32 * s, left * .65f);
        var start = accent == 0 ? 0 : Math.Min(8 * s, left * .15f);
        var gap = accent == 0 ? 0 : Math.Min(6 * s, left * .15f);
        var tab = accent == 0 ? c.Primary : Vector4.Lerp(c.OnSurface, c.Primary, .10f);
        var dl = ImGui.GetWindowDrawList();
        var alpha = ImGui.GetStyle().Alpha;
        dl.PushClipRect(origin, origin + new Vector2(width, height), true);
        try
        {
            dl.AddRectFilled(origin, origin + new Vector2(width, height), Ink(c.SurfaceContainerHigh, alpha), 2 * s);
            if (accent != 0)
                dl.AddQuadFilled(origin, origin + new Vector2(lead + start, 0), origin + new Vector2(start, height), origin + new Vector2(0, height), Ink(c.Secondary, alpha));
            dl.AddQuadFilled(origin + new Vector2(lead + start + gap, 0), origin + new Vector2(tabWidth - cut, 0),
                origin + new Vector2(tabWidth, height), origin + new Vector2(start + gap, height), Ink(tab, alpha));
            if (accent == 0 && width - tabWidth >= 104 * s)
                for (var i = 0; i < 4; i++)
                {
                    var slash = origin + new Vector2(width - (62 - i * 14) * s, height * .34f);
                    dl.AddQuadFilled(slash + new Vector2(6 * s, 0), slash + new Vector2(14 * s, 0),
                        slash + new Vector2(8 * s, height * .42f), slash + new Vector2(0, height * .42f), Ink(c.Primary, alpha));
                }
            MaterialText.AddText(dl, origin + new Vector2(left, (height - text.Y) * .5f), Ink(ReadableInk(tab), alpha), title, textWidth);
        }
        finally { dl.PopClipRect(); }
        ImGui.Dummy(new(width, height));
    }

    /// <summary>Use with PaintButton in a background draw channel; semantic native Button colours remain intact.</summary>
    public static MaterialStyleScope NativeButtonStyle(Vector4? accent = null)
    {
        if (MaterialTheme.Current.IsLcars) return accent.HasValue ? MaterialLcars.NativeButtonStyle(accent.Value) : MaterialLcars.NativeButtonStyle();
        var scope = new MaterialStyleScope();
        if (!HasMaterial) return scope;
        scope.Style(ImGuiStyleVar.FrameRounding, (MaterialTheme.Current.IsCyberpunk ? 3 : 6) * MaterialTheme.Metrics.Scale);
        scope.Style(ImGuiStyleVar.FrameBorderSize, 0);
        scope.Color(ImGuiCol.Border, accent ?? MaterialTheme.Current.Colors.Outline);
        return scope;
    }

    /// <summary>Suppress native button ink while retaining its native item. Paint its face in the caller's background channel.</summary>
    public static MaterialStyleScope TransparentButtonFace()
    {
        var scope = new MaterialStyleScope();
        if (!HasMaterial && !MaterialTheme.Current.IsLcars) return scope;
        scope.Color(ImGuiCol.Button, Vector4.Zero);
        scope.Color(ImGuiCol.ButtonHovered, Vector4.Zero);
        scope.Color(ImGuiCol.ButtonActive, Vector4.Zero);
        scope.Color(ImGuiCol.Border, Vector4.Zero);
        return scope;
    }

    /// <summary>Paint the last native button behind its caption. Pass captured alpha if its disabled scope has already ended.</summary>
    public static void PaintButton(Vector2 min, Vector2 max, float? alpha = null)
    {
        var theme = MaterialTheme.Current;
        PaintButton(min, max, alpha, (theme.IsLcars ? 7 : theme.IsCyberpunk ? 3 : 6) * MaterialTheme.Metrics.Scale);
    }

    /// <summary>Paint the last native button with an already-scaled pixel radius, clamped to its bounds. Alpha remains the third argument.</summary>
    public static void PaintButton(Vector2 min, Vector2 max, float? alpha, float cornerRadiusPixels)
    {
        if (!float.IsFinite(cornerRadiusPixels) || cornerRadiusPixels < 0)
            throw new ArgumentOutOfRangeException(nameof(cornerRadiusPixels));
        var theme = MaterialTheme.Current;
        if (!HasMaterial && !theme.IsLcars) return;
        var style = ImGui.GetStyle();
        var hovered = ImGui.IsItemHovered();
        var active = ImGui.IsItemActive();
        var state = new MaterialState(min, max, 0, alpha ?? style.Alpha, hovered, active,
            ImGui.IsItemFocused() && ImGui.GetIO().NavVisible, ImGui.IsItemVisible());
        var fill = style.Colors[(int)(active ? ImGuiCol.ButtonActive : hovered ? ImGuiCol.ButtonHovered : ImGuiCol.Button)];
        var radius = Math.Min(cornerRadiusPixels, Math.Min(max.X - min.X, max.Y - min.Y) * .5f);
        PaintControl(state, fill, radius, ImDrawFlags.RoundCornersAll, false);
        if (theme.IsLcars)
            ImGui.GetWindowDrawList().AddRect(min, max, Ink(style.Colors[(int)ImGuiCol.Border], state.Alpha, hovered || active ? 1 : .7f), radius,
                ImDrawFlags.None, (active ? 2 : 1) * MaterialTheme.Metrics.Scale);
        if (state.Focused)
            ImGui.GetWindowDrawList().AddRect(min + new Vector2(MaterialTheme.Metrics.Scale), max - new Vector2(MaterialTheme.Metrics.Scale),
                Ink(theme.IsLcars ? MaterialLcars.Accent(2) : theme.Colors.Primary, state.Alpha), radius, ImDrawFlags.None, 2 * MaterialTheme.Metrics.Scale);
    }

    /// <summary>Enter immediately before a retained native field, after its width and height scopes.</summary>
    public static MaterialStyleScope NativeFieldStyle()
    {
        var scope = new MaterialStyleScope();
        if (!HasMaterial) return scope;
        var theme = MaterialTheme.Current;
        var min = ImGui.GetCursorScreenPos();
        var max = min + new Vector2(ImGui.CalcItemWidth(), ImGui.GetFrameHeight());
        PaintPanel(min, max, true, theme.IsSpatial ? theme.Colors.SurfaceContainer : theme.Colors.SurfaceContainerLowest,
            (theme.IsCyberpunk ? 3 : 6) * MaterialTheme.Metrics.Scale, ImGui.GetStyle().Alpha);
        scope.Color(ImGuiCol.FrameBg, Vector4.Zero);
        scope.Color(ImGuiCol.FrameBgHovered, MaterialColor.Alpha(theme.Colors.OnSurface, .06f));
        scope.Color(ImGuiCol.FrameBgActive, MaterialColor.Alpha(theme.Colors.OnSurface, .10f));
        scope.Style(ImGuiStyleVar.FrameRounding, (theme.IsCyberpunk ? 0 : 5) * MaterialTheme.Metrics.Scale);
        return scope;
    }

    internal static void PaintControl(in MaterialState state, Vector4 fill, float radius, ImDrawFlags corners, bool preserveFill)
    {
        if (!state.Visible) return;
        var theme = MaterialTheme.Current;
        var c = theme.Colors;
        var dl = ImGui.GetWindowDrawList();
        var s = MaterialTheme.Metrics.Scale;
        var min = state.Min; var max = state.Max; var alpha = state.Alpha;
        // Transparent text/outlined actions retain their transparent interiors.
        var filled = fill.W > 0;
        if (theme.IsGlassmorphism)
        {
            if (filled) GlassGlow(dl, min, max, radius, alpha, state.Pressed ? 1 : state.Hovered ? .9f : .7f, corners);
            dl.AddRectFilled(min, max, Ink(fill, alpha, preserveFill ? 1 : .68f), radius, corners);
            if (filled)
            {
                dl.AddRect(min, max, Ink(Vector4.Lerp(c.Primary, c.OnSurface, .35f), alpha, state.Pressed ? .95f : state.Hovered ? .85f : .65f), radius, corners);
                TopLight(dl, min, max, radius, c.OnSurface, alpha, .25f);
            }
        }
        else if (theme.IsSpatial)
        {
            if (filled && !state.Pressed) ContactShadow(dl, min, max - new Vector2(0, 2 * s), radius, alpha, corners);
            dl.AddRectFilled(min, max, Ink(fill, alpha), radius, corners);
            if (filled)
            {
                dl.AddRect(min, max, Ink(c.Outline, alpha, state.Hovered ? .6f : .3f), radius, corners);
                TopLight(dl, min, max, radius, state.Pressed ? c.Outline : Vector4.One, alpha, state.Pressed ? .18f : .85f);
            }
        }
        else if (theme.IsCyberpunk)
        {
            if (filled && !state.Pressed)
                dl.AddRectFilled(min + new Vector2(0, 2 * s), max + new Vector2(0, 2 * s), Ink(new(0, 0, 0, .24f), alpha), radius, corners);
            dl.AddRectFilled(min, max, Ink(fill, alpha), radius, corners);
            if (filled)
            {
                dl.AddRect(min, max, Ink(c.OnSurface, alpha, state.Pressed ? .32f : state.Hovered ? .26f : .16f), radius, corners);
                var cut = Math.Min(4 * s, Math.Min(max.X - min.X, max.Y - min.Y) * .25f);
                dl.PathClear();
                dl.PathLineTo(new(min.X + s, max.Y - cut));
                dl.PathLineTo(new(min.X + cut, max.Y - s));
                dl.PathLineTo(max - new Vector2(cut, s));
                dl.PathLineTo(max - new Vector2(s, cut));
                dl.PathStroke(Ink(c.Primary, alpha), ImDrawFlags.None, 2 * s);
            }
        }
        else dl.AddRectFilled(min, max, Ink(fill, alpha), radius, corners);
    }

    private static void GlassGlow(ImDrawListPtr dl, Vector2 min, Vector2 max, float radius, float alpha, float opacity,
        ImDrawFlags corners = ImDrawFlags.RoundCornersAll)
    {
        var s = MaterialTheme.Metrics.Scale;
        var controls = MaterialTheme.Current.Colors.Primary;
        for (var layer = 4; layer > 0; layer--)
        {
            var spread = new Vector2(layer * .75f * s);
            dl.AddRect(min - spread, max + spread, Ink(controls, alpha, opacity * (5 - layer) * .035f),
                radius + spread.X, corners, 2 * s);
        }
    }

    private static void ContactShadow(ImDrawListPtr dl, Vector2 min, Vector2 max, float radius, float alpha,
        ImDrawFlags corners = ImDrawFlags.RoundCornersAll)
    {
        var s = MaterialTheme.Metrics.Scale;
        for (var layer = 5; layer > 0; layer--)
        {
            var spread = new Vector2(layer * .7f * s, layer * .4f * s);
            dl.AddRectFilled(min - spread + new Vector2(0, 3 * s), max + spread + new Vector2(0, 3 * s),
                Ink(new(.09f, .16f, .23f, .035f), alpha), radius + layer * s, corners);
        }
    }

    private static void TopLight(ImDrawListPtr dl, Vector2 min, Vector2 max, float radius, Vector4 color, float alpha, float opacity, float thickness = 1)
    {
        var s = MaterialTheme.Metrics.Scale;
        if (max.X - min.X > 2 * radius)
            dl.AddLine(min + new Vector2(radius, s), new(max.X - radius, min.Y + s), Ink(color, alpha, opacity), thickness * s);
    }

    private static Vector4 Rgb(uint rgb) => new(MaterialColorSettings.Rgb(rgb), 1);
    internal static Vector4 ReadableInk(Vector4 surface) => MaterialColor.Contrast(surface, Vector4.One) >= MaterialColor.Contrast(surface, new(0, 0, 0, 1))
        ? Vector4.One : new(0, 0, 0, 1);
    private static uint Ink(Vector4 color, float alpha, float opacity = 1)
        => MaterialDraw.Color(new(color.X, color.Y, color.Z, color.W * opacity), alpha);
}
