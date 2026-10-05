using System.Numerics;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;

namespace AethertekUI.Dalamud;

/// <summary>Consumer-owned bridge between one Dalamud window and its native fold.</summary>
public sealed class MaterialWindowMotion
{
    private readonly MaterialWindowFold fold = new();
    private Window? preparedWindow;
    private WindowSizeConstraints? constraints;
    private bool replacedConstraints;

    public bool IsAnimating => fold.IsAnimating;

    /// <summary>Call last in the owner's PreDraw, under its theme.</summary>
    public void Prepare(Window window, bool reducedMotion = false)
    {
        ArgumentNullException.ThrowIfNull(window);
        if (preparedWindow is not null)
            throw new InvalidOperationException("Restore the prepared window before preparing another frame.");
        constraints = window.SizeConstraints;
        var scale = ImGuiHelpers.GlobalScale;
        replacedConstraints = fold.PreDraw(window.WindowName,
            constraints?.MinimumSize * scale, constraints?.MaximumSize * scale, reducedMotion);
        preparedWindow = window;
        if (replacedConstraints) window.SizeConstraints = null;
    }

    /// <summary>Call first in PostDraw, after native End, including when collapsed.</summary>
    public void Restore(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);
        if (preparedWindow is null) return;
        if (!ReferenceEquals(window, preparedWindow))
            throw new InvalidOperationException("Restore the same window that was prepared.");
        fold.PostDraw();
        if (replacedConstraints && window.SizeConstraints is null) window.SizeConstraints = constraints;
        preparedWindow = null;
        constraints = null;
        replacedConstraints = false;
    }

    /// <summary>Read inside the owner's Draw when observing saved or repairable geometry.</summary>
    public Vector2 GetLogicalSize() => fold.GetLogicalSize();
}
