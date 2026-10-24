using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace AethertekUI;

/// <summary>LCARS chrome inside existing owner bounds. It does not replace windows, controls or their native IDs.</summary>
public static class MaterialLcars
{
    public static Vector4 Orange => ActiveColors?.Primary ?? new(1, 170f / 255, 102f / 255, 1);
    public static Vector4 Lilac => ActiveColors?.Secondary ?? new(204f / 255, 153f / 255, 204f / 255, 1);
    public static Vector4 Periwinkle => ActiveColors?.Tertiary ?? new(153f / 255, 153f / 255, 1, 1);
    public static Vector4 Amber => new(1, 204f / 255, 153f / 255, 1);
    public static Vector4 Background => ActiveColors?.Background ?? new(7f / 255, 9f / 255, 13f / 255, 1);
    public static Vector4 Text => ActiveColors?.OnSurface ?? new(244f / 255, 241f / 255, 234f / 255, 1);
    public static Vector4 ControlBlue => new(53f / 255, 156f / 255, 1, 1);

    private static MaterialColorScheme? ActiveColors => MaterialTheme.CurrentOrNull is { IsLcars: true } theme ? theme.Colors : null;

    public static Vector4 Accent(int index) => ((index % 3 + 3) % 3) switch
    {
        1 => Lilac, 2 => Periwinkle, _ => Orange,
    };

    public static void Feedback(bool activated, MaterialUiSoundCue cue = MaterialUiSoundCue.Click)
    {
        if (activated && MaterialTheme.Current.IsLcars) MaterialTheme.Current.Feedback?.Invoke(cue);
    }

    /// <summary>Dark outlined native buttons; explicit semantic fills and native IDs remain owned by the caller.</summary>
    public static MaterialStyleScope NativeButtonStyle() => NativeButtonStyle(Lilac);

    public static MaterialStyleScope NativeButtonStyle(Vector4 accent)
    {
        var scope = new MaterialStyleScope();
        if (!MaterialTheme.Current.IsLcars) return scope;
        var c = MaterialTheme.Current.Colors;
        var fill = ImGui.GetStyle().Colors[(int)ImGuiCol.Button];
        if (fill == c.SurfaceContainerHighest)
        {
            fill = Background;
            scope.Color(ImGuiCol.Button, fill);
            scope.Color(ImGuiCol.ButtonHovered, MaterialColor.Layer(fill, accent, .12f));
            scope.Color(ImGuiCol.ButtonActive, MaterialColor.Layer(fill, accent, .22f));
            scope.Color(ImGuiCol.Border, accent);
            scope.Style(ImGuiStyleVar.FrameBorderSize, 1.5f * MaterialTheme.Metrics.Scale);
            scope.Style(ImGuiStyleVar.FrameRounding, 6 * MaterialTheme.Metrics.Scale);
        }
        if (fill.W > .01f)
            scope.Color(ImGuiCol.Text, MaterialSkin.ReadableInk(fill));
        return scope;
    }

