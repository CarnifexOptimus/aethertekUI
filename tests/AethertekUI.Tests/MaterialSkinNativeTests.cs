using System.Numerics;
using Dalamud.Bindings.ImGui;
using Xunit;

namespace AethertekUI.Tests;

public sealed class MaterialSkinNativeTests
{
    [Fact]
    public void GlassRibbonsRespectStillMotionSpeedAndIndependentSavedWaves()
    {
        using var host = new NativeTestContext();
        var selected = new MaterialColorSettings { Mode = MaterialColorMode.Glassmorphism, GlassAnimation = false };
        var reducedMotion = false;
        var windowSize = new Vector2(900, 650);
        uint[] colors = [];
        int vertices = 0;
        void Draw()
        {
            var theme = new MaterialTheme(new Vector3(.3f, .7f, .4f), selected);
            theme.Motion.Enabled = !reducedMotion;
            using var scope = MaterialTheme.Push(theme);
            ImGui.SetNextWindowPos(new(0, 0)); ImGui.SetNextWindowSize(windowSize);
            using (MaterialSkin.BackdropStyle()) ImGui.Begin("Glass ribbons", ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoTitleBar);
            var list = ImGui.GetWindowDrawList();
            var first = list.VtxBuffer.Size;
            MaterialSkin.Backdrop();
            vertices = list.VtxBuffer.Size - first;
            colors = new uint[vertices];
            for (var i = 0; i < vertices; i++) colors[i] = list.VtxBuffer[first + i].Col;
            ImGui.End();
        }
        host.Frame(Draw); host.Frame(Draw);
        var still = colors;
        host.Frame(Draw);
        Assert.Equal(still, colors);
        Assert.InRange(vertices, 6144, 22528);
        Assert.True(colors.Distinct().Count() > 1500); // Smooth corner interpolation rather than solid circles.
        foreach (var wave in new[] { 1, 2, 3 })
        {
            selected = wave switch
            {
                1 => selected with { GlassWave1Rgb = 0xFF1828 },
                2 => selected with { GlassWave2Rgb = 0x31F020 },
                _ => selected with { GlassWave3Rgb = 0x3220FF },
            };
            host.Frame(Draw);
            Assert.False(still.SequenceEqual(colors));
            still = colors;
        }
        reducedMotion = true;
        selected = selected with { GlassAnimation = true };
        host.Frame(Draw);
        Assert.Equal(still, colors);
        reducedMotion = false;
        host.Frame(Draw);
        Assert.False(still.SequenceEqual(colors));
        var moving = colors;
        host.Frame(Draw);
        Assert.False(moving.SequenceEqual(colors));
        selected = selected with { GlassAnimationSpeed = 10 };
        host.Frame(Draw);
        Assert.True(colors.Zip(moving).Count(pair => pair.First != pair.Second) > vertices / 2);
        selected = selected with { GlassAnimation = false };
        host.Frame(Draw);
        Assert.Equal(still, colors);
        windowSize = new(8000, 5000);
        host.Frame(Draw);
        Assert.Equal(22528, vertices);
        AssertMeshIndices();
    }

