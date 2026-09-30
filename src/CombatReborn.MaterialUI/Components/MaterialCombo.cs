using Dalamud.Bindings.ImGui;

namespace CombatReborn.MaterialUI;

public static class MaterialCombo
{
    public static bool Draw<T>(string id, MaterialOptions<T> options, ref T value, float width = -1,
        bool searchable = true, bool disabled = false, string emptyLabel = "Select...")
    {
        ArgumentNullException.ThrowIfNull(options);
        ImGui.PushID(id);
        ImGui.BeginDisabled(disabled);
        ImGui.SetNextItemWidth(width < 0 ? ImGui.GetContentRegionAvail().X : width * MaterialTheme.Metrics.Scale);
        var changed = false;
        if (ImGui.BeginCombo("##combo", options.LabelFor(value, emptyLabel), ImGuiComboFlags.HeightLarge))
        {
            if (searchable)
            {
                var query = options.Query;
                if (ImGui.IsWindowAppearing()) ImGui.SetKeyboardFocusHere();
                if (MaterialTextField.SearchField("search", ref query, "Filter options...")) options.SetQuery(query);
            }
            if (options.Count == 0) ImGui.TextDisabled("No matching options");
            // Native clipping preserves scrolling/navigation without submitting thousands of rows.
            using (var clipper = new MaterialClipper(options.Count))
            {
            if (!searchable && ImGui.IsWindowAppearing())
                for (var i = 0; i < options.Count; i++)
                    if (EqualityComparer<T>.Default.Equals(value,options[i].Value)) { clipper.Include(i); break; }
            while (clipper.Step())
                for (var i = clipper.Start; i < clipper.End; i++)
                {
                    var option = options[i];
                    ImGui.PushID(option.Key);
                    var selected = EqualityComparer<T>.Default.Equals(value, option.Value);
                    ImGui.BeginDisabled(option.Disabled);
                    if (ImGui.Selectable(options.NativeLabel(i), selected))
                    {
                        changed = !selected;
                        value = option.Value;
                    }
                    if (selected && !searchable) ImGui.SetItemDefaultFocus();
                    ImGui.EndDisabled();
                    ImGui.PopID();
                }
            }
            ImGui.EndCombo();
        }
        ImGui.EndDisabled();
        ImGui.PopID();
        return changed;
    }
}
