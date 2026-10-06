using System.Numerics;
using Dalamud.Bindings.ImGui;
using Xunit;

namespace AethertekUI.Tests;

public sealed unsafe class MaterialTextScopeTests
{
    [Fact]
    public void NestedTextScopesRestoreTheOwnerAndRejectPrematureResourceDisposal()
    {
        using var host=new NativeTestContext();
        using var first=Renderer();
        using var second=Renderer();
        host.Frame(()=>
        {
            var outer=first.Push();
            var inner=second.Push();
            Assert.Throws<InvalidOperationException>(()=>outer.Dispose());
            Assert.Throws<InvalidOperationException>(()=>first.Clear());
            Assert.Throws<InvalidOperationException>(()=>second.Dispose());
            inner.Dispose();inner.Dispose();
            using(var sameOwner=first.Push())
                Assert.Throws<InvalidOperationException>(()=>outer.Dispose());
            outer.Dispose();outer.Dispose();
        });
        first.Clear();second.Clear();
        static MaterialTextRenderer Renderer()=>new(_=>throw new Xunit.Sdk.XunitException("This scope test does not upload text."));
    }

    [Theory]
    [InlineData(MaterialDensity.Standard,1f)]
    [InlineData(MaterialDensity.Compact,1f)]
    [InlineData(MaterialDensity.Standard,1.5f)]
    [InlineData(MaterialDensity.Compact,1.5f)]
    public void EnglishAndPasswordFieldsKeepNativeGeometryAndDoNotUploadShapedTextures(MaterialDensity density,float scale)
    {
        using var host=new NativeTestContext(650,350,scale);
        using var renderer=new MaterialTextRenderer(_=>throw new Xunit.Sdk.XunitException("Native fields unexpectedly uploaded shaped text."));
        var theme=new MaterialTheme(new Vector3(.3f,.7f,.4f),density);
        var value="alphabet";
        var password="कि";
        var scoped=false;
        var bounds=new List<(Vector2 Min,Vector2 Max)>();
        void Draw()
        {
            using var themeScope=MaterialTheme.Push(theme,scale);
            using var textScope=scoped?renderer.Push():null;
            ImGui.SetNextWindowPos(Vector2.Zero);ImGui.SetNextWindowSize(new(650,350));
            ImGui.Begin("Native field preservation",ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoTitleBar);
            Assert.Equal(ImGui.CalcTextSize("a##id",true),MaterialText.Measure("a##id",true));
            Assert.Equal(ImGui.CalcTextSize("Native wrapped text",false,70),MaterialText.Measure("Native wrapped text",false,70));
            MaterialTextField.SearchField("english",ref value,hint:"Search",width:220);
            bounds.Add((ImGui.GetItemRectMin(),ImGui.GetItemRectMax()));
            MaterialTextField.TextField("password",ref password,width:220,flags:ImGuiInputTextFlags.Password);
            bounds.Add((ImGui.GetItemRectMin(),ImGui.GetItemRectMax()));
            ImGui.End();
        }
        host.Frame(Draw);bounds.Clear();host.Frame(Draw);
        var original=bounds.ToArray();bounds.Clear();scoped=true;host.Frame(Draw);
        Assert.Equal(original,bounds);
        Assert.Equal("alphabet",value);Assert.Equal("कि",password);
    }

    [Fact]
    public void FailedHostTextUploadRestoresNativeFieldStacksAndStyles()
    {
        using var host=new NativeTestContext();
        using var renderer=new MaterialTextRenderer(_=>throw new InvalidOperationException("Diagnostic uploader failure."));
        var theme=new MaterialTheme(new Vector3(.3f,.7f,.4f));
        var value="कि";
        host.Frame(()=>
        {
            using var themeScope=MaterialTheme.Push(theme);
            using var textScope=renderer.Push();
            ImGui.Begin("Failed upload",ImGuiWindowFlags.NoSavedSettings);
            var id=ImGui.GetID("following");
            var padding=ImGui.GetStyle().FramePadding;
            var alpha=ImGui.GetStyle().Alpha;
            var foreground=ImGui.GetStyle().Colors[(int)ImGuiCol.Text];
            Assert.Throws<InvalidOperationException>(()=>MaterialTextField.SearchField("search",ref value,"खोजें",disabled:true));
            Assert.Equal(id,ImGui.GetID("following"));
            Assert.Equal(padding,ImGui.GetStyle().FramePadding);
            Assert.Equal(alpha,ImGui.GetStyle().Alpha);
            Assert.Equal(foreground,ImGui.GetStyle().Colors[(int)ImGuiCol.Text]);
            ImGui.Button("following");
            ImGui.End();
        });
    }

