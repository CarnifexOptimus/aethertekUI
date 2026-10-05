using System.Globalization;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Xunit;

namespace AethertekUI.Tests;

public unsafe class WindowFoldNativeTests
{
    [Theory]
    [InlineData(MaterialDensity.Compact, 1f)]
    [InlineData(MaterialDensity.Compact, 1.5f)]
    [InlineData(MaterialDensity.Standard, 1f)]
    [InlineData(MaterialDensity.Standard, 1.5f)]
    public void NativeCallbackRunsForAutoFitThenFinalSizeAfterCollapseInput(MaterialDensity density, float scale)
    {
        using var fixture = new WindowFixture(density, scale, useFold: false, recordCallback: true);
        fixture.Settle();
        Assert.Equal(2, fixture.Samples.Count);
        Assert.NotEqual(fixture.Samples[0].DesiredSize, fixture.Samples[1].DesiredSize);
        Assert.Equal(fixture.Native.SizeFull, fixture.Samples[1].DesiredSize);
        var full = fixture.Native.SizeFull;
        fixture.ClickArrow();
        Assert.True(fixture.Native.WantCollapseToggle);
        var restoredFlags = fixture.Native.Flags;
        fixture.SecondPass = data =>
        {
            var native = ImGuiP.GetCurrentWindow();
            native.Collapsed = false;
            native.Flags |= ImGuiWindowFlags.AlwaysAutoResize;
            data->DesiredSize = new(full.X, ImGuiP.TitleBarHeight(native) + 42 * scale);
        };
        fixture.AfterEnd = () => { fixture.Native.Flags = restoredFlags; fixture.Native.SizeFull = full; };
        fixture.Frame();
        Assert.Equal(2, fixture.Samples.Count);
        Assert.All(fixture.Samples, sample => Assert.True(sample.Collapsed));
        Assert.True(fixture.Visible);
        AssertClose(fixture.TitleHeight + 42 * scale, fixture.VisibleSize.Y);
        Assert.False(fixture.Native.WantCollapseToggle);
        Assert.Equal(full, fixture.Native.SizeFull);
    }

    [Theory]
    [InlineData(MaterialDensity.Compact, 1f)]
    [InlineData(MaterialDensity.Compact, 1.5f)]
    [InlineData(MaterialDensity.Standard, 1f)]
    [InlineData(MaterialDensity.Standard, 1.5f)]
    public void NativeArrowCollapsesExpandsAndReversesWithoutChangingIdsOrSavedSize(MaterialDensity density, float scale)
    {
        using var fixture = new WindowFixture(density, scale, recordCallback: true);
        fixture.Settle();
        var full = fixture.Native.SizeFull;
        var id = fixture.Native.ID;
        var arrowId = fixture.ArrowId;
        var bodyId = fixture.BodyId;
        var position = fixture.Native.Pos;
        fixture.SaveDuringDraw = true;
        fixture.ClickArrow();
        Assert.True(fixture.Native.WantCollapseToggle);
        fixture.Frame();
        Assert.True(fixture.Fold.IsAnimating);
        Assert.InRange(fixture.VisibleSize.Y, fixture.TitleHeight + 1, full.Y - 1);
        Assert.Equal(full, fixture.LogicalSize);
        Assert.Equal(full, fixture.Native.SizeFull);
        AssertIniSize(fixture.SavedDuringDraw, full);
        Assert.Equal(id, fixture.Native.ID);
        Assert.Equal(arrowId, fixture.ArrowId);
        Assert.Equal(bodyId, fixture.BodyId);
        Assert.True(fixture.Open);
        var shrinking = fixture.VisibleSize.Y;
        fixture.Frame();
        Assert.True(fixture.VisibleSize.Y < shrinking);
        fixture.ClickArrow();
        fixture.Frame();
        var reversing = fixture.VisibleSize.Y;
        fixture.Frame();
        Assert.True(fixture.VisibleSize.Y > reversing);
        fixture.WaitForAnimation();
        Assert.False(fixture.Native.Collapsed);
        Assert.Equal(full, fixture.Native.SizeFull);
        Assert.Equal(full, fixture.VisibleSize);
        fixture.ClickArrow();
        fixture.Frame();
        fixture.WaitForAnimation();
        Assert.True(fixture.Native.Collapsed);
        Assert.False(fixture.Visible);
        AssertClose(fixture.TitleHeight, fixture.VisibleSize.Y);
        Assert.Equal(full, fixture.Native.SizeFull);
        AssertIniSize(ImGui.SaveIniSettingsToMemory(), full);
        Assert.Contains("Collapsed=1", ImGui.SaveIniSettingsToMemory());
        fixture.ClickArrow();
        fixture.Frame();
        Assert.True(fixture.Fold.IsAnimating);
        Assert.True(fixture.Visible);
        fixture.WaitForAnimation();
        Assert.False(fixture.Native.Collapsed);
        Assert.Equal(full, fixture.VisibleSize);
        Assert.Equal(position, fixture.Native.Pos);
        Assert.Equal(id, fixture.Native.ID);
        Assert.Equal(bodyId, fixture.BodyId);
        Assert.True(fixture.Open);
        fixture.ClickBody();
        Assert.Equal(1, fixture.Actions);
    }

