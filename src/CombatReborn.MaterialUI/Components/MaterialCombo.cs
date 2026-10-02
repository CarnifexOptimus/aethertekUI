using Dalamud.Bindings.ImGui;

namespace CombatReborn.MaterialUI;

/// <summary>Popup width is logical units; zero fits the field with a 200-unit minimum. Field width is independent.</summary>
public readonly record struct MaterialComboLayout(float PopupWidth = 0, int VisibleRows = 8, bool ConstrainFieldWidth = true)
{
    public MaterialComboLayout() : this(0,8,true) { }
    public static MaterialComboLayout Default => new(0,8,true);
}

public static class MaterialCombo
{
    public static bool Draw<T>(string id, MaterialOptions<T> options, ref T value, float width = -1,
        bool searchable = true, bool disabled = false, string emptyLabel = "Select...")
        => Draw(id,options,ref value,MaterialComboLayout.Default,width,searchable,disabled,emptyLabel);

    public static bool Draw<T>(string id, MaterialOptions<T> options, ref T value, MaterialComboLayout layout, float width = -1,
        bool searchable = true, bool disabled = false, string emptyLabel = "Select...")
    {
        ArgumentNullException.ThrowIfNull(options);
        if (!float.IsFinite(layout.PopupWidth) || layout.PopupWidth < 0 || layout.VisibleRows < 1)
            throw new ArgumentOutOfRangeException(nameof(layout));
        using var controls = MaterialControls.Push();
        var scale = MaterialTheme.Metrics.Scale;
        var fieldWidth = MaterialLayout.ResolveWidth(width,200*scale,ImGui.GetContentRegionAvail().X,scale,layout.ConstrainFieldWidth);
        var popupWidth = Math.Min(Math.Max(1,ImGui.GetIO().DisplaySize.X-2*ImGui.GetStyle().WindowPadding.X),
            layout.PopupWidth == 0 ? Math.Max(fieldWidth,200*scale) : Math.Max(fieldWidth,layout.PopupWidth*scale));
        var popupHeight = ImGui.GetTextLineHeightWithSpacing()*layout.VisibleRows + 2*ImGui.GetStyle().WindowPadding.Y
            + (searchable ? MaterialControls.Metrics.Height + ImGui.GetStyle().ItemSpacing.Y : 0);
        ImGui.PushID(id);
        ImGui.BeginDisabled(disabled);
        ImGui.SetNextItemWidth(fieldWidth);
        ImGui.SetNextWindowSizeConstraints(new(popupWidth,0),new(popupWidth,popupHeight));
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