    [Theory]
    [InlineData(MaterialDensity.Compact, 1f)]
    [InlineData(MaterialDensity.Compact, 1.5f)]
    [InlineData(MaterialDensity.Standard, 1f)]
    [InlineData(MaterialDensity.Standard, 1.5f)]
    public void SelectedMaterialsKeepActionsIdsGeometryAndNativeScopes(MaterialDensity density, float scale)
    {
        using var host = new NativeTestContext((int)(1000 * scale), (int)(750 * scale), scale);
        var mode = MaterialColorMode.Auto;
        var disabled = false;
        var actions = 0;
        var controlVertices = 0;
        var cardVertices = 0;
        uint id = 0;
        Vector2 min = default, max = default;
        void Draw()
        {
            var theme = new MaterialTheme(new Vector3(.3f, .7f, .4f), new MaterialColorSettings { Mode = mode }, density);
            using var palette = MaterialTheme.Push(theme, scale);
            ImGui.SetNextWindowPos(new(10, 10)); ImGui.SetNextWindowSize(new Vector2(800, 600) * scale);
            ImGui.Begin("Material actions", ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoTitleBar);
            var list = ImGui.GetWindowDrawList(); var first = list.VtxBuffer.Size;
            if (MaterialButton.FilledButton("run", "Run", size: new(160, 40), disabled: disabled)) actions++;
            controlVertices = list.VtxBuffer.Size - first;
            id = ImGui.GetCurrentContext().LastItemData.ID;
            min = ImGui.GetItemRectMin(); max = ImGui.GetItemRectMax();
            var style = ImGui.GetStyle();
            var rounding = style.FrameRounding;
            var border = style.FrameBorderSize;
            var button = style.Colors[(int)ImGuiCol.Button];
            var field = style.Colors[(int)ImGuiCol.FrameBg];
            using (MaterialSkin.NativeButtonStyle())
            {
                list.ChannelsSplit(2);
                try
                {
                    list.ChannelsSetCurrent(1);
                    using (MaterialSkin.TransparentButtonFace()) ImGui.Button("Retained action");
                    list.ChannelsSetCurrent(0);
                    MaterialSkin.PaintButton(ImGui.GetItemRectMin(), ImGui.GetItemRectMax());
                }
                finally { list.ChannelsMerge(); }
            }
            ImGui.SetNextItemWidth(200 * scale);
            var value = 5;
            using (MaterialSkin.NativeFieldStyle()) ImGui.InputInt("##field", ref value);
            Assert.Equal(rounding, style.FrameRounding);
            Assert.Equal(border, style.FrameBorderSize);
            Assert.Equal(button, style.Colors[(int)ImGuiCol.Button]);
            Assert.Equal(field, style.Colors[(int)ImGuiCol.FrameBg]);
            var beforeCard = list.VtxBuffer.Size;
            using (var card = MaterialSurface.Card("card", new(300, 90)))
                if (card.Visible)
                {
                    cardVertices = list.VtxBuffer.Size - beforeCard + ImGui.GetWindowDrawList().VtxBuffer.Size;
                    MaterialText.Text("Native child content");
                }
            ImGui.End();
        }
        host.Frame(Draw); host.Frame(Draw);
        var original = (id, min, max);
        var originalControlVertices = controlVertices;
        var originalCardVertices = cardVertices;
        foreach (var selected in Enum.GetValues<MaterialColorMode>())
        {
            mode = selected;
            host.Frame(Draw); host.Frame(Draw);
            Assert.Equal(original, (id, min, max));
            if (mode is MaterialColorMode.Glassmorphism or MaterialColorMode.Spatial)
            {
                Assert.True(controlVertices > originalControlVertices);
                Assert.True(cardVertices > originalCardVertices);
            }
            var before = actions;
            host.Click((min + max) * .5f, Draw);
            Assert.Equal(before + 1, actions);
            disabled = true;
            host.Click((min + max) * .5f, Draw);
            Assert.Equal(before + 1, actions);
            disabled = false;
            AssertMeshIndices();
        }
    }

