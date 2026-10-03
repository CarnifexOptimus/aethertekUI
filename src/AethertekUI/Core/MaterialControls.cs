using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace AethertekUI;

/// <summary>Layout intent, independent of the theme's compact/standard density.</summary>
public enum MaterialControlContext { Regular, Toolbar, Dense }

/// <summary>Measured pixel dimensions. FontHeight is the actual current font height, already scaled.</summary>
public readonly record struct MaterialControlMetrics
{
    public float Height { get; init; }
    public Vector2 Padding { get; init; }
    public Vector2 NativePadding => new(Math.Min(Padding.X, Gap), Padding.Y);
    public float Gap { get; init; }
    public float IconSize { get; init; }
    public float Rounding { get; init; }
    public Vector2 ItemSpacing { get; init; }
    public Vector2 CellPadding { get; init; }

    public static MaterialControlMetrics Measure(MaterialMetrics theme, float fontHeight,
        MaterialControlContext context = MaterialControlContext.Regular)
    {
        if (!float.IsFinite(theme.Scale) || theme.Scale <= 0) throw new ArgumentOutOfRangeException(nameof(theme));
        if (!float.IsFinite(fontHeight) || fontHeight <= 0) throw new ArgumentOutOfRangeException(nameof(fontHeight));
        if (!Enum.IsDefined(context)) throw new ArgumentOutOfRangeException(nameof(context));
        var standard = theme.Density == MaterialDensity.Standard;
        var scale = theme.Scale;
        var minimum = context switch
        {
            MaterialControlContext.Dense => standard ? 24 : 20,
            MaterialControlContext.Toolbar => standard ? 30 : 24,
            _ => standard ? 36 : 28,
        };
        var vertical = context switch
        {
            MaterialControlContext.Dense => standard ? 2 : 1,
            MaterialControlContext.Toolbar => standard ? 4 : 2,
            _ => standard ? 7 : 4,
        };
        var horizontal = context switch
        {
            MaterialControlContext.Dense => standard ? 6 : 4,
            MaterialControlContext.Toolbar => standard ? 8 : 6,
            _ => standard ? 16 : 12,
        };
        var gap = (context == MaterialControlContext.Dense ? 3 : standard ? 8 : 6) * scale;
        var height = MathF.Ceiling(Math.Max(minimum * scale, fontHeight + 2 * vertical * scale));
        return new()
        {
            Height = height,
            Padding = new(horizontal * scale, (height - fontHeight) * .5f),
            Gap = gap,
            IconSize = Math.Min(height - 2 * scale, Math.Max(theme.IconSize, fontHeight * .9f)),
            Rounding = Math.Min(4 * scale, height * .2f),
            // Native grouped controls truncate cursor positions: avoid half-pixel row drift.
            ItemSpacing = new(MathF.Ceiling(context == MaterialControlContext.Dense ? 3 * scale : gap)),
            CellPadding = context == MaterialControlContext.Dense ? new(4 * scale, 2 * scale) : theme.CellPadding,
        };
    }
}

/// <summary>Composable, allocation-free sizing for native and Material controls. Does not own windows or fonts.</summary>
public static class MaterialControls
{
    [ThreadStatic] private static MaterialControlContext context;
    [ThreadStatic] private static MaterialControlMetrics? explicitMetrics;
    public static MaterialControlContext Context => context;
    public static MaterialControlMetrics Metrics
        => explicitMetrics ?? MaterialControlMetrics.Measure(MaterialTheme.Metrics, ImGui.GetTextLineHeight(), context);

    internal static MaterialControlMetrics? SetMetrics(MaterialControlMetrics? value)
    {
        var previous = explicitMetrics;
        explicitMetrics = value;
        return previous;
    }

    internal static MaterialControlContext SetContext(MaterialControlContext value)
    {
        var previous = context;
        context = value;
        return previous;
    }

    public static ControlScope Push(MaterialControlContext? value = null)
        => new(value ?? context, value.HasValue ? null : explicitMetrics);
    /// <summary>Explicit pixel metrics, already scaled. Nested parameterless scopes inherit them.</summary>
    public static ControlScope Push(MaterialControlMetrics metrics) => new(context, metrics);

    public ref struct ControlScope
    {
        private MaterialStyleScope style;
        private readonly MaterialControlContext previous;
        private readonly MaterialControlMetrics? previousMetrics;
        private bool active;
        internal ControlScope(MaterialControlContext value, MaterialControlMetrics? supplied)
        {
            // Validate and measure before touching either stack.
            var m = supplied ?? MaterialControlMetrics.Measure(MaterialTheme.Metrics, ImGui.GetTextLineHeight(), value);
            if (!float.IsFinite(m.Height) || m.Height <= 0) throw new ArgumentOutOfRangeException(nameof(supplied));
            ReadOnlySpan<float> values=[m.Padding.X,m.Padding.Y,m.Gap,m.IconSize,m.Rounding,m.ItemSpacing.X,m.ItemSpacing.Y,m.CellPadding.X,m.CellPadding.Y];
            foreach(var v in values) if(!float.IsFinite(v) || v<0) throw new ArgumentOutOfRangeException(nameof(supplied));
            previous = SetContext(value);
            previousMetrics = SetMetrics(supplied);
            style = new();
            style.Style(ImGuiStyleVar.FramePadding, m.NativePadding);
            style.Style(ImGuiStyleVar.FrameRounding, m.Rounding);
            style.Style(ImGuiStyleVar.ItemSpacing, m.ItemSpacing);
            style.Style(ImGuiStyleVar.CellPadding, m.CellPadding);
            active = true;
        }
        public void Dispose()
        {
            if (!active) return;
            style.Dispose();
            SetContext(previous);
            SetMetrics(previousMetrics);
            active = false;
        }
    }
}

public static class MaterialLayout
{
    /// <summary>Resolve a logical width into pixels. Negative fills; zero uses natural width. No double scaling of measured text.</summary>
    public static float ResolveWidth(float requested, float naturalPixels, float availablePixels, float scale,
        bool constrain = true)
    {
        if (!float.IsFinite(requested) || !float.IsFinite(naturalPixels) || !float.IsFinite(availablePixels)
            || !float.IsFinite(scale) || scale <= 0) throw new ArgumentOutOfRangeException(nameof(requested));
        var width = requested < 0 ? availablePixels : requested == 0 ? naturalPixels : requested * scale;
        return Math.Max(1, constrain ? Math.Min(width, Math.Max(1, availablePixels)) : width);
    }
}
