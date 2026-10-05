using System.Numerics;
using AethertekUI.Demo;
using Dalamud.Bindings.ImGui;
using Xunit;

namespace AethertekUI.Tests;

public unsafe class VisualRegressionTests
{
    [Theory] [InlineData(false,28f)] [InlineData(true,28f)] [InlineData(true,35f)] [InlineData(true,37f)]
    public void AppearanceSelectorReportsAccentAndLanguageSeparately(bool separated,float swatchSize)
    {
        using var host=new NativeTestContext();
        ImGui.GetIO().ConfigFlags|=ImGuiConfigFlags.NavEnableKeyboard;
        var theme=new MaterialTheme(new Vector3(.2f,.7f,.8f));
        theme.Motion.Enabled=false;
        var accent=new Vector3(28/255f,201/255f,230/255f);var language="en";
        var options=new MaterialOptions<string>([new("en","en","English"),new("de","de","Deutsch")]);
        var accentChanges=0;var languageChanges=0;Vector2 swatch=default,combo=default;
        MaterialControlMetrics presetMetrics=default;Vector2 presetSpacing=default;
        void Draw()
        {
            using var scope=MaterialTheme.Push(theme);
            ImGui.SetNextWindowPos(Vector2.Zero);ImGui.SetNextWindowSize(new(700,500));
            ImGui.Begin("Selectors",ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoTitleBar);
            presetMetrics=MaterialControls.Metrics;presetSpacing=ImGui.GetStyle().ItemSpacing;
            var origin=ImGui.GetCursorScreenPos();
            var labels=new MaterialAppearanceLabels("Color","Language","Teal","Blue","Pink","Custom RGB");
            MaterialAppearanceChange changed;
            if(separated)
            {
                ImGui.PushID("appearance");ImGui.PushID("accent");var expected=ImGui.GetID("##button");ImGui.PopID();ImGui.PopID();
                var accentChanged=swatchSize==28?MaterialAppearanceSelector.DrawAccent("appearance",ref accent,labels)
                    :MaterialAppearanceSelector.DrawAccent("appearance",ref accent,labels,swatchSize);
                Assert.Equal(expected,ImGuiP.GetItemID());
                Assert.Equal(new Vector2(swatchSize),ImGui.GetItemRectSize());
                ImGui.SameLine(); MaterialButton.Draw("existing-action","Existing action");
                ImGui.SameLine();
                var languageChanged=MaterialAppearanceSelector.DrawLanguage("appearance",ref language,options);
                changed=new(accentChanged,languageChanged);
            }
            else changed=MaterialAppearanceSelector.Draw("appearance",ref accent,ref language,options,labels);
            swatch=origin+new Vector2(swatchSize*.5f);combo=(ImGui.GetItemRectMin()+ImGui.GetItemRectMax())*.5f;
            if(changed.AccentChanged) accentChanges++;if(changed.LanguageChanged) languageChanges++;
            ImGui.End();
        }
        host.Frame(Draw);host.Frame(Draw);host.Click(swatch,Draw);host.Frame(Draw);
        var popup=new ImGuiWindowPtr(ImGui.GetCurrentContext().OpenPopupStack[0].Window);
        var contrastFailures=new List<string>();
        void PresetContrast(int index,string state)
        {
            var label=new[]{"Teal","Blue","Pink"}[index];
            var min=popup.DC.CursorStartPos+new Vector2(0,index*(presetMetrics.Height+presetSpacing.Y));
            var max=min+new Vector2(ImGui.CalcTextSize(label).X+2*presetMetrics.Padding.X,presetMetrics.Height);
            var list=popup.DrawList;var vertices=new ImDrawVert[list.VtxBuffer.Size];
            for(var i=0;i<vertices.Length;i++) vertices[i]=list.VtxBuffer[i];
            var ink=new List<uint>();
            for(var i=0;i+3<vertices.Length;i++)
            {
                var a=vertices[i];var b=vertices[i+1];var c=vertices[i+2];var d=vertices[i+3];
                if(a.Pos.X>=min.X && a.Pos.Y>=min.Y && c.Pos.X<=max.X && c.Pos.Y<=max.Y
                    && a.Uv.X<c.Uv.X && a.Uv.Y<c.Uv.Y && a.Uv.Y==b.Uv.Y && b.Uv.X==c.Uv.X
                    && c.Uv.Y==d.Uv.Y && d.Uv.X==a.Uv.X && a.Pos.Y==b.Pos.Y && b.Pos.X==c.Pos.X
                    && c.Pos.Y==d.Pos.Y && d.Pos.X==a.Pos.X) { ink.Add(a.Col);i+=3; }
            }
            Assert.Equal(label.Length,ink.Count);Assert.All(ink,color=>Assert.Equal(ink[0],color));
            var frame=SoftwarePreview.Capture(host);
            var pixel=((int)(min.Y+presetMetrics.Height*.5f)*frame.Width+(int)(min.X+presetMetrics.Padding.X*.5f))*4;
            var fill=new Vector4(frame.Rgba[pixel]/255f,frame.Rgba[pixel+1]/255f,frame.Rgba[pixel+2]/255f,1);
            var foreground=ImGui.ColorConvertU32ToFloat4(ink[0]);
            var ratio=MaterialColor.Contrast(fill,foreground);
            if(ratio<4.5f) contrastFailures.Add($"{label} {state} label contrast {ratio:F2}:1 is below 4.5:1.");
            if(!separated && index==1 && Environment.GetEnvironmentVariable("AETHERTEKUI_PREVIEW_DIR") is { Length:>0 } directory)
                SoftwarePreview.Write(Path.Combine(directory,$"selector-presets-{state}.png"),frame);
        }
        ImGui.GetIO().AddMousePosEvent(-1000,-1000);host.Frame(Draw);
        for(var i=0;i<3;i++) PresetContrast(i,"idle");
        for(var i=0;i<3;i++)
        {
            var point=popup.DC.CursorStartPos+new Vector2(20,i*(presetMetrics.Height+presetSpacing.Y)+presetMetrics.Height*.5f);
            ImGui.GetIO().AddMousePosEvent(point.X,point.Y);host.Frame(Draw);PresetContrast(i,"hover");
            ImGui.GetIO().AddMouseButtonEvent(0,true);host.Frame(Draw);PresetContrast(i,"pressed");
            ImGui.GetIO().AddMousePosEvent(-1000,-1000);host.Frame(Draw);
            ImGui.GetIO().AddMouseButtonEvent(0,false);host.Frame(Draw);
        }
        Assert.Equal(0,accentChanges);Assert.Equal(0,languageChanges);
        Assert.True(contrastFailures.Count==0,string.Join(Environment.NewLine,contrastFailures));
        var blue=popup.Pos+ImGui.GetStyle().WindowPadding+new Vector2(20,MaterialControlMetrics.Measure(new(MaterialDensity.Compact,1),ImGui.GetTextLineHeight()).Height*1.5f+ImGui.GetStyle().ItemSpacing.Y);
        host.Click(blue,Draw);
        Assert.Equal(new Vector3(66/255f,133/255f,244/255f),accent);Assert.Equal(1,accentChanges);Assert.Equal(0,languageChanges);
        var custom=popup.DC.CursorStartPos+new Vector2(30,155);
        ImGui.GetIO().AddMousePosEvent(custom.X,custom.Y);host.Frame(Draw);
        ImGui.GetIO().AddMouseButtonEvent(0,true);host.Frame(Draw);
        Assert.Equal(1,accentChanges);
        ImGui.GetIO().AddMousePosEvent(custom.X+20,custom.Y+20);host.Frame(Draw);
        Assert.Equal(1,accentChanges);
        ImGui.GetIO().AddMouseButtonEvent(0,false);host.Frame(Draw);
        Assert.Equal(2,accentChanges);Assert.Equal(0,languageChanges);
        ImGui.GetIO().AddKeyEvent(ImGuiKey.Escape,true);host.Frame(Draw);ImGui.GetIO().AddKeyEvent(ImGuiKey.Escape,false);host.Frame(Draw);
        host.Click(combo,Draw);host.Frame(Draw);
        Assert.True(ImGui.GetCurrentContext().OpenPopupStack.Size>0);
        popup=new ImGuiWindowPtr(ImGui.GetCurrentContext().OpenPopupStack[0].Window);
        host.Click(popup.DC.CursorStartPos+new Vector2(30,25),Draw);
        Assert.Equal("de",language);Assert.Equal(2,accentChanges);Assert.Equal(1,languageChanges);
    }
    [Theory] [InlineData(false)] [InlineData(true)]
    public void AppearanceButtonAndSizedSwitchRetainNativeInteraction(bool disabled)
    {
        using var host=new NativeTestContext();
        var theme=new MaterialTheme(new Vector3(.2f,.7f,.8f));
        var appearance=new MaterialControlAppearance(theme.Colors.Surface,theme.Colors.OnSurface,theme.Colors.Outline);
        var clicked=false;var enabled=false;Vector2 button=default,toggle=default;
        void Draw()
        {
            using var scope=MaterialTheme.Push(theme);
            ImGui.SetNextWindowPos(Vector2.Zero);ImGui.SetNextWindowSize(new(500,300));
            ImGui.Begin("Appearance interaction",ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoTitleBar);
            clicked|=MaterialButton.Draw("action","Action",appearance,new(120,52),disabled);
            button=(ImGui.GetItemRectMin()+ImGui.GetItemRectMax())*.5f;
            MaterialSwitch.Draw("switch","",ref enabled,new Vector2(44,26),disabled);
            Assert.Equal(44,ImGui.GetItemRectSize().X);
            Assert.InRange(ImGui.GetItemRectSize().Y,26,28); // Track is 26; the native hit area also covers the text/control line.
            toggle=(ImGui.GetItemRectMin()+ImGui.GetItemRectMax())*.5f;
            ImGui.End();
        }
        host.Frame(Draw);host.Frame(Draw);
        host.Click(button,Draw);host.Click(toggle,Draw);
        Assert.Equal(!disabled,clicked);Assert.Equal(!disabled,enabled);
    }