    [Theory]
    [InlineData(MaterialDensity.Compact, 1f)]
    [InlineData(MaterialDensity.Compact, 1.5f)]
    [InlineData(MaterialDensity.Standard, 1f)]
    [InlineData(MaterialDensity.Standard, 1.5f)]
    public void NativeTitleDoubleClickUsesTheSameAnimatedTransition(MaterialDensity density, float scale)
    {
        using var fixture = new WindowFixture(density, scale);
        fixture.Settle();
        var full = fixture.Native.SizeFull;
        fixture.DoubleClickTitle();
        Assert.True(fixture.Fold.IsAnimating);
        Assert.True(fixture.Visible);
        Assert.True(fixture.VisibleSize.Y < full.Y);
        Assert.Equal(full, fixture.LogicalSize);
        fixture.WaitForAnimation();
        Assert.True(fixture.Native.Collapsed);
        fixture.Idle(24);
        fixture.DoubleClickTitle();
        Assert.True(fixture.Fold.IsAnimating);
        fixture.WaitForAnimation();
        Assert.False(fixture.Native.Collapsed);
        Assert.Equal(full, fixture.Native.SizeFull);
    }

    [Theory]
    [InlineData(MaterialDensity.Compact, 1f)]
    [InlineData(MaterialDensity.Compact, 1.5f)]
    [InlineData(MaterialDensity.Standard, 1f)]
    [InlineData(MaterialDensity.Standard, 1.5f)]
    public void NormalNativeResizeMatchesAnUnwrappedWindow(MaterialDensity density, float scale)
    {
        Vector2 baseline;
        using (var fixture = new WindowFixture(density, scale, useFold: false, recordCallback: true))
        {
            fixture.Settle();
            fixture.DragResize(new(72 * scale, 48 * scale));
            baseline = fixture.Native.SizeFull;
            Assert.True(fixture.MaximumCallbackCount > 2);
        }
        using (var fixture = new WindowFixture(density, scale, recordCallback: true))
        {
            fixture.Settle();
            var initial = fixture.Native.SizeFull;
            fixture.DragResize(new(72 * scale, 48 * scale));
            Assert.True(fixture.Native.SizeFull.X > initial.X + 30 * scale);
            Assert.True(fixture.Native.SizeFull.Y > initial.Y + 20 * scale);
            Assert.Equal(baseline, fixture.Native.SizeFull);
            Assert.Equal(baseline, fixture.Fold.ExpandedSize);
            Assert.False(fixture.Fold.IsAnimating);
            Assert.Equal(fixture.Flags, fixture.Native.Flags);
        }
    }

