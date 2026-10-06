using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Bindings.ImGui;
using Xunit;

namespace AethertekUI.Tests;

public unsafe class WindowOpacityNativeTests
{
    [Theory]
    [InlineData(MaterialDensity.Compact, 1f)]
    [InlineData(MaterialDensity.Compact, 1.5f)]
    [InlineData(MaterialDensity.Standard, 1f)]
    [InlineData(MaterialDensity.Standard, 1.5f)]
    public void CompleteOwnedMeshesFadeOnceWithoutChangingOtherWindowsOrNativeState(MaterialDensity density, float scale)
    {
        using var scene = new Scene(density, scale) { Opacity = .5f, AutoFade = false, Extras = true };
        scene.Settle();
        Assert.Equal(.5f, scene.Fade.CurrentOpacity);
        Assert.True(scene.OwnedLists >= 4); // Root, child, popup and tooltip.
        Assert.True(scene.UnrelatedLists >= 1);
        Assert.True(scene.CheckedVertices > 100);
        var id = scene.ActionId;
        var size = scene.Root.SizeFull;
        var position = scene.Root.Pos;
        var saved = ImGui.SaveIniSettingsToMemory();
        scene.Extras = false;
        scene.Frame();
        scene.ClickAction();
        Assert.Equal(1, scene.Actions);
        Assert.Equal(id, scene.ActionId);
        Assert.Equal(size, scene.Root.SizeFull);
        Assert.Equal(position, scene.Root.Pos);
        Assert.Equal(saved, ImGui.SaveIniSettingsToMemory());
        scene.Enabled = false;
        scene.Frame();
        Assert.Equal(1, scene.Fade.CurrentOpacity);
    }

    [Theory]
    [InlineData(MaterialDensity.Compact, 1f)]
    [InlineData(MaterialDensity.Compact, 1.5f)]
    [InlineData(MaterialDensity.Standard, 1f)]
    [InlineData(MaterialDensity.Standard, 1.5f)]
    public void DefaultDelayUsesWindowFocusIncludingOwnedChildrenAndResetsAfterClosing(MaterialDensity density, float scale)
    {
        using var scene = new Scene(density, scale);
        scene.Settle();
        scene.Focus = scene.ChildName;
        scene.Frame();
        for (var i = 0; i < 12; i++) scene.Frame(1);
        Assert.Equal(1, scene.Fade.CurrentOpacity);
        scene.Focus = Scene.OtherName;
        scene.Frame();
        for (var i = 0; i < 9; i++) scene.Frame(1);
        Assert.Equal(1, scene.Fade.CurrentOpacity);
        scene.Frame(1);
        Assert.Equal(.5f, scene.Fade.CurrentOpacity);
        scene.Focus = scene.ChildName;
        scene.Frame();
        Assert.Equal(1, scene.Fade.CurrentOpacity);
        scene.Focus = Scene.OtherName;
        scene.Frame();
        for (var i = 0; i < 11; i++) scene.Frame(1);
        Assert.Equal(.5f, scene.Fade.CurrentOpacity);
        scene.ClosedFrame();
        scene.Frame();
        Assert.Equal(1, scene.Fade.CurrentOpacity);
        for (var i = 0; i < 9; i++) scene.Frame(1);
        Assert.Equal(1, scene.Fade.CurrentOpacity);
    }

    [Fact]
    public void HostFocusLossFadesEvenWithRetainedNativeNavigationOwner()
    {
        using var scene = new Scene(MaterialDensity.Standard, 1) { ReducedMotion = true };
        scene.Settle();
        scene.Focus = Scene.RootName;
        scene.Frame();
        Assert.Equal(1, scene.Fade.CurrentOpacity);
        var navigationOwner = (nint)ImGui.GetCurrentContext().NavWindow.Handle;
        ImGui.GetIO().AddFocusEvent(false);
        scene.Frame();
        Assert.True(scene.ObservedFocusEvents > 0);
        Assert.False(scene.LastFocusEvent);
        Assert.Equal(navigationOwner, scene.ObservedNavigationOwner);
        for (var i = 0; i < 9; i++) scene.Frame(1);
        Assert.Equal(1, scene.Fade.CurrentOpacity);
        scene.Frame(1);
        Assert.Equal(.5f, scene.Fade.CurrentOpacity);
        for (var i = 0; i < 3; i++) scene.Frame(1);
        Assert.Equal(.5f, scene.Fade.CurrentOpacity);
        ImGui.GetIO().AddFocusEvent(true);
        scene.Frame();
        Assert.True(scene.ObservedFocusEvents > 0);
        Assert.True(scene.LastFocusEvent);
        Assert.Equal(1, scene.Fade.CurrentOpacity);
    }

    [Fact]
    public void PreEndContractRejectsAnOpenOwnerBelowAnIndependentWindow()
    {
        using var scene = new Scene(MaterialDensity.Standard, 1) { VerifyPreEndContract = true };
        scene.Settle();
        Assert.Equal(1, scene.Fade.CurrentOpacity);
    }