    [Theory]
    [InlineData(MaterialColorMode.Glassmorphism)]
    [InlineData(MaterialColorMode.Spatial)]
    [InlineData(MaterialColorMode.Cyberpunk)]
    public void MaterialCardsPaintTheirOuterEdgesBehindNativeScrollbarsAndRetainedCaptions(MaterialColorMode mode)
    {
        using var host = new NativeTestContext(650, 420);
        var theme = new MaterialTheme(Vector3.UnitY, new MaterialColorSettings { Mode = mode }) { SurfaceOpacity = 1 };
        Vector2 captionMin = default, captionMax = default, cardMin = default, cardMax = default;
        Vector4 caption = default;
        var scrollbarWidth = 0f;
        void Draw()
        {
            using var scope = MaterialTheme.Push(theme);
            ImGui.SetNextWindowPos(new(0, 0)); ImGui.SetNextWindowSize(new(600, 390));
            ImGui.Begin("Material ordering", ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoTitleBar);
            var list = ImGui.GetWindowDrawList();
            using (MaterialSkin.NativeButtonStyle())
            {
                caption = ImGui.GetStyle().Colors[(int)ImGuiCol.Text];
                list.ChannelsSplit(2);
                try
                {
                    list.ChannelsSetCurrent(1);
                    using (MaterialSkin.TransparentButtonFace()) ImGui.Button("Retained action", new(180, 36));
                    var textSize = ImGui.CalcTextSize("Retained action");
                    captionMin = ImGui.GetItemRectMin() + (ImGui.GetItemRectSize() - textSize) * .5f;
                    captionMax = captionMin + textSize;
                    list.ChannelsSetCurrent(0);
                    MaterialSkin.PaintButton(ImGui.GetItemRectMin(), ImGui.GetItemRectMax());
                }
                finally { list.ChannelsMerge(); }
            }
            using (var card = MaterialSurface.Begin("scrolling-card", new(300, 180), MaterialElevation.Low,
                       outline: true, radius: 0, flags: ImGuiWindowFlags.AlwaysVerticalScrollbar))
            {
                cardMin = ImGui.GetWindowPos(); cardMax = cardMin + ImGui.GetWindowSize();
                scrollbarWidth = ImGui.GetStyle().ScrollbarSize;
                if (card.Visible)
                    for (var row = 0; row < 30; row++) ImGui.TextUnformatted("Scrollable native content");
            }
            ImGui.End();
        }
        host.Frame(Draw); host.Frame(Draw); host.Frame(Draw);
        var image = SoftwarePreview.Capture(host);
        Assert.True(CountInk(captionMin, captionMax, caption) > 5); // A face painted over the native text fails this.
        Assert.True(CountInk(new(cardMax.X - scrollbarWidth, cardMin.Y), cardMax, theme.Colors.OutlineVariant) > 5);
        var expectedFill = mode == MaterialColorMode.Glassmorphism
            ? Vector4.Lerp(theme.Colors.Background, theme.Colors.SurfaceContainerLow, .42f)
            : theme.Colors.SurfaceContainerLow;
        Assert.True(CountInk(cardMin + new Vector2(4, 2), cardMin + new Vector2(9, 3), expectedFill) >= 4);

        int CountInk(Vector2 min, Vector2 max, Vector4 expected)
        {
            var count = 0;
            for (var y = Math.Max(0, (int)min.Y); y < Math.Min(image.Height, (int)MathF.Ceiling(max.Y)); y++)
                for (var x = Math.Max(0, (int)min.X); x < Math.Min(image.Width, (int)MathF.Ceiling(max.X)); x++)
                {
                    var offset = (y * image.Width + x) * 4;
                    if (Math.Abs(image.Rgba[offset] - expected.X * 255) <= 8
                        && Math.Abs(image.Rgba[offset + 1] - expected.Y * 255) <= 8
                        && Math.Abs(image.Rgba[offset + 2] - expected.Z * 255) <= 8) count++;
                }
            return count;
        }
    }

    [Theory]
    [InlineData(MaterialColorMode.Glassmorphism)]
    [InlineData(MaterialColorMode.Spatial)]
    [InlineData(MaterialColorMode.Cyberpunk)]
    public void MaterialBackgroundFillsRespectSurfaceOpacityAndApplyNativeAlphaOnce(MaterialColorMode mode)
    {
        using var host = new NativeTestContext();
        var theme = new MaterialTheme(Vector3.UnitY, new MaterialColorSettings { Mode = mode }) { SurfaceOpacity = .4f };
        var backdropAlpha = new List<uint>();
        var panelAlpha = new List<uint>();
        void Draw()
        {
            using var scope = MaterialTheme.Push(theme);
            using var opacity = new MaterialStyleScope(); opacity.Style(ImGuiStyleVar.Alpha, .5f);
            ImGui.SetNextWindowSize(new(600, 400));
            var nativeBackground = ImGui.GetStyle().Colors[(int)ImGuiCol.WindowBg];
            using (MaterialSkin.BackdropStyle()) ImGui.Begin("Material surface alpha", ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoTitleBar);
            Assert.Equal(nativeBackground, ImGui.GetStyle().Colors[(int)ImGuiCol.WindowBg]);
            var list = ImGui.GetWindowDrawList(); var first = list.VtxBuffer.Size;
            MaterialSkin.Backdrop();
            backdropAlpha.Clear();
            for (var i = first; i < list.VtxBuffer.Size; i++)
                if (list.VtxBuffer[i].Col >> 24 > 0) backdropAlpha.Add(list.VtxBuffer[i].Col >> 24);
            first = list.VtxBuffer.Size;
            var min = ImGui.GetCursorScreenPos();
            MaterialSkin.PanelFrame(min, min + new Vector2(250, 100));
            var fill = ImGui.ColorConvertFloat4ToU32(theme.Colors.SurfaceContainerLow) & 0xFFFFFF;
            panelAlpha.Clear();
            for (var i = first; i < list.VtxBuffer.Size; i++)
                if ((list.VtxBuffer[i].Col & 0xFFFFFF) == fill && list.VtxBuffer[i].Col >> 24 > 0)
                    panelAlpha.Add(list.VtxBuffer[i].Col >> 24);
            ImGui.End();
        }
        host.Frame(Draw); host.Frame(Draw);
        Assert.NotEmpty(backdropAlpha); Assert.NotEmpty(panelAlpha);
        Assert.All(backdropAlpha, value => Assert.InRange(value, 50u, 52u));
        var expected = 255 * .5f * .4f * (mode == MaterialColorMode.Glassmorphism ? .42f : 1);
        Assert.InRange(panelAlpha.Max(), (uint)expected, (uint)expected + 1);
        var partial = BodyPixel();
        theme.SurfaceOpacity = 0; host.Frame(Draw);
        var clear = BodyPixel();
        Assert.Equal(new byte[] { 26, 29, 33 }, clear);
        theme.SurfaceOpacity = 1; host.Frame(Draw);
        var full = BodyPixel();
        for (var channel = 0; channel < 3; channel++)
            Assert.InRange(partial[channel], clear[channel] + (full[channel] - clear[channel]) * .4f - 2,
                clear[channel] + (full[channel] - clear[channel]) * .4f + 2);

        byte[] BodyPixel()
        {
            var frame = SoftwarePreview.Capture(host);
            var offset = (150 * frame.Width + 450) * 4;
            return [frame.Rgba[offset], frame.Rgba[offset + 1], frame.Rgba[offset + 2]];
        }
    }

