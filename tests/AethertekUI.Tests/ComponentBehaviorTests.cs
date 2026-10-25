using System.Numerics;
using Dalamud.Bindings.ImGui;
using Xunit;

namespace AethertekUI.Tests;

public class ComponentBehaviorTests
{
    [Theory]
    [InlineData(MaterialDensity.Compact, 1f)] [InlineData(MaterialDensity.Standard, 1f)]
    [InlineData(MaterialDensity.Compact, 1.5f)] [InlineData(MaterialDensity.Standard, 1.5f)]
    public void ThinSliderRetainsNativeEditingAndUnclampedIdleValues(MaterialDensity density, float scale)
    {
        foreach (var mode in Enum.GetValues<MaterialColorMode>())
        {
            using var host = new NativeTestContext(scale: scale);
            var theme = new MaterialTheme(new Vector3(.1f, .7f, .8f), new MaterialColorSettings { Mode = mode }, density);
            var value = 150f; var changes = 0; var disabled = false;
            Vector2 point = default;
            void Draw()
            {
                using var palette = MaterialTheme.Push(theme, scale);
                ImGui.SetNextWindowPos(Vector2.Zero); ImGui.SetNextWindowSize(new Vector2(500, 180) * scale);
                ImGui.Begin("Thin slider", ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoTitleBar);
                var root = ImGui.GetID(""); var color = ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBg];
                ImGui.PushID("retained-slider"); var expected = ImGui.GetID("##value"); ImGui.PopID();
                if (MaterialInput.SliderTrack("retained-slider", ref value, 0, 100, 260, disabled)) changes++;
                Assert.Equal(expected, ImGuiP.GetItemID()); Assert.Equal(root, ImGui.GetID(""));
                Assert.Equal(color, ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBg]);
                var min = ImGui.GetItemRectMin(); var max = ImGui.GetItemRectMax();
                point = new(min.X + (max.X - min.X) * .25f, (min.Y + max.Y) * .5f);
                ImGui.End();
            }
            host.Frame(Draw); host.Frame(Draw); Assert.Equal(150, value); Assert.Equal(0, changes);
            host.Click(point, Draw); Assert.InRange(value, 15, 35); Assert.True(changes > 0);
            var before = value; var edits = changes; disabled = true;
            host.Click(point + new Vector2(100 * scale, 0), Draw);
            Assert.Equal(before, value); Assert.Equal(edits, changes);
        }
    }

    [Theory]
    [InlineData(MaterialDensity.Compact, 1f)] [InlineData(MaterialDensity.Standard, 1f)]
    [InlineData(MaterialDensity.Compact, 1.5f)] [InlineData(MaterialDensity.Standard, 1.5f)]
    public void RebuiltSettingsWrapAndEditThroughOriginalNativeIds(MaterialDensity density, float scale)
    {
        foreach (var mode in Enum.GetValues<MaterialColorMode>())
        {
            using var host = new NativeTestContext((int)(1000 * scale), (int)(900 * scale), scale);
            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
            var theme = new MaterialTheme(new Vector3(.1f, .7f, .8f), new MaterialColorSettings { Mode = mode }, density);
            theme.Motion.Enabled = false;
            var enabled = false; var switched = false; var choice = 0; var number = 5; var changes = 0; var disabled = false; var narrow = false;
            var focus = false;
            Vector2 checkPoint = default, radioPoint = default, stepPoint = default, switchPoint = default, helpPoint = default;
            uint stepId = 0;
            void Draw()
            {
                using var scope = MaterialTheme.Push(theme, scale);
                using var controls = MaterialControls.Push();
                ImGui.SetNextWindowPos(Vector2.Zero); ImGui.SetNextWindowSize(new Vector2(900, 800) * scale);
                ImGui.Begin("Retained settings", ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoTitleBar);
                var root = ImGui.GetID(""); var alpha = ImGui.GetStyle().Alpha;
                var width = 260 * scale;
                ImGui.BeginDisabled(disabled);
                try
                {
                    var checkId = ImGui.GetID("Original checkbox");
                    if (focus) ImGui.SetKeyboardFocusHere();
                    if (MaterialSelection.Checkbox("Original checkbox", "A longer checkbox caption that must wrap inside this narrow settings card",
                        ref enabled, width)) { changes++; Assert.True(ImGui.IsItemEdited()); }
                    Assert.Equal(checkId, ImGuiP.GetItemID());
                    Assert.Equal(width, ImGui.GetItemRectSize().X);
                    Assert.True(ImGui.GetItemRectSize().Y > MaterialControls.Metrics.Height);
                    checkPoint = ImGui.GetItemRectMax() - new Vector2(8 * scale);
                    var radioId = ImGui.GetID("Original radio###stable-radio");
                    if (MaterialSelection.Radio("Original radio###stable-radio", "A wrapped manual mode radio choice", 1, ref choice, width))
                    { changes++; Assert.True(ImGui.IsItemEdited()); }
                    Assert.Equal(radioId, ImGuiP.GetItemID());
                    radioPoint = (ImGui.GetItemRectMin() + ImGui.GetItemRectMax()) * .5f;
                    var switchId = ImGui.GetID("Original trailing switch");
                    if (MaterialSwitch.Row("Original trailing switch", "A longer wrapped label with a trailing switch", ref switched, "ON", "OFF", width,
                        "Retained switch help")) { changes++; Assert.True(ImGui.IsItemEdited()); }
                    Assert.Equal(switchId, ImGuiP.GetItemID());
                    Assert.Equal(MaterialSwitch.MeasureRow("A longer wrapped label with a trailing switch", "ON", "OFF", width, "Retained switch help"), ImGui.GetItemRectSize());
                    Assert.Equal(width, ImGui.GetItemRectSize().X);
                    switchPoint = ImGui.GetItemRectMax() - new Vector2(8 * scale);
                    var switchTrack = MaterialTheme.Metrics.SwitchSize * Math.Min(1, (MaterialControls.Metrics.Height - 2 * scale) / MaterialTheme.Metrics.SwitchSize.Y);
                    var switchTail = switchTrack.X + MaterialControls.Metrics.Gap + Math.Max(MaterialText.Measure("ON").X, MaterialText.Measure("OFF").X);
                    helpPoint = new Vector2(ImGui.GetItemRectMax().X - switchTail - MaterialControls.Metrics.Gap - MaterialText.Measure("(?)").X * .5f,
                        (ImGui.GetItemRectMin().Y + ImGui.GetItemRectMax().Y) * .5f);
                    var nativeId = ImGui.GetID("Original scalar###stable-scalar");
                    ImGui.PushID("Original scalar###stable-scalar"); stepId = ImGui.GetID("+"); ImGui.PopID();
                    var rowOrigin = ImGui.GetCursorScreenPos();
                    if (MaterialSettings.Field("Original scalar###stable-scalar", "A retained scalar label that can wrap beside its editor", 135 * scale,
                        label =>
                        {
                            Assert.Empty(label); Assert.Equal(nativeId, ImGui.GetID(label));
                            var changed = ImGui.InputInt(label, ref number, 1);
                            var min = ImGui.GetItemRectMin(); var max = ImGui.GetItemRectMax();
                            if (narrow) { Assert.Equal(rowOrigin.X, min.X); Assert.True(min.Y > rowOrigin.Y); }
                            else Assert.True(min.X > rowOrigin.X);
                            stepPoint = new Vector2(max.X - ImGui.GetFrameHeight() * .5f, (min.Y + max.Y) * .5f);
                            if (ImGui.IsMouseDown(0) && ImGui.IsMouseHoveringRect(stepPoint - Vector2.One, stepPoint + Vector2.One) && !disabled)
                                Assert.Equal(stepId, ImGui.GetCurrentContext().ActiveId);
                            return changed;
                        }, narrow ? 180 * scale : width)) changes++;
                    Assert.Equal(root, ImGui.GetID(""));
                }
                finally { ImGui.EndDisabled(); }
                Assert.Equal(alpha, ImGui.GetStyle().Alpha);
                var padding = ImGui.GetStyle().CellPadding;
                var spacing = ImGui.GetStyle().ItemSpacing;
                MaterialSettings.Columns("Paired cards", 100 * scale,
                    () => { Assert.Equal(root, ImGui.GetID("")); Assert.Equal(spacing, ImGui.GetStyle().ItemSpacing); },
                    () => { Assert.Equal(root, ImGui.GetID("")); Assert.Equal(spacing, ImGui.GetStyle().ItemSpacing); }, divider: false);
                Assert.Equal(padding, ImGui.GetStyle().CellPadding); Assert.Equal(root, ImGui.GetID(""));
                var restoredCursor = ImGui.GetCursorScreenPos();
                Assert.Throws<InvalidOperationException>(() => MaterialSettings.Field("Throwing editor", "Label", 100 * scale,
                    _ => throw new InvalidOperationException("consumer editor"), width));
                Assert.Equal(root, ImGui.GetID("")); Assert.True(ImGui.GetCursorScreenPos().Y > restoredCursor.Y);
                ImGui.End();
            }
            host.Frame(Draw); host.Frame(Draw);
            host.Click(checkPoint, Draw); Assert.True(enabled);
            host.Click(radioPoint, Draw); Assert.Equal(1, choice);
            host.Click(switchPoint, Draw); Assert.True(switched);
            host.Click(helpPoint, Draw); Assert.True(switched); Assert.Equal(3, changes);
            host.Click(stepPoint, Draw); Assert.Equal(6, number); Assert.Equal(4, changes);
            disabled = true; host.Frame(Draw);
            host.Click(checkPoint, Draw); host.Click(stepPoint, Draw); host.Click(switchPoint, Draw);
            Assert.True(enabled); Assert.True(switched); Assert.Equal(6, number); Assert.Equal(4, changes);
            disabled = false; narrow = true; host.Frame(Draw); host.Click(stepPoint, Draw); Assert.Equal(7, number);
            focus = true; host.Frame(Draw); host.Frame(Draw); focus = false;
            ImGui.GetIO().AddKeyEvent(ImGuiKey.Space, true); host.Frame(Draw);
            ImGui.GetIO().AddKeyEvent(ImGuiKey.Space, false); host.Frame(Draw);
            Assert.False(enabled);
            if (Environment.GetEnvironmentVariable("AETHERTEKUI_PREVIEW_DIR") is { Length: > 0 } directory)
                SoftwarePreview.Save(Path.Combine(directory, $"retained-settings-{mode}-{density}-{scale:0.0}.png"), host);
        }
    }

    [Theory]
    [InlineData(MaterialDensity.Compact, 1f)] [InlineData(MaterialDensity.Standard, 1f)]
    [InlineData(MaterialDensity.Compact, 1.5f)] [InlineData(MaterialDensity.Standard, 1.5f)]
    public void GroupedAppearanceReflowsWithoutChangingNativeActionsOrLeakingScopes(MaterialDensity density, float scale)
    {
        using var host = new NativeTestContext((int)(1400 * scale), (int)(900 * scale), scale);
        var theme = new MaterialTheme(new Vector3(.1f, .7f, .8f), density);
        var wide = true; var enabled = false;
        Vector2 themePosition = default, mainPosition = default, opacityPosition = default, center = default;
        void Draw()
        {
            using var scope = MaterialTheme.Push(theme, scale);
            ImGui.SetNextWindowPos(Vector2.Zero); ImGui.SetNextWindowSize(new Vector2(wide ? 1050 : 450, 800) * scale);
            ImGui.Begin("Appearance scopes", ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoTitleBar);
            var root = ImGui.GetID(""); var actionId = ImGui.GetID("Existing action");
            var spacing = ImGui.GetStyle().ItemSpacing; var padding = ImGui.GetStyle().CellPadding;
            MaterialAppearanceTab.Draw("appearance", new("Theme", "Main controls", "Opacity", "Unfocused"), 300 * scale,
                () => { themePosition = ImGui.GetCursorScreenPos(); ImGui.TextUnformatted("Theme content"); },
                () => { mainPosition = ImGui.GetCursorScreenPos(); ImGui.TextUnformatted("Main controls"); },
                () => {
                    opacityPosition = ImGui.GetCursorScreenPos();
                    ImGui.Checkbox("Existing action", ref enabled);
                    Assert.Equal(actionId, ImGuiP.GetItemID());
                    center = (ImGui.GetItemRectMin() + ImGui.GetItemRectMax()) * .5f;
                }, () => ImGui.TextUnformatted("Unfocused content"));
            Assert.Equal(root, ImGui.GetID(""));
            Assert.Equal(spacing, ImGui.GetStyle().ItemSpacing); Assert.Equal(padding, ImGui.GetStyle().CellPadding);
            if (wide) { Assert.Equal(themePosition.Y, opacityPosition.Y); Assert.True(opacityPosition.X > themePosition.X + 300 * scale); }
            else { Assert.Equal(themePosition.X, opacityPosition.X); Assert.True(opacityPosition.Y > mainPosition.Y); }
            Assert.Throws<InvalidOperationException>(() => MaterialSettings.Columns("throwing", 300 * scale,
                () => throw new InvalidOperationException("consumer callback"), () => { }));
            Assert.Equal(root, ImGui.GetID("")); Assert.Equal(padding, ImGui.GetStyle().CellPadding);
            ImGui.TextUnformatted("Scope restored"); ImGui.End();
        }
        host.Frame(Draw); host.Frame(Draw); host.Click(center, Draw); Assert.True(enabled);
        wide = false; host.Frame(Draw); host.Frame(Draw); host.Click(center, Draw); Assert.False(enabled);
    }

    [Theory] [InlineData(false)] [InlineData(true)]
    public void ExplicitRadiusRetainsSizingAndDisabledBehavior(bool disabled)
    {
        using var host=new NativeTestContext(scale:1.5f);
        var theme=new MaterialTheme(new Vector3(.04f,.68f,.84f));
        var label="Start"; var activated=false; Vector2 center=default;
        void Draw()
        {
            using var scope=MaterialTheme.Push(theme,1.5f);
            ImGui.SetNextWindowPos(new(0,0)); ImGui.SetNextWindowSize(new(500,300));
            ImGui.Begin("Radius",ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoTitleBar);
            activated|=MaterialButton.Draw("stable",label,4,MaterialButtonVariant.Outlined,new(120,32),disabled,MaterialIcon.Refresh);
            Assert.Equal(new Vector2(180,48),ImGui.GetItemRectSize());
            center=(ImGui.GetItemRectMin()+ImGui.GetItemRectMax())*.5f;
            MaterialStatus.Badge("Available"); var pillSize=ImGui.GetItemRectSize();
            MaterialStatus.Badge("Available",4); Assert.Equal(pillSize,ImGui.GetItemRectSize());
            ImGui.End();
        }
        host.Frame(Draw); host.Frame(Draw); label="Reload";
        host.Click(center,Draw); Assert.Equal(!disabled,activated);
    }

    [Theory]
    [InlineData(MaterialButtonVariant.Filled)] [InlineData(MaterialButtonVariant.Tonal)]
    [InlineData(MaterialButtonVariant.Outlined)] [InlineData(MaterialButtonVariant.Text)]
    public void EachButtonVariantActivatesOnRelease(MaterialButtonVariant variant)
    {
        using var host=new NativeTestContext(); var theme=new MaterialTheme(new Vector3(.3f,.7f,.4f));
        var activated=false;Vector2 center=default;
        void Draw()
        {
            using var scope=MaterialTheme.Push(theme);
            ImGui.SetNextWindowPos(new(0,0));ImGui.SetNextWindowSize(new(500,300));
            ImGui.Begin("Button",ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoTitleBar);
            activated|=MaterialButton.Draw("stable","Action",variant,leading:MaterialIcon.Play,trailing:MaterialIcon.ArrowRight);
            center=(ImGui.GetItemRectMin()+ImGui.GetItemRectMax())*.5f;
            ImGui.End();
        }
        host.Frame(Draw);host.Frame(Draw);host.Click(center,Draw);Assert.True(activated);
    }

    [Fact]
    public void ModalTablesAndChildScopesRestoreStylesEvenWhenClosedOrClipped()
    {
        using var host=new NativeTestContext(); var theme=new MaterialTheme(new Vector3(.3f,.7f,.4f));
        var first=true;var open=true;var seen=false;
        void Draw()
        {
            using var scope=MaterialTheme.Push(theme);
            ImGui.SetNextWindowSize(new(600,400));
            ImGui.Begin("Scopes",ImGuiWindowFlags.NoSavedSettings);
            var padding=ImGui.GetStyle().CellPadding;
            using(var tableStyle=MaterialTable.PushStyle())
            {
                if(ImGui.BeginTable("table",2,ImGuiTableFlags.RowBg))
                {
                    ImGui.TableNextRow();ImGui.TableNextColumn();
                    ImGui.Selectable("Row");MaterialTable.RowState(true,false);
                    ImGui.TableNextColumn();ImGui.TextUnformatted("Data");
                    ImGui.EndTable();
                }
            }
            Assert.Equal(padding,ImGui.GetStyle().CellPadding);
            using(var card=MaterialSurface.Card("child",new(200,30)))
                if(card.Visible) ImGui.TextUnformatted("Content");
            if(first) { ImGui.OpenPopup("modal");first=false; }
            using(var modal=MaterialDialog.Begin("modal",ref open))
                if(modal.Visible) { seen=true;ImGui.CloseCurrentPopup();open=false; }
            ImGui.End();
        }
        host.Frame(Draw);host.Frame(Draw);Assert.True(seen);
    }

    [Theory] [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)]
    public void SelectionAndNavigationControlsActivate(int control)
    {
        using var host=new NativeTestContext();var theme=new MaterialTheme(new Vector3(.3f,.7f,.4f));
        var value=false;var choice=0;var activated=false;Vector2 center=default;
        void Draw()
        {
            using var scope=MaterialTheme.Push(theme);
            ImGui.SetNextWindowPos(new(0,0));ImGui.SetNextWindowSize(new(500,300));
            ImGui.Begin("Other controls",ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoTitleBar);
            activated |= control switch
            {
                0=>MaterialChip.FilterChip("chip","Selected",ref value),
                1=>MaterialNavigation.Item("nav","Data",MaterialIcon.Table,value),
                2=>MaterialActionTile.Draw("tile",MaterialIcon.Play,"Start"),
                3=>MaterialSelection.Checkbox("checkbox","Check",ref value),
                5=>MaterialChip.FilterChip("chip","Selected",ref value,new MaterialControlAppearance(theme.Colors.Primary,theme.Colors.OnPrimary,theme.Colors.Outline,24),showSelectionCheck:false),
                _=>MaterialSelection.Radio("radio","Choice",1,ref choice)
            };
            center=(ImGui.GetItemRectMin()+ImGui.GetItemRectMax())*.5f;
            ImGui.End();
        }
        host.Frame(Draw);host.Frame(Draw);host.Click(center,Draw);Assert.True(activated);
        if(control is 0 or 3 or 5)Assert.True(value);
        if(control==4)Assert.Equal(1,choice);
    }

    [Fact]
    public void PerOptionDisableRejectsSelection()
    {
        using var host=new NativeTestContext();var theme=new MaterialTheme(new Vector3(.3f,.7f,.4f));
        var selected=0;Vector2 center=default;
        MaterialSegment<int>[] options=[new(0,"Auto"),new(1,"Blocked",Disabled:true),new(2,"Off")];
        void Draw()
        {
            using var scope=MaterialTheme.Push(theme);
            ImGui.SetNextWindowPos(new(0,0));ImGui.SetNextWindowSize(new(500,300));
            ImGui.Begin("Disabled option",ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoTitleBar);
            MaterialSegmented.Draw<int>("segments",options,ref selected,300);
            center=(ImGui.GetItemRectMin()+ImGui.GetItemRectMax())*.5f;
            ImGui.End();
        }
        host.Frame(Draw);host.Frame(Draw);host.Click(center,Draw);Assert.Equal(0,selected);
    }
}
