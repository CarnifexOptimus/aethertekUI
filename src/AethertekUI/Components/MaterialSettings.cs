using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace AethertekUI;

/// <summary>Compact semantic settings groups. The caller owns controls, IDs, localization and persistence.</summary>
public static class MaterialSettings
{
    /// <summary>Lay out a wrapped label beside one retained native editor. All widths are already-scaled pixels.</summary>
    /// <remarks>The callback must submit its editor with the supplied empty label, without another ID scope.
    /// It then retains GetID(nativeLabel), including native scalar step buttons. Values and handlers remain caller-owned.
    /// A narrow row stacks the label above the editor; the editor's last-item data remains available afterward.</remarks>
    public static bool Field(string nativeLabel, string display, float editorWidthPixels, Func<string, bool> drawEditor,
        float widthPixels = -1)
    {
        ArgumentNullException.ThrowIfNull(nativeLabel);
        ArgumentNullException.ThrowIfNull(display);
        ArgumentNullException.ThrowIfNull(drawEditor);
        if (!float.IsFinite(editorWidthPixels) || editorWidthPixels <= 0) throw new ArgumentOutOfRangeException(nameof(editorWidthPixels));
        if (!float.IsFinite(widthPixels)) throw new ArgumentOutOfRangeException(nameof(widthPixels));
        var id = ImGui.GetID(nativeLabel);
        var origin = ImGui.GetCursorScreenPos();
        var width = Math.Max(1, widthPixels <= 0 ? ImGui.GetContentRegionAvail().X : widthPixels);
        var controls = MaterialControls.Metrics;
        var gap = controls.Gap;
        var editorHeight = ImGui.GetFrameHeight();
        var editorWidth = Math.Min(editorWidthPixels, width);
        var stacked = width < editorWidth + gap + Math.Min(MaterialText.Measure(display).X, 80 * MaterialTheme.Metrics.Scale);
        var labelWidth = stacked ? width : Math.Max(1, width - editorWidth - gap);
        var text = MaterialText.Measure(display, wrapWidth: labelWidth);
        var rowHeight = stacked ? text.Y + gap + editorHeight : Math.Max(text.Y, editorHeight);
        var labelPosition = origin + new Vector2(0, stacked ? 0 : (rowHeight - text.Y) * .5f);
        ImGui.Dummy(new Vector2(width, rowHeight));
        var next = ImGui.GetCursorScreenPos();
        if (ImGui.IsItemVisible())
        {
            var list = ImGui.GetWindowDrawList();
            list.PushClipRect(origin, origin + new Vector2(labelWidth, rowHeight), true);
            try { MaterialText.AddText(list, labelPosition, ImGui.GetColorU32(ImGuiCol.Text), display, labelWidth); }
            finally { list.PopClipRect(); }
        }
        ImGui.SetCursorScreenPos(origin + (stacked ? new Vector2(0, text.Y + gap) : new Vector2(width - editorWidth, (rowHeight - editorHeight) * .5f)));
        ImGuiP.PushOverrideID(id);
        try
        {
            ImGui.SetNextItemWidth(editorWidth);
            return drawEditor(string.Empty);
        }
        finally
        {
            ImGui.PopID();
            next.Y = Math.Max(next.Y, ImGui.GetItemRectMax().Y + ImGui.GetStyle().ItemSpacing.Y);
            ImGui.SetCursorScreenPos(next);
        }
    }

    /// <summary>A wrapped settings heading constrained to an already-scaled pixel width.</summary>
    public static void Section(string label, float widthPixels)
    {
        ArgumentNullException.ThrowIfNull(label);
        if (!float.IsFinite(widthPixels) || widthPixels <= 0) throw new ArgumentOutOfRangeException(nameof(widthPixels));
        if (MaterialTheme.Current.IsLcars)
        {
            ImGui.Dummy(new Vector2(0, 6 * MaterialTheme.Metrics.Scale));
            MaterialLcars.PanelHeading(label, 1, widthPixels);
            return;
        }
        ImGui.Dummy(new Vector2(0, 6 * MaterialTheme.Metrics.Scale));
        var origin = ImGui.GetCursorScreenPos();
        var size = MaterialText.Measure(label, wrapWidth: widthPixels);
        ImGui.Dummy(new Vector2(widthPixels, size.Y + 4 * MaterialTheme.Metrics.Scale));
        if (!ImGui.IsItemVisible()) return;
        var list = ImGui.GetWindowDrawList();
        list.PushClipRect(origin, origin + new Vector2(widthPixels, size.Y), true);
        try { MaterialText.AddText(list, origin, ImGui.GetColorU32(MaterialTheme.Current.Colors.Primary), label, widthPixels); }
        finally { list.PopClipRect(); }
    }

