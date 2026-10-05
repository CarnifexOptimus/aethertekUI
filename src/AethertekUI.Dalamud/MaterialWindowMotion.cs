using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;

namespace AethertekUI.Dalamud;

/// <summary>Consumer-owned bridge between one Dalamud window and its native fold.</summary>
public sealed class MaterialWindowMotion
{
    private readonly MaterialWindowFold fold = new();
    private readonly MaterialWindowDecorations decorations = new();
    private readonly Action<ImGuiWindowPtr> prepareDecorations;
    private Window? preparedWindow;
    private WindowSizeConstraints? constraints;
    private bool replacedConstraints;

    public MaterialWindowMotion() => prepareDecorations = decorations.Prepare;

    public bool IsAnimating => fold.IsAnimating;

    /// <summary>Call last in the owner's PreDraw, under its theme.</summary>
    public void Prepare(Window window, bool reducedMotion = false) => Prepare(window, reducedMotion, false);

    /// <summary>Prepare native fold and optional owned-viewport rounded chrome together.</summary>
    public void Prepare(Window window, bool reducedMotion, bool roundedCorners)
    {
        ArgumentNullException.ThrowIfNull(window);
        if (preparedWindow is not null)
            throw new InvalidOperationException("Restore the prepared window before preparing another frame.");
        constraints = window.SizeConstraints;
        var scale = ImGuiHelpers.GlobalScale;
        replacedConstraints = fold.PreDraw(window.WindowName,
            constraints?.MinimumSize * scale, constraints?.MaximumSize * scale, reducedMotion,
            roundedCorners ? prepareDecorations : null);
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
        decorations.Paint();
        fold.PostDraw();
        if (replacedConstraints && window.SizeConstraints is null) window.SizeConstraints = constraints;
        preparedWindow = null;
        constraints = null;
        replacedConstraints = false;
    }

    /// <summary>Read inside the owner's Draw when observing saved or repairable geometry.</summary>
    public Vector2 GetLogicalSize() => fold.GetLogicalSize();

    /// <summary>Call first in Draw to paint owned-viewport chrome before consumer content.</summary>
    public void DrawChrome() => decorations.Paint();
}
