using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace AethertekUI;

/// <summary>Consumer-owned rounded decoration painting for an undocked platform window.</summary>
public sealed class MaterialWindowDecorations
{
    private ImGuiWindowPtr window;
    private ImGuiContextPtr context;
    private float radius;
    private float? ownerFontSize;

    /// <summary>Call from the final native sizing callback, after viewport selection.</summary>
    public void Prepare(ImGuiWindowPtr native)
    {
        if (!window.IsNull) throw new InvalidOperationException("Finish native decorations before preparing again.");
        const ImGuiWindowFlags excluded = ImGuiWindowFlags.ChildWindow | ImGuiWindowFlags.Popup |
            ImGuiWindowFlags.Tooltip | ImGuiWindowFlags.DockNodeHost;
        if (!native.ViewportOwned || native.DockIsActive || (native.Flags & excluded) != 0) return;
        window = native;
        context = ImGui.GetCurrentContext();
        radius = MaterialTheme.Metrics.OuterRadius;
        ownerFontSize = null;
    }

    /// <summary>Call first in Draw and again after End. Painting waits until the owner's final End.</summary>
    public unsafe void Paint()
    {
        if (window.IsNull) return;
        if (ImGui.GetCurrentContext() != context)
            throw new InvalidOperationException("Finish decorations in the context that prepared them.");
        // Tables merge index buffers in command order. Inserting chrome commands
        // before those merges redirects consumer geometry into unrelated clips.
        for (var index = 0; index < context.CurrentWindowStack.Size; index++)
            if (context.CurrentWindowStack.Data[index].Window == window.Handle)
            {
                if (ImGuiP.GetCurrentWindow() == window) ownerFontSize = ImGui.GetFontSize();
                return;
            }
        var native = window;
        window = default;
        var fontSize = ownerFontSize ?? ImGui.GetFontSize();
        ownerFontSize = null;
        var flags = native.Flags;
        var list = native.DrawList;
        var min = native.Pos;
        var max = min + native.Size;
        var titleHeight = ImGuiP.TitleBarHeight(native);
        var originalVertices = list.VtxBuffer.Size;
        // Match native geometry and its atlas white UV; retain emitted colors,
        // including host fades, docking opacity and navigation-resize feedback.
        // Only matched decoration vertices change. Global styles stay intact.
        uint Erase(Action drawSquare)
        {
            var first = list.VtxBuffer.Size;
            drawSquare();
            var count = list.VtxBuffer.Size - first;
            var color = 0u;
            if (count == 0) return color;
            for (var candidate = 0; candidate + count <= originalVertices; candidate++)
            {
                var matches = true;
                for (var i = 0; i < count; i++)
                {
                    var actual = list.VtxBuffer[candidate + i];
                    var expected = list.VtxBuffer[first + i];
                    if (actual.Pos != expected.Pos || actual.Uv != expected.Uv) { matches = false; break; }
                }
                if (!matches) continue;
                var emitted = 0u;
                for (var i = 0; i < count; i++)
                {
                    var vertexColor = list.VtxBuffer[candidate + i].Col;
                    if ((vertexColor >> 24) > (emitted >> 24)) emitted = vertexColor;
                }
                // Two native decorations may have identical geometry. Do not match
                // the already-cleared first mesh again and leave the next one square.
                if ((emitted >> 24) == 0) continue;
                color = emitted;
                for (var i = 0; i < count; i++)
                {
                    var vertex = list.VtxBuffer[candidate + i];
                    vertex.Col &= 0x00ffffff;
                    list.VtxBuffer[candidate + i] = vertex;
                }
                break;
            }
            // Reference meshes are invisible; original offsets remain valid.
            for (var i = first; i < first + count; i++)
            {
                var vertex = list.VtxBuffer[i]; vertex.Col &= 0x00ffffff; list.VtxBuffer[i] = vertex;
            }
            return color;
        }
        var background = native.Collapsed || flags.HasFlag(ImGuiWindowFlags.NoBackground) ? 0 :
            Erase(() => list.AddRectFilled(min + new Vector2(0, titleHeight), max, uint.MaxValue));
        var title = flags.HasFlag(ImGuiWindowFlags.NoTitleBar) ? 0 :
            Erase(() => list.AddRectFilled(min, min + new Vector2(native.Size.X, titleHeight), uint.MaxValue));
        var border = native.WindowBorderSize <= 0 || (!native.Collapsed && flags.HasFlag(ImGuiWindowFlags.NoBackground)) ? 0 :
            Erase(() => list.AddRect(min, max, uint.MaxValue, 0, ImDrawFlags.None, native.WindowBorderSize));
        var shadow = !native.Collapsed || native.WindowBorderSize <= 0 ? 0 :
            Erase(() => list.AddRect(min + Vector2.One, max + Vector2.One, uint.MaxValue, 0, ImDrawFlags.None, native.WindowBorderSize));
        var menuMin = min + new Vector2(native.WindowBorderSize, titleHeight);
        var menuMax = Vector2.Min(max, min + new Vector2(native.Size.X - native.WindowBorderSize, titleHeight + ImGuiP.MenuBarHeight(native)));
        var menu = !native.Collapsed && flags.HasFlag(ImGuiWindowFlags.MenuBar) ?
            Erase(() => list.AddRectFilled(menuMin, menuMax, uint.MaxValue)) : 0;
        var horizontal = native.ScrollbarX && !native.Collapsed ? ImGuiP.GetWindowScrollbarRect(native, ImGuiAxis.X) : default;
        var vertical = native.ScrollbarY && !native.Collapsed ? ImGuiP.GetWindowScrollbarRect(native, ImGuiAxis.Y) : default;
        var scrollX = native.ScrollbarX && !native.Collapsed ? Erase(() => list.AddRectFilled(horizontal.Min, horizontal.Max, uint.MaxValue)) : 0;
        var scrollY = native.ScrollbarY && !native.Collapsed ? Erase(() => list.AddRectFilled(vertical.Min, vertical.Max, uint.MaxValue)) : 0;
        var canResize = !native.Collapsed && (flags & (ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize)) == 0 &&
            native.AutoFitFramesX <= 0 && native.AutoFitFramesY <= 0;
        var grip0 = canResize ? Erase(() => Grip(0, 0, uint.MaxValue)) : 0;
        var grip1 = canResize && ImGui.GetIO().ConfigWindowsResizeFromEdges ? Erase(() => Grip(1, 0, uint.MaxValue)) : 0;
        var held = !native.Collapsed && native.ResizeBorderHeld >= 0 ? Erase(() => HeldBorder(0, uint.MaxValue)) : 0;

        // Keep chrome in the owner's list; a shared viewport background changes layering.
        // Native dimmed-background painting uses this complete-command front insertion.
        var viewport = native.Viewport.ImGuiViewport;
        list.PushClipRect(viewport.Pos - Vector2.One, viewport.Pos + viewport.Size + Vector2.One, false);
        list.AddDrawCmd();
        var firstCommand = list.CmdBuffer.Size - 1;
        native.WindowRounding = radius;
        if (!native.Collapsed)
        {
            list.AddRectFilled(min + new Vector2(0, titleHeight), max, background, radius,
                flags.HasFlag(ImGuiWindowFlags.NoTitleBar) ? ImDrawFlags.RoundCornersAll : ImDrawFlags.RoundCornersBottom);
            list.AddRectFilled(menuMin, menuMax, menu, flags.HasFlag(ImGuiWindowFlags.NoTitleBar) ? radius : 0, ImDrawFlags.RoundCornersTop);
            if (native.ScrollbarX)
                list.AddRectFilled(horizontal.Min, horizontal.Max, scrollX, radius, ImDrawFlags.RoundCornersBottomLeft |
                    (native.ScrollbarY ? ImDrawFlags.None : ImDrawFlags.RoundCornersBottomRight));
            if (native.ScrollbarY)
            {
                var corners = ImDrawFlags.RoundCornersNone;
                if ((flags & (ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.MenuBar)) == ImGuiWindowFlags.NoTitleBar)
                    corners |= ImDrawFlags.RoundCornersTopRight;
                if (!native.ScrollbarX) corners |= ImDrawFlags.RoundCornersBottomRight;
                list.AddRectFilled(vertical.Min, vertical.Max, scrollY, radius, corners);
            }
            Grip(0, radius, grip0); Grip(1, radius, grip1);
            if (held != 0) HeldBorder(radius, held);
        }
        list.AddRectFilled(min, min + new Vector2(native.Size.X, titleHeight), title, radius,
            native.Collapsed ? ImDrawFlags.RoundCornersAll : ImDrawFlags.RoundCornersTop);
        if (shadow != 0) list.AddRect(min + Vector2.One, max + Vector2.One, shadow, radius, ImDrawFlags.None, native.WindowBorderSize);
        if (border != 0) list.AddRect(min, max, border, radius, ImDrawFlags.None, native.WindowBorderSize);
        var lastCommand = list.CmdBuffer.Size - 1;
        // Index/vertex offsets, textures and clips are preserved with each moved command.
        for (var n = firstCommand; n <= lastCommand; n++)
        {
            var command = list.CmdBuffer[lastCommand];
            for (var i = lastCommand; i > 0; i--) list.CmdBuffer[i] = list.CmdBuffer[i - 1];
            list.CmdBuffer[0] = command;
        }
        list.PopClipRect();
        list.AddDrawCmd();

        void Grip(int n, float rounding, uint color)
        {
            if (color == 0) return;
            var gripSize = MathF.Floor(MathF.Max(fontSize * 1.1f, rounding + 1 + fontSize * .2f));
            var direction = new Vector2(n == 0 ? -1 : 1, -1);
            var corner = min + new Vector2(n == 0 ? native.Size.X : 0, native.Size.Y);
            list.PathLineTo(corner + direction * (n == 0 ? new Vector2(gripSize, native.WindowBorderSize) : new Vector2(native.WindowBorderSize, gripSize)));
            list.PathLineTo(corner + direction * (n == 0 ? new Vector2(native.WindowBorderSize, gripSize) : new Vector2(gripSize, native.WindowBorderSize)));
            list.PathArcToFast(corner + direction * (rounding + native.WindowBorderSize), rounding, n == 0 ? 0 : 3, n == 0 ? 3 : 6);
            list.PathFillConvex(color);
        }
        void HeldBorder(float rounding, uint color)
        {
            var edge = native.ResizeBorderHeld;
            var innerMin = min + new Vector2(rounding + .5f);
            var innerMax = max - new Vector2(rounding + .5f);
            var first = edge switch { 0 => new Vector2(innerMin.X, innerMax.Y), 1 => new Vector2(innerMax.X, innerMin.Y), 2 => innerMin, _ => innerMax };
            var second = edge switch { 0 => innerMin, 1 => innerMax, 2 => new Vector2(innerMax.X, innerMin.Y), _ => new Vector2(innerMin.X, innerMax.Y) };
            var angle = edge switch { 0 => MathF.PI, 1 => 0, 2 => MathF.PI * 1.5f, _ => MathF.PI * .5f };
            list.PathArcTo(first, rounding, angle - MathF.PI * .25f, angle);
            list.PathArcTo(second, rounding, angle, angle + MathF.PI * .25f);
            list.PathStroke(color, ImDrawFlags.None, MathF.Max(2, native.WindowBorderSize));
        }
    }
}
