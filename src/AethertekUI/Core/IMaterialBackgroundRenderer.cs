using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace AethertekUI;

/// <summary>Consumer-owned glass and image drawing. Core does not acquire textures or own a graphics device.</summary>
public interface IMaterialBackgroundRenderer
{
    bool ImageMode { get; }
    bool ImageAvailable { get; }
    float Opacity { get; set; }
    /// <summary>Returns true when the requested backdrop is ready. Window backgrounds use the full native window bounds.</summary>
    bool DrawBackground(ImDrawListPtr drawList, Vector2 min, Vector2 max, float rounding,
        Vector4 tint, float frost, bool windowBackground = false);
    /// <summary>Draws the image through the owner's backend; returns false while its image is unavailable.</summary>
    bool DrawPlainImage(ImDrawListPtr drawList, Vector2 min, Vector2 max);
}

public static partial class MaterialSkin
{
    [ThreadStatic] private static BackgroundRendererState? backgroundRenderer;

    /// <summary>Supplies a renderer for this lexical UI scope. Keep it active through the owner's native Begin/End.</summary>
    /// <remarks>UI-thread/context-bound and LIFO; do not copy the scope. Null disables an outer renderer until disposal.</remarks>
    public static BackgroundRendererScope PushBackgroundRenderer(IMaterialBackgroundRenderer? renderer,
        bool blurEnabled, float frost, float opacity = 1)
        => new(renderer, blurEnabled, frost, opacity);

    /// <summary>Reveals a successfully rendered backdrop only within the same root window and native frame.</summary>
    public static Vector4 BackgroundFill(Vector4 color) => RootBackgroundAvailable ? Vector4.Zero : color;

    /// <summary>Opt retained native children and tables into the current root backdrop without changing foreground controls.</summary>
    public static MaterialStyleScope StructuralBackgroundStyle()
    {
        var scope = new MaterialStyleScope();
        if (!RootBackgroundAvailable) return scope;
        scope.Color(ImGuiCol.ChildBg, Vector4.Zero);
        scope.Color(ImGuiCol.TableHeaderBg, Vector4.Zero);
        scope.Color(ImGuiCol.TableRowBg, Vector4.Zero);
        scope.Color(ImGuiCol.TableRowBgAlt, Vector4.Zero);
        return scope;
    }

    internal static bool RootBackgroundAvailable => backgroundRenderer?.CoversCurrentRoot() == true;
    private static bool HasBackgroundRequest => backgroundRenderer is { } state &&
        (state.BlurEnabled || state.Renderer.ImageMode);

    private sealed class BackgroundRendererState(IMaterialBackgroundRenderer renderer, bool blurEnabled, float frost, float opacity)
    {
        internal readonly IMaterialBackgroundRenderer Renderer = renderer;
        internal readonly bool BlurEnabled = blurEnabled;
        internal readonly float Frost = frost;
        internal readonly float Opacity = opacity;
        private readonly HashSet<uint> roots = [];
        private nint context;
        private int frame = -1;

        private unsafe void CheckFrame()
        {
            var currentContext = (nint)ImGui.GetCurrentContext().Handle;
            var currentFrame = ImGui.GetFrameCount();
            if (context == currentContext && frame == currentFrame) return;
            roots.Clear();
            context = currentContext;
            frame = currentFrame;
        }

        internal void SetCurrentRoot(bool available)
        {
            CheckFrame();
            var window = ImGuiP.GetCurrentWindowRead();
            if (window.IsNull || window.RootWindow.IsNull) return;
            if (available) roots.Add(window.RootWindow.ID);
            else roots.Remove(window.RootWindow.ID);
        }

        internal bool CoversCurrentRoot()
        {
            CheckFrame();
            var window = ImGuiP.GetCurrentWindowRead();
            return !window.IsNull && !window.RootWindow.IsNull && roots.Contains(window.RootWindow.ID);
        }
    }

    public ref struct BackgroundRendererScope
    {
        private readonly BackgroundRendererState? previous;
        private readonly IMaterialBackgroundRenderer? renderer;
        private readonly float previousOpacity;
        private bool active;

        internal BackgroundRendererScope(IMaterialBackgroundRenderer? renderer, bool blurEnabled, float frost, float opacity)
        {
            if (!float.IsFinite(frost)) throw new ArgumentOutOfRangeException(nameof(frost));
            if (!float.IsFinite(opacity)) throw new ArgumentOutOfRangeException(nameof(opacity));
            previous = backgroundRenderer;
            this.renderer = renderer;
            previousOpacity = renderer?.Opacity ?? 1;
            var selectedOpacity = Math.Clamp(opacity, 0, 1);
            if (renderer == null) backgroundRenderer = null;
            else
            {
                renderer.Opacity = selectedOpacity;
                backgroundRenderer = new(renderer, blurEnabled, Math.Clamp(frost, 0, 1), selectedOpacity);
            }
            active = true;
        }

        public void Dispose()
        {
            if (!active) return;
            backgroundRenderer = previous;
            if (renderer != null) renderer.Opacity = previousOpacity;
            active = false;
        }
    }
}