    [Fact]
    public void PopupBackdropStyleEndsAfterBeginAndNestedNativePopupsKeepTheirBackground()
    {
        using var host = new NativeTestContext();
        var theme = new MaterialTheme(Vector3.UnitY, new MaterialColorSettings { Mode = MaterialColorMode.Glassmorphism });
        var opening = true;
        var nestedOpening = true;
        var nativeBackgroundVertices = 0;
        void Draw()
        {
            using var themeScope = MaterialTheme.Push(theme);
            ImGui.SetNextWindowSize(new(600, 400));
            ImGui.Begin("Popup material owner", ImGuiWindowFlags.NoSavedSettings);
            if (opening) { ImGui.OpenPopup("appearance"); opening = false; }
            var popupColor = ImGui.GetStyle().Colors[(int)ImGuiCol.PopupBg];
            bool visible;
            using (MaterialSkin.BackdropStyle(popup: true)) visible = ImGui.BeginPopup("appearance");
            Assert.Equal(popupColor, ImGui.GetStyle().Colors[(int)ImGuiCol.PopupBg]);
            if (visible)
            {
                MaterialSkin.Backdrop();
                ImGui.TextUnformatted("Appearance");
                if (nestedOpening) { ImGui.OpenPopup("native-colour"); nestedOpening = false; }
                if (ImGui.BeginPopup("native-colour"))
                {
                    var list = ImGui.GetWindowDrawList();
                    var packed = ImGui.GetColorU32(ImGuiCol.PopupBg);
                    nativeBackgroundVertices = 0;
                    for (var i = 0; i < list.VtxBuffer.Size; i++)
                        if (list.VtxBuffer[i].Col == packed && packed >> 24 > 0) nativeBackgroundVertices++;
                    ImGui.TextUnformatted("Native nested picker");
                    ImGui.EndPopup();
                }
                ImGui.EndPopup();
            }
            ImGui.End();
        }
        host.Frame(Draw); host.Frame(Draw); host.Frame(Draw); host.Frame(Draw);
        Assert.True(nativeBackgroundVertices >= 4);
    }