    [Theory]
    [InlineData(MaterialDensity.Compact, 1f)]
    [InlineData(MaterialDensity.Compact, 1.5f)]
    [InlineData(MaterialDensity.Standard, 1f)]
    [InlineData(MaterialDensity.Standard, 1.5f)]
    public void ReducedMotionAndNativeOptOutsRetainNativeBehavior(MaterialDensity density, float scale)
    {
        using var fixture = new WindowFixture(density, scale);
        fixture.Settle();
        fixture.ReducedMotion = true;
        fixture.ClickArrow();
        fixture.Frame();
        Assert.True(fixture.Native.Collapsed);
        Assert.False(fixture.Fold.IsAnimating);
        fixture.ClickArrow();
        fixture.Frame();
        Assert.False(fixture.Native.Collapsed);
        fixture.ReducedMotion = false;
        fixture.Frame();
        Assert.False(fixture.Native.Collapsed);
        Assert.False(fixture.Fold.IsAnimating);
        fixture.Theme.Motion.Enabled = false;
        fixture.ClickArrow();
        fixture.Frame();
        Assert.True(fixture.Native.Collapsed);
        Assert.False(fixture.Fold.IsAnimating);
        fixture.Theme.Motion.Enabled = true;
        fixture.Frame();
        Assert.True(fixture.Native.Collapsed);
        Assert.False(fixture.Fold.IsAnimating);
        fixture.Flags = ImGuiWindowFlags.NoCollapse;
        fixture.Frame();
        Assert.False(fixture.Native.Collapsed);
        fixture.DoubleClickTitle();
        Assert.False(fixture.Native.Collapsed);
        Assert.False(fixture.Fold.IsAnimating);
        fixture.Flags = ImGuiWindowFlags.NoTitleBar;
        fixture.Frame();
        fixture.Native.WantCollapseToggle = true;
        fixture.Frame();
        Assert.False(fixture.Native.Collapsed);
        Assert.False(fixture.Fold.IsAnimating);
        fixture.Flags = ImGuiWindowFlags.NoMouseInputs;
        fixture.Frame();
        fixture.Native.WantCollapseToggle = true;
        fixture.Frame();
        Assert.True(fixture.Native.Collapsed);
        Assert.False(fixture.Fold.IsAnimating);
        fixture.Flags = ImGuiWindowFlags.NoNavFocus;
        fixture.Frame();
        fixture.ClickArrow();
        fixture.Frame();
        Assert.True(fixture.Fold.IsAnimating);
        fixture.ReducedMotion = true;
        fixture.Frame();
        Assert.False(fixture.Native.Collapsed);
        Assert.Equal(fixture.Fold.ExpandedSize, fixture.VisibleSize);
        Assert.False(fixture.Fold.IsAnimating);
        fixture.ReducedMotion = false;
        fixture.Frame();
        fixture.ClickArrow();
        fixture.Frame();
        Assert.True(fixture.Fold.IsAnimating);
        fixture.ReducedMotion = true;
        fixture.Frame();
        Assert.True(fixture.Native.Collapsed);
        Assert.False(fixture.Fold.IsAnimating);
    }

    [Theory]
    [InlineData(MaterialDensity.Compact, 1f)]
    [InlineData(MaterialDensity.Compact, 1.5f)]
    [InlineData(MaterialDensity.Standard, 1f)]
    [InlineData(MaterialDensity.Standard, 1.5f)]
    public void AutoFitAndOwnerSizeChangesKeepExpandedGeometry(MaterialDensity density, float scale)
    {
        using var fixture = new WindowFixture(density, scale);
        fixture.Flags = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollbar;
        fixture.WidthOnly = true;
        fixture.Settle();
        var full = fixture.Native.SizeFull;
        fixture.ClickArrow();
        fixture.Frame();
        fixture.WaitForAnimation();
        Assert.Equal(full, fixture.Native.SizeFull);
        fixture.ClickArrow();
        fixture.Frame();
        fixture.WaitForAnimation();
        Assert.Equal(full, fixture.Native.SizeFull);
        fixture.Flags = ImGuiWindowFlags.None;
        fixture.WidthOnly = false;
        fixture.SizeCondition = ImGuiCond.Always;
        fixture.RequestedSize = new(470 * scale, 390 * scale);
        fixture.Frame();
        var changed = fixture.Native.SizeFull;
        fixture.ClickArrow();
        fixture.Frame();
        Assert.True(fixture.Fold.IsAnimating);
        Assert.Equal(changed, fixture.LogicalSize);
        Assert.Equal(changed, fixture.Native.SizeFull);
        fixture.WaitForAnimation();
        Assert.Equal(changed, fixture.Native.SizeFull);
        Assert.Equal(fixture.Flags, fixture.Native.Flags);
        AssertIniSize(ImGui.SaveIniSettingsToMemory(), changed);
        fixture.ClickArrow();
        fixture.Frame();
        Assert.True(fixture.Fold.IsAnimating);
        var bodySize = new Vector2(520, 430) * scale;
        fixture.BodySizeRequest = bodySize;
        fixture.Frame();
        Assert.Equal(bodySize, fixture.Native.SizeFull);
        Assert.Equal(bodySize, fixture.Fold.ExpandedSize);
        fixture.SizeCondition = ImGuiCond.FirstUseEver;
        fixture.WaitForAnimation();
        Assert.Equal(bodySize, fixture.Native.SizeFull);
    }

