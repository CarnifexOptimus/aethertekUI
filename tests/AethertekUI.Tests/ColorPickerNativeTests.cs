using System.Numerics;
using AethertekUI.Dalamud;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Xunit;

namespace AethertekUI.Tests;

public sealed unsafe class ColorPickerNativeTests
{
    [Theory]
    [InlineData(MaterialDensity.Compact, 1f)]
    [InlineData(MaterialDensity.Compact, 1.5f)]
    [InlineData(MaterialDensity.Standard, 1f)]
    [InlineData(MaterialDensity.Standard, 1.5f)]
    public void NativeTitlebarPickerEditsThreeIndependentHexValuesAndRetainsThemAcrossAuto(MaterialDensity density, float scale)
    {
        using var host = new NativeTestContext((int)(1200 * scale), (int)(900 * scale), scale);
        var theme = new MaterialTheme(new Vector3(.3f, .7f, .4f), density);
        var owner = new ProbeWindow { IsOpen = true };
        var settings = new MaterialColorSettings();
        MaterialColorSettings? applied = null;
        var saved = settings;
        var accent = new Vector3(.3f, .7f, .4f);
        var saves = 0;
        ImGuiWindowPtr popup = default;
        var actions = MaterialAppearanceTitleBar.Register(owner, () => new("Language", "Compact", "Transparency", "Colour"),
            () => { }, () => { }, () => { }, () =>
            {
                popup = ImGuiP.GetCurrentWindow();
                Assert.Equal(ImGuiP.FindWindowByName(owner.WindowName), popup.ParentWindowInBeginStack);
                if (MaterialAppearanceSelector.DrawColors("colours", ref settings, ref accent,
                    new("Colour", "Language", "Teal", "Blue", "Pink", "Custom RGB"),
                    new("Auto", "Picker", "DEFAULT", "Background, controls and text follow your theme.", "Theme colour",
                        "Choose each colour independently.", "Background", "Controls", "Text", "Aa  Preview text", "Control",
                        "Switch to Picker to set each colour yourself.", "Switch back to Auto to use the automatic theme.")
                        { Lcars = "LCARS", LcarsHelp = "LCARS panels, bars and controls" }))
                { saved = settings; saves++; }
            });
        void Draw()
        {
            if (applied != settings) { theme = new(accent, settings, density); applied = settings; }
            using var palette = MaterialTheme.Push(theme, scale);
            ImGui.SetNextWindowPos(new Vector2(20, 20) * scale);
            ImGui.SetNextWindowSize(new Vector2(500, 200) * scale);
            ImGui.SetNextWindowCollapsed(true);
            ImGui.Begin(owner.WindowName, ImGuiWindowFlags.NoSavedSettings);
            ImGui.End();
            actions.DrawPopups();
        }
        host.Frame(Draw); host.Frame(Draw);
        actions.RefreshVisibility();
        owner.TitleBarButtons[3].Click!(ImGuiMouseButton.Left);
        host.Frame(Draw); host.Frame(Draw); host.Frame(Draw);
        host.Click(Center("Picker"), Draw);
        Assert.Equal(MaterialColorMode.Picker, saved.Mode);
        Assert.Equal(1, saves);
        Assert.True(ImGuiP.FindWindowByName(owner.WindowName).Collapsed);
        var originals = new[] { settings.BackgroundRgb, settings.ControlsRgb, settings.TextRgb };
        var replacements = new[] { "#193149", "#3579BD", "#EDF5FD" };
        for (var field = 0; field < 3; field++)
        {
            var before = settings;
            host.Click(Center("#" + originals[field].ToString("X6")), Draw);
            ImGui.GetIO().AddKeyEvent(ImGuiKey.ModCtrl, true);
            ImGui.GetIO().AddKeyEvent(ImGuiKey.A, true); host.Frame(Draw);
            ImGui.GetIO().AddKeyEvent(ImGuiKey.A, false);
            ImGui.GetIO().AddKeyEvent(ImGuiKey.ModCtrl, false); host.Frame(Draw);
            foreach (var character in replacements[field]) ImGui.GetIO().AddInputCharacter(character);
            host.Frame(Draw);
            Assert.Equal(1 + field, saves); // Native editing previews, but has not requested persistence yet.
            ImGui.GetIO().AddKeyEvent(ImGuiKey.Enter, true); host.Frame(Draw);
            ImGui.GetIO().AddKeyEvent(ImGuiKey.Enter, false); host.Frame(Draw);
            Assert.Equal(2 + field, saves);
            Assert.Equal(settings, saved);
            var expected = Convert.ToUInt32(replacements[field][1..], 16);
            Assert.Equal(field == 0 ? expected : before.BackgroundRgb, saved.BackgroundRgb);
            Assert.Equal(field == 1 ? expected : before.ControlsRgb, saved.ControlsRgb);
            Assert.Equal(field == 2 ? expected : before.TextRgb, saved.TextRgb);
        }
        var manual = settings;
        host.Click(Center("LCARS"), Draw);
        host.Frame(Draw); host.Frame(Draw);
        Assert.Equal(manual with { Mode = MaterialColorMode.Lcars }, saved);
        Assert.True(theme.IsLcars);
        host.Click(Center("Auto"), Draw);
        Assert.Equal(manual with { Mode = MaterialColorMode.Auto }, saved);
        host.Click(Center("Picker"), Draw);
        Assert.Equal(manual, saved);
        host.Frame(Draw); host.Frame(Draw);
        Assert.InRange(popup.Size.X, 300 * scale, 700 * scale);
        Assert.True(popup.Pos.X >= 0 && popup.Pos.Y >= 0);
        Assert.True(popup.Pos.X + popup.Size.X <= ImGui.GetIO().DisplaySize.X + 1);
        if (Environment.GetEnvironmentVariable("AETHERTEKUI_PREVIEW_DIR") is { Length: > 0 } directory)
            SoftwarePreview.Save(Path.Combine(directory, $"colours-picker-{density}-{scale:0.0}.png"), host);

        Vector2 Center(string text)
        {
            var list = popup.DrawList;
            var font = ImGui.GetFont();
            for (var index = 0; index + text.Length * 4 <= list.VtxBuffer.Size; index++)
            {
                var matched = true;
                for (var character = 0; character < text.Length; character++)
                {
                    var glyph = ImGui.FindGlyphNoFallback(font, text[character]);
                    var first = list.VtxBuffer[index + character * 4];
                    var last = list.VtxBuffer[index + character * 4 + 2];
                    if (first.Uv != new Vector2(glyph.Handle->U0, glyph.Handle->V0)
                        || last.Uv != new Vector2(glyph.Handle->U1, glyph.Handle->V1)) { matched = false; break; }
                }
                if (matched) return (list.VtxBuffer[index].Pos + list.VtxBuffer[index + text.Length * 4 - 2].Pos) / 2;
            }
            throw new InvalidOperationException("Native colour editor text was not visible: " + text);
        }
    }

    private sealed class ProbeWindow() : Window("Colour pilot###colour-pilot")
    {
        public override void Draw() { }
    }
}
