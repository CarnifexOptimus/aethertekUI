using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace AethertekUI;

public static class MaterialNavigation
{
    public static MaterialSurface.SurfaceScope Rail(string id,bool expanded=true,float height=0)
    {
        var m=MaterialTheme.Metrics;
        return MaterialSurface.Begin(id,new((expanded?m.RailWidth:m.CompactRailWidth)/m.Scale,height),MaterialElevation.Lowest,padding:6);
    }
    public static bool Item(string id,string label,MaterialIcon icon,bool selected,bool expanded=true,bool disabled=false)
    {
        var m=MaterialTheme.Metrics; var c=MaterialTheme.Current.Colors;
        var controls=MaterialControls.Metrics;
        var size=new Vector2(Math.Max(1,ImGui.GetContentRegionAvail().X),controls.Height+controls.Gap);
        ImGui.PushID(id);
        var clicked=MaterialDraw.Hit("##navigation",size,disabled,out var state);
        var emphasis=MaterialDraw.Animate(state.Id,3,selected?1:0);
        var bg=MaterialColor.Alpha(c.SecondaryContainer,emphasis);
        MaterialDraw.Shape(state,bg,c.OnSecondaryContainer,size.Y*.5f);
        if(expanded) MaterialDraw.Content(state,label,selected?c.OnSecondaryContainer:c.OnSurfaceVariant,icon,MaterialIcon.None,true);
        else if(state.Visible)
        {
            var iconSize=Math.Min(controls.IconSize,Math.Max(1,size.X-2*m.Scale));
            MaterialIcons.Draw(icon,state.Min+(size-new Vector2(iconSize))*.5f,iconSize,selected?c.OnSecondaryContainer:c.OnSurfaceVariant,state.Alpha);
        }
        ImGui.PopID();
        return clicked;
    }
    public static bool ListItem(string id,string label,bool selected=false,MaterialIcon icon=MaterialIcon.None,bool disabled=false)
        => Item(id,label,icon,selected,true,disabled);
}

/// <summary>Secondary sibling tabs using native tab lifecycle and behavior.</summary>
public static class MaterialTabs
{
    public static TabBarScope Begin(string id,ImGuiTabBarFlags flags=ImGuiTabBarFlags.None) => new(id,flags);
    public static TabItemScope Item(string label,ImGuiTabItemFlags flags=ImGuiTabItemFlags.None) => new(label,flags);
    public ref struct TabBarScope
    {
        private bool active;
        public bool Visible { get; }
        internal TabBarScope(string id,ImGuiTabBarFlags flags) { active=Visible=ImGui.BeginTabBar(id,flags); }
        public void Dispose() { if(active) ImGui.EndTabBar(); active=false; }
    }
    public ref struct TabItemScope
    {
        private bool active;
        public bool Visible { get; }
        internal TabItemScope(string label,ImGuiTabItemFlags flags) { active=Visible=ImGui.BeginTabItem(label,flags); }
        public void Dispose() { if(active) ImGui.EndTabItem(); active=false; }
    }
}
