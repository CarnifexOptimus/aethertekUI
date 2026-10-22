using System.Numerics;
using Dalamud.Bindings.ImGui;
using Xunit;

namespace AethertekUI.Tests;

public class AdaptiveSizingTests
{
    [Fact]
    public void AccountRowDetailsRetainTheLegacyCardActionId()
    {
        using var host = new NativeTestContext();
        for (var frame = 0; frame < 3; frame++)
            host.Frame(() =>
            {
                ImGui.SetNextWindowSize(new(700, 400));
                ImGui.Begin("Account details identity", ImGuiWindowFlags.NoSavedSettings);
                var caller = ImGui.GetID("caller-probe");
                var legacyCards = ImGui.GetID("dad-home-participant-cards");
                Assert.True(ImGui.BeginTable("dad-home-participant-cards", 1));
                ImGui.TableNextColumn();
                Assert.True(ImGui.BeginTable("dad-home-participant-fixture-worker", 1));
                ImGui.TableNextColumn();
                ImGui.Button("Details##dad-home-participant-details");
                var previousAction = ImGuiP.GetItemID();
                ImGui.EndTable();
                ImGui.EndTable();
                Assert.Equal(caller, ImGui.GetID("caller-probe"));

                Assert.True(ImGui.BeginTable("dad-account-summary", 2));
                ImGui.TableNextColumn();
                ImGui.PushID("fixture-account");
                var account = ImGui.GetID("account-probe");
                ImGuiP.PushOverrideID(legacyCards);
                ImGui.PushID("dad-home-participant-fixture-worker");
                ImGui.Button("Details##dad-home-participant-details");
                Assert.Equal(previousAction, ImGuiP.GetItemID());
                ImGui.PopID();
                ImGui.PopID();
                Assert.Equal(account, ImGui.GetID("account-probe"));
                ImGui.PopID();
                ImGui.EndTable();
                Assert.Equal(caller, ImGui.GetID("caller-probe"));
                ImGui.End();
            });
    }

    [Theory]
    [InlineData(false, 1f, 1f)]
    [InlineData(true, 1f, 1f)]
    [InlineData(false, 1.5f, 1f)]
    [InlineData(true, 1.5f, 1f)]
    [InlineData(false, 1f, 2f)]
    [InlineData(true, 1f, 2f)]
    [InlineData(false, 1.5f, 2f)]
    [InlineData(true, 1.5f, 2f)]
    public void LongSummaryCaptionsStaySingleLineAndRestoreWrapping(bool compact, float scale, float fontScale)
    {
        using var host = new NativeTestContext(1800, 1400, fontScale);
        var theme = new MaterialTheme(new Vector3(.3f, .7f, .4f), compact ? MaterialDensity.Compact : MaterialDensity.Standard);
        var caption = "Waiting\r\n" + string.Join(" ", Enumerable.Repeat("A long translated summary caption with e\u0301 accents.", 160));
        for (var frame = 0; frame < 3; frame++)
            host.Frame(() =>
            {
                using var scope = MaterialTheme.Push(theme, scale, MaterialStyleMode.Material);
                ImGui.SetNextWindowSize(new(720 * scale, 600 * scale));
                ImGui.Begin("Bounded summary captions", ImGuiWindowFlags.NoSavedSettings);
                ImGui.PushTextWrapPos(0);
                var wrap = ImGuiP.GetCurrentWindow().DC.TextWrapPos;
                var width = ImGui.GetContentRegionAvail().X;
                var start = ImGui.GetCursorScreenPos();
                MaterialText.SingleLine("Idle");
                var lineHeight = ImGui.GetItemRectSize().Y;
                var longStart = ImGui.GetCursorScreenPos();
                MaterialText.SingleLine(caption);
                Assert.Equal(lineHeight, ImGui.GetItemRectSize().Y);
                Assert.True(ImGui.GetItemRectSize().X <= width + .01f);
                Assert.Equal(wrap, ImGuiP.GetCurrentWindow().DC.TextWrapPos);
                ImGui.Button("Details##retained-details");
                Assert.True(ImGui.GetItemRectMin().Y - longStart.Y < lineHeight + ImGui.GetStyle().ItemSpacing.Y + 1);
                Assert.True(ImGui.GetCursorScreenPos().Y - start.Y < lineHeight * 3 + ImGui.GetFrameHeightWithSpacing() * 2);
                ImGui.PopTextWrapPos();
                ImGui.End();
            });
    }