    [Theory]
    [InlineData(MaterialColorMode.Glassmorphism, 1f)]
    [InlineData(MaterialColorMode.Glassmorphism, 1.5f)]
    [InlineData(MaterialColorMode.Spatial, 1f)]
    [InlineData(MaterialColorMode.Spatial, 1.5f)]
    [InlineData(MaterialColorMode.Cyberpunk, 1f)]
    [InlineData(MaterialColorMode.Cyberpunk, 1.5f)]
    public unsafe void BackdropsFillPaddingBehindNativeChromeAndKeepTheirMaterialsThroughOwnedCornerPainting(MaterialColorMode mode, float scale)
    {
        using var host = new NativeTestContext((int)(700 * scale), (int)(520 * scale), scale);
        var theme = new MaterialTheme(Vector3.UnitY, new MaterialColorSettings { Mode = mode, GlassAnimation = false,
            GlassWave1Rgb = 0xFF1818, GlassWave2Rgb = 0x1820FF, GlassWave3Rgb = 0x18FF20 }) { SurfaceOpacity = 1 };
        var decorations = new MaterialWindowDecorations();
        var fold = new MaterialWindowFold();
        var owned = new ImGuiSizeCallback(_ => ImGuiP.GetCurrentWindow().Handle->ViewportOwned = 1);
        var noTitle = true;
        var square = false;
        var noBackground = false;
        var presses = 0;
        uint actionId = 0;
        Vector2 action = default, min = default, max = default;
        var scrollbarWidth = 0f;
        var titleHeight = 0f;
        var titleColor = new Vector4(.8f, .2f, .1f, 1);
        void Draw()
        {
            using var themeScope = MaterialTheme.Push(theme, scale);
            using var chrome = MaterialWindowChrome.Push();
            using var ink = new MaterialStyleScope();
            if (square) ink.Style(ImGuiStyleVar.WindowRounding, 0);
            ink.Color(ImGuiCol.ScrollbarGrab, new(1, 1, 0, 1));
            ink.Color(ImGuiCol.TitleBg, titleColor); ink.Color(ImGuiCol.TitleBgActive, titleColor);
            ImGui.SetNextWindowSizeConstraints(new(100), new(1000), owned);
            fold.PreDraw("Material backdrop owner", null, null, true, decorations.Prepare);
            ImGui.SetNextWindowPos(Vector2.Zero);
            ImGui.SetNextWindowSize(new Vector2(500, 350) * scale);
            var flags = ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.AlwaysVerticalScrollbar;
            if (noTitle) flags |= ImGuiWindowFlags.NoTitleBar;
            if (noBackground) flags |= ImGuiWindowFlags.NoBackground;
            bool visible;
            using (MaterialSkin.BackdropStyle()) visible = ImGui.Begin("Material backdrop owner", flags);
            min = ImGui.GetWindowPos(); max = min + ImGui.GetWindowSize();
            scrollbarWidth = ImGui.GetStyle().ScrollbarSize;
            titleHeight = ImGuiP.TitleBarHeight(ImGuiP.GetCurrentWindow());
            if (visible)
            {
                MaterialSkin.Backdrop();
                decorations.Paint();
                if (ImGui.BeginTable("native-table", 2))
                {
                    ImGui.TableNextRow(); ImGui.TableNextColumn();
                    if (ImGui.Button("Retained native action")) presses++;
                    actionId = ImGui.GetCurrentContext().LastItemData.ID;
                    action = (ImGui.GetItemRectMin() + ImGui.GetItemRectMax()) * .5f;
                    ImGui.TableNextColumn(); ImGui.TextUnformatted("Native table content");
                    ImGui.EndTable();
                }
                ImGui.Dummy(new Vector2(100, 1000) * scale);
            }
            ImGui.End();
            decorations.Paint(); fold.PostDraw();
        }
        host.Frame(Draw); host.Frame(Draw); host.Frame(Draw);
        var image = SoftwarePreview.Capture(host);
        Assert.Equal(new byte[] { 26, 29, 33 }, Pixel(min + Vector2.One * scale));
        Assert.NotEqual(new byte[] { 26, 29, 33 }, Pixel(new(min.X + 4 * scale, (min.Y + max.Y) * .5f)));
        var colors = new HashSet<uint>();
        for (var y = 1; y < 7; y++)
            for (var x = 1; x < 7; x++)
            {
                var rgb = Pixel(min + (max - min) * new Vector2(x / 8f, y / 8f));
                colors.Add((uint)(rgb[0] << 16 | rgb[1] << 8 | rgb[2]));
            }
        if (mode == MaterialColorMode.Glassmorphism) Assert.True(colors.Count > 20);
        else if (mode == MaterialColorMode.Spatial) Assert.True(colors.Count > 5);
        var yellow = 0;
        for (var y = (int)(min.Y + 8 * scale); y < max.Y - 8 * scale; y++)
            for (var x = (int)(max.X - scrollbarWidth + 3 * scale); x < max.X - 3 * scale; x++)
            {
                var rgb = Pixel(new(x, y));
                if (rgb[0] > 250 && rgb[1] > 250 && rgb[2] < 5) yellow++;
            }
        Assert.True(yellow > 5);
        var originalId = actionId;
        host.Click(action, Draw);
        Assert.Equal(1, presses); Assert.Equal(originalId, actionId);
        noTitle = false;
        host.Frame(Draw); host.Frame(Draw);
        image = SoftwarePreview.Capture(host);
        var title = Pixel(min + new Vector2((max.X - min.X) * .8f, titleHeight * .5f));
        Assert.InRange(title[0], 202, 206); Assert.InRange(title[1], 49, 53); Assert.InRange(title[2], 24, 28);
        noTitle = true; square = true;
        host.Frame(Draw); host.Frame(Draw);
        image = SoftwarePreview.Capture(host);
        Assert.False(Pixel(min + new Vector2(2 * scale)).SequenceEqual(new byte[] { 26, 29, 33 }));
        noBackground = true;
        host.Frame(Draw); host.Frame(Draw);
        image = SoftwarePreview.Capture(host);
        Assert.Equal(new byte[] { 26, 29, 33 }, Pixel(new(min.X + 4 * scale, (min.Y + max.Y) * .5f)));
        AssertMeshIndices();

        byte[] Pixel(Vector2 point)
        {
            var offset = ((int)point.Y * image.Width + (int)point.X) * 4;
            return [image.Rgba[offset], image.Rgba[offset + 1], image.Rgba[offset + 2]];
        }
    }