    [Fact]
    public void TextTexturesRemainOwnedUntilFrameSubmissionAndForeignContextsAreRejected()
    {
        using var host=new NativeTestContext();
        using var renderer=new MaterialTextRenderer(bitmap=>
        {
            var texture=host.RegisterTexture(bitmap.Rgba,bitmap.Width,bitmap.Height);
            return new(texture.Id,texture);
        });
        host.Frame(()=>
        {
            ImGui.Begin("Text ownership",ImGuiWindowFlags.NoSavedSettings);
            using(var scope=renderer.Push()) MaterialText.Text("क्षेत्र");
            ImGui.End();
            Assert.Throws<InvalidOperationException>(()=>renderer.Clear());
            Assert.Equal(0,host.ReleasedTextures);
            var original=ImGui.GetCurrentContext();
            var foreign=ImGui.CreateContext();
            try
            {
                ImGui.SetCurrentContext(foreign);
                Assert.Throws<InvalidOperationException>(()=>renderer.Push());
                Assert.Throws<InvalidOperationException>(()=>renderer.GetLayout("क्षेत्र",13));
            }
            finally { ImGui.DestroyContext(foreign);ImGui.SetCurrentContext(original); }
        });
        SoftwarePreview.Capture(host);
        renderer.Clear();
        Assert.Equal(1,host.ReleasedTextures);
    }

    [Fact]
    public void OptionalHindiShowcaseUsesActualTextureRenderingWithoutChangingExistingPages()
    {
        using var host=new NativeTestContext(900,600);
        var uploads=0;
        using var renderer=new MaterialTextRenderer(bitmap=>
        {
            uploads++;
            var texture=host.RegisterTexture(bitmap.Rgba,bitmap.Width,bitmap.Height);
            return new(texture.Id,texture);
        });
        var showcase=new AethertekUI.Demo.Showcase { HindiPreviewOpen=true,HindiRenderer=renderer };
        host.Frame(()=>showcase.Draw());host.Frame(()=>showcase.Draw());
        Assert.True(uploads>=3);
        Assert.False(showcase.IsOpen);
        var preview=Environment.GetEnvironmentVariable("AETHERTEKUI_PREVIEW_DIR");
        if(!string.IsNullOrWhiteSpace(preview)) SoftwarePreview.Save(Path.Combine(preview,"hindi-editing.png"),host);
        else SoftwarePreview.Capture(host);
    }

    [Fact]
    public void EndingTheFrameStillRetainsTextTexturesUntilRenderBuildsDrawData()
    {
        using var host=new NativeTestContext();
        using var renderer=new MaterialTextRenderer(bitmap=>
        {
            var texture=host.RegisterTexture(bitmap.Rgba,bitmap.Width,bitmap.Height);
            return new(texture.Id,texture);
        });
        host.Frame(()=>
        {
            ImGui.Begin("EndFrame text",ImGuiWindowFlags.NoSavedSettings);
            using(var scope=renderer.Push()) MaterialText.Text("कि");
            ImGui.End();
            ImGui.EndFrame();
            Assert.Throws<InvalidOperationException>(()=>renderer.Clear());
            Assert.Equal(0,host.ReleasedTextures);
        });
        SoftwarePreview.Capture(host);
        renderer.Clear();
        Assert.Equal(1,host.ReleasedTextures);
    }
}
