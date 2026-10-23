using System.Buffers.Binary;
using System.Numerics;
using System.Text.Json;
using Dalamud.Bindings.ImGui;
using Xunit;

namespace AethertekUI.Tests;

public sealed class LcarsTests
{
    [Fact]
    public void ThirdModePreservesStoredPickerValuesAndSemanticErrors()
    {
        Assert.Equal(0, (int)MaterialColorMode.Auto);
        Assert.Equal(1, (int)MaterialColorMode.Picker);
        Assert.Equal(2, (int)MaterialColorMode.Lcars);
        var automatic = new MaterialTheme(new Vector3(.3f, .7f, .4f));
        var settings = new MaterialColorSettings { Mode = MaterialColorMode.Lcars,
            BackgroundRgb = 0x123456, ControlsRgb = 0x456789, TextRgb = 0xFEDCBA };
        var restored = JsonSerializer.Deserialize<MaterialColorSettings>(JsonSerializer.Serialize(settings))!;
        Assert.Equal(settings, restored);
        var theme = new MaterialTheme(restored.Apply(automatic.Colors));
        Assert.True(theme.IsLcars);
        Assert.Equal(MaterialLcars.Background, theme.Colors.Background);
        Assert.Equal(MaterialLcars.Orange, theme.Colors.Primary);
        Assert.Equal(MaterialLcars.Lilac, theme.Colors.Secondary);
        Assert.Equal(automatic.Colors.Error, theme.Colors.Error);
        Assert.Same(automatic.Colors, (restored with { Mode = MaterialColorMode.Auto }).Apply(automatic.Colors));
        Assert.Equal(new Vector4(MaterialColorSettings.Rgb(0x123456), 1),
            (restored with { Mode = MaterialColorMode.Picker }).Apply(automatic.Colors).Background);
        Assert.True(MaterialColor.Contrast(theme.Colors.Primary, theme.Colors.OnPrimary) > 7);
        Assert.True(MaterialColor.Contrast(theme.Colors.OnSurface, theme.Colors.Background) > 12);
    }

    [Fact]
    public void TonesAreShortQuietOriginalPcmAndAnUnusedPlayerOwnsNoDevice()
    {
        var clips = Enum.GetValues<MaterialUiSoundCue>().Select(MaterialUiSounds.CreateSamples).ToArray();
        foreach (var clip in clips)
        {
            Assert.InRange(clip.Length, 2000, 10000);
            Assert.Equal(0, clip.Length % 2);
            Assert.Equal(0, BinaryPrimitives.ReadInt16LittleEndian(clip));
            Assert.Equal(0, BinaryPrimitives.ReadInt16LittleEndian(clip.AsSpan(clip.Length - 2)));
            var nonzero = false;
            for (var offset = 0; offset < clip.Length; offset += 2)
            {
                var sample = BinaryPrimitives.ReadInt16LittleEndian(clip.AsSpan(offset));
                Assert.InRange(sample, -6000, 6000);
                nonzero |= sample != 0;
            }
            Assert.True(nonzero);
        }
        Assert.False(clips[0].SequenceEqual(clips[1]));
        Assert.Throws<ArgumentOutOfRangeException>(() => MaterialUiSounds.CreateSamples((MaterialUiSoundCue)99));
        using var player = new MaterialUiSoundPlayer();
        Assert.True(player.Stop());
        player.Dispose();
        Assert.False(player.TryPlay()); // Does not open an actual audio device in offline verification.
    }