    public static IEnumerable<object[]> Contexts()
    {
        foreach (var density in Enum.GetValues<MaterialDensity>())
        foreach (var context in Enum.GetValues<MaterialControlContext>())
        foreach (var scale in new[] { 1f, 1.25f, 1.5f })
        foreach (var fontScale in new[] { 1f, 2f })
            yield return new object[] { density, context, scale, fontScale };
    }

    [Theory, MemberData(nameof(Contexts))]
    public void NativeAndCustomControlsShareHeightAcrossContextsFontsAndScales(
        MaterialDensity density, MaterialControlContext context, float scale, float fontScale)
    {
        using var host = new NativeTestContext(1600,1500,fontScale);
        var theme = new MaterialTheme(new Vector3(.3f,.7f,.4f),density);
        var text = ""; var number = 10; var selected = 0; var enabled = true;
        var options = new MaterialOptions<int>([new("one",0,"One"),new("two",1,"Two")]);
        host.Frame(() =>
        {
            using var scope = MaterialTheme.Push(theme,scale);
            ImGui.SetNextWindowSize(new(1200,1400));
            ImGui.Begin("Sizing",ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoTitleBar);
            using var controls = MaterialControls.Push(context);
            var height = MaterialControls.Metrics.Height;
            Assert.True(height >= ImGui.GetTextLineHeight());
            void Check(string name) => Assert.True(Math.Abs(height-ImGui.GetItemRectSize().Y) < .1f,
                $"{name}: expected {height}, got {ImGui.GetItemRectSize().Y}");
            ImGui.Button("Native"); Check("Native button");
            ImGui.SetNextItemWidth(220); ImGui.InputText("Native input",ref text,128); Check("Native input");
            MaterialButton.FilledButton("material","Material"); Check("Material button");
            MaterialButton.IconButton("icon",MaterialIcon.Edit); Check("Icon button");
            MaterialCanvas.GlyphButton("glyph","W",ImGui.GetFont()); Check("Borrowed glyph");
            MaterialInput.Integer("number",ref number,width:220); Check("Number");
            MaterialCombo.Draw("combo",options,ref selected,width:220); Check("Combo");
            MaterialTextField.SearchField("search",ref text,width:220); Check("Search");
            MaterialSwitch.Draw("switch","Enabled",ref enabled); Check("Switch");
            MaterialSelection.Checkbox("checkbox","Selected",ref enabled); Check("Checkbox");
            ImGui.End();
        });
    }

    [Fact]
    public void ReadableNativeFieldMovesOffExhaustedRowAndKeepsItsMinimumInNarrowPanes()
    {
        using var host=new NativeTestContext();
        var theme=new MaterialTheme(new Vector3(.3f,.7f,.4f));
        var number=12345;
        host.Frame(() =>
        {
            using var themeScope=MaterialTheme.Push(theme);
            ImGui.SetNextWindowSize(new(380,300));
            ImGui.Begin("Readable row",ImGuiWindowFlags.NoSavedSettings);
            var rowWidth=ImGui.GetContentRegionAvail().X;
            ImGui.Button("Long translated action",new(rowWidth-24,0));
            var actionBottom=ImGui.GetItemRectMax().Y;
            ImGui.SameLine();
            var minimum=ImGui.CalcTextSize("00000").X+2*ImGui.GetStyle().FramePadding.X;
            var width=MaterialLayout.FitNextItemWidth(1,minimum);
            Assert.True(ImGui.GetCursorScreenPos().Y>=actionBottom);
            ImGui.SetNextItemWidth(width);
            ImGui.InputInt("##retained-field",ref number,0);
            Assert.True(ImGui.GetItemRectSize().X>=minimum);
            Assert.True(ImGui.GetItemRectMax().X<=ImGui.GetWindowPos().X+ImGui.GetWindowContentRegionMax().X+.1f);
            ImGui.BeginChild("Narrow pane",new(40,80),false);
            ImGui.SetNextItemWidth(MaterialLayout.FitNextItemWidth(-1,minimum));
            ImGui.InputInt("##retained-narrow-field",ref number,0);
            Assert.True(ImGui.GetItemRectSize().X>=minimum);
            ImGui.EndChild();
            ImGui.Button("Filler",new(rowWidth-24,0));
            var fillerBottom=ImGui.GetItemRectMax().Y;
            ImGui.SameLine();
            const string action="Use the default settings for this profile";
            MaterialButton.FilledButton("retained-action",action);
            Assert.True(ImGui.GetItemRectMin().Y>=fillerBottom);
            Assert.True(ImGui.GetItemRectSize().X>=ImGui.CalcTextSize(action).X+2*MaterialControls.Metrics.Padding.X);
            Assert.Equal(12345,number);
            ImGui.End();
        });
    }