    [Fact]
    public void InvalidAppearanceLeavesNativeStacksUntouched()
    {
        using var host=new NativeTestContext();
        var theme=new MaterialTheme(new Vector3(.2f,.7f,.8f));
        host.Frame(()=>
        {
            using var scope=MaterialTheme.Push(theme);
            ImGui.Begin("Invalid appearance");
            var id=ImGui.GetID("probe");var padding=ImGui.GetStyle().FramePadding;
            var invalid=new MaterialControlAppearance(Vector4.One,Vector4.One,Vector4.One,float.NaN);
            Assert.Throws<ArgumentOutOfRangeException>(()=>MaterialButton.Draw("bad","Bad",invalid));
            Assert.Equal(id,ImGui.GetID("probe"));Assert.Equal(padding,ImGui.GetStyle().FramePadding);
            var selected=0;var options=new MaterialOptions<int>([new("one",0,"One")]);
            Assert.Throws<ArgumentOutOfRangeException>(()=>MaterialCombo.Draw("bad",options,ref selected,invalid));
            Assert.Equal(id,ImGui.GetID("probe"));Assert.Equal(padding,ImGui.GetStyle().FramePadding);
            ImGui.End();
        });
    }
    [Fact]
    public void AppearanceExamplesMatchReviewedBaseline()
    {
        using var host=new NativeTestContext(900,600);
        var demo=new Showcase();
        var theme=new MaterialTheme(new Vector3(.2f,.7f,.8f));theme.Motion.Enabled=false;
        for(var i=0;i<4;i++) host.Frame(()=>
        {
            using var scope=MaterialTheme.Push(theme,1,MaterialStyleMode.Material);
            ImGui.SetNextWindowPos(Vector2.Zero);ImGui.SetNextWindowSize(new(900,600));
            ImGui.Begin("Appearance examples",ImGuiWindowFlags.NoSavedSettings);
            demo.AppearanceExamples();ImGui.End();
        });
        SoftwarePreview.Verify("appearance",host,Path.Combine(AppContext.BaseDirectory,"Baselines"),Path.GetFullPath("artifacts/visual-failures"));
    }
    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)] [InlineData(6)] [InlineData(7)]
    public void ShowcaseMatchesReviewedBaseline(int page)
    {
        using var host=new NativeTestContext();
        ImGui.GetIO().AddMousePosEvent(-1000,-1000);
        var demo=new Showcase { IsOpen=true,Page=page };
        for(var i=0;i<4;i++) host.Frame(()=> { ImGui.SetNextWindowPos(Vector2.Zero); demo.Draw(); });
        SoftwarePreview.Verify("showcase-"+page,host,Path.Combine(AppContext.BaseDirectory,"Baselines"),
            Path.GetFullPath("artifacts/visual-failures"));
    }

    [Fact]
    public void ComparatorDetectsShiftWrongFillAndMissingElement()
    {
        using var host=new NativeTestContext(240,160);
        SoftwarePreview.Frame Render(int defect)
        {
            host.Frame(()=>
            {
                var dl=ImGui.GetBackgroundDrawList();
                if(defect!=3) dl.AddRectFilled(new(30+(defect==1?3:0),30),new(180,90),
                    ImGui.ColorConvertFloat4ToU32(defect==2?new Vector4(1,0,0,1):new Vector4(0,1,0,1)),4);
            });
            return SoftwarePreview.Capture(host);
        }
        var expected=Render(0);
        foreach(var defect in new[]{1,2,3}) Assert.False(SoftwarePreview.Equal(expected,Render(defect),out _));
        var tolerant=new SoftwarePreview.Frame(expected.Width,expected.Height,(byte[])expected.Rgba.Clone());
        tolerant.Rgba[0]+=2;
        Assert.True(SoftwarePreview.Equal(expected,tolerant,out _));
        tolerant.Rgba[0]++;
        Assert.False(SoftwarePreview.Equal(expected,tolerant,out _));
    }

    [Fact]
    public void ExactMetricsRestoreThroughControlsAndThemes()
    {
        using var host=new NativeTestContext();
        var theme=new MaterialTheme(new Vector3(.2f,.6f,.8f));
        host.Frame(()=>
        {
            using var scope=MaterialTheme.Push(theme);
            ImGui.Begin("Scopes");
            var original=MaterialControls.Metrics;
            var exact=original with { Height=52,Padding=new(12,17),IconSize=28 };
            using(var controls=MaterialControls.Push(exact))
            {
                Assert.Equal(exact,MaterialControls.Metrics);
                using(var inherited=MaterialControls.Push()) Assert.Equal(exact,MaterialControls.Metrics);
                using(var dense=MaterialControls.Push(MaterialControlContext.Dense)) Assert.NotEqual(exact,MaterialControls.Metrics);
                Assert.Equal(exact,MaterialControls.Metrics);
                using(var nested=MaterialTheme.Push(theme,2)) Assert.NotEqual(exact,MaterialControls.Metrics);
                Assert.Equal(exact,MaterialControls.Metrics);
            }
            Assert.Equal(original,MaterialControls.Metrics);
            ImGui.End();
        });
    }
}
