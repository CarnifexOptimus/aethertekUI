using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Bindings.ImGui;

namespace AethertekUI;

/// <summary>Consumer-owned animation of one native window's existing collapse controls.</summary>
public sealed unsafe class MaterialWindowFold
{
    private readonly ImGuiSizeCallback callback;
    private readonly nint callbackAddress;
    private ImGuiContextPtr context;
    private ImGuiWindowPtr window;
    private ImGuiNextWindowData previousNext;
    private ImGuiSizeCallback? previousCallback;
    private ImGuiWindowFlags originalFlags;
    private ImGuiCond collapsedAllowFlags;
    private uint windowId;
    private int lastFrame = -1;
    private int callbackCount;
    private bool prepared;
    private bool initialized;
    private bool collapsedBeforeBegin;
    private bool targetCollapsed;
    private bool animate;
    private bool changedGeometry;
    private bool completed;
    private float progress = 1;
    private Vector2 transientSize;

    public MaterialWindowFold()
    {
        callback = OnSize;
        callbackAddress = Marshal.GetFunctionPointerForDelegate(callback);
    }

    /// <summary>Whether the current native window is between its expanded and collapsed heights.</summary>
    public bool IsAnimating { get; private set; }

    /// <summary>Expanded native pixel size, for geometry observers while the visible window folds.</summary>
    public Vector2 ExpandedSize { get; private set; }

    /// <summary>
    /// Call last in PreDraw, under the theme, before native Begin. Optional constraints are pixels.
    /// When true is returned, suppress any later host constraint setter; restore it after PostDraw.
    /// </summary>
    public bool PreDraw(string windowName, Vector2? minimumSize = null, Vector2? maximumSize = null,
        bool reducedMotion = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(windowName);
        if (minimumSize.HasValue != maximumSize.HasValue)
            throw new ArgumentException("Supply both size-constraint bounds.");
        if (prepared) throw new InvalidOperationException("Pair PreDraw with PostDraw before reusing the fold.");

        var current = ImGui.GetCurrentContext();
        if (current.IsNull) throw new InvalidOperationException("A native ImGui context is required.");
        var frame = ImGui.GetFrameCount();
        var native = ImGuiP.FindWindowByName(windowName);
        if (context != current || frame <= lastFrame || frame > lastFrame + 1 ||
            (!native.IsNull && native.ID != windowId))
        {
            initialized = false;
            IsAnimating = false;
        }

        context = current;
        window = native;
        lastFrame = frame;
        prepared = true;
        callbackCount = 0;
        changedGeometry = false;
        completed = false;
        animate = !reducedMotion && MaterialTheme.Current.Motion.Enabled;
        previousNext = current.NextWindowData;
        previousCallback = null;

        if (!native.IsNull)
        {
            collapsedBeforeBegin = native.Collapsed;
            collapsedAllowFlags = native.SetWindowCollapsedAllowFlags;
            if (!initialized)
            {
                windowId = native.ID;
                targetCollapsed = native.Collapsed;
                progress = native.Collapsed ? 0 : 1;
                ExpandedSize = native.SizeFull;
                initialized = true;
            }
            if (!animate && IsAnimating)
            {
                native.Collapsed = targetCollapsed;
                progress = targetCollapsed ? 0 : 1;
                collapsedBeforeBegin = targetCollapsed;
                IsAnimating = false;
                ImGuiP.MarkIniSettingsDirty(native);
            }
        }

        if (!animate) return false;
        var hasConstraints = previousNext.Flags.HasFlag(ImGuiNextWindowDataFlags.HasSizeConstraint);
        var minimum = minimumSize ?? (hasConstraints ? previousNext.SizeConstraintRect.Min : Vector2.Zero);
        var maximum = maximumSize ?? (hasConstraints ? previousNext.SizeConstraintRect.Max : new Vector2(float.MaxValue));
        if (minimumSize.HasValue && (maximum.X < minimum.X || maximum.Y < minimum.Y))
            maximum = new(float.MaxValue);
        if (hasConstraints && previousNext.SizeCallback != null)
            previousCallback = Marshal.GetDelegateForFunctionPointer<ImGuiSizeCallback>((nint)previousNext.SizeCallback);
        ImGui.SetNextWindowSizeConstraints(minimum, maximum, callback);
        return true;
    }

