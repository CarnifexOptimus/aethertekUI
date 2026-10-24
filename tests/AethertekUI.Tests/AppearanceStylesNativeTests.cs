using System.Numerics;
using AethertekUI.Demo;
using Dalamud.Bindings.ImGui;
using Xunit;

namespace AethertekUI.Tests;

public sealed unsafe class AppearanceStylesNativeTests
{
    private static readonly MaterialAppearanceLabels Palette = new("Colour", "Language", "Teal", "Blue", "Pink", "Custom RGB");
    private static readonly MaterialColorLabels Labels = new("Auto", "Picker", "DEFAULT", "Automatic palette", "Theme colour",
        "Choose colours", "Background", "Controls", "Text", "Preview", "Control", "Automatic hint", "Picker hint")
    {
        Lcars = "LCARS", LcarsHelp = "Panel colours", Glassmorphism = "Glassmorphism", Spatial = "Spatial", Cyberpunk = "Cyberpunk",
        LcarsBar1 = "Bar one", LcarsBar2 = "Bar two", LcarsBar3 = "Bar three", Reset = "Undo", ResetColors = "Defaults",
        Animation = "Animate", AnimationSpeed = "Speed", WaveColors = "Waves", GlassWave1 = "Wave one", GlassWave2 = "Wave two",
        GlassWave3 = "Wave three", ResetWaveColors = "Wave defaults",
    };

