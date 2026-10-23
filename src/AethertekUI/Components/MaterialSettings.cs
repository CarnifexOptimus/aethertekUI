using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace AethertekUI;

/// <summary>Compact semantic settings groups. The caller owns controls, IDs, localization and persistence.</summary>
public static class MaterialSettings
{
    /// <summary>Keep whole groups together; use two columns only when both measured minimum widths fit.</summary>
    public static void Columns(string id, float minimumColumnPixels, Action left, Action right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        if (!float.IsFinite(minimumColumnPixels) || minimumColumnPixels <= 0)
            throw new ArgumentOutOfRangeException(nameof(minimumColumnPixels));
        var root = ImGui.GetID("");
        var scale = MaterialTheme.Metrics.Scale;
        using var style = new MaterialStyleScope();
        style.Style(ImGuiStyleVar.ItemSpacing, new Vector2(ImGui.GetStyle().ItemSpacing.X, 2 * scale));
        style.Style(ImGuiStyleVar.CellPadding, new Vector2(10 * scale, 0));
        if (ImGui.GetContentRegionAvail().X < 2 * minimumColumnPixels + 40 * scale)
        {
            left();
            right();
            return;
        }
        if (!ImGui.BeginTable(id, 2, ImGuiTableFlags.SizingStretchSame | ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.NoSavedSettings))
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