    [Fact]
    public void PopupFocusAndMotionPreferenceRetainTheNormalOpacity()
    {
        using var scene = new Scene(MaterialDensity.Standard, 1) { Extras = true, Delay = 0 };
        scene.Settle();
        scene.Focus = scene.PopupName;
        scene.Frame();
        Assert.Equal(1, scene.Fade.CurrentOpacity);
        scene.Focus = Scene.OtherName;
        scene.Theme.Motion.Enabled = false;
        scene.Frame();
        Assert.Equal(.5f, scene.Fade.CurrentOpacity);
        scene.Theme.Motion.Enabled = true;
        scene.Focus = scene.PopupName;
        scene.Frame();
        Assert.Equal(1, scene.Fade.CurrentOpacity);
        scene.ReducedMotion = true;
        scene.Focus = Scene.OtherName;
        scene.Frame();
        Assert.Equal(.5f, scene.Fade.CurrentOpacity);
    }

    [Fact]
    public void CollapsedTitleClampingAndDuplicateFrameContractAreVerified()
    {
        using var scene = new Scene(MaterialDensity.Compact, 1) { AutoFade = false, Opacity = -2 };
        scene.Settle();
        Assert.Equal(.1f, scene.Fade.CurrentOpacity);
        scene.Collapsed = true;
        scene.Frame();
        Assert.True(scene.Root.Collapsed);
        Assert.True(scene.CheckedVertices > 0);
        scene.OpacityTwice = true;
        scene.Frame();
        scene.OpacityTwice = false;
        scene.Collapsed = false;
        scene.Opacity = 2;
        scene.Frame(1);
        Assert.Equal(1, scene.Fade.CurrentOpacity);
        scene.InvalidOpacity = true;
        scene.Frame();
    }

    private sealed class Scene : IDisposable
    {
        internal const string RootName = "Opacity owner###opacity-native-owner";
        internal const string OtherName = "Independent window###opacity-native-other";
        private readonly NativeTestContext host;
        private readonly float scale;
        private Vector2 actionCenter;
        internal readonly MaterialWindowOpacity Fade = new();
        internal readonly MaterialTheme Theme;
        internal float Opacity = 1;
        internal bool Enabled = true;
        internal bool AutoFade = true;
        internal bool ReducedMotion;
        internal bool Extras;
        internal bool OpacityTwice;
        internal bool InvalidOpacity;
        internal bool VerifyPreEndContract;
        internal int ObservedFocusEvents;
        internal bool LastFocusEvent;
        internal nint ObservedNavigationOwner;
        internal bool Collapsed;
        internal float Delay = 10;
        internal string? Focus;
        internal string ChildName = "";
        internal string PopupName = "";
        internal uint ActionId;
        internal int Actions;
        internal int OwnedLists;
        internal int UnrelatedLists;
        internal int CheckedVertices;
        internal ImGuiWindowPtr Root => ImGuiP.FindWindowByName(RootName);

        internal Scene(MaterialDensity density, float scale)
        {
            this.scale = scale;
            host = new(1600, 1200, scale);
            Theme = new(new Vector3(.3f, .7f, .4f), density);
        }
        internal void Settle() { for (var i = 0; i < 5; i++) Frame(); }
        internal void Frame(float deltaTime = 1f / 60)
        {
            ImGui.GetIO().DeltaTime = deltaTime;
            host.Frame(Draw);
        }
        internal void ClosedFrame() => host.Frame(() => { });
        internal void ClickAction() => host.Click(actionCenter, Draw);

