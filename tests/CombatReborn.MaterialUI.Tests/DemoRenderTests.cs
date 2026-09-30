using CombatReborn.MaterialUI.Demo;
using Dalamud.Bindings.ImGui;
using Xunit;

namespace CombatReborn.MaterialUI.Tests;

public class DemoRenderTests
{
    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)]
    public void ActualDemoPageRendersWithoutStackErrors(int page)
    {
        using var host=new NativeTestContext();
        var demo=new Showcase { IsOpen=true,Page=page };
        for(var i=0;i<3;i++) host.Frame(()=>demo.Draw());
        Assert.True(ImGui.GetDrawData().TotalVtxCount>100);
        if(Environment.GetEnvironmentVariable("MATERIALUI_PREVIEW_DIR") is { Length:>0 } directory)
            SoftwarePreview.Save(Path.Combine(directory,$"page-{page}.png"),host);
    }

    [Fact]
    public void ActualDemoScalesWithHostFontAndGeometry()
    {
        using var host=new NativeTestContext(1650,1275,1.5f);
        var demo=new Showcase { IsOpen=true,Page=4 };
        for(var i=0;i<3;i++) host.Frame(()=>demo.Draw(1.5f));
        Assert.True(ImGui.GetDrawData().TotalVtxCount>100);
        if(Environment.GetEnvironmentVariable("MATERIALUI_PREVIEW_DIR") is { Length:>0 } directory)
            SoftwarePreview.Save(Path.Combine(directory,"settings-150-percent.png"),host);
    }
}
