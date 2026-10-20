using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace AethertekUI;

public readonly record struct MaterialColorLabels(string Auto, string Picker, string Default, string AutoHelp,
    string ThemeColor, string PickerHelp, string Background, string Controls, string Text,
    string PreviewText, string PreviewControl, string AutoHint, string PickerHint);

public static partial class MaterialAppearanceSelector
{
    /// <summary>
    /// Auto/Picker editor shared by Settings and the native-titlebar Colour popup.
    /// Retain choices and accent as consumer-owned drafts. Returns true for mode/preset changes
    /// and completed RGB edits; dragging updates the drafts without requesting a save each frame.
    /// The Auto palette retains DrawPalette's existing native IDs under id.
    /// </summary>
    public static bool DrawColors(string id, ref MaterialColorSettings choices, ref Vector3 accent,
        MaterialAppearanceLabels palette, MaterialColorLabels labels, float minimumWidth = 300)
    {
        ArgumentNullException.ThrowIfNull(choices);
        var scale = MaterialTheme.Metrics.Scale;
        var labelWidth = new[] { labels.Background, labels.Controls, labels.Text }.Max(label => MaterialText.Measure(label).X);
        var editorWidth = MathF.Ceiling(Math.Max(100 * scale, ImGui.CalcTextSize("#FFFFFF").X + 2 * ImGui.GetStyle().FramePadding.X)
            + ImGui.GetFrameHeight() + ImGui.GetStyle().ItemInnerSpacing.X);
        var width = Math.Max(minimumWidth * scale, labelWidth + editorWidth + 2 * ImGui.GetStyle().ItemSpacing.X);
        var mode = choices.Mode;
        var committed = false;
        ImGui.PushID(id);
        try
        {
            MaterialText.Text(palette.Color);
            ImGui.Dummy(new(width, 0));
            using (MaterialControls.Push(MaterialControlContext.Toolbar))
            {
                committed |= MaterialSelection.Radio("auto", labels.Auto, MaterialColorMode.Auto, ref mode);
                ImGui.SameLine();
                using (var subdued = new MaterialStyleScope())
                {
                    subdued.Color(ImGuiCol.Text, MaterialTheme.Current.Colors.OnSurfaceVariant);
                    MaterialText.Text(labels.Default);
                }
                var nextWidth = MaterialText.Measure(labels.Picker).X + MaterialControls.Metrics.Height + 2 * MaterialControls.Metrics.Gap;
                if (ImGui.GetItemRectMax().X + ImGui.GetStyle().ItemSpacing.X + nextWidth
                    <= ImGui.GetWindowPos().X + ImGui.GetWindowContentRegionMax().X) ImGui.SameLine();
                committed |= MaterialSelection.Radio("picker", labels.Picker, MaterialColorMode.Picker, ref mode);
            }
            if (mode != choices.Mode) choices = choices with { Mode = mode };
            ImGui.Separator();
            if (mode == MaterialColorMode.Picker)
            {
                Wrapped(labels.PickerHelp);
                if (ImGui.BeginTable("picker-fields", 2, ImGuiTableFlags.SizingStretchProp, new(width, 0)))
                {
                    try
                    {
                        ImGui.TableSetupColumn("labels", ImGuiTableColumnFlags.WidthStretch, 1);
                        ImGui.TableSetupColumn("colours", ImGuiTableColumnFlags.WidthFixed, editorWidth);
                        var background = choices.BackgroundRgb;
                        var controls = choices.ControlsRgb;
                        var text = choices.TextRgb;
                        committed |= Field("background", labels.Background, ref background);
                        committed |= Field("controls", labels.Controls, ref controls);
                        committed |= Field("text", labels.Text, ref text);
                        if (background != choices.BackgroundRgb || controls != choices.ControlsRgb || text != choices.TextRgb)
                            choices = choices with { BackgroundRgb = background, ControlsRgb = controls, TextRgb = text };
                    }
                    finally { ImGui.EndTable(); }
                }
                Preview(choices, labels, width);
                Wrapped(labels.PickerHint);
            }
            else
            {
                Wrapped(labels.AutoHelp);
                MaterialText.Text(labels.ThemeColor);
            }
        }
        finally { ImGui.PopID(); }
        if (mode == MaterialColorMode.Auto)
        {
            committed |= DrawPalette(id, ref accent, palette with { Color = string.Empty });
            Wrapped(labels.AutoHint);
        }
        return committed;

        bool Field(string fieldId, string label, ref uint rgb)
        {
            ImGui.TableNextRow(); ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding(); MaterialText.Text(label);
            ImGui.TableNextColumn();
            var color = MaterialColorSettings.Rgb(rgb);
            ImGui.SetNextItemWidth(editorWidth);
            var style = ImGui.GetStyle();
            var oldPosition = style.ColorButtonPosition;
            bool edited;
            try
            {
                style.ColorButtonPosition = ImGuiDir.Left;
                edited = ImGui.ColorEdit3("##" + fieldId, ref color,
                    ImGuiColorEditFlags.NoAlpha | ImGuiColorEditFlags.DisplayHex | ImGuiColorEditFlags.NoOptions);
            }
            finally { style.ColorButtonPosition = oldPosition; }
            if (edited) rgb = MaterialColorSettings.ToRgb(color);
            return ImGui.IsItemDeactivatedAfterEdit();
        }

        void Wrapped(string text)
        {
            ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + width);
            try { MaterialText.Text(text); }
            finally { ImGui.PopTextWrapPos(); }
        }
    }

    private static void Preview(MaterialColorSettings choices, MaterialColorLabels labels, float width)
    {
        var scale = MaterialTheme.Metrics.Scale;
        var textSize = MaterialText.Measure(labels.PreviewText);
        var controlSize = MaterialText.Measure(labels.PreviewControl);
        var padding = 10 * scale;
        var height = Math.Max(textSize.Y, controlSize.Y) + 2 * padding;
        var origin = ImGui.GetCursorScreenPos();
        ImGui.Dummy(new(width, height));
        var list = ImGui.GetWindowDrawList();
        var text = new Vector4(MaterialColorSettings.Rgb(choices.TextRgb), 1);
        list.AddRectFilled(origin, origin + new Vector2(width, height),
            MaterialCanvas.Color(new(MaterialColorSettings.Rgb(choices.BackgroundRgb), 1)), 4 * scale);
        list.AddRect(origin, origin + new Vector2(width, height), MaterialCanvas.Color(MaterialTheme.Current.Colors.OutlineVariant), 4 * scale);
        MaterialText.AddText(list, origin + new Vector2(padding, (height - textSize.Y) / 2), MaterialCanvas.Color(text), labels.PreviewText);
        var buttonMin = origin + new Vector2(width - controlSize.X - 3 * padding, padding / 2);
        list.AddRectFilled(buttonMin, origin + new Vector2(width - padding / 2, height - padding / 2),
            MaterialCanvas.Color(new(MaterialColorSettings.Rgb(choices.ControlsRgb), 1)), 4 * scale);
        MaterialText.AddText(list, buttonMin + new Vector2(padding, (height - padding - controlSize.Y) / 2),
            MaterialCanvas.Color(text), labels.PreviewControl);
    }
}
