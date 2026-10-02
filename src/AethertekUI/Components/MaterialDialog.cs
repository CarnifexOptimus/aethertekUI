using Dalamud.Bindings.ImGui;

namespace AethertekUI;

public static class MaterialDialog
{
    public static DialogScope Begin(string id,ref bool open,ImGuiWindowFlags flags=ImGuiWindowFlags.AlwaysAutoResize) => new(id,ref open,flags);
    public ref struct DialogScope
    {
        private bool active;
        private MaterialStyleScope style;
        public bool Visible { get; }
        internal DialogScope(string id,ref bool open,ImGuiWindowFlags flags)
        {
            style=new();
            style.Color(ImGuiCol.PopupBg,MaterialTheme.Current.SurfaceColor(MaterialTheme.Current.Colors.SurfaceContainerHigh));
            style.Style(ImGuiStyleVar.PopupRounding,MaterialTheme.Metrics.OuterRadius);
            active=Visible=ImGui.BeginPopupModal(id,ref open,flags);
        }
        public void Dispose()
        {
            if(active) ImGui.EndPopup();
            active=false;
            style.Dispose();
        }
    }
}