    [Theory]
    [InlineData(MaterialDensity.Compact, 1f)]
    [InlineData(MaterialDensity.Compact, 1.5f)]
    [InlineData(MaterialDensity.Standard, 1f)]
    [InlineData(MaterialDensity.Standard, 1.5f)]
    public void ExistingConstraintCallbackKeepsItsUserDataAndGeometryWhileAnimating(MaterialDensity density, float scale)
    {
        using var fixture = new WindowFixture(density, scale, recordCallback: true);
        fixture.CallbackUserData = 430;
        fixture.UseUserDataWidth = true;
        fixture.Settle();
        var full = fixture.Native.SizeFull;
        Assert.Equal(430 * scale, full.X);
        Assert.All(fixture.Samples, sample => Assert.Equal((nint)430, sample.UserData));
        fixture.ClickArrow();
        fixture.Frame();
        Assert.True(fixture.Fold.IsAnimating);
        Assert.All(fixture.Samples, sample => Assert.Equal((nint)430, sample.UserData));
        Assert.Equal(full.X, fixture.VisibleSize.X);
        Assert.Equal(full, fixture.LogicalSize);
        fixture.WaitForAnimation();
        Assert.Equal(full, fixture.Native.SizeFull);
        fixture.ClickArrow();
        fixture.Frame();
        fixture.WaitForAnimation();
        Assert.Equal(full, fixture.Native.SizeFull);
        Assert.All(fixture.Samples, sample => Assert.Equal((nint)430, sample.UserData));
    }

    [Fact]
    public void ContextReplacementAndReopeningCancelStaleTransientGeometry()
    {
        var fold = new MaterialWindowFold();
        using (var fixture = new WindowFixture(MaterialDensity.Compact, 1, fold: fold))
        {
            fixture.Settle();
            var full = fixture.Native.SizeFull;
            fixture.ClickArrow();
            fixture.Frame();
            Assert.True(fold.IsAnimating);
            fixture.ClosedFrames(3);
            fixture.Frame();
            Assert.False(fold.IsAnimating);
            Assert.False(fixture.Native.Collapsed);
            Assert.Equal(full, fixture.Native.SizeFull);
            fixture.ClickArrow();
            fixture.Frame();
            Assert.True(fold.IsAnimating);
        }
        using (var fixture = new WindowFixture(MaterialDensity.Standard, 1.5f, fold: fold))
        {
            fixture.RequestedSize = new(700, 580);
            fixture.Frame();
            Assert.False(fold.IsAnimating);
            Assert.False(fixture.Native.Collapsed);
            Assert.Equal(fixture.RequestedSize, fixture.Native.SizeFull);
        }
    }

    [Fact]
    public void PreparationMustFinishInItsOriginalNativeContext()
    {
        using var host = new NativeTestContext();
        var theme = new MaterialTheme(new Vector3(.3f, .7f, .4f));
        var fold = new MaterialWindowFold();
        host.Frame(() =>
        {
            using var scope = MaterialTheme.Push(theme, 1, MaterialStyleMode.ColorsOnly);
            var original = ImGui.GetCurrentContext();
            fold.PreDraw("Context-bound###context-fold");
            var other = ImGui.CreateContext();
            ImGui.SetCurrentContext(other);
            try { Assert.Throws<InvalidOperationException>(() => fold.PostDraw()); }
            finally
            {
                ImGui.DestroyContext(other);
                ImGui.SetCurrentContext(original);
            }
            fold.PostDraw();
            Assert.False(ImGui.GetCurrentContext().NextWindowData.Flags.HasFlag(ImGuiNextWindowDataFlags.HasSizeConstraint));
        });
    }

    [Fact]
    public void UnusedPreparationRestoresTheNativeConstraintCallbackAndBalancedScopes()
    {
        using var host = new NativeTestContext();
        var theme = new MaterialTheme(new Vector3(.3f, .7f, .4f));
        var fold = new MaterialWindowFold();
        ImGuiSizeCallback previous = data => data->DesiredSize.X = 330;
        host.Frame(() =>
        {
            using var scope = MaterialTheme.Push(theme, 1, MaterialStyleMode.ColorsOnly);
            ImGui.SetNextWindowSizeConstraints(new(250, 150), new(600, 500), previous);
            var next = ImGui.GetCurrentContext().NextWindowData;
            Assert.True(fold.PreDraw("Unused###unused-fold"));
            fold.PostDraw();
            Assert.Equal((nint)next.SizeCallback, (nint)ImGui.GetCurrentContext().NextWindowData.SizeCallback);
            Assert.Equal(next.SizeConstraintRect.Min, ImGui.GetCurrentContext().NextWindowData.SizeConstraintRect.Min);
            Assert.Equal(next.SizeConstraintRect.Max, ImGui.GetCurrentContext().NextWindowData.SizeConstraintRect.Max);
            ImGui.SetNextWindowSize(new(450, 300));
            ImGui.Begin("Unused###unused-fold");
            Assert.Equal(330, ImGui.GetWindowSize().X);
            ImGui.End();
        });
        GC.KeepAlive(previous);
    }

