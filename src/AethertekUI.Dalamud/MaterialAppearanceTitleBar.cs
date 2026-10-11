using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;

namespace AethertekUI.Dalamud;

public readonly record struct MaterialAppearanceTitleBarLabels(string Language, string Compact, string Transparency, string Colour);
public readonly record struct MaterialAppearanceTitleBarVisibility(bool Language = true, bool Compact = true, bool Transparency = true, bool Colour = true);

/// <summary>Manual appearance shortcuts; the consumer retains editors, saved settings and window lifecycle.</summary>
public sealed class MaterialAppearanceTitleBar
{
    private readonly Window owner;
    private readonly Action drawLanguage;
    private readonly Action drawTransparency;
    private readonly Action drawColour;
    private readonly Func<MaterialAppearanceTitleBarVisibility>? visibility;
    private readonly List<(TitleBarButton Button, Func<MaterialAppearanceTitleBarVisibility, bool> Visible)> buttons = new();
    private MaterialAppearanceTitleBarVisibility visible;
    private string? requestedPopup;
    private bool popupOpen;

    private MaterialAppearanceTitleBar(Window owner, Func<MaterialAppearanceTitleBarLabels> labels,
        Action drawLanguage, Action toggleCompact, Action drawTransparency, Action drawColour,
        Func<MaterialAppearanceTitleBarVisibility>? visibility)
    {
        this.owner = owner;
        this.drawLanguage = drawLanguage;
        this.drawTransparency = drawTransparency;
        this.drawColour = drawColour;
        this.visibility = visibility;
        var priority = owner.TitleBarButtons.Count == 0 ? 0 : owner.TitleBarButtons.Min(button => button.Priority);
        Add(FontAwesomeIcon.Language, () => labels().Language, () => requestedPopup = "language", value => value.Language);
        Add(FontAwesomeIcon.CompressAlt, () => labels().Compact, toggleCompact, value => value.Compact);
        Add(FontAwesomeIcon.Adjust, () => labels().Transparency, () => requestedPopup = "transparency", value => value.Transparency);
        Add(FontAwesomeIcon.Palette, () => labels().Colour, () => requestedPopup = "colour", value => value.Colour);
        RefreshVisibility();

        void Add(FontAwesomeIcon icon, Func<string> tooltip, Action click, Func<MaterialAppearanceTitleBarVisibility, bool> shown)
        {
            // The current host sorts descending, then assigns IDs by index.
            // Append below existing custom priorities to retain their IDs.
            priority = (int)Math.Max((long)priority - 1, int.MinValue + 1L);
            buttons.Add((new()
            {
                Icon = icon,
                Priority = priority,
                IconOffset = new(2, 1),
                Click = button => { if (button == ImGuiMouseButton.Left && shown(GetVisibility())) click(); },
                ShowTooltip = () => MaterialText.SetTooltip(tooltip()),
            }, shown));
        }
    }

    /// <summary>Register once after the owner's existing titlebar buttons. Labels are read when hovered.</summary>
    public static MaterialAppearanceTitleBar Register(Window owner, Func<MaterialAppearanceTitleBarLabels> labels,
        Action drawLanguage, Action toggleCompact, Action drawTransparency, Action drawColour,
        Func<MaterialAppearanceTitleBarVisibility>? visibility = null)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(labels);
        ArgumentNullException.ThrowIfNull(drawLanguage);
        ArgumentNullException.ThrowIfNull(toggleCompact);
        ArgumentNullException.ThrowIfNull(drawTransparency);
        ArgumentNullException.ThrowIfNull(drawColour);
        return new(owner, labels, drawLanguage, toggleCompact, drawTransparency, drawColour, visibility);
    }

    /// <summary>Call before native Begin to retain the consumer's existing main-window visibility preferences.</summary>
    public void RefreshVisibility()
    {
        visible = GetVisibility();
        foreach (var (button, shown) in buttons)
        {
            if (!shown(visible)) owner.TitleBarButtons.Remove(button);
            else if (!owner.TitleBarButtons.Contains(button)) owner.TitleBarButtons.Add(button);
        }
    }

    private MaterialAppearanceTitleBarVisibility GetVisibility()
        => visibility?.Invoke() ?? new(true, true, true, true);

    /// <summary>
    /// Draw after native End and motion restore, before opacity, including collapsed frames.
    /// A central host may instead call after WindowSystem.Draw, consistently in that scope.
    /// </summary>
    public void DrawPopups()
    {
        var requested = requestedPopup;
        requestedPopup = null;
        if (!owner.IsOpen) { popupOpen = false; return; }
        if (requested is null && !popupOpen) return;
        var native = ImGuiP.FindWindowByName(owner.WindowName);
        if (native.IsNull || native.LastFrameActive != ImGui.GetFrameCount()) { popupOpen = false; return; }
        // A second balanced Begin appends no body or decorations. It supplies
        // the real parent stack even when the first Begin was collapsed, so
        // existing owner focus and complete-window opacity include the popup.
        ImGui.Begin(owner.WindowName, native.Flags);
        try
        {
            ImGui.PushID("aethertek-appearance-titlebar");
            try
            {
                if (requested is not null && (requested switch
                    { "language" => visible.Language, "transparency" => visible.Transparency, "colour" => visible.Colour, _ => false }))
                    ImGui.OpenPopup(requested);
                Draw("language", drawLanguage, visible.Language);
                Draw("transparency", drawTransparency, visible.Transparency);
                Draw("colour", drawColour, visible.Colour);
                popupOpen = ImGui.IsPopupOpen("language") || ImGui.IsPopupOpen("transparency") || ImGui.IsPopupOpen("colour");
            }
            finally { ImGui.PopID(); }
        }
        finally { ImGui.End(); }

        static void Draw(string id, Action editor, bool shown)
        {
            if (!ImGui.BeginPopup(id, ImGuiWindowFlags.NoSavedSettings)) return;
            try { if (shown) editor(); else ImGui.CloseCurrentPopup(); }
            finally { ImGui.EndPopup(); }
        }
    }
}
