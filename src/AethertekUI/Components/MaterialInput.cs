using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace AethertekUI;

/// <summary>Native editors under the current theme. Explicit IDs keep label/localization changes safe.</summary>
public static class MaterialInput
{
    public static bool Integer(string id, ref int value, int minimum = int.MinValue, int maximum = int.MaxValue,
        float width = -1, bool disabled = false, int step = 1)
    {
        if (minimum > maximum) throw new ArgumentException("Minimum exceeds maximum.");
        using var controls = MaterialControls.Push();
        var pixels = Begin(id, width, disabled);
        Span<byte> text = stackalloc byte[16];
        System.Buffers.Text.Utf8Formatter.TryFormat(value,text,out var written);
        var minimumEditor = ImGui.CalcTextSize(text[..written]).X + 2*ImGui.GetStyle().FramePadding.X + 4*MaterialTheme.Metrics.Scale;
        var stepperWidth = 2*(ImGui.GetFrameHeight()+ImGui.GetStyle().ItemInnerSpacing.X);
        // Preserve the editable value when a cell cannot also fit two step buttons.
        var effectiveStep = pixels >= minimumEditor+stepperWidth ? step : 0;
        var changed = ImGui.InputInt("##value", ref value, effectiveStep);
        if (changed) value = Math.Clamp(value, minimum, maximum);
        End();
        return changed;
    }

    public static bool Number(string id, ref float value, float minimum = float.MinValue, float maximum = float.MaxValue,
        float width = -1, bool disabled = false, string format = "%.2f")
    {
        if (!float.IsFinite(minimum) || !float.IsFinite(maximum) || minimum > maximum)
            throw new ArgumentException("Bounds must be finite and ordered.");
        using var controls = MaterialControls.Push();
        Begin(id, width, disabled);
        var changed = ImGui.InputFloat("##value", ref value, 0, 0, format);
        if (changed) value = float.IsFinite(value) ? Math.Clamp(value, minimum, maximum) : minimum;
        End();
        return changed;
    }

    public static bool Slider(string id, ref float value, float minimum, float maximum, float width = -1,
        bool disabled = false, string format = "%.2f")
    {
        if (!float.IsFinite(minimum) || !float.IsFinite(maximum) || minimum >= maximum)
            throw new ArgumentException("Slider bounds must be finite and increasing.");
        using var controls = MaterialControls.Push();
        Begin(id, width, disabled);
        var changed = ImGui.SliderFloat("##value", ref value, minimum, maximum, format, ImGuiSliderFlags.AlwaysClamp);
        End();
        return changed;
    }

    /// <summary>A thin linear track with the existing native slider's input, navigation and ID scope.</summary>
    public static bool SliderTrack(string id, ref float value, float minimum, float maximum, float width = -1, bool disabled = false)
    {
        if (!float.IsFinite(minimum) || !float.IsFinite(maximum) || minimum >= maximum)
            throw new ArgumentException("Slider bounds must be finite and increasing.");
        using var controls = MaterialControls.Push();
        Begin(id, width, disabled);
        try
        {
            var nativeId = ImGui.GetID("##value");
            using var style = new MaterialStyleScope();
            if (ImGui.GetCurrentContext().TempInputId != nativeId)
            {
                foreach (var color in new[] { ImGuiCol.FrameBg, ImGuiCol.FrameBgHovered, ImGuiCol.FrameBgActive, ImGuiCol.SliderGrab, ImGuiCol.SliderGrabActive })
                    style.Color(color, Vector4.Zero);
            }
            var changed = ImGui.SliderFloat("##value", ref value, minimum, maximum, "");
            if (ImGui.IsItemVisible() && ImGui.GetCurrentContext().TempInputId != nativeId)
            {
                var min = ImGui.GetItemRectMin(); var max = ImGui.GetItemRectMax();
                var scale = MaterialTheme.Metrics.Scale;
                var inset = Math.Min((max.X - min.X) * .5f, 2 * scale + ImGui.GetStyle().GrabMinSize * .5f);
                var left = min.X + inset; var right = max.X - inset;
                var fraction = float.IsFinite(value) ? Math.Clamp((value - minimum) / (maximum - minimum), 0, 1) : 0;
                var center = new Vector2(left + (right - left) * fraction, (min.Y + max.Y) * .5f);
                var theme = MaterialTheme.Current;
                var accent = theme.IsLcars ? MaterialLcars.ControlBlue : theme.Colors.Primary;
                var draw = ImGui.GetWindowDrawList();
                draw.PushClipRect(min, max, true);
                try
                {
                    draw.AddRectFilled(new(left, center.Y - 2 * scale), new(right, center.Y + 2 * scale), MaterialCanvas.Color(theme.Colors.SurfaceContainerHighest), 2 * scale);
                    draw.AddRectFilled(new(left, center.Y - 2 * scale), new(center.X, center.Y + 2 * scale), MaterialCanvas.Color(accent), 2 * scale);
                    draw.AddCircleFilled(center, 7 * scale, MaterialCanvas.Color(accent), 24);
                    if (ImGui.IsItemHovered() || ImGui.IsItemFocused())
                        draw.AddCircle(center, 10 * scale, MaterialCanvas.Color(accent with { W = .35f }), 24, scale);
                }
                finally { draw.PopClipRect(); }
            }
            return changed;
        }
        finally { End(); }
    }

    public static bool Multiline(string id, ref string value, Vector2 size, int maxLength = 500_000,
        bool disabled = false, bool error = false, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
    {
        if (maxLength < 1) throw new ArgumentOutOfRangeException(nameof(maxLength));
        using var controls = MaterialControls.Push();
        ImGui.PushID(id);
        ImGui.BeginDisabled(disabled);
        using var style = new MaterialStyleScope();
        style.Style(ImGuiStyleVar.FrameRounding, MaterialTheme.Metrics.Radius);
        if (error) { style.Color(ImGuiCol.Border, MaterialTheme.Current.Colors.Error); style.Style(ImGuiStyleVar.FrameBorderSize, MaterialTheme.Metrics.Border); }
        try { return MaterialShapedInput.Multiline("##value", ref value, maxLength, MaterialTheme.Metrics.Scaled(size), flags); }
        finally { ImGui.EndDisabled(); ImGui.PopID(); }
    }

    private static float Begin(string id, float width, bool disabled)
    {
        var pixels = MaterialLayout.ResolveWidth(width,180*MaterialTheme.Metrics.Scale,ImGui.GetContentRegionAvail().X,MaterialTheme.Metrics.Scale);
        ImGui.PushID(id);
        ImGui.BeginDisabled(disabled);
        ImGui.SetNextItemWidth(pixels);
        return pixels;
    }
    private static void End() { ImGui.EndDisabled(); ImGui.PopID(); }
}
