using Dalamud.Bindings.ImGui;

namespace AethertekUI;

/// <summary>Consumer-owned opacity and focus delay for one native window and its owned content.</summary>
public sealed unsafe class MaterialWindowOpacity
{
    private ImGuiContextPtr context;
    private uint windowId;
    private int lastFrame = -1;
    private double unfocusedSince;
    private bool initialized;
    private bool applicationFocused = true;

    public float CurrentOpacity { get; private set; } = 1;

    /// <summary>
    /// Call once after the owner's native End and after any decoration painting, under its theme.
    /// Percentages are fractions; disabling opacity restores the original emitted alpha.
    /// Preferences and storage belong to the consumer.
    /// </summary>
    public void Apply(string windowName, float opacity = 1, bool enabled = true,
        bool autoFade = true, float fadedOpacity = .5f, float unfocusedDelaySeconds = 10,
        bool reducedMotion = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(windowName);
        if (!float.IsFinite(opacity)) throw new ArgumentOutOfRangeException(nameof(opacity));
        if (!float.IsFinite(fadedOpacity)) throw new ArgumentOutOfRangeException(nameof(fadedOpacity));
        if (!float.IsFinite(unfocusedDelaySeconds) || unfocusedDelaySeconds < 0)
            throw new ArgumentOutOfRangeException(nameof(unfocusedDelaySeconds));
        var current = ImGui.GetCurrentContext();
        if (current.IsNull) throw new InvalidOperationException("A native ImGui context is required.");
        var frame = ImGui.GetFrameCount();
        var window = ImGuiP.FindWindowByName(windowName);
        if (window.IsNull || window.LastFrameActive != frame)
        {
            initialized = false;
            CurrentOpacity = 1;
            return;
        }
        for (var index = 0; index < current.CurrentWindowStack.Size; index++)
            if (current.CurrentWindowStack.Data[index].Window == window.Handle)
                throw new InvalidOperationException("Apply opacity after the owner's final native End.");
        if (initialized && context == current && windowId == window.ID && lastFrame == frame)
            throw new InvalidOperationException("Apply opacity only once per window frame.");

        var now = ImGui.GetTime();
        var restarted = !initialized || context != current || windowId != window.ID ||
            frame < lastFrame || frame > lastFrame + 1;
        var normal = enabled ? Math.Clamp(opacity, .1f, 1) : 1;
        if (restarted)
        {
            unfocusedSince = now;
            CurrentOpacity = normal;
            applicationFocused = true;
        }
        context = current;
        windowId = window.ID;
        lastFrame = frame;
        initialized = true;

        // ImGui clears its per-frame focus-loss flag; retain the actual focus events
        // so an idle unfocused application does not immediately look focused again.
        for (var index = 0; index < current.InputEventsTrail.Size; index++)
        {
            var input = current.InputEventsTrail.Data[index];
            if (input.Type == ImGuiInputEventType.Focus) applicationFocused = input.Union.AppFocused.Focused != 0;
        }
        var focused = applicationFocused && OwnedBy(current.NavWindow, window);
        if (focused || !enabled || !autoFade) unfocusedSince = now;
        var target = enabled && autoFade && !focused && now - unfocusedSince >= unfocusedDelaySeconds
            ? Math.Min(normal, Math.Clamp(fadedOpacity, .1f, 1)) : normal;
        if (focused || !enabled || restarted || reducedMotion || !MaterialTheme.Current.Motion.Enabled)
            CurrentOpacity = target;
        else
        {
            var step = Math.Clamp(ImGui.GetIO().DeltaTime / .2f, 0, 1);
            CurrentOpacity += Math.Clamp(target - CurrentOpacity, -step, step);
        }
        if (CurrentOpacity >= 1) return;

        // Native widgets, custom meshes and image tints all retain their original alpha.
        // Apply one multiplier to the emitted owner content, without global style changes.
        for (var index = 0; index < current.Windows.Size; index++)
        {
            var owned = new ImGuiWindowPtr(current.Windows.Data[index]);
            if (owned.LastFrameActive != frame || !OwnedBy(owned, window)) continue;
            var list = owned.DrawList;
            for (var vertexIndex = 0; vertexIndex < list.VtxBuffer.Size; vertexIndex++)
            {
                var vertex = list.VtxBuffer[vertexIndex];
                var alpha = (uint)MathF.Round((vertex.Col >> 24) * CurrentOpacity);
                vertex.Col = (vertex.Col & 0x00ffffff) | (alpha << 24);
                list.VtxBuffer[vertexIndex] = vertex;
            }
        }
    }

    private static bool OwnedBy(ImGuiWindowPtr candidate, ImGuiWindowPtr owner)
    {
        const ImGuiWindowFlags ownedFlags = ImGuiWindowFlags.ChildWindow |
            ImGuiWindowFlags.Popup | ImGuiWindowFlags.Tooltip;
        while (!candidate.IsNull)
        {
            if (candidate == owner) return true;
            if ((candidate.Flags & ownedFlags) == 0) return false;
            candidate = candidate.ParentWindowInBeginStack;
        }
        return false;
    }
}
