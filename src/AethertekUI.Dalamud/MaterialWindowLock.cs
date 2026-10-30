using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace AethertekUI.Dalamud;

/// <summary>Temporarily locks native movement and resizing without changing saved pin or Escape behavior.</summary>
public sealed class MaterialWindowLock
{
    private Window? preparedWindow;
    private ImGuiWindowFlags previousFlags;

    /// <summary>Call last in PreDraw. The consumer owns the saved lock preference.</summary>
    public void Apply(Window window, bool locked)
    {
        ArgumentNullException.ThrowIfNull(window);
        if (preparedWindow != null) throw new InvalidOperationException("Restore the locked window before applying another frame.");
        if (!locked) return;
        previousFlags = window.Flags;
        preparedWindow = window;
        window.Flags |= ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize;
    }

    /// <summary>Call first in PostDraw, including collapsed frames, to restore the owner's exact flags.</summary>
    public void Restore(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);
        if (preparedWindow == null) return;
        if (!ReferenceEquals(window, preparedWindow)) throw new InvalidOperationException("Restore the same window that was locked.");
        window.Flags = previousFlags;
        preparedWindow = null;
    }
}