    private static void AssertClose(float expected, float actual)
        => Assert.InRange(MathF.Abs(expected - actual), 0, 1.01f);

    private static void AssertIniSize(string ini, Vector2 size)
        => Assert.Contains(string.Create(CultureInfo.InvariantCulture, $"Size={(int)size.X},{(int)size.Y}"), ini);

    private readonly record struct CallbackSample(Vector2 DesiredSize, Vector2 CurrentSize, bool Collapsed, nint UserData);

    private sealed unsafe class WindowFixture : IDisposable
    {
        private readonly NativeTestContext host;
        private readonly MaterialDensity density;
        private readonly float scale;
        private readonly bool useFold;
        private readonly bool recordCallback;
        private readonly ImGuiSizeCallback recorder;
        private const string Name = "Native fold###fold-native";
        private readonly List<CallbackSample> samples = [];
        public MaterialTheme Theme { get; }
        public MaterialWindowFold Fold { get; }
        public ImGuiWindowPtr Native => ImGuiP.FindWindowByName(Name);
        public IReadOnlyList<CallbackSample> Samples => samples;
        public ImGuiSizeCallback? SecondPass { get; set; }
        public Action? AfterEnd { get; set; }
        public ImGuiWindowFlags Flags { get; set; }
        public ImGuiCond SizeCondition { get; set; } = ImGuiCond.FirstUseEver;
        public Vector2 RequestedSize { get; set; }
        public Vector2? BodySizeRequest { get; set; }
        public bool WidthOnly { get; set; }
        public bool ReducedMotion { get; set; }
        public bool SaveDuringDraw { get; set; }
        public string SavedDuringDraw { get; private set; } = "";
        public bool Visible { get; private set; }
        public bool Open = true;
        public Vector2 VisibleSize { get; private set; }
        public Vector2 LogicalSize { get; private set; }
        public float TitleHeight { get; private set; }
        public uint ArrowId { get; private set; }
        public uint BodyId { get; private set; }
        public int Actions { get; private set; }
        public int MaximumCallbackCount { get; private set; }
        public nint CallbackUserData { get; set; }
        public bool UseUserDataWidth { get; set; }
        private Vector2 bodyCenter;
        private Vector2 arrowCenter;
        private Vector2 titleCenter;

        public WindowFixture(MaterialDensity density, float scale, bool useFold = true, bool recordCallback = false,
            MaterialWindowFold? fold = null)
        {
            this.density = density;
            this.scale = scale;
            this.useFold = useFold;
            this.recordCallback = recordCallback;
            Fold = fold ?? new();
            RequestedSize = new(400 * scale, 340 * scale);
            Theme = new(new Vector3(.3f, .7f, .4f), density);
            host = new NativeTestContext(1600, 1200, scale);
            recorder = data =>
            {
                if (UseUserDataWidth) data->DesiredSize.X = (nint)data->UserData * scale;
                samples.Add(new(data->DesiredSize, data->CurrentSize, ImGuiP.GetCurrentWindow().Collapsed, (nint)data->UserData));
                if (samples.Count == 2) SecondPass?.Invoke(data);
            };
        }