    [Theory]
    [InlineData(MaterialDensity.Compact, 1f)]
    [InlineData(MaterialDensity.Compact, 1.5f)]
    [InlineData(MaterialDensity.Standard, 1f)]
    [InlineData(MaterialDensity.Standard, 1.5f)]
    public void NativeChromeHasAnElbowAndRailsAndFeedbackRetainsButtonIdentity(MaterialDensity density, float scale)
    {
        using var host = new NativeTestContext((int)(900 * scale), (int)(650 * scale), scale);
        var theme = new MaterialTheme(new Vector3(.3f, .7f, .4f), density);
        var cues = new List<MaterialUiSoundCue>();
        theme.Feedback = cues.Add;
        Vector2 button = default, origin = default;
        uint id = 0;
        var disabled = false;
        void Draw()
        {
            using var scope = MaterialTheme.Push(theme, scale);
            ImGui.SetNextWindowPos(new Vector2(20, 20) * scale);
            ImGui.SetNextWindowSize(new Vector2(700, 500) * scale);
            ImGui.Begin("LCARS native", ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoTitleBar);
            if (theme.IsLcars)
            {
                origin = ImGui.GetCursorScreenPos();
                MaterialLcars.Header("Fren Rider");
                MaterialLcars.PanelHeading("Operator", 1);
            }
            MaterialButton.FilledButton("run", "Run", disabled: disabled);
            id = ImGui.GetCurrentContext().LastItemData.ID;
            button = (ImGui.GetItemRectMin() + ImGui.GetItemRectMax()) * .5f;
            ImGui.End();
        }
        host.Frame(Draw); host.Frame(Draw);
        var originalId = id;
        host.Click(button, Draw);
        Assert.Empty(cues);
        theme = new MaterialTheme(new Vector3(.3f, .7f, .4f), new MaterialColorSettings { Mode = MaterialColorMode.Lcars }, density)
            { Feedback = cues.Add, SurfaceOpacity = 1 };
        host.Frame(Draw); host.Frame(Draw);
        Assert.Equal(originalId, id);
        var frame = SoftwarePreview.Capture(host);
        Pixel(origin + new Vector2(3, density == MaterialDensity.Compact ? 42 : 58) * scale, MaterialLcars.Orange);
        Pixel(origin + new Vector2(40, density == MaterialDensity.Compact ? 45 : 61) * scale, MaterialLcars.Background);
        host.Click(button, Draw);
        Assert.Single(cues);
        disabled = true;
        host.Frame(Draw);
        host.Click(button, Draw);
        Assert.Single(cues);
        disabled = false;
        host.Frame(Draw);
        if (Environment.GetEnvironmentVariable("AETHERTEKUI_PREVIEW_DIR") is { Length: > 0 } directory)
            SoftwarePreview.Save(Path.Combine(directory, $"lcars-{density}-{scale:0.0}.png"), host);

        void Pixel(Vector2 point, Vector4 expected)
        {
            var offset = ((int)point.Y * frame.Width + (int)point.X) * 4;
            Assert.InRange(frame.Rgba[offset], (int)(expected.X * 255) - 2, (int)(expected.X * 255) + 2);
            Assert.InRange(frame.Rgba[offset + 1], (int)(expected.Y * 255) - 2, (int)(expected.Y * 255) + 2);
            Assert.InRange(frame.Rgba[offset + 2], (int)(expected.Z * 255) - 2, (int)(expected.Z * 255) + 2);
        }
    }

    [Fact]
    public void ChromeAppliesNativeStyleAlphaOnce()
    {
        using var host = new NativeTestContext();
        var theme = new MaterialTheme(Vector3.One, new MaterialColorSettings { Mode = MaterialColorMode.Lcars });
        var alphas = new List<uint>();
        void Draw()
        {
            using var palette = MaterialTheme.Push(theme);
            using var style = new MaterialStyleScope();
            style.Style(ImGuiStyleVar.Alpha, .5f);
            ImGui.SetNextWindowSize(new(650, 300));
            ImGui.Begin("LCARS alpha", ImGuiWindowFlags.NoSavedSettings);
            var list = ImGui.GetWindowDrawList(); var first = list.VtxBuffer.Size;
            MaterialLcars.Header("LCARS");
            alphas.Clear();
            var orange = ImGui.ColorConvertFloat4ToU32(MaterialLcars.Orange) & 0xFFFFFF;
            for (var i = first; i < list.VtxBuffer.Size; i++)
                if ((list.VtxBuffer[i].Col & 0xFFFFFF) == orange) alphas.Add(list.VtxBuffer[i].Col >> 24);
            ImGui.End();
        }
        host.Frame(Draw); host.Frame(Draw);
        Assert.NotEmpty(alphas);
        Assert.InRange(alphas.Max(), 127u, 128u);
    }
}
