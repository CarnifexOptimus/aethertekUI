using Dalamud.Bindings.ImGui;

namespace AethertekUI;

/// <summary>Explicit outer-window styling for hosts retaining their own layout.</summary>
public static class MaterialWindowChrome
{
    /// <summary>Enter after the theme and before native Begin; keep active through End.</summary>
    public static MaterialStyleScope Push()
    {
        var scope = new MaterialStyleScope();
        scope.Style(ImGuiStyleVar.WindowRounding, MaterialTheme.Metrics.OuterRadius);
        return scope;
    }
}