        public void Frame() => host.Frame(Draw);
        public void Idle(int frames) { for (var i = 0; i < frames; i++) Frame(); }
        public void Settle() { Idle(5); ImGui.SaveIniSettingsToMemory(); }
        public void ClosedFrames(int frames)
        {
            Open = false;
            for (var i = 0; i < frames; i++) host.Frame(() => { });
            Open = true;
        }
        public void WaitForAnimation()
        {
            for (var i = 0; i < 80 && Fold.IsAnimating; i++) Frame();
            Assert.False(Fold.IsAnimating);
        }
        public void ClickArrow() => host.Click(arrowCenter, Draw);
        public void ClickBody() => host.Click(bodyCenter, Draw);
        public void DoubleClickTitle() { host.Click(titleCenter, Draw); host.Click(titleCenter, Draw); }
        public void DragResize(Vector2 delta)
        {
            var point = Native.Pos + Native.Size - new Vector2(3 * scale);
            ImGui.GetIO().AddMousePosEvent(point.X, point.Y); Frame();
            ImGui.GetIO().AddMouseButtonEvent(0, true); Frame();
            for (var i = 1; i <= 3; i++)
            {
                var moved = point + delta * i / 3;
                ImGui.GetIO().AddMousePosEvent(moved.X, moved.Y); Frame();
            }
            ImGui.GetIO().AddMouseButtonEvent(0, false); Frame();
            Frame();
        }

        private void Draw()
        {
            var context = ImGui.GetCurrentContext();
            var styles = context.StyleVarStack.Size;
            var colors = context.ColorStack.Size;
            var windows = context.CurrentWindowStack.Size;
            using (var theme = MaterialTheme.Push(Theme, scale, MaterialStyleMode.ColorsOnly))
            using (var chrome = MaterialWindowChrome.Push())
            {
                ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(4 * scale,
                    (density == MaterialDensity.Compact ? 4 : 7) * scale));
                ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, new Vector2(180, 160) * scale);
                try
                {
                    var nativeStyles = context.StyleVarStack.Size;
                    samples.Clear();
                    var minimum = new Vector2(240, 200) * scale;
                    var maximum = new Vector2(900, 700) * scale;
                    if (recordCallback) ImGui.SetNextWindowSizeConstraints(minimum, maximum, recorder, (void*)CallbackUserData);
                    var installed = useFold && Fold.PreDraw(Name, minimum, maximum, ReducedMotion);
                    if (!installed) ImGui.SetNextWindowSizeConstraints(minimum, maximum, recordCallback ? recorder : null,
                        (void*)CallbackUserData);
                    ImGui.SetNextWindowPos(new(40, 40), ImGuiCond.FirstUseEver);
                    ImGui.SetNextWindowSize(WidthOnly ? new(RequestedSize.X, 0) : RequestedSize,
                        WidthOnly ? ImGuiCond.Always : SizeCondition);
                    Visible = ImGui.Begin(Name, ref Open, Flags);
                    try
                    {
                        VisibleSize = ImGui.GetWindowSize();
                        LogicalSize = useFold ? Fold.GetLogicalSize() : VisibleSize;
                        TitleHeight = ImGuiP.TitleBarHeight(Native);
                        var title = ImGuiP.TitleBarRect(Native);
                        arrowCenter = title.Min + new Vector2(ImGui.GetFontSize() * .5f + ImGui.GetStyle().FramePadding.X,
                            TitleHeight * .5f);
                        titleCenter = new((title.Min.X + title.Max.X) * .5f, title.Min.Y + TitleHeight * .5f);
                        ArrowId = Native.GetID("#COLLAPSE");
                        if (Visible)
                        {
                            BodyId = ImGui.GetID("retained-action");
                            if (ImGui.Button("retained-action")) Actions++;
                            bodyCenter = (ImGui.GetItemRectMin() + ImGui.GetItemRectMax()) * .5f;
                            Assert.Equal(BodyId, ImGuiP.GetItemID());
                            ImGui.Dummy(new Vector2(100, 180) * scale);
                            if (BodySizeRequest is { } bodySize)
                            {
                                ImGui.SetWindowSize(bodySize);
                                BodySizeRequest = null;
                            }
                        }
                        if (SaveDuringDraw) SavedDuringDraw = ImGui.SaveIniSettingsToMemory();
                    }
                    finally { ImGui.End(); }
                    if (useFold) Fold.PostDraw();
                    AfterEnd?.Invoke();
                    Assert.Equal(nativeStyles, context.StyleVarStack.Size);
                    Assert.Equal(windows, context.CurrentWindowStack.Size);
                    Assert.Equal(Flags, Native.Flags);
                    MaximumCallbackCount = Math.Max(MaximumCallbackCount, samples.Count);
                }
                finally { ImGui.PopStyleVar(2); }
            }
            Assert.Equal(styles, context.StyleVarStack.Size);
            Assert.Equal(colors, context.ColorStack.Size);
        }

        public void Dispose() => host.Dispose();
    }
}
