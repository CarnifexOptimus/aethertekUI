using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace AethertekUI;

public static class MaterialContextMenu
{
    public static PopupScope Begin(string id, ImGuiPopupFlags flags = ImGuiPopupFlags.MouseButtonRight) => new(id, flags);
    public ref struct PopupScope
    {
        private bool active;
        public bool Visible => active;
        internal PopupScope(string id, ImGuiPopupFlags flags) => active = ImGui.BeginPopupContextItem(id, flags);
        public void Dispose() { if (!active) return; ImGui.EndPopup(); active = false; }
    }
}

public static class MaterialTree
{
    public static TreeScope Node(string id, string label, ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.None) => new(id,label,flags);
    public ref struct TreeScope
    {
        private bool active;
        private readonly bool popTree;
        public bool Visible { get; }
        internal TreeScope(string id, string label, ImGuiTreeNodeFlags flags)
        {
            ImGui.PushID(id);
            Visible = ImGui.TreeNodeEx("##node", flags, label);
            popTree = Visible && (flags & ImGuiTreeNodeFlags.NoTreePushOnOpen) == 0;
            active = true;
        }
        public void Dispose()
        {
            if (!active) return;
            if (popTree) ImGui.TreePop();
            ImGui.PopID();
            active = false;
        }
    }
}

/// <summary>Domain colors are passed through unchanged. Theme scopes never reinterpret their meaning.</summary>
public static class MaterialSemantic
{
    public static MaterialStyleScope Text(Vector4 color)
    {
        var scope = new MaterialStyleScope();
        scope.Color(ImGuiCol.Text, color);
        return scope;
    }
    public static MaterialStyleScope Colors(ReadOnlySpan<(ImGuiCol Role, Vector4 Color)> overrides)
    {
        var scope = new MaterialStyleScope();
        foreach (var entry in overrides) scope.Color(entry.Role, entry.Color);
        return scope;
    }
    public static void Feedback(string text, Vector4? color = null)
    {
        using var scope = Text(color ?? MaterialTheme.Current.Colors.OnSurfaceVariant);
        ImGui.TextWrapped(text);
    }
    public static void Tooltip(string text, bool allowDisabled = true)
    {
        if (ImGui.IsItemHovered(allowDisabled ? ImGuiHoveredFlags.AllowWhenDisabled : ImGuiHoveredFlags.None))
            ImGui.SetTooltip(text);
    }
}