    public static void Header(string title)
    {
        var scale = MaterialTheme.Metrics.Scale;
        var compact = MaterialTheme.Current.Density == MaterialDensity.Compact;
        var min = ImGui.GetCursorScreenPos();
        var width = Math.Max(1, ImGui.GetContentRegionAvail().X);
        var text = MaterialText.Measure(title);
        var height = Math.Max((compact ? 50 : 66) * scale, text.Y + 30 * scale);
        var elbow = Math.Min(62 * scale, width * .24f);
        var rail = (compact ? 12 : 16) * scale;
        var dl = ImGui.GetWindowDrawList();
        dl.PushClipRect(min, min + new Vector2(width, height), true);
        dl.AddRectFilled(min, min + new Vector2(width, height), Ink(Background));
        dl.AddRectFilled(min, min + new Vector2(elbow, height), Ink(Orange), 26 * scale, ImDrawFlags.RoundCornersTopLeft);
        dl.AddRectFilled(min + new Vector2(Math.Min(20 * scale, elbow * .4f), rail),
            min + new Vector2(elbow + 2 * scale, height + scale), Ink(Background),
            18 * scale, ImDrawFlags.RoundCornersTopLeft);
        var start = min.X + elbow + 4 * scale;
        var remaining = Math.Max(0, min.X + width - start);
        var gap = Math.Min(5 * scale, remaining / 12);
        for (var i = 0; i < 3; i++)
        {
            var part = Math.Max(0, (remaining - 2 * gap) / 3);
            var x = start + i * (part + gap);
            dl.AddRectFilled(new(x, min.Y), new(x + part, min.Y + rail), Ink(Accent(i + 1)), rail * .5f);
        }
        MaterialText.AddText(dl, min + new Vector2(Math.Min(38 * scale, width * .1f), rail + 8 * scale),
            Ink(Text), title);
        dl.PopClipRect();
        ImGui.Dummy(new(width, height));
    }

    public static void PanelHeading(string title, int accent = 0)
    {
        var scale = MaterialTheme.Metrics.Scale;
        var width = Math.Max(1, ImGui.GetContentRegionAvail().X);
        var wrap = Math.Max(1, width - 28 * scale);
        var text = MaterialText.Measure(title, wrapWidth: wrap);
        var height = Math.Max((MaterialTheme.Current.Density == MaterialDensity.Compact ? 26 : 32) * scale, text.Y + 8 * scale);
        var min = ImGui.GetCursorScreenPos();
        var dl = ImGui.GetWindowDrawList();
        dl.PushClipRect(min, min + new Vector2(width, height), true);
        dl.AddRectFilled(min, min + new Vector2(width, height), Ink(Accent(accent)), 5 * scale);
        MaterialText.AddText(dl, min + new Vector2(14 * scale, (height - text.Y) * .5f),
            Ink(MaterialSkin.ReadableInk(Accent(accent))), title, wrap);
        dl.PopClipRect();
        ImGui.Dummy(new(width, height));
    }

    /// <summary>Paint in the owner's background channel, behind its existing content.</summary>
    public static void PanelFrame(Vector2 min, Vector2 max, int accent = 0)
    {
        var scale = MaterialTheme.Metrics.Scale;
        var dl = ImGui.GetWindowDrawList();
        var colour = Ink(Accent(accent));
        var radius = Math.Min(18 * scale, Math.Min(max.X - min.X, max.Y - min.Y) * .5f);
        dl.AddRectFilled(min, max, Ink(Background), radius);
        dl.AddRect(min, max, colour, radius, ImDrawFlags.None, 2 * scale);
        // The caller reserves this twelve-pixel band outside its content bounds.
        dl.AddRectFilled(min, new(min.X + 12 * scale, max.Y), colour, radius, ImDrawFlags.RoundCornersLeft);
        dl.AddRectFilled(min + new Vector2(9 * scale, 0),
            new(Math.Min(max.X, min.X + 44 * scale), min.Y + 8 * scale), colour);
    }

    public static void Footer(string caption)
    {
        var scale = MaterialTheme.Metrics.Scale;
        var min = ImGui.GetCursorScreenPos();
        var width = Math.Max(1, ImGui.GetContentRegionAvail().X);
        var gap = Math.Min(5 * scale, width / 12);
        var part = Math.Max(0, (width - 2 * gap) / 3);
        var dl = ImGui.GetWindowDrawList();
        for (var i = 0; i < 3; i++)
        {
            var x = min.X + i * (part + gap);
            dl.AddRectFilled(new(x, min.Y), new(x + part, min.Y + 6 * scale), Ink(Accent(i)), 3 * scale);
        }
        ImGui.Dummy(new(width, 8 * scale));
        MaterialText.TextDisabled(caption);
    }

    private static uint Ink(Vector4 color)
    {
        return MaterialCanvas.Color(color);
    }
}
