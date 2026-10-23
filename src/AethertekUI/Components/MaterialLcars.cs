using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace AethertekUI;

/// <summary>LCARS chrome inside existing owner bounds. It does not replace windows, controls or their native IDs.</summary>
public static class MaterialLcars
{
    public static Vector4 Orange => new(1, 170f / 255, 102f / 255, 1);
    public static Vector4 Lilac => new(204f / 255, 153f / 255, 204f / 255, 1);
    public static Vector4 Periwinkle => new(153f / 255, 153f / 255, 1, 1);
    public static Vector4 Amber => new(1, 204f / 255, 153f / 255, 1);
    public static Vector4 Background => new(7f / 255, 9f / 255, 13f / 255, 1);
    public static Vector4 Text => new(244f / 255, 241f / 255, 234f / 255, 1);

    public static Vector4 Accent(int index) => ((index % 4 + 4) % 4) switch
    {
        1 => Lilac, 2 => Periwinkle, 3 => Amber, _ => Orange,
    };

    public static void Feedback(bool activated, MaterialUiSoundCue cue = MaterialUiSoundCue.Click)
    {
        if (activated && MaterialTheme.Current.IsLcars) MaterialTheme.Current.Feedback?.Invoke(cue);
    }

    /// <summary>Pastel native buttons with contrasting captions; respects explicit caller fills and native IDs.</summary>
    public static MaterialStyleScope NativeButtonStyle()
    {
        var scope = new MaterialStyleScope();
        if (!MaterialTheme.Current.IsLcars) return scope;
        var c = MaterialTheme.Current.Colors;
        var fill = ImGui.GetStyle().Colors[(int)ImGuiCol.Button];
        if (fill == c.SurfaceContainerHighest)
        {
            fill = Lilac;
            scope.Color(ImGuiCol.Button, fill);
            scope.Color(ImGuiCol.ButtonHovered, MaterialColor.Layer(fill, Background, .08f));
            scope.Color(ImGuiCol.ButtonActive, MaterialColor.Layer(fill, Background, .16f));
        }
        if (fill.W > .01f)
            scope.Color(ImGuiCol.Text, MaterialColor.Contrast(fill, Background) >= MaterialColor.Contrast(fill, Text) ? Background : Text);
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
        dl.AddRectFilled(min, min + new Vector2(width, height), Ink(Accent(accent)), height * .5f);
        MaterialText.AddText(dl, min + new Vector2(14 * scale, (height - text.Y) * .5f),
            Ink(Background), title, wrap);
        dl.PopClipRect();
        ImGui.Dummy(new(width, height));
    }

    /// <summary>Paint in the owner's background channel, behind its existing content.</summary>
    public static void PanelFrame(Vector2 min, Vector2 max, int accent = 0)
    {
        var scale = MaterialTheme.Metrics.Scale;
        var dl = ImGui.GetWindowDrawList();
        dl.AddRectFilled(min, max, Ink(MaterialTheme.Current.Colors.Background), 6 * scale);
        dl.AddLine(new(min.X, min.Y + 12 * scale), new(min.X, max.Y - 5 * scale), Ink(Accent(accent)), 2 * scale);
        dl.AddLine(new(min.X + 3 * scale, max.Y), new(Math.Min(max.X, min.X + 50 * scale), max.Y), Ink(Accent(accent)), 3 * scale);
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
