using System.Numerics;
using Dalamud.Bindings.ImGui;
using Xunit;

namespace AethertekUI.Tests;

public unsafe class MaterialTextConsumerTests
{
    [Fact]
    public void CaptionReplacementPreservesNativeEditorIdentityAndIdenticalUserGlyphs()
    {
        using var host = new NativeTestContext();
        var value = "Name";
        host.Frame(() =>
        {
            ImGui.SetNextWindowSize(new(600, 200));
            ImGui.Begin("Caption bounds", ImGuiWindowFlags.NoSavedSettings);
            ImGui.SetNextItemWidth(180);
            var list = ImGui.GetWindowDrawList();
            var first = list.VtxBuffer.Size;
            var font = ImGui.GetFont();
            var origin = ImGui.GetCursorScreenPos();
            var width = ImGui.CalcItemWidth();
            var expectedId = ImGui.GetID("Name##retained-editor");
            ImGui.InputText("Name##retained-editor", ref value, 100);
            Assert.Equal(expectedId, ImGui.GetCurrentContext().LastItemData.ID);
            var original = Enumerable.Range(first, list.VtxBuffer.Size - first)
                .Select(index => list.VtxBuffer[index]).ToArray();
            var captionMin = origin + new Vector2(width + ImGui.GetStyle().ItemInnerSpacing.X, 0);
            var captionMax = new Vector2(captionMin.X + MaterialText.Measure("Translated name").X, ImGui.GetItemRectMax().Y);
            MaterialText.ReplaceNativeCaption(list, first, font, "Name##retained-editor", captionMin, captionMax,
                captionMin + new Vector2(0, ImGui.GetStyle().FramePadding.Y), "Translated name", ImGui.GetColorU32(ImGuiCol.Text));
            var changedCaptionVertices = 0;
            for (var index = 0; index < original.Length; index++)
            {
                var current = list.VtxBuffer[first + index];
                Assert.Equal(original[index].Pos, current.Pos);
                Assert.Equal(original[index].Uv, current.Uv);
                if (original[index].Pos.X < captionMin.X) Assert.Equal(original[index].Col, current.Col);
                else if (original[index].Col != current.Col) changedCaptionVertices++;
            }
            Assert.True(changedCaptionVertices > 0);
            Assert.Equal("Name", value);
            Assert.Equal(expectedId, ImGui.GetCurrentContext().LastItemData.ID);
            Assert.Equal(font, ImGui.GetFont());
            ImGui.End();
        });
    }
    [Fact]
    public void MissingRequestedFamilyUsesNativeHindiFallbackAndKeepsGlyphValidation()
    {
        using var layout = new MaterialTextLayout("\u0915\u094d\u0937\u0947\u0924\u094d\u0930", 32,
            fontFamily: "AethertekUI deliberately absent diagnostic family");
        layout.CheckGlyphs();
        Assert.True(layout.Size.X > 0);
        Assert.Contains(layout.Rasterize().Rgba, value => value != 0);
        using var renderer = new MaterialTextRenderer(_ => throw new Xunit.Sdk.XunitException("Optional probe uploaded texture."),
            fontFamily: "AethertekUI deliberately absent diagnostic family");
        Assert.True(renderer.TryCheckGlyphs(["\u0939\u093f\u0928\u094d\u0926\u0940"], 24, out var error), error);
        Assert.False(renderer.TryCheckGlyphs(["\U00011B00"], 24, out error));
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void AsciiUnavailableCaptionRetainsOriginalHindiOptionIdentityAndDisabledInput()
    {
        using var host = new NativeTestContext();
        Vector2 center = default;
        var disabled = true;
        var clicks = 0;
        void Draw()
        {
            ImGui.SetNextWindowSize(new(500, 200));
            ImGui.Begin("Unavailable language", ImGuiWindowFlags.NoSavedSettings);
            var id = ImGui.GetID("\u0939\u093f\u0928\u094d\u0926\u0940###language-hi");
            ImGui.BeginDisabled(disabled);
            var list = ImGui.GetWindowDrawList();
            var first = list.VtxBuffer.Size;
            if (MaterialText.Selectable("\u0939\u093f\u0928\u094d\u0926\u0940###language-hi", display: "Hindi (unavailable)")) clicks++;
            Assert.Equal(id, ImGui.GetCurrentContext().LastItemData.ID);
            center = (ImGui.GetItemRectMin() + ImGui.GetItemRectMax()) * .5f;
            Assert.True(list.VtxBuffer.Size - first >= 4 * "Hindi (unavailable)".Count(c => c != ' '));
            ImGui.EndDisabled();
            ImGui.End();
        }
        host.Frame(Draw); host.Frame(Draw); host.Click(center, Draw);
        Assert.Equal(0, clicks);
        disabled = false; host.Frame(Draw); host.Click(center, Draw);
        Assert.Equal(1, clicks);
    }

    [Fact]
    public void ShapedCoverageRejectsMissingGlyphsWithoutAllocatingHostTextures()
    {
        using var renderer = new MaterialTextRenderer(_ => throw new Xunit.Sdk.XunitException("Coverage validation uploaded a texture."));
        renderer.CheckGlyphs(new[] { "हिन्दी", "क्षेत्र", "प्रार्थना", "अनुवाद\r\nखोजें", "English" }, 24);
        using var missing = new MaterialTextLayout("\u0378", 24);
        Assert.Throws<InvalidOperationException>(() => missing.CheckGlyphs());
    }

    [Fact]
    public void NativeGlyphFilteringRetainsOtherRunesAndRemovesSupplementaryDevanagari()
    {
        Assert.Equal("Latin🙂中", MaterialText.NativeGlyphText("Latinह🙂\U00011B00中"));
        Assert.Equal("Latin🙂中", MaterialText.NativeGlyphText("Latin🙂中"));
    }

    [Fact]
    public void HiddenPreviewAndNoPreviewRetainNativeGeometryWithoutUploadingText()
    {
        using var host = new NativeTestContext();
        using var renderer = new MaterialTextRenderer(_ => throw new Xunit.Sdk.XunitException("Hidden preview uploaded shaped text."));
        var wrapped = false;
        Vector2 original = default, actual = default;
        void Draw()
        {
            using var shaping = wrapped ? renderer.Push() : null;
            ImGui.SetNextWindowPos(Vector2.Zero); ImGui.SetNextWindowSize(new(600, 400));
            ImGui.Begin("Hidden preview", ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoTitleBar);
            var id = ImGui.GetID("retained-combo");
            if (MaterialText.BeginCombo("retained-combo", "हिन्दी", ImGuiComboFlags.NoPreview)) ImGui.EndCombo();
            Assert.Equal(id, ImGui.GetCurrentContext().LastItemData.ID);
            if (wrapped) actual = ImGui.GetItemRectSize(); else original = ImGui.GetItemRectSize();
            if (MaterialText.BeginCombo("retained-hidden-combo", "English##हिन्दी")) ImGui.EndCombo();
            ImGui.End();
        }
        host.Frame(Draw); host.Frame(Draw); wrapped = true; host.Frame(Draw);
        Assert.Equal(original, actual);
    }

    [Fact]
    public void ComboShapesItsVisibleNativeLabelAndPreviewWithHiddenIdentitySuffixes()
    {
        using var host = new NativeTestContext();
        var uploads = 0;
        using var renderer = new MaterialTextRenderer(bitmap =>
        {
            uploads++;
            var texture = host.RegisterTexture(bitmap.Rgba, bitmap.Width, bitmap.Height);
            return new(texture.Id, texture);
        });
        host.Frame(() =>
        {
            using var shaping = renderer.Push();
            ImGui.SetNextWindowSize(new(600, 400));
            ImGui.Begin("Combo label", ImGuiWindowFlags.NoSavedSettings);
            var id = ImGui.GetID("सेटिंग्स###retained-label");
            if (MaterialText.BeginCombo("सेटिंग्स###retained-label", "खोजें##hidden")) ImGui.EndCombo();
            Assert.Equal(id, ImGui.GetCurrentContext().LastItemData.ID);
            Assert.True(ImGui.GetItemRectSize().Y >= MaterialText.Measure("सेटिंग्स").Y);
            ImGui.End();
        });
        Assert.Equal(2, uploads);
        SoftwarePreview.Capture(host);
    }

    [Fact]
    public void ComboUploadFailureRestoresAnAlreadyOpenPopupAndItsCallerStacks()
    {
        using var host = new NativeTestContext();
        using var renderer = new MaterialTextRenderer(_ => throw new InvalidOperationException("Selected upload failure."));
        var fail = false;
        Vector2 center = default;
        void Draw()
        {
            using var shaping = renderer.Push();
            ImGui.SetNextWindowPos(Vector2.Zero); ImGui.SetNextWindowSize(new(600, 400));
            ImGui.Begin("Combo failure", ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoTitleBar);
            var context = ImGui.GetCurrentContext();
            var windows = context.CurrentWindowStack.Size;
            var popups = context.BeginPopupStack.Size;
            var styles = context.StyleVarStack.Size;
            if (fail)
            {
                Assert.Throws<InvalidOperationException>(() => MaterialText.BeginCombo("retained-combo", "हिन्दी"));
                Assert.Equal(windows, context.CurrentWindowStack.Size);
                Assert.Equal(popups, context.BeginPopupStack.Size);
                Assert.Equal(styles, context.StyleVarStack.Size);
            }
            else if (MaterialText.BeginCombo("retained-combo", "English")) ImGui.EndCombo();
            else center = (ImGui.GetItemRectMin() + ImGui.GetItemRectMax()) * .5f;
            ImGui.End();
        }
        host.Frame(Draw); host.Frame(Draw); host.Click(center, Draw);
        fail = true; host.Frame(Draw);
    }

    [Theory]
    [InlineData(MaterialDensity.Standard, 1f)]
    [InlineData(MaterialDensity.Compact, 1f)]
    [InlineData(MaterialDensity.Standard, 1.5f)]
    [InlineData(MaterialDensity.Compact, 1.5f)]
    public void LanguageSelectorShapesHindiInAnEnglishFrameAndRetainsNativeActions(MaterialDensity density, float scale)
    {
        using var host = new NativeTestContext(700, 500, scale);
        using var renderer = Renderer(host);
        var theme = new MaterialTheme(new Vector3(.3f, .7f, .4f), density);
        var preview = "English";
        Vector2 comboMin = default, comboMax = default, optionCenter = default;
        uint comboId = 0, optionId = 0;
        var opened = false;
        var optionVisible = false;
        void Draw()
        {
            using var palette = MaterialTheme.Push(theme, scale);
            using var shaping = renderer.Push();
            ImGui.SetNextWindowPos(Vector2.Zero); ImGui.SetNextWindowSize(new(700, 500));
            ImGui.Begin("English language frame", ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoTitleBar);
            ImGui.SetNextItemWidth(280 * scale);
            var expected = ImGui.GetID("original-language");
            opened = MaterialText.BeginCombo("original-language", preview);
            if (!opened)
            {
                Assert.Equal(expected, ImGui.GetCurrentContext().LastItemData.ID);
                comboId = expected;
                comboMin = ImGui.GetItemRectMin(); comboMax = ImGui.GetItemRectMax();
            }
            else
            {
                var expectedOption = ImGui.GetID("Hindi###retained-option");
                var chosen = MaterialText.Selectable("Hindi###retained-option", preview == "हिन्दी", display: "हिन्दी");
                Assert.Equal(expectedOption, ImGui.GetCurrentContext().LastItemData.ID);
                optionId = expectedOption;
                optionVisible = ImGui.IsItemVisible();
                optionCenter = (ImGui.GetItemRectMin() + ImGui.GetItemRectMax()) * .5f;
                Assert.True(ImGui.GetItemRectMax().Y - ImGui.GetItemRectMin().Y >= renderer.GetLayout("हिन्दी", ImGui.GetFontSize()).Size.Y);
                if (chosen) preview = "हिन्दी";
                ImGui.EndCombo();
            }
            ImGui.End();
        }
        host.Frame(Draw); host.Frame(Draw);
        host.Click((comboMin + comboMax) * .5f, Draw);
        host.Frame(Draw);
        Assert.True(opened); Assert.True(optionVisible); Assert.NotEqual(0u, optionId);
        host.Click(optionCenter, Draw);
        host.Frame(Draw);
        Assert.Equal("हिन्दी", preview);
        Assert.NotEqual(0u, comboId);
        Assert.True(comboMax.Y - comboMin.Y >= renderer.GetLayout("हिन्दी", ImGui.GetFontSize()).Size.Y);
        SoftwarePreview.Capture(host);
    }

    [Fact]
    public void ExposedEditorRetainsTheConsumersEmptyLabelOverrideIdentity()
    {
        using var host = new NativeTestContext();
        using var renderer = Renderer(host);
        var value = "कि";
        Vector2 center = default;
        uint id = 0;
        void Draw()
        {
            using var shaping = renderer.Push();
            ImGui.Begin("Consumer editor", ImGuiWindowFlags.NoSavedSettings);
            id = ImGui.GetID("Search duties");
            ImGuiP.PushOverrideID(id);
            try
            {
                ImGui.SetNextItemWidth(250);
                using var height = MaterialText.PushLineHeight(value, "खोजें");
                MaterialShapedInput.SingleLine("", "खोजें", ref value, 1024);
                Assert.Equal(id, ImGui.GetCurrentContext().LastItemData.ID);
                Assert.True(ImGui.GetItemRectMax().Y - ImGui.GetItemRectMin().Y >= renderer.GetLayout(value, ImGui.GetFontSize()).Size.Y);
                center = (ImGui.GetItemRectMin() + ImGui.GetItemRectMax()) * .5f;
            }
            finally { ImGui.PopID(); }
            ImGui.End();
        }
        host.Frame(Draw); host.Frame(Draw);
        host.Click(center, Draw);
        Assert.Equal(id, ImGui.GetCurrentContext().InputTextState.ID);
        ImGui.GetIO().AddInputCharacter('x'); host.Frame(Draw);
        Assert.Contains('x', value);
    }

    [Fact]
    public void AuthoredWrappedTextUsesTheRetainedNativeWrapLimit()
    {
        using var host = new NativeTestContext();
        using var renderer = Renderer(host);
        host.Frame(() =>
        {
            using var shaping = renderer.Push();
            ImGui.Begin("Wrapped consumer", ImGuiWindowFlags.NoSavedSettings);
            const string text = "अनुवाद हिन्दी सेटिंग्स खोजें अनुवाद हिन्दी";
            var width = 85f;
            ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + width);
            MaterialText.Text(text);
            ImGui.PopTextWrapPos();
            var expected = renderer.GetLayout(text, ImGui.GetFontSize(), width).Size;
            Assert.InRange(ImGui.GetItemRectMax().Y - ImGui.GetItemRectMin().Y, expected.Y - 1, expected.Y + 1);
            Assert.True(expected.Y > renderer.GetLayout(text, ImGui.GetFontSize()).Size.Y);
            ImGui.End();
        });
        SoftwarePreview.Capture(host);
    }