    [Theory]
    [InlineData(MaterialDensity.Compact, 1f)]
    [InlineData(MaterialDensity.Compact, 1.5f)]
    [InlineData(MaterialDensity.Standard, 1f)]
    [InlineData(MaterialDensity.Standard, 1.5f)]
    public void NativeStylesKeepIndependentDraftsAndResetOnlyTheirOwnRoles(MaterialDensity density, float scale)
    {
        using var host = new NativeTestContext((int)(1100 * scale), (int)(1600 * scale), scale);
        var settings = new MaterialColorSettings { Mode = MaterialColorMode.Picker };
        var defaults = new MaterialColorSettings
        {
            BackgroundRgb = 0x112233, ControlsRgb = 0x445566, TextRgb = 0xDDEEFF,
            GlassBackgroundRgb = 0x102030, GlassControlsRgb = 0x405060, GlassTextRgb = 0xD0E0F0,
            SpatialBackgroundRgb = 0xE0E1E2, SpatialControlsRgb = 0x607080, SpatialTextRgb = 0x203040,
            CyberpunkBackgroundRgb = 0x101112, CyberpunkControlsRgb = 0x20D0E0, CyberpunkTextRgb = 0xE0F0FF,
            LcarsBackgroundRgb = 0x0A0B0C, LcarsBar1Rgb = 0xFF8844, LcarsBar2Rgb = 0xBB88CC, LcarsBar3Rgb = 0x8888EE,
            GlassAnimationSpeed = 2.5f,
            GlassWave1Rgb = 0x60C0D0, GlassWave2Rgb = 0x80D060, GlassWave3Rgb = 0xD060C0,
        };
        var accent = new Vector3(.2f, .4f, .6f);
        var defaultAccent = new Vector3(.4f, .5f, .6f);
        var saved = settings;
        var saves = 0;
        ImGuiWindowPtr window = default;
        MaterialColorSettings? applied = null;
        var theme = new MaterialTheme(accent, density);
        void Draw()
        {
            if (applied != settings) { theme = new(accent, settings, density); applied = settings; }
            using var scope = MaterialTheme.Push(theme, scale);
            ImGui.SetNextWindowPos(new Vector2(20, 20) * scale);
            ImGui.SetNextWindowSize(new Vector2(330, 1500) * scale);
            ImGui.Begin("Independent styles", ImGuiWindowFlags.NoSavedSettings);
            window = ImGuiP.GetCurrentWindow();
            if (MaterialAppearanceSelector.DrawColors("colours", ref settings, ref accent, Palette, Labels, defaults, defaultAccent))
            { saved = settings; saves++; }
            ImGui.End();
        }
        host.Frame(Draw); host.Frame(Draw);
        foreach (var mode in new[] { MaterialColorMode.Picker, MaterialColorMode.Glassmorphism, MaterialColorMode.Spatial, MaterialColorMode.Cyberpunk })
        {
            if (settings.Mode != mode) Select(mode.ToString());
            var before = settings;
            var old = mode switch
            {
                MaterialColorMode.Picker => new[] { settings.BackgroundRgb, settings.ControlsRgb, settings.TextRgb },
                MaterialColorMode.Glassmorphism => [settings.GlassBackgroundRgb, settings.GlassControlsRgb, settings.GlassTextRgb],
                MaterialColorMode.Spatial => [settings.SpatialBackgroundRgb, settings.SpatialControlsRgb, settings.SpatialTextRgb],
                _ => [settings.CyberpunkBackgroundRgb, settings.CyberpunkControlsRgb, settings.CyberpunkTextRgb],
            };
            var replacements = new uint[] { 0x193149, 0x3579BD, 0xEDF5FD };
            for (var index = 0; index < 3; index++) Edit(Hex(old[index]), Hex(replacements[index]));
            var expected = mode switch
            {
                MaterialColorMode.Picker => before with { BackgroundRgb = 0x193149, ControlsRgb = 0x3579BD, TextRgb = 0xEDF5FD },
                MaterialColorMode.Glassmorphism => before with { GlassBackgroundRgb = 0x193149, GlassControlsRgb = 0x3579BD, GlassTextRgb = 0xEDF5FD },
                MaterialColorMode.Spatial => before with { SpatialBackgroundRgb = 0x193149, SpatialControlsRgb = 0x3579BD, SpatialTextRgb = 0xEDF5FD },
                _ => before with { CyberpunkBackgroundRgb = 0x193149, CyberpunkControlsRgb = 0x3579BD, CyberpunkTextRgb = 0xEDF5FD },
            };
            Assert.Equal(expected, saved);
            ClickResetBeside(Hex(replacements[0]));
            expected = mode switch
            {
                MaterialColorMode.Picker => expected with { BackgroundRgb = defaults.BackgroundRgb },
                MaterialColorMode.Glassmorphism => expected with { GlassBackgroundRgb = defaults.GlassBackgroundRgb },
                MaterialColorMode.Spatial => expected with { SpatialBackgroundRgb = defaults.SpatialBackgroundRgb },
                _ => expected with { CyberpunkBackgroundRgb = defaults.CyberpunkBackgroundRgb },
            };
            Assert.Equal(expected, saved);
            var unchangedSaves = saves;
            ClickResetBeside(Hex(mode switch
            {
                MaterialColorMode.Picker => defaults.BackgroundRgb, MaterialColorMode.Glassmorphism => defaults.GlassBackgroundRgb,
                MaterialColorMode.Spatial => defaults.SpatialBackgroundRgb, _ => defaults.CyberpunkBackgroundRgb,
            }));
            Assert.Equal(unchangedSaves, saves);
            host.Click(Center(window, "Defaults"), Draw);
            expected = mode switch
            {
                MaterialColorMode.Picker => expected with { ControlsRgb = defaults.ControlsRgb, TextRgb = defaults.TextRgb },
                MaterialColorMode.Glassmorphism => expected with { GlassControlsRgb = defaults.GlassControlsRgb, GlassTextRgb = defaults.GlassTextRgb },
                MaterialColorMode.Spatial => expected with { SpatialControlsRgb = defaults.SpatialControlsRgb, SpatialTextRgb = defaults.SpatialTextRgb },
                _ => expected with { CyberpunkControlsRgb = defaults.CyberpunkControlsRgb, CyberpunkTextRgb = defaults.CyberpunkTextRgb },
            };
            Assert.Equal(expected, saved);
        }

        Select("LCARS");
        var beforeLcars = saved;
        var oldLcars = new[] { settings.LcarsBackgroundRgb, settings.LcarsBar1Rgb, settings.LcarsBar2Rgb, settings.LcarsBar3Rgb };
        var newLcars = new uint[] { 0x141516, 0xEEAA77, 0xCC88DD, 0x8899EE };
        for (var index = 0; index < 4; index++) Edit(Hex(oldLcars[index]), Hex(newLcars[index]));
        Assert.Equal(beforeLcars with { LcarsBackgroundRgb = newLcars[0], LcarsBar1Rgb = newLcars[1], LcarsBar2Rgb = newLcars[2], LcarsBar3Rgb = newLcars[3] }, saved);
        Assert.Empty(TextBounds(window, "Animate"));
        Assert.Empty(TextBounds(window, Hex(settings.TextRgb)));
        ClickResetBeside(Hex(newLcars[1]));
        Assert.Equal(defaults.LcarsBar1Rgb, saved.LcarsBar1Rgb);
        host.Click(Center(window, "Defaults"), Draw);
        Assert.Equal(beforeLcars with { LcarsBackgroundRgb = defaults.LcarsBackgroundRgb, LcarsBar1Rgb = defaults.LcarsBar1Rgb,
            LcarsBar2Rgb = defaults.LcarsBar2Rgb, LcarsBar3Rgb = defaults.LcarsBar3Rgb }, saved);

        Select("Glassmorphism");
        Edit("1.00", "20");
        Assert.Equal(10, saved.GlassAnimationSpeed);
        Edit("10.00", "-2");
        Assert.Equal(.1f, saved.GlassAnimationSpeed);
        ClickResetBeside("0.10");
        Assert.Equal(defaults.GlassAnimationSpeed, saved.GlassAnimationSpeed);
        var speedResetSaves = saves;
        ClickResetBeside("2.50");
        Assert.Equal(speedResetSaves, saves);
        host.Click(Center(window, "Animate"), Draw);
        Assert.False(saved.GlassAnimation);
        var beforeWaves = saved;
        var oldWaves = new[] { settings.GlassWave1Rgb, settings.GlassWave2Rgb, settings.GlassWave3Rgb };
        var newWaves = new uint[] { 0x24A0B0, 0xB024A0, 0xA0B024 };
        for (var index = 0; index < 3; index++) Edit(Hex(oldWaves[index]), Hex(newWaves[index]));
        Assert.Equal(beforeWaves with { GlassWave1Rgb = newWaves[0], GlassWave2Rgb = newWaves[1], GlassWave3Rgb = newWaves[2] }, saved);
        Edit(Hex(settings.GlassBackgroundRgb), "#112A3B");
        var withWaves = saved;
        host.Click(Center(window, "Defaults"), Draw);
        Assert.Equal(withWaves with { GlassBackgroundRgb = defaults.GlassBackgroundRgb }, saved);
        ClickResetBeside(Hex(newWaves[1]));
        Assert.Equal(defaults.GlassWave2Rgb, saved.GlassWave2Rgb);
        host.Click(Center(window, "Wave defaults"), Draw);
        Assert.Equal(beforeWaves with { GlassWave1Rgb = defaults.GlassWave1Rgb, GlassWave2Rgb = defaults.GlassWave2Rgb,
            GlassWave3Rgb = defaults.GlassWave3Rgb }, saved);
        Assert.False(saved.GlassAnimation);
        Select("Spatial");
        Assert.Empty(TextBounds(window, "Animate"));
        Assert.Empty(TextBounds(window, "Wave one"));
        Select("Auto");
        var palettes = saved;
        host.Click(Center(window, "Blue"), Draw);
        Assert.Equal(palettes, saved);
        Assert.Equal(new Vector3(66 / 255f, 133 / 255f, 244 / 255f), accent);
        host.Click(Center(window, "Defaults"), Draw);
        Assert.Equal(defaultAccent, accent);
        Assert.Equal(palettes, saved);
        Assert.Equal(0, window.ScrollMax.X);

        void Select(string caption)
        {
            var count = saves;
            host.Click(Center(window, caption), Draw);
            Assert.Equal(count + 1, saves);
            Assert.Equal(settings, saved);
            host.Frame(Draw);
        }
        void Edit(string current, string replacement)
        {
            var count = saves;
            host.Click(Center(window, current), Draw);
            Assert.NotEqual(0u, ImGui.GetCurrentContext().ActiveId);
            ImGui.GetIO().AddKeyEvent(ImGuiKey.ModCtrl, true);
            ImGui.GetIO().AddKeyEvent(ImGuiKey.A, true); host.Frame(Draw);
            ImGui.GetIO().AddKeyEvent(ImGuiKey.A, false);
            ImGui.GetIO().AddKeyEvent(ImGuiKey.ModCtrl, false); host.Frame(Draw);
            foreach (var character in replacement) ImGui.GetIO().AddInputCharacter(character);
            host.Frame(Draw);
            Assert.Equal(count, saves);
            ImGui.GetIO().AddKeyEvent(ImGuiKey.Enter, true); host.Frame(Draw);
            ImGui.GetIO().AddKeyEvent(ImGuiKey.Enter, false); host.Frame(Draw);
            Assert.Equal(count + 1, saves);
            Assert.Equal(settings, saved);
        }
        void ClickResetBeside(string text)
        {
            var y = Center(window, text).Y;
            var reset = TextBounds(window, "Undo").MinBy(bounds => Math.Abs((bounds.Min.Y + bounds.Max.Y) / 2 - y));
            host.Click((reset.Min + reset.Max) / 2, Draw);
            host.Frame(Draw); // The reset follows the colour field; inspect its updated native text on the next frame.
        }
    }