    /// <summary>Call first in PostDraw, after native End, including when Begin returned false.</summary>
    public void PostDraw()
    {
        if (!prepared) return;
        if (ImGui.GetCurrentContext() != context)
            throw new InvalidOperationException("Finish the fold in the native context that prepared it.");

        if (callbackCount == 0 && (nint)context.NextWindowData.SizeCallback == callbackAddress)
        {
            ref var next = ref context.NextWindowData;
            next.SizeConstraintRect = previousNext.SizeConstraintRect;
            next.SizeCallback = previousNext.SizeCallback;
            next.SizeCallbackUserData = previousNext.SizeCallbackUserData;
            next.Flags = (next.Flags & ~ImGuiNextWindowDataFlags.HasSizeConstraint) |
                         (previousNext.Flags & ImGuiNextWindowDataFlags.HasSizeConstraint);
        }
        if (!window.IsNull)
        {
            if (changedGeometry)
            {
                // A retained control may explicitly resize its window from Draw.
                if (window.SizeFull != transientSize) ExpandedSize = window.SizeFull;
                window.SizeFull = ExpandedSize;
                window.Flags = originalFlags;
            }
            else
            {
                ExpandedSize = window.SizeFull;
                if (!animate)
                {
                    targetCollapsed = window.Collapsed;
                    progress = targetCollapsed ? 0 : 1;
                }
            }
            if (completed) ImGuiP.MarkIniSettingsDirty(window);
        }
        prepared = false;
        previousCallback = null;
    }

    /// <summary>Read inside this window's Draw when observing saved or repairable geometry.</summary>
    public Vector2 GetLogicalSize()
        => IsAnimating && prepared && !window.IsNull && ImGuiP.GetCurrentWindow() == window
            ? ExpandedSize : ImGui.GetWindowSize();

    private void OnSize(ImGuiSizeCallbackData* data)
    {
        if (previousCallback is not null)
        {
            var userData = data->UserData;
            data->UserData = previousNext.SizeCallbackUserData;
            previousCallback(data);
            data->UserData = userData;
        }
        // The focused native fixture verifies this sizing phase separately from auto-fit
        // and later manual-resize callbacks; do not advance motion on those other calls.
        if (++callbackCount != 2) return;
        window = ImGuiP.GetCurrentWindow();
        var flags = window.Flags;
        var collapsed = window.Collapsed;
        const ImGuiWindowFlags excluded = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar |
                                         ImGuiWindowFlags.ChildWindow | ImGuiWindowFlags.Tooltip |
                                         ImGuiWindowFlags.Popup | ImGuiWindowFlags.DockNodeHost |
                                         ImGuiWindowFlags.NoMouseInputs;
        if (!initialized || window.ID != windowId || (flags & excluded) != 0 || window.DockIsActive)
        {
            windowId = window.ID;
            targetCollapsed = collapsed;
            progress = collapsed ? 0 : 1;
            initialized = true;
            IsAnimating = false;
            return;
        }

        ref var next = ref context.NextWindowData;
        var hasCollapseCommand = next.Flags.HasFlag(ImGuiNextWindowDataFlags.HasCollapsed) &&
            (next.CollapsedCond is ImGuiCond.None or ImGuiCond.Always ||
             (collapsedAllowFlags & next.CollapsedCond) != 0);
        if (hasCollapseCommand) targetCollapsed = collapsed;
        else if (collapsed != collapsedBeforeBegin) targetCollapsed = !targetCollapsed;

        var target = targetCollapsed ? 0f : 1f;
        progress += (target - progress) * (1 - MathF.Exp(-18 * Math.Clamp(ImGui.GetIO().DeltaTime, 0, .1f)));
        if (MathF.Abs(progress - target) < .001f) progress = target;
        var wasAnimating = IsAnimating;
        IsAnimating = progress != target;
        if (!IsAnimating)
        {
            window.Collapsed = targetCollapsed;
            completed = wasAnimating;
            return;
        }

        var expanded = data->DesiredSize;
        if ((flags & (ImGuiWindowFlags.ChildWindow | ImGuiWindowFlags.AlwaysAutoResize)) == 0)
        {
            expanded = Vector2.Max(expanded, ImGui.GetStyle().WindowMinSize);
            expanded.Y = MathF.Max(expanded.Y, ImGuiP.TitleBarHeight(window) + ImGuiP.MenuBarHeight(window) +
                                              MathF.Max(0, ImGui.GetStyle().WindowRounding - 1));
        }
        ExpandedSize = expanded;
        originalFlags = flags;
        changedGeometry = true;
        window.Flags = flags | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoResize |
                       ImGuiWindowFlags.NoSavedSettings;
        window.Collapsed = false;
        data->DesiredSize = new(expanded.X, ImGuiP.TitleBarHeight(window) +
                                         (expanded.Y - ImGuiP.TitleBarHeight(window)) * progress);
        transientSize = new(MathF.Floor(data->DesiredSize.X), MathF.Floor(data->DesiredSize.Y));
    }
}