    [Fact]
    public unsafe void BackdropRejectsAnActiveCallerSplitterWithoutChangingItsChannels()
    {
        using var host = new NativeTestContext();
        var theme = new MaterialTheme(Vector3.UnitY, new MaterialColorSettings { Mode = MaterialColorMode.Glassmorphism });
        host.Frame(() =>
        {
            using var scope = MaterialTheme.Push(theme);
            ImGui.SetNextWindowSize(new(500, 350));
            using (MaterialSkin.BackdropStyle()) ImGui.Begin("Existing caller channels", ImGuiWindowFlags.NoSavedSettings);
            var list = ImGui.GetWindowDrawList();
            list.ChannelsSplit(2);
            try
            {
                list.ChannelsSetCurrent(1);
                Assert.Throws<InvalidOperationException>(() => MaterialSkin.Backdrop());
                Assert.Equal(2, list.Splitter.Count);
                Assert.Equal(1, list.Splitter.Current);
                ImGui.TextUnformatted("Retained caller channel");
            }
            finally { list.ChannelsMerge(); }
            ImGui.End();
        });
        AssertMeshIndices();
    }

    [Theory]
    [InlineData(MaterialColorMode.Glassmorphism)]
    [InlineData(MaterialColorMode.Spatial)]
    [InlineData(MaterialColorMode.Cyberpunk)]
    public void ExplicitSemanticFillAndDisabledAlphaSurviveTheNativeHitScope(MaterialColorMode mode)
    {
        using var host = new NativeTestContext();
        var theme = new MaterialTheme(new Vector3(.3f, .7f, .4f), new MaterialColorSettings { Mode = mode });
        var appearance = new MaterialControlAppearance(new(.91f, .12f, .08f, .8f), new(.9f, .95f, 1, 1), new(.12f, .88f, .15f, 1), 0);
        var fillAlphas = new List<uint>();
        var borderAlphas = new List<uint>();
        float expectedAlpha = 0;
        void Draw()
        {
            using var palette = MaterialTheme.Push(theme);
            using var style = new MaterialStyleScope();
            style.Style(ImGuiStyleVar.Alpha, .5f);
            ImGui.SetNextWindowSize(new(600, 400));
            ImGui.Begin("Semantic material", ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoTitleBar);
            var list = ImGui.GetWindowDrawList(); var first = list.VtxBuffer.Size;
            expectedAlpha = .5f * ImGui.GetStyle().DisabledAlpha;
            MaterialButton.Draw("error", "Error", appearance, new(160, 40), disabled: true);
            Assert.Equal(.5f, ImGui.GetStyle().Alpha);
            var min = ImGui.GetItemRectMin(); var max = ImGui.GetItemRectMax();
            var fill = ImGui.ColorConvertFloat4ToU32(appearance.Fill) & 0xFFFFFF;
            var edge = ImGui.ColorConvertFloat4ToU32(appearance.Border) & 0xFFFFFF;
            var facePoints = new List<Vector2>();
            fillAlphas.Clear(); borderAlphas.Clear();
            for (var i = first; i < list.VtxBuffer.Size; i++)
            {
                var vertex = list.VtxBuffer[i];
                if ((vertex.Col & 0xFFFFFF) == fill)
                {
                    fillAlphas.Add(vertex.Col >> 24);
                    facePoints.Add(vertex.Pos);
                    Assert.InRange(vertex.Pos.X, min.X, max.X);
                    Assert.InRange(vertex.Pos.Y, min.Y, max.Y);
                }
                if ((vertex.Col & 0xFFFFFF) == edge) borderAlphas.Add(vertex.Col >> 24);
            }
            Assert.Contains(min, facePoints); Assert.Contains(max, facePoints);
            ImGui.End();
        }
        host.Frame(Draw); host.Frame(Draw);
        Assert.NotEmpty(fillAlphas); Assert.NotEmpty(borderAlphas);
        Assert.InRange(fillAlphas.Max(), (uint)(255 * .8f * expectedAlpha), (uint)(255 * .8f * expectedAlpha) + 1);
        Assert.InRange(borderAlphas.Max(), (uint)(255 * expectedAlpha), (uint)(255 * expectedAlpha) + 1);
    }