    [Fact]
    public void CompatibilityThemePreservesHostLayoutAndNestedScopesRestoreIt()
    {
        using var host = new NativeTestContext();
        var theme = new MaterialTheme(new Vector3(.3f,.7f,.4f));
        var style = ImGui.GetStyle();
        style.FramePadding=new(3,2); style.WindowPadding=new(7,9);
        style.CellPadding=new(2,1); style.ItemSpacing=new(5,4);
        var frame=style.FramePadding; var window=style.WindowPadding; var cell=style.CellPadding; var spacing=style.ItemSpacing;
        host.Frame(() =>
        {
            using (var compatible = MaterialTheme.Push(theme))
            {
                Assert.Equal(frame,style.FramePadding); Assert.Equal(window,style.WindowPadding);
                Assert.Equal(cell,style.CellPadding); Assert.Equal(spacing,style.ItemSpacing);
                using (var dense = MaterialTable.PushControls())
                {
                    var densePadding=style.FramePadding;
                    Assert.Equal(MaterialControlContext.Dense,MaterialControls.Context);
                    using (var regular = MaterialControls.Push(MaterialControlContext.Regular))
                        Assert.True(style.FramePadding.Y > densePadding.Y);
                    Assert.Equal(densePadding,style.FramePadding);
                    using (var nested = MaterialTheme.Push(theme,1.5f,MaterialStyleMode.Material))
                        Assert.Equal(MaterialControlContext.Regular,MaterialControls.Context);
                    Assert.Equal(MaterialControlContext.Dense,MaterialControls.Context);
                    Assert.Equal(densePadding,style.FramePadding);
                }
                Assert.Equal(MaterialControlContext.Regular,MaterialControls.Context);
                Assert.Equal(frame,style.FramePadding);
            }
            Assert.Equal(frame,style.FramePadding); Assert.Equal(window,style.WindowPadding);
            Assert.Equal(cell,style.CellPadding); Assert.Equal(spacing,style.ItemSpacing);
        });
    }

