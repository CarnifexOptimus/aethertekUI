using Dalamud.Bindings.ImGui;
using System.Numerics;

namespace AethertekUI;

/// <summary>Popup width is logical units; zero fits the field with a 200-unit minimum. Field width is independent.</summary>
public readonly record struct MaterialComboLayout(float PopupWidth = 0, int VisibleRows = 8, bool ConstrainFieldWidth = true)
{
    public MaterialComboLayout() : this(0,8,true) { }
    public static MaterialComboLayout Default => new(0,8,true);
    public MaterialIcon? LeadingIcon { get; init; }
}

public static class MaterialCombo
{
    public static bool Draw<T>(string id, MaterialOptions<T> options, ref T value, float width = -1,
        bool searchable = true, bool disabled = false, string emptyLabel = "Select...")
        => Draw(id,options,ref value,MaterialComboLayout.Default,width,searchable,disabled,emptyLabel);

    public static bool Draw<T>(string id, MaterialOptions<T> options, ref T value, MaterialComboLayout layout, float width = -1,
        bool searchable = true, bool disabled = false, string emptyLabel = "Select...")
        => DrawCore(id,options,ref value,layout,width,searchable,disabled,emptyLabel,null,"Filter options...","No matching options");

    public static bool Draw<T>(string id, MaterialOptions<T> options, ref T value, MaterialControlAppearance appearance,
        float width = -1, bool searchable = true, bool disabled = false, string emptyLabel = "Select...",
        string searchHint = "Filter options...", string noOptionsLabel = "No matching options")
        => DrawCore(id,options,ref value,MaterialComboLayout.Default,width,searchable,disabled,emptyLabel,appearance,searchHint,noOptionsLabel);

    public static bool Draw<T>(string id, MaterialOptions<T> options, ref T value, MaterialComboLayout layout,
        MaterialControlAppearance appearance, float width = -1, bool searchable = true, bool disabled = false,
        string emptyLabel = "Select...", string searchHint = "Filter options...", string noOptionsLabel = "No matching options")
        => DrawCore(id,options,ref value,layout,width,searchable,disabled,emptyLabel,appearance,searchHint,noOptionsLabel);

    private static bool DrawCore<T>(string id, MaterialOptions<T> options, ref T value, MaterialComboLayout layout, float width,
        bool searchable, bool disabled, string emptyLabel, MaterialControlAppearance? appearance, string searchHint, string noOptionsLabel)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (!float.IsFinite(layout.PopupWidth) || layout.PopupWidth < 0 || layout.VisibleRows < 1)
            throw new ArgumentOutOfRangeException(nameof(layout));
        if(appearance is { } validated) validated.Radius(MaterialTheme.Metrics.Scale);
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
        var origin=ImGui.GetCursorScreenPos();
        var dl=ImGui.GetWindowDrawList();
        var customPreview=appearance.HasValue || layout.LeadingIcon.HasValue;
        var previewForeground=appearance?.Foreground ?? ImGui.GetStyle().Colors[(int)ImGuiCol.Text];
        var style=new MaterialStyleScope();
        if(appearance is { } a)
        {
            style.Color(ImGuiCol.FrameBg,a.Fill);
            style.Color(ImGuiCol.FrameBgHovered,MaterialColor.Layer(a.Fill,a.Foreground,.08f));
            style.Color(ImGuiCol.FrameBgActive,MaterialColor.Layer(a.Fill,a.Foreground,.14f));
            style.Color(ImGuiCol.Text,Vector4.Zero);
            style.Color(ImGuiCol.Border,a.Border);
            style.Style(ImGuiStyleVar.FrameRounding,a.Radius(scale));
            style.Style(ImGuiStyleVar.FrameBorderSize,MaterialTheme.Metrics.Border);
        }
        else if (customPreview) style.Color(ImGuiCol.Text,Vector4.Zero);
        var opened=ImGui.BeginCombo("##combo", options.LabelFor(value, emptyLabel), ImGuiComboFlags.HeightLarge |
            (customPreview ? ImGuiComboFlags.NoArrowButton : ImGuiComboFlags.None));
        style.Dispose();
        if(customPreview)
        {
            var height=MaterialControls.Metrics.Height;
            var gap=MaterialControls.Metrics.Gap;
            var textX=gap;
            if(layout.LeadingIcon is { } icon)
            {
                var iconSize=Math.Min(MaterialControls.Metrics.IconSize,height);
                // BeginCombo can switch to the popup; artwork belongs to the retained field.
                dl.PushClipRect(origin,origin+new Vector2(fieldWidth,height),true);
                MaterialIcons.Draw(icon,dl,origin+new Vector2(gap,(height-iconSize)*.5f),iconSize,previewForeground);
                dl.PopClipRect();
                textX+=iconSize+gap;
            }
            dl.PushClipRect(origin+new Vector2(textX,0),origin+new Vector2(Math.Max(textX,fieldWidth-height),height),true);
            dl.AddText(origin+new Vector2(textX,(height-ImGui.GetTextLineHeight())*.5f),MaterialDraw.Color(previewForeground),options.LabelFor(value,emptyLabel));
            dl.PopClipRect();
            // Draw into the original field's list even while the popup is current.
            var center=origin+new Vector2(fieldWidth-height*.5f,height*.5f);
            var ink=MaterialDraw.Color(previewForeground);
            dl.AddLine(center+new Vector2(-4*scale,-2*scale),center+new Vector2(0,2*scale),ink,scale);
            dl.AddLine(center+new Vector2(0,2*scale),center+new Vector2(4*scale,-2*scale),ink,scale);
        }
        if (opened)
        {
            if (searchable)
            {
                var query = options.Query;
                if (ImGui.IsWindowAppearing()) ImGui.SetKeyboardFocusHere();
                if (MaterialTextField.SearchField("search", ref query, searchHint)) options.SetQuery(query);
            }
            if (options.Count == 0) ImGui.TextDisabled(noOptionsLabel);
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