    [Fact]
    public void EnglishNativeTextHelpersRetainTheirGeometryUnderTheRendererScope()
    {
        using var host = new NativeTestContext();
        using var renderer = new MaterialTextRenderer(_ => throw new Xunit.Sdk.XunitException("English uploaded shaped text."));
        var wrapped = false;
        var original = new List<(Vector2 Min, Vector2 Max, uint Id)>();
        var actual = new List<(Vector2 Min, Vector2 Max, uint Id)>();
        void Draw()
        {
            using var shaping = wrapped ? renderer.Push() : null;
            ImGui.SetNextWindowPos(Vector2.Zero); ImGui.SetNextWindowSize(new(600, 400));
            ImGui.Begin("English native helpers", ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoTitleBar);
            var result = wrapped ? actual : original;
            ImGui.SetNextItemWidth(220);
            if (MaterialText.BeginCombo("original-combo", "English")) ImGui.EndCombo();
            result.Add((ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), ImGui.GetCurrentContext().LastItemData.ID));
            MaterialText.Selectable("original-option", display: "English");
            result.Add((ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), ImGui.GetCurrentContext().LastItemData.ID));
            MaterialText.TextWrapped("A retained English paragraph uses native wrapping and spacing.");
            result.Add((ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), ImGui.GetCurrentContext().LastItemData.ID));
            using (MaterialText.PushLineHeight("English")) Assert.Equal(new Vector2(4, 3), ImGui.GetStyle().FramePadding);
            ImGui.End();
        }
        host.Frame(Draw); original.Clear(); host.Frame(Draw);
        wrapped = true; host.Frame(Draw);
        Assert.Equal(original, actual);
    }

    private static MaterialTextRenderer Renderer(NativeTestContext host) => new(bitmap =>
    {
        var texture = host.RegisterTexture(bitmap.Rgba, bitmap.Width, bitmap.Height);
        return new(texture.Id, texture);
    });
}