    /// <summary>Keep whole groups together; use two columns only when both measured minimum widths fit.</summary>
    public static void Columns(string id, float minimumColumnPixels, Action left, Action right)
        => Columns(id, minimumColumnPixels, left, right, true);

    /// <summary>Opt out of the divider for paired cards; uses the current theme gap without outer cell padding.</summary>
    public static void Columns(string id, float minimumColumnPixels, Action left, Action right, bool divider)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        if (!float.IsFinite(minimumColumnPixels) || minimumColumnPixels <= 0)
            throw new ArgumentOutOfRangeException(nameof(minimumColumnPixels));
        var root = ImGui.GetID("");
        var scale = MaterialTheme.Metrics.Scale;
        using var style = new MaterialStyleScope();
        if (divider) style.Style(ImGuiStyleVar.ItemSpacing, new Vector2(ImGui.GetStyle().ItemSpacing.X, 2 * scale));
        style.Style(ImGuiStyleVar.CellPadding, new Vector2(divider ? 10 * scale : MaterialTheme.Metrics.Gap * .5f, 0));
        if (ImGui.GetContentRegionAvail().X < 2 * minimumColumnPixels + (divider ? 40 * scale : MaterialTheme.Metrics.Gap))
        {
            left();
            right();
            return;
        }
        if (!ImGui.BeginTable(id, 2, ImGuiTableFlags.SizingStretchSame | ImGuiTableFlags.NoSavedSettings
            | (divider ? ImGuiTableFlags.BordersInnerV : ImGuiTableFlags.NoPadOuterX)))
            return;
        try
        {
            ImGui.TableNextRow();
            DrawColumn(0, left);
            DrawColumn(1, right);
        }
        finally { ImGui.EndTable(); }

        void DrawColumn(int column, Action draw)
        {
            ImGui.TableSetColumnIndex(column);
            // Presentation must not create a new identity for the caller's native controls.
            ImGuiP.PushOverrideID(root);
            try { draw(); }
            finally { ImGui.PopID(); }
        }
    }

    public static void Section(string label)
    {
        if (MaterialTheme.Current.IsLcars)
        {
            ImGui.Dummy(new Vector2(0, 6 * MaterialTheme.Metrics.Scale));
            MaterialLcars.PanelHeading(label, 1);
            return;
        }
        var scale = MaterialTheme.Metrics.Scale;
        ImGui.Dummy(new Vector2(0, 6 * scale));
        var start = ImGui.GetCursorScreenPos();
        MaterialText.TextColored(MaterialTheme.Current.Colors.Primary, label);
        var end = ImGui.GetItemRectMax();
        var right = start.X + ImGui.GetContentRegionAvail().X;
        if (right > end.X + 12 * scale)
            ImGui.GetWindowDrawList().AddLine(new Vector2(end.X + 10 * scale, (start.Y + end.Y) * .5f),
                new Vector2(right, (start.Y + end.Y) * .5f), MaterialCanvas.Color(MaterialTheme.Current.Colors.OutlineVariant));
    }
}

public readonly record struct MaterialAppearanceSections(string ThemeAndLanguage, string MainWindowControls,
    string WindowOpacity, string WhenUnfocused);

/// <summary>The standard grouped appearance tab; hosts retain all configuration and native editor ownership.</summary>
public static class MaterialAppearanceTab
{
    public static void Draw(string id, MaterialAppearanceSections labels, float minimumColumnPixels,
        Action themeAndLanguage, Action mainWindowControls, Action windowOpacity, Action whenUnfocused)
        => MaterialSettings.Columns(id, minimumColumnPixels, () =>
        {
            MaterialSettings.Section(labels.ThemeAndLanguage);
            themeAndLanguage();
            MaterialSettings.Section(labels.MainWindowControls);
            mainWindowControls();
        }, () =>
        {
            MaterialSettings.Section(labels.WindowOpacity);
            windowOpacity();
            MaterialSettings.Section(labels.WhenUnfocused);
            whenUnfocused();
        });
}