    [Theory]
    [InlineData(MaterialDensity.Standard, 1f, 1f)]
    [InlineData(MaterialDensity.Standard, 1.5f, 1f)]
    [InlineData(MaterialDensity.Compact, 1f, 1f)]
    [InlineData(MaterialDensity.Compact, 1.5f, 1f)]
    [InlineData(MaterialDensity.Standard, 1.5f, 2f)]
    [InlineData(MaterialDensity.Compact, 1.5f, 2f)]
    public void FreshIndentedSettingsAndTableCellsDoNotAcquireAnExtraEditorLine(
        MaterialDensity density, float scale, float fontScale)
    {
        using var host = new NativeTestContext(1200, 1100, fontScale);
        var theme = new MaterialTheme(new Vector3(.3f, .7f, .4f), density);
        var quantity = 22;
        host.Frame(() =>
        {
            using var themeScope = MaterialTheme.Push(theme, scale);
            ImGui.SetNextWindowSize(new(1000, 1000));
            ImGui.Begin("Settings subframes", ImGuiWindowFlags.NoSavedSettings);
            ImGui.BeginChild("Settings body", new(700, 850), false);
            using var controls = MaterialTable.PushControls();
            using var tightRows = MaterialTable.PushTightRows();
            ImGui.Indent(20 * scale);
            ImGui.BeginGroup();
            ImGui.BeginChild("Indented field", new(95 * scale, 100 * scale), false);
            ImGui.Indent(12 * scale);
            var freshPosition = ImGui.GetCursorScreenPos();
            var narrowMinimum = ImGui.GetContentRegionAvail().X + 8 * scale;
            ImGui.SetNextItemWidth(MaterialLayout.FitNextItemWidth(-1, narrowMinimum));
            Assert.Equal(freshPosition, ImGui.GetCursorScreenPos());
            ImGui.InputInt("##indented-quantity", ref quantity, 0);
            Assert.Equal(freshPosition.Y, ImGui.GetItemRectMin().Y);
            ImGui.Unindent(12 * scale);
            ImGui.EndChild();

            Assert.True(ImGui.BeginTable("Stock rows", 2, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.BordersInnerV));
            try
            {
                ImGui.TableSetupColumn("Item", ImGuiTableColumnFlags.WidthFixed, 180 * scale);
                ImGui.TableSetupColumn("Target", ImGuiTableColumnFlags.WidthFixed, 60 * scale);
                float? previousBottom = null;
                for (var row = 0; row < 5; row++)
                {
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    var rowTop = ImGui.GetCursorScreenPos().Y;
                    ImGui.AlignTextToFramePadding();
                    ImGui.TextUnformatted("Versatile Lure");
                    ImGui.TableSetColumnIndex(1);
                    var cellPosition = ImGui.GetCursorScreenPos();
                    var minimum = ImGui.GetContentRegionAvail().X + 8 * scale;
                    ImGui.SetNextItemWidth(MaterialLayout.FitNextItemWidth(-1, minimum));
                    Assert.Equal(cellPosition, ImGui.GetCursorScreenPos());
                    ImGui.PushID(row);
                    ImGui.InputInt("##quantity", ref quantity, 0);
                    ImGui.PopID();
                    Assert.Equal(rowTop, ImGui.GetItemRectMin().Y);
                    if (previousBottom.HasValue)
                        Assert.True(Math.Abs(previousBottom.Value - ImGui.GetItemRectMin().Y) <= 1.1f);
                    previousBottom = ImGui.GetItemRectMax().Y;
                }
            }
            finally { ImGui.EndTable(); }
            ImGui.EndGroup();
            ImGui.Unindent(20 * scale);
            ImGui.EndChild();
            ImGui.End();
        });
        Assert.Equal(22, quantity);
    }

