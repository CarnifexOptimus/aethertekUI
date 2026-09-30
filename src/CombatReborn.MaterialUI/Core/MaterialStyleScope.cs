using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace CombatReborn.MaterialUI;

/// <summary>Stack-only owner of exactly the style/color entries it pushes. Do not copy.</summary>
public ref struct MaterialStyleScope
{
    private int colors;
    private int styles;
    public void Color(ImGuiCol role, Vector4 color) { ImGui.PushStyleColor(role,color); colors++; }
    public void Style(ImGuiStyleVar role, float value) { ImGui.PushStyleVar(role,value); styles++; }
    public void Style(ImGuiStyleVar role, Vector2 value) { ImGui.PushStyleVar(role,value); styles++; }
    public void Dispose()
    {
        if (styles > 0) ImGui.PopStyleVar(styles);
        if (colors > 0) ImGui.PopStyleColor(colors);
        styles = colors = 0;
    }
}