    [Fact]
    public void LegacyAndOptedInPickerKeepTheOriginalNativeFieldAndAutoPaletteIds()
    {
        using var host = new NativeTestContext();
        var settings = new MaterialColorSettings { Mode = MaterialColorMode.Picker };
        var accent = new Vector3(.2f, .4f, .6f);
        var theme = new MaterialTheme(accent);
        var modern = false;
        var labels = new MaterialColorLabels("Auto", "Picker", "DEFAULT", "Auto help", "Theme colour", "Picker help",
            "Background", "Controls", "Text", "Preview", "Control", "Auto hint", "Picker hint") { Lcars = "LCARS" };
        ImGuiWindowPtr window = default;
        uint inputId = 0, presetId = 0;
        void Draw()
        {
            using var scope = MaterialTheme.Push(theme);
            ImGui.SetNextWindowSize(new(650, 700));
            ImGui.Begin("Native identity", ImGuiWindowFlags.NoSavedSettings);
            window = ImGuiP.GetCurrentWindow();
            ImGui.PushID("colours");
            ImGui.PushID("blue-inline"); presetId = ImGui.GetID("##button"); ImGui.PopID();
            ImGui.PopID();
            MaterialAppearanceSelector.DrawColors("colours", ref settings, ref accent, Palette, modern ? Labels : labels);
            ImGui.End();
        }
        host.Frame(Draw); host.Frame(Draw);
        Assert.Empty(TextBounds(window, "Glassmorphism"));
        Assert.Empty(TextBounds(window, "Undo"));
        foreach (var optIn in new[] { false, true })
        {
            modern = optIn;
            host.Frame(Draw);
            host.Click(Center(window, Hex(settings.BackgroundRgb)), Draw);
            if (!optIn) inputId = ImGui.GetCurrentContext().ActiveId;
            Assert.NotEqual(0u, inputId);
            Assert.Equal(inputId, ImGui.GetCurrentContext().ActiveId);
            host.Click(Center(window, "Auto"), Draw);
            host.Frame(Draw);
            var center = Center(window, "Blue");
            ImGui.GetIO().AddMousePosEvent(center.X, center.Y); host.Frame(Draw);
            Assert.Equal(presetId, ImGui.GetCurrentContext().HoveredId);
            host.Click(Center(window, "Picker"), Draw);
        }
        modern = false;
        host.Frame(Draw);
        host.Click(Center(window, "LCARS"), Draw); host.Frame(Draw);
        Assert.Empty(TextBounds(window, Hex(settings.LcarsBackgroundRgb)));
        Assert.Empty(TextBounds(window, "Bar one"));
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.5f)]
    public void ActualShowcaseKeepsAllSixModeCaptionsInsideItsAppearanceColumn(float scale)
    {
        using var host = new NativeTestContext((int)(1100 * scale), (int)(850 * scale), scale);
        var demo = new Showcase { IsOpen = true, Page = 4 };
        for (var frame = 0; frame < 4; frame++) host.Frame(() =>
        {
            ImGui.SetNextWindowPos(Vector2.Zero);
            demo.Draw(scale);
        });
        ImGuiWindowPtr content = default;
        var context = ImGui.GetCurrentContext();
        for (var index = 0; index < context.Windows.Size; index++)
        {
            var candidate = new ImGuiWindowPtr(context.Windows.Data[index]);
            if (TextBounds(candidate, "THEME & LANGUAGE").Count > 0 && TextBounds(candidate, "WINDOW OPACITY").Count > 0)
            { content = candidate; break; }
        }
        Assert.False(content.IsNull);
        var left = TextBounds(content, "THEME & LANGUAGE")[0].Min.X;
        // The existing grouped layout reserves ten scaled pixels on each side of its column divider.
        var right = TextBounds(content, "WINDOW OPACITY")[0].Min.X - 20 * scale;
        foreach (var caption in new[] { "Auto", "Picker", "LCARS", "Glassmorphism", "Spatial", "Cyberpunk" })
        {
            var bounds = TextBounds(content, caption);
            Assert.True(bounds.Count > 0, "Native showcase mode caption was not visible: " + caption);
            Assert.InRange(bounds[0].Min.X, left, right);
            Assert.InRange(bounds[0].Max.X, left, right);
        }
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.5f)]
    public void TranslatedStylePopupKeepsItsMeasuredWidthAcrossModesAndFrames(float scale)
    {
        using var host = new NativeTestContext((int)(1200 * scale), (int)(1600 * scale), scale);
        var settings = new MaterialColorSettings();
        var accent = new Vector3(.2f, .4f, .6f);
        var theme = new MaterialTheme(accent);
        var translated = Labels with { Background = "A longer translated background label", Controls = "A longer translated control label",
            Reset = "Restore this colour", ResetColors = "Restore palette colours", ResetWaveColors = "Restore wave colours",
            LcarsBar1 = "Translated first panel bar", LcarsBar2 = "Translated second panel bar", LcarsBar3 = "Translated third panel bar" };
        var opening = true;
        ImGuiWindowPtr popup = default;
        void Draw()
        {
            using var scope = MaterialTheme.Push(theme, scale);
            ImGui.SetNextWindowPos(new Vector2(20, 20) * scale);
            ImGui.Begin("Popup owner", ImGuiWindowFlags.NoSavedSettings);
            if (opening) { ImGui.OpenPopup("styles"); opening = false; }
            if (ImGui.BeginPopup("styles", ImGuiWindowFlags.NoSavedSettings))
            {
                popup = ImGuiP.GetCurrentWindow();
                MaterialAppearanceSelector.DrawColors("colours", ref settings, ref accent, Palette, translated, new(), accent);
                ImGui.EndPopup();
            }
            ImGui.End();
        }
        host.Frame(Draw); host.Frame(Draw); host.Frame(Draw);
        var expectedWidth = popup.Size.X;
        foreach (var caption in new[] { "Picker", "LCARS", "Glassmorphism", "Spatial", "Cyberpunk", "Auto" })
        {
            host.Click(Center(popup, caption), Draw);
            for (var frame = 0; frame < 4; frame++)
            {
                host.Frame(Draw);
                Assert.True(MathF.Abs(popup.Size.X - expectedWidth) <= 1,
                    $"{caption} frame {frame}: expected width {expectedWidth}, actual {popup.Size.X}, height {popup.Size.Y}, vertical scrollbar {popup.ScrollbarY}, scroll range {popup.ScrollMax}.");
                Assert.Equal(0, popup.ScrollMax.X);
                Assert.False(popup.ScrollbarY);
                Assert.InRange(popup.Size.Y, 1, (caption == "Glassmorphism" ? 900 : 550) * scale);
            }
            if (caption is not "Auto")
            {
                var resets = TextBounds(popup, translated.ResetColors!);
                Assert.NotEmpty(resets);
                foreach (var bounds in resets)
                {
                    Assert.True(bounds.Min.X >= popup.Pos.X);
                    Assert.True(bounds.Max.X <= popup.Pos.X + popup.Size.X);
                }
            }
        }
        Assert.InRange(expectedWidth, 300 * scale, 1000 * scale);
    }

    private static string Hex(uint rgb) => "#" + rgb.ToString("X6");
    private static Vector2 Center(ImGuiWindowPtr window, string text)
    {
        var bounds = TextBounds(window, text);
        Assert.True(bounds.Count > 0, "Native editor caption was not visible: " + text);
        return (bounds[0].Min + bounds[0].Max) / 2;
    }

    private static List<(Vector2 Min, Vector2 Max)> TextBounds(ImGuiWindowPtr window, string text)
    {
        var list = window.DrawList;
        var font = ImGui.GetFont();
        var glyphs = text.Where(character => !char.IsWhiteSpace(character)).Select(character => ImGui.FindGlyphNoFallback(font, character))
            .Where(glyph => glyph.Handle != null).ToArray();
        var result = new List<(Vector2 Min, Vector2 Max)>();
        for (var index = 0; index + glyphs.Length * 4 <= list.VtxBuffer.Size; index++)
        {
            var matched = true;
            for (var character = 0; character < glyphs.Length; character++)
            {
                var glyph = glyphs[character];
                var first = list.VtxBuffer[index + character * 4];
                var last = list.VtxBuffer[index + character * 4 + 2];
                if (first.Uv != new Vector2(glyph.Handle->U0, glyph.Handle->V0)
                    || last.Uv != new Vector2(glyph.Handle->U1, glyph.Handle->V1)) { matched = false; break; }
            }
            if (matched) result.Add((list.VtxBuffer[index].Pos, list.VtxBuffer[index + glyphs.Length * 4 - 2].Pos));
        }
        return result;
    }
}
