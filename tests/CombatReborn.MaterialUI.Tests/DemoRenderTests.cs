using CombatReborn.MaterialUI.Demo;
using Dalamud.Bindings.ImGui;
using Xunit;

namespace CombatReborn.MaterialUI.Tests;

public class DemoRenderTests
{
    private readonly Xunit.Abstractions.ITestOutputHelper output;
    public DemoRenderTests(Xunit.Abstractions.ITestOutputHelper output) => this.output=output;

    [Theory]
    [InlineData(5,1f,MaterialDensity.Compact)] [InlineData(6,1f,MaterialDensity.Compact)]
    [InlineData(5,1.5f,MaterialDensity.Standard)] [InlineData(6,1.5f,MaterialDensity.Standard)]
    [InlineData(7,1f,MaterialDensity.Compact)] [InlineData(7,1.5f,MaterialDensity.Standard)]
    public void IntegrationPagesRenderAtBothDensitiesAndScales(int page,float scale,MaterialDensity density)
    {
        using var host=new NativeTestContext((int)(1100*scale),(int)(850*scale),scale);
        var demo=new Showcase { IsOpen=true,Page=page,DemoDensity=density };
        for(var i=0;i<3;i++) host.Frame(()=>demo.Draw(scale));
        Assert.True(ImGui.GetDrawData().TotalVtxCount>100);
        if(page==6) Assert.InRange(demo.SubmittedCatalogRows,1,100);
        if(Environment.GetEnvironmentVariable("MATERIALUI_PREVIEW_DIR") is { Length:>0 } directory)
            SoftwarePreview.Save(Path.Combine(directory,$"integration-{page}-{density}-{scale:0.0}.png"),host);
    }

    [Fact]
    public void LargeCatalogSteadyStateSubmissionAndAllocationStayBounded()
    {
        using var host=new NativeTestContext();
        var demo=new Showcase { IsOpen=true,Page=6 };
        Action draw=()=>demo.Draw();
        for(var i=0;i<20;i++) host.Frame(draw);
        var before=GC.GetAllocatedBytesForCurrentThread();
        var watch=System.Diagnostics.Stopwatch.StartNew();
        var rows=0;
        for(var i=0;i<300;i++) { host.Frame(draw); rows+=demo.SubmittedCatalogRows; }
        watch.Stop();
        var bytes=GC.GetAllocatedBytesForCurrentThread()-before;
        output.WriteLine($"300 warmed frames: {watch.Elapsed.TotalMilliseconds:F2} ms, {bytes} managed bytes, {rows} submitted rows (source: 10,000). Native allocations/GPU time excluded.");
        Assert.InRange(rows,300,30_000);
        Assert.InRange(bytes,0,1_000_000);
    }
    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)] [InlineData(6)] [InlineData(7)]
    public void ActualDemoPageRendersWithoutStackErrors(int page)
    {
        using var host=new NativeTestContext();
        var demo=new Showcase { IsOpen=true,Page=page };
        for(var i=0;i<3;i++) host.Frame(()=>demo.Draw());
        Assert.True(ImGui.GetDrawData().TotalVtxCount>100);
        if(page==6) Assert.InRange(demo.SubmittedCatalogRows,1,100);
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