    [Fact]
    public void LcarsChromeUsesOnlyTheThreeSavedBarsAndReadableCaptionInk()
    {
        using var host = new NativeTestContext();
        var settings = new MaterialColorSettings { Mode = MaterialColorMode.Lcars, LcarsBackgroundRgb = 0xE0E9ED,
            LcarsBar1Rgb = 0x13272E, LcarsBar2Rgb = 0xF0D720, LcarsBar3Rgb = 0x8E15C0 };
        var theme = new MaterialTheme(Vector3.One, settings);
        var colors = new HashSet<uint>();
        void Draw()
        {
            using var palette = MaterialTheme.Push(theme);
            ImGui.SetNextWindowSize(new(700, 500));
            ImGui.Begin("Saved LCARS bars", ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoTitleBar);
            var list = ImGui.GetWindowDrawList(); var first = list.VtxBuffer.Size;
            MaterialLcars.Header("Operator");
            for (var i = 0; i < 4; i++) MaterialLcars.PanelHeading("Panel", i);
            MaterialLcars.Footer("Ready");
            colors.Clear();
            for (var i = first; i < list.VtxBuffer.Size; i++) colors.Add(list.VtxBuffer[i].Col & 0xFFFFFF);
            Assert.Equal(theme.Colors.Primary, MaterialLcars.Orange);
            Assert.Equal(theme.Colors.Secondary, MaterialLcars.Lilac);
            Assert.Equal(theme.Colors.Tertiary, MaterialLcars.Periwinkle);
            Assert.Equal(MaterialLcars.Accent(0), MaterialLcars.Accent(3));
            ImGui.End();
        }
        host.Frame(Draw); host.Frame(Draw);
        foreach (var bar in new[] { theme.Colors.Primary, theme.Colors.Secondary, theme.Colors.Tertiary })
            Assert.Contains(ImGui.ColorConvertFloat4ToU32(bar) & 0xFFFFFF, colors);
        Assert.Contains(0xFFFFFFu, colors); Assert.Contains(0u, colors);
        Assert.DoesNotContain(ImGui.ColorConvertFloat4ToU32(MaterialLcars.Amber) & 0xFFFFFF, colors);
        Assert.NotEqual(theme.Colors.Primary, MaterialLcars.Orange); // Default preview outside the selected theme.
    }

    private static unsafe void AssertMeshIndices()
    {
        var data = ImGui.GetDrawData();
        for (var li = 0; li < data.CmdListsCount; li++)
        {
            var list = new ImDrawListPtr(data.CmdLists[li]);
            for (var ci = 0; ci < list.CmdBuffer.Size; ci++)
            {
                var command = list.CmdBuffer[ci];
                Assert.True(command.IdxOffset + command.ElemCount <= list.IdxBuffer.Size);
                for (var i = command.IdxOffset; i < command.IdxOffset + command.ElemCount; i++)
                    Assert.True(list.IdxBuffer[(int)i] + command.VtxOffset < list.VtxBuffer.Size);
            }
        }
    }
}
