using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace CombatReborn.MaterialUI;

/// <summary>Native editors under the current theme. Explicit IDs keep label/localization changes safe.</summary>
public static class MaterialInput
{
    public static bool Integer(string id, ref int value, int minimum = int.MinValue, int maximum = int.MaxValue,
        float width = -1, bool disabled = false, int step = 1)
    {
        if (minimum > maximum) throw new ArgumentException("Minimum exceeds maximum.");
        Begin(id, width, disabled);
        var changed = ImGui.InputInt("##value", ref value, step);
        if (changed) value = Math.Clamp(value, minimum, maximum);
        End();
        return changed;
    }

    public static bool Number(string id, ref float value, float minimum = float.MinValue, float maximum = float.MaxValue,
        float width = -1, bool disabled = false, string format = "%.2f")
    {
        if (!float.IsFinite(minimum) || !float.IsFinite(maximum) || minimum > maximum)
            throw new ArgumentException("Bounds must be finite and ordered.");
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
        Begin(id, width, disabled);
        var changed = ImGui.SliderFloat("##value", ref value, minimum, maximum, format, ImGuiSliderFlags.AlwaysClamp);
        End();
        return changed;
    }

    public static bool Multiline(string id, ref string value, Vector2 size, int maxLength = 500_000,
        bool disabled = false, bool error = false, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
    {
        if (maxLength < 1) throw new ArgumentOutOfRangeException(nameof(maxLength));
        ImGui.PushID(id);
        ImGui.BeginDisabled(disabled);
        using var style = new MaterialStyleScope();
        style.Style(ImGuiStyleVar.FrameRounding, MaterialTheme.Metrics.Radius);
        if (error) { style.Color(ImGuiCol.Border, MaterialTheme.Current.Colors.Error); style.Style(ImGuiStyleVar.FrameBorderSize, MaterialTheme.Metrics.Border); }
        var changed = ImGui.InputTextMultiline("##value", ref value, maxLength, MaterialTheme.Metrics.Scaled(size), flags);
        ImGui.EndDisabled();
        ImGui.PopID();
        return changed;
    }

    private static void Begin(string id, float width, bool disabled)
    {
        ImGui.PushID(id);
        ImGui.BeginDisabled(disabled);
        ImGui.SetNextItemWidth(width < 0 ? ImGui.GetContentRegionAvail().X : width * MaterialTheme.Metrics.Scale);
    }
    private static void End() { ImGui.EndDisabled(); ImGui.PopID(); }
}
