using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;

namespace AethertekUI.Dalamud;

/// <summary>Optional adapter. The consumer retains subscriptions, windows, configuration and disposal.</summary>
public sealed class MaterialWindowHost
{
    public MaterialThemeController Controller { get; }
    public MaterialStyleMode StyleMode { get; set; } = MaterialStyleMode.Compatible;
    public MaterialWindowHost(MaterialThemeSettings? settings = null) => Controller = new(settings);

    /// <summary>Wraps the entire native window lifecycle, including PreDraw and PostDraw.</summary>
    public void Draw(WindowSystem windows, bool applyMetrics = true)
    {
        ArgumentNullException.ThrowIfNull(windows);
        using var scope = BeginFrame(applyMetrics);
        windows.Draw();
    }

    /// <summary>Explicit rounded outer chrome while preserving the selected host layout mode.</summary>
    public void Draw(WindowSystem windows, bool applyMetrics, bool roundedCorners)
    {
        ArgumentNullException.ThrowIfNull(windows);
        using var scope = BeginFrame(applyMetrics);
        if (roundedCorners)
        {
            using var chrome = MaterialWindowChrome.Push();
            windows.Draw();
        }
        else windows.Draw();
    }

    /// <summary>Use around a host's normal draw path. Dispose before leaving the UI thread/frame.</summary>
    public MaterialTheme.ThemeScope BeginFrame(bool applyMetrics = true)
        => MaterialTheme.Push(Controller.Theme, ImGuiHelpers.GlobalScale,
            applyMetrics || StyleMode == MaterialStyleMode.Native ? StyleMode : MaterialStyleMode.ColorsOnly);
}