        private void Draw()
        {
            using var theme = MaterialTheme.Push(Theme, scale, MaterialStyleMode.Material);
            var context = ImGui.GetCurrentContext();
            var styleAlpha = ImGui.GetStyle().Alpha;
            var styles = context.StyleVarStack.Size;
            var colors = context.ColorStack.Size;
            var stack = context.CurrentWindowStack.Size;
            ImGui.SetNextWindowPos(new(40, 40), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSize(new Vector2(420, 360) * scale, ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowCollapsed(Collapsed, ImGuiCond.Always);
            if (ImGui.Begin(RootName))
            {
                if (VerifyPreEndContract)
                {
                    ImGui.Begin("Nested independent window###opacity-native-nested");
                    Assert.Throws<InvalidOperationException>(() => Fade.Apply(RootName));
                    ImGui.End();
                }
                ActionId = ImGui.GetID("retained-action");
                if (ImGui.Button("retained-action")) Actions++;
                actionCenter = (ImGui.GetItemRectMin() + ImGui.GetItemRectMax()) * .5f;
                var list = ImGui.GetWindowDrawList();
                var point = ImGui.GetCursorScreenPos();
                list.AddRectFilled(point, point + new Vector2(35, 16), 0x80445566);
                list.AddImage(new ImTextureID((nint)2), point + new Vector2(40, 0), point + new Vector2(60, 16), Vector2.Zero, Vector2.One, 0xc0778899);
                ImGui.Dummy(new(70, 20));
                if (ImGui.BeginChild("owned-child", new Vector2(200, 80) * scale))
                {
                    ChildName = Marshal.PtrToStringUTF8((nint)ImGuiP.GetCurrentWindow().Name)!;
                    ImGui.TextUnformatted("Child text");
                    MaterialButton.IconButton("child-icon", MaterialIcon.Heart);
                }
                ImGui.EndChild();
                if (Extras)
                {
                    ImGui.OpenPopup("owned-popup");
                    if (ImGui.BeginPopup("owned-popup"))
                    {
                        PopupName = Marshal.PtrToStringUTF8((nint)ImGuiP.GetCurrentWindow().Name)!;
                        ImGui.TextUnformatted("Popup text");
                        ImGui.EndPopup();
                    }
                    ImGui.BeginTooltip();
                    ImGui.TextUnformatted("Tooltip text");
                    ImGui.EndTooltip();
                }
                else if (ImGui.BeginPopup("owned-popup"))
                {
                    ImGui.CloseCurrentPopup();
                    ImGui.EndPopup();
                }
            }
            ImGui.End();
            ImGui.SetNextWindowPos(new(900, 40), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSize(new(280, 180), ImGuiCond.FirstUseEver);
            if (ImGui.Begin(OtherName)) ImGui.TextUnformatted("This window keeps its alpha");
            ImGui.End();
            if (Focus is not null) { ImGui.SetWindowFocus(Focus); Focus = null; }
            var lists = new List<(ImGuiWindowPtr Window, ImDrawVert[] Vertices, ushort[] Indices)>();
            for (var index = 0; index < context.Windows.Size; index++)
            {
                var window = new ImGuiWindowPtr(context.Windows.Data[index]);
                if (window.LastFrameActive != ImGui.GetFrameCount() || window.DrawList.VtxBuffer.Size == 0) continue;
                var list = window.DrawList;
                var vertices = new ImDrawVert[list.VtxBuffer.Size];
                var indices = new ushort[list.IdxBuffer.Size];
                for (var i = 0; i < vertices.Length; i++) vertices[i] = list.VtxBuffer[i];
                for (var i = 0; i < indices.Length; i++) indices[i] = list.IdxBuffer[i];
                lists.Add((window, vertices, indices));
            }
            if (InvalidOpacity)
                Assert.Throws<ArgumentOutOfRangeException>(() => Fade.Apply(RootName, float.NaN));
            ObservedFocusEvents = 0;
            for (var eventIndex = 0; eventIndex < context.InputEventsTrail.Size; eventIndex++)
            {
                var input = context.InputEventsTrail.Data[eventIndex];
                if (input.Type != ImGuiInputEventType.Focus) continue;
                ObservedFocusEvents++;
                LastFocusEvent = input.Union.AppFocused.Focused != 0;
            }
            ObservedNavigationOwner = (nint)context.NavWindow.Handle;
            Fade.Apply(RootName, Opacity, Enabled, AutoFade, .5f, Delay, ReducedMotion);
            if (OpacityTwice) Assert.Throws<InvalidOperationException>(() => Fade.Apply(RootName));
            OwnedLists = UnrelatedLists = CheckedVertices = 0;
            foreach (var saved in lists)
            {
                var owned = IsOwned(saved.Window, Root);
                if (owned) OwnedLists++; else UnrelatedLists++;
                var list = saved.Window.DrawList;
                Assert.Equal(saved.Vertices.Length, list.VtxBuffer.Size);
                Assert.Equal(saved.Indices.Length, list.IdxBuffer.Size);
                for (var i = 0; i < saved.Indices.Length; i++) Assert.Equal(saved.Indices[i], list.IdxBuffer[i]);
                for (var i = 0; i < saved.Vertices.Length; i++)
                {
                    var before = saved.Vertices[i];
                    var after = list.VtxBuffer[i];
                    Assert.Equal(before.Pos, after.Pos);
                    Assert.Equal(before.Uv, after.Uv);
                    Assert.Equal(before.Col & 0x00ffffff, after.Col & 0x00ffffff);
                    var expected = owned ? (uint)MathF.Round((before.Col >> 24) * Fade.CurrentOpacity) : before.Col >> 24;
                    Assert.Equal(expected, after.Col >> 24);
                    if (owned) CheckedVertices++;
                }
            }
            Assert.Equal(styleAlpha, ImGui.GetStyle().Alpha);
            Assert.Equal(styles, context.StyleVarStack.Size);
            Assert.Equal(colors, context.ColorStack.Size);
            Assert.Equal(stack, context.CurrentWindowStack.Size);
        }

        private static bool IsOwned(ImGuiWindowPtr window, ImGuiWindowPtr root)
        {
            // The fixture explicitly knows its created root, child, popup and tooltip.
            var name = Marshal.PtrToStringUTF8((nint)window.Name)!;
            return window == root || name.Contains("owned-child", StringComparison.Ordinal) ||
                name.StartsWith("##Popup_", StringComparison.Ordinal) || name.StartsWith("##Tooltip_", StringComparison.Ordinal);
        }
        public void Dispose() => host.Dispose();
    }
}
