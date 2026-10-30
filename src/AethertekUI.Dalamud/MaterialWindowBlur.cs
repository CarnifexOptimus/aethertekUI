using Dalamud.Interface.Windowing;

namespace AethertekUI.Dalamud;

/// <summary>Opt-in native host blur. The host retains its global preference and per-window strength overrides.</summary>
public static class MaterialWindowBlur
{
    /// <summary>Call before the consumer's WindowSystem.Draw. This never changes the host's saved preferences.</summary>
    public static void Configure(WindowSystem windows, MaterialColorSettings settings, MaterialColorMode mode, bool enabled = true)
    {
        ArgumentNullException.ThrowIfNull(windows);
        ArgumentNullException.ThrowIfNull(settings);
        var active = enabled && settings.HasBackgroundBlur(mode) && settings.BackgroundBlurPercent > 0;
        foreach (var window in windows.Windows) window.AllowBackgroundBlur = active;
    }
}
