using Xunit;

namespace CombatReborn.MaterialUI.Tests;

public class GeometryMotionTests
{
    [Theory] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(17)]
    public void SegmentsCoverWidthWithoutGaps(int count)
    {
        var end=0f;
        for(var i=0;i<count;i++)
        {
            var bounds=MaterialGeometry.SegmentBounds(317,count,i);
            Assert.InRange(Math.Abs(bounds.Start-end),0,1e-4f);
            Assert.True(bounds.Width>0);
            end=bounds.Start+bounds.Width;
        }
        Assert.Equal(317,end);
    }
    [Fact]
    public void MetricsScaleOnceAndPreserveSentinels()
    {
        var a=new MaterialMetrics(MaterialDensity.Compact,1);
        var b=new MaterialMetrics(MaterialDensity.Compact,1.5f);
        Assert.Equal(a.ButtonHeight*1.5f,b.ButtonHeight);
        Assert.Equal(-1,MaterialGeometry.ScaleDimension(-1,2));
        Assert.Equal(0,MaterialGeometry.ScaleDimension(0,2));
        Assert.Equal(14,MaterialGeometry.PillRadius(a.ButtonHeight));
        Assert.True(new MaterialMetrics(MaterialDensity.Standard,1).ButtonHeight>a.ButtonHeight);
        Assert.Throws<ArgumentOutOfRangeException>(()=>MaterialGeometry.SegmentBounds(100,1,0));
    }
    [Fact]
    public void MotionInterpolatesOncePerFrameAndPrunes()
    {
        var motion=new MaterialMotion(8,10);
        Assert.Equal(0,motion.Sample(1,0,.016f,0));
        var value=motion.Sample(1,1,.016f,1);
        Assert.InRange(value,.1f,.9f);
        Assert.Equal(value,motion.Sample(1,1,.016f,1));
        motion.Prune(12);
        Assert.Equal(0,motion.Count);
        Assert.Equal(1,motion.Sample(1,1,.016f,13));
        for(ulong i=0;i<100;i++) motion.Sample(i,0,.016f,14);
        Assert.Equal(8,motion.Count);
        motion.Enabled=false;
        Assert.Equal(1,motion.Sample(99,1,.016f,15));
        motion.Prune(0); // context/frame reset
        Assert.Equal(0,motion.Count);
    }
}