    [Fact]
    public void WidthResolutionSeparatesMeasuredPixelsFromLogicalRequests()
    {
        Assert.Equal(80,MaterialLayout.ResolveWidth(-1,30,80,2));
        Assert.Equal(50,MaterialLayout.ResolveWidth(0,50,80,2));
        Assert.Equal(80,MaterialLayout.ResolveWidth(100,50,80,2));
        Assert.Equal(200,MaterialLayout.ResolveWidth(100,50,80,2,false));
        Assert.Equal(1,MaterialLayout.ResolveWidth(-1,50,-5,2));
        Assert.Throws<ArgumentOutOfRangeException>(()=>MaterialLayout.ResolveWidth(float.NaN,20,80,1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>MaterialControlMetrics.Measure(new(MaterialDensity.Compact,1),float.NaN));
    }

    [Theory]
    [InlineData(MaterialDensity.Compact,1f,MaterialControlContext.Regular)]
    [InlineData(MaterialDensity.Compact,1.5f,MaterialControlContext.Regular)]
    [InlineData(MaterialDensity.Standard,1f,MaterialControlContext.Regular)]
    [InlineData(MaterialDensity.Standard,1.5f,MaterialControlContext.Regular)]
    [InlineData(MaterialDensity.Compact,1f,MaterialControlContext.Dense)]
    [InlineData(MaterialDensity.Compact,1.5f,MaterialControlContext.Dense)]
    [InlineData(MaterialDensity.Standard,1f,MaterialControlContext.Dense)]
    [InlineData(MaterialDensity.Standard,1.5f,MaterialControlContext.Dense)]
    public unsafe void TightTableRowsMeetWithoutChangingEditorsOrNativeActions(
        MaterialDensity density,float scale,MaterialControlContext context)
    {
        using var host=new NativeTestContext(1200,900,scale*1.5f);
        var theme=new MaterialTheme(new Vector3(.3f,.7f,.4f),density);
        int[] nativeValues=[10,20,30],materialValues=[1,2,3];
        var presses=0;
        Vector2 actionPoint=default,inputPoint=default;
        void Draw()
        {
            using var themeScope=MaterialTheme.Push(theme,scale);
            ImGui.SetNextWindowPos(new(20,20));ImGui.SetNextWindowSize(new(1100,700));
            ImGui.Begin("Tight grid",ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoTitleBar);
            using(var controls=MaterialControls.Push(context))
            {
                var style=ImGui.GetStyle();
                var padding=style.FramePadding;var spacing=style.ItemSpacing;var cell=style.CellPadding;
                var header=style.Colors[(int)ImGuiCol.TableHeaderBg];
                var metrics=MaterialControls.Metrics;
                using(var tight=MaterialTable.PushTightRows(alternateRows:context==MaterialControlContext.Regular))
                {
                    Assert.Equal(padding,style.FramePadding);Assert.Equal(spacing,style.ItemSpacing);
                    Assert.Equal(metrics,MaterialControls.Metrics);Assert.Equal(context,MaterialControls.Context);
                    Assert.Equal(new Vector2(scale,0),style.CellPadding);
                    if(context==MaterialControlContext.Dense)Assert.Equal(Vector4.Zero,style.Colors[(int)ImGuiCol.TableRowBgAlt]);
                    Assert.True(ImGui.BeginTable("retained-table",3,ImGuiTableFlags.Borders|ImGuiTableFlags.RowBg));
                    ImGui.TableSetupColumn("Native");ImGui.TableSetupColumn("Material");ImGui.TableSetupColumn("Action");
                    ImGui.TableHeadersRow();
                    var previousBottom=0f;
                    for(var row=0;row<nativeValues.Length;row++)
                    {
                        ImGui.PushID(row);ImGui.TableNextRow();ImGui.TableSetColumnIndex(0);
                        var expectedId=ImGui.GetID("##retained-editor");
                        ImGui.SetNextItemWidth(-1);
                        ImGui.InputInt("##retained-editor",ref nativeValues[row],0);
                        Assert.Equal(expectedId,ImGuiP.GetItemID());
                        var min=ImGui.GetItemRectMin();var max=ImGui.GetItemRectMax();
                        Assert.InRange(MathF.Abs(max.Y-min.Y-metrics.Height),0,.1f);
                        // Native groups floor fractional cursor positions; allow subpixel rounding only.
                        if(row>0)Assert.InRange(MathF.Abs(min.Y-previousBottom),0,.51f);
                        previousBottom=max.Y;
                        if(row==0)inputPoint=(min+max)*.5f;
                        ImGui.TableSetColumnIndex(1);
                        MaterialInput.Integer("retained-material-editor",ref materialValues[row],step:0);
                        Assert.InRange(MathF.Abs(ImGui.GetItemRectMin().Y-min.Y),0,.1f);
                        Assert.InRange(MathF.Abs(ImGui.GetItemRectSize().Y-metrics.Height),0,.1f);
                        ImGui.TableSetColumnIndex(2);
                        if(ImGui.Button("X##retained-action"))presses++;
                        if(row==1)actionPoint=(ImGui.GetItemRectMin()+ImGui.GetItemRectMax())*.5f;
                        Assert.InRange(MathF.Abs(ImGui.GetItemRectSize().Y-metrics.Height),0,.1f);
                        Assert.Equal(0,ImGuiP.GetCurrentTable().CellPaddingY);
                        ImGui.PopID();
                    }
                    ImGui.EndTable();
                }
                Assert.Equal(padding,style.FramePadding);Assert.Equal(spacing,style.ItemSpacing);
                Assert.Equal(cell,style.CellPadding);Assert.Equal(header,style.Colors[(int)ImGuiCol.TableHeaderBg]);
                Assert.Equal(metrics,MaterialControls.Metrics);Assert.Equal(context,MaterialControls.Context);
            }
            ImGui.End();
        }
        host.Frame(Draw);host.Frame(Draw);
        host.Click(actionPoint,Draw);
        Assert.Equal(1,presses);
        host.Click(inputPoint,Draw);
        ImGui.GetIO().AddKeyEvent(ImGuiKey.End,true);host.Frame(Draw);
        ImGui.GetIO().AddKeyEvent(ImGuiKey.End,false);host.Frame(Draw);
        ImGui.GetIO().AddInputCharacter('7');host.Frame(Draw);
        host.Frame(Draw);
        Assert.Equal(107,nativeValues[0]);
        Assert.Equal(20,nativeValues[1]);Assert.Equal(30,nativeValues[2]);
        Assert.Equal(new[]{1,2,3},materialValues);
        if(Environment.GetEnvironmentVariable("AETHERTEKUI_PREVIEW_DIR") is { Length:>0 } directory)
            SoftwarePreview.Save(Path.Combine(directory,$"tight-rows-{density}-{context}-{scale:0.0}.png"),host);
    }

    [Fact]
    public void ActionTileMeasuresWrappedContentAndHonorsExplicitFixedMode()
    {
        using var host = new NativeTestContext(1200,1200,2);
        var theme = new MaterialTheme(new Vector3(.3f,.7f,.4f));
        host.Frame(() =>
        {
            using var scope=MaterialTheme.Push(theme);
            ImGui.SetNextWindowSize(new(1100,1100));
            ImGui.Begin("Tile sizing",ImGuiWindowFlags.NoSavedSettings);
            const string label="A longer translated action label";
            const string detail="Status text needs room below the action.";
            var requested=new Vector2(240,72);
            var measured=MaterialActionTile.Measure(label,detail,requested);
            Assert.True(measured.Y>requested.Y);
            var narrow=MaterialActionTile.Measure(label,detail,new(120,72));
            Assert.True(narrow.Y>measured.Y);
            MaterialActionTile.Draw("tile",MaterialIcon.Play,label,detail,requested);
            Assert.Equal(measured,ImGui.GetItemRectSize());
            Assert.Equal(72,MaterialActionTile.Measure(label,detail,requested,MaterialTileSizing.Fixed).Y);
            ImGui.End();
        });
    }

    [Fact]
    public void NarrowFieldCanOpenWiderSearchablePopup()
    {
        using var host=new NativeTestContext();
        var theme=new MaterialTheme(new Vector3(.3f,.7f,.4f));
        var options=new MaterialOptions<int>([new("a",0,"First long option"),new("b",1,"Second long option")]);
        var value=0; Vector2 point=default;
        void Draw()
        {
            using var scope=MaterialTheme.Push(theme);
            ImGui.SetNextWindowPos(new(20,20)); ImGui.SetNextWindowSize(new(240,500));
            ImGui.Begin("Narrow plugin",ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoTitleBar);
            var available=ImGui.GetContentRegionAvail().X;
            MaterialCombo.Draw("combo",options,ref value,new MaterialComboLayout(360,4),width:800);
            if(options.Query.Length==0) point=(ImGui.GetItemRectMin()+ImGui.GetItemRectMax())*.5f;
            Assert.InRange(ImGui.GetItemRectSize().X,1,available+.01f);
            ImGui.End();
        }
        host.Frame(Draw); host.Frame(Draw); host.Click(point,Draw); host.Frame(Draw);
        foreach(var ch in "Second") ImGui.GetIO().AddInputCharacter(ch);
        host.Frame(Draw); host.Frame(Draw);
        Assert.Equal("Second",options.Query);
        Assert.Equal(1,options.Count);
        if(Environment.GetEnvironmentVariable("AETHERTEKUI_PREVIEW_DIR") is { Length:>0 } directory)
            SoftwarePreview.Save(Path.Combine(directory,"narrow-field-wide-popup.png"),host);
    }

    [Fact]
    public void NarrowNumberKeepsEditorWhileWideNumberRetainsStepButtons()
    {
        using var host=new NativeTestContext(1200,800,1.5f);
        var theme=new MaterialTheme(new Vector3(.3f,.7f,.4f),MaterialDensity.Standard);
        var value=12; var narrow=true; Vector2 right=default;
        void Draw()
        {
            using var scope=MaterialTheme.Push(theme,1.5f);
            ImGui.SetNextWindowPos(new(20,20)); ImGui.SetNextWindowSize(new(900,600));
            ImGui.Begin("Steppers",ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoTitleBar);
            MaterialInput.Integer("value",ref value,width:narrow?95:240);
            right=ImGui.GetItemRectMax()-new Vector2(5,ImGui.GetItemRectSize().Y*.5f);
            ImGui.End();
        }
        host.Frame(Draw); host.Frame(Draw);
        host.Click(right,Draw); Assert.Equal(12,value);
        narrow=false; host.Frame(Draw); host.Frame(Draw);
        host.Click(right,Draw); Assert.Equal(13,value);
    }

    [Fact]
    public void TinyExplicitGlyphAndImageButtonsKeepTheirBoundsAndActivate()
    {
        using var host=new NativeTestContext();
        var theme=new MaterialTheme(new Vector3(.3f,.7f,.4f));
        Vector2 glyph=default; var clicked=false;
        void Draw()
        {
            using var scope=MaterialTheme.Push(theme);
            ImGui.SetNextWindowPos(new(20,20)); ImGui.SetNextWindowSize(new(600,400));
            ImGui.Begin("Small icons",ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoTitleBar);
            clicked|=MaterialCanvas.GlyphButton("glyph","W",ImGui.GetFont(),size:8);
            Assert.Equal(new Vector2(8),ImGui.GetItemRectSize());
            glyph=(ImGui.GetItemRectMin()+ImGui.GetItemRectMax())*.5f;
            MaterialButton.IconButton("vector",MaterialIcon.Edit,size:8);
            Assert.Equal(new Vector2(8),ImGui.GetItemRectSize());
            MaterialCanvas.ImageButton("image",new ImTextureID((nint)1),new(8));
            Assert.Equal(new Vector2(8),ImGui.GetItemRectSize());
            var value="";
            MaterialTextField.SearchField("tiny",ref value,width:4);
            Assert.InRange(ImGui.GetItemRectSize().X,1,4.1f);
            ImGui.End();
        }
        host.Frame(Draw); host.Frame(Draw); host.Click(glyph,Draw);
        Assert.True(clicked);
    }

    [Fact]
    public void AppearanceLanguageRetainsReadableLabelAndNativeIdentityInNarrowPanes()
    {
        using var host=new NativeTestContext(900,600);
        var options=new MaterialOptions<string>([new("long","long","A readable language choice")]);
        var language="long";var accent=new Vector3(.2f,.7f,.8f);
        foreach(var density in Enum.GetValues<MaterialDensity>()) foreach(var scale in new[]{1f,1.5f})
        {
            var theme=new MaterialTheme(accent,density);
            void Draw()=>Window();
            void Window()
            {
                using var frame=MaterialTheme.Push(theme,scale);
                ImGui.SetNextWindowSize(new(100*scale,300));
                ImGui.Begin("Language sizing",ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.HorizontalScrollbar);
                using var controls=MaterialControls.Push();
                var minimum=ImGui.CalcTextSize(options.LabelFor(language,"Select...")).X
                    +MaterialControls.Metrics.Height+3*MaterialControls.Metrics.Gap+Math.Min(MaterialControls.Metrics.IconSize,MaterialControls.Metrics.Height);
                ImGui.PushID("appearance");ImGui.PushID("language");var expected=ImGui.GetID("##combo");ImGui.PopID();ImGui.PopID();
                var firstRow=ImGui.GetCursorScreenPos().Y;
                MaterialAppearanceSelector.Draw("appearance",ref accent,ref language,options,new("Colour","Language","Teal","Blue","Pink","Custom"));
                Assert.Equal(expected,ImGuiP.GetItemID());
                Assert.True(ImGui.GetItemRectSize().X>=minimum);
                Assert.Equal("long",language);
                Assert.True(ImGui.GetItemRectMin().Y>=firstRow+28*scale+ImGui.GetStyle().ItemSpacing.Y-.1f);
                ImGui.End();
            }
            host.Frame(Draw);host.Frame(Draw);
        }
    }

    [Fact]
    public void InvalidScopeAndComboArgumentsDoNotDisturbCurrentContext()
    {
        using var host=new NativeTestContext(); var theme=new MaterialTheme(new Vector3(.3f,.7f,.4f));
        host.Frame(() =>
        {
            using var scope=MaterialTheme.Push(theme);
            using var dense=MaterialControls.Push(MaterialControlContext.Dense);
            var before=ImGui.GetStyle().FramePadding;
            Assert.Throws<ArgumentOutOfRangeException>(()=> { using var invalid=MaterialControls.Push((MaterialControlContext)99); });
            Assert.Throws<ArgumentOutOfRangeException>(()=> { using var invalid=MaterialTheme.Push(theme,1,(MaterialStyleMode)99); });
            Assert.Equal(before,ImGui.GetStyle().FramePadding);
            Assert.Equal(MaterialControlContext.Dense,MaterialControls.Context);
        });
    }
}
