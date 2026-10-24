using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace AethertekUI;

public enum MaterialElevation { Lowest, Low, Container, High, Highest }

public static class MaterialSurface
{
    public static SurfaceScope Begin(string id, Vector2 size = default, MaterialElevation elevation = MaterialElevation.Container,
        bool outline = false, float? padding = null, float? radius = null, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
        => new(id,size,elevation,outline,padding,radius,flags);
    public static SurfaceScope Card(string id, Vector2 size = default, bool outline = false)
        => Begin(id,size,MaterialElevation.Low,outline);

    public static Vector4 Color(MaterialElevation elevation)
    {
        var c=MaterialTheme.Current.Colors;
        return elevation switch {
            MaterialElevation.Lowest=>c.SurfaceContainerLowest,MaterialElevation.Low=>c.SurfaceContainerLow,
            MaterialElevation.High=>c.SurfaceContainerHigh,MaterialElevation.Highest=>c.SurfaceContainerHighest,_=>c.SurfaceContainer };
    }
    public static bool ClickableCard(string id, string title, string? detail = null, Vector2 size = default, bool selected = false, bool disabled = false)
    {
        var m=MaterialTheme.Metrics; var c=MaterialTheme.Current.Colors;
        var text=MaterialText.Measure(title);
        var titleHeight=MaterialNativeCaption.IsShaped(title)?text.Y:ImGui.GetTextLineHeight();
        var detailSize=detail is null?Vector2.Zero:MaterialText.Measure(detail);
        var height=m.TileHeight;
        if(MaterialNativeCaption.IsShaped(title) || (detail is not null && MaterialNativeCaption.IsShaped(detail)))
            height=Math.Max(height,2*m.Padding+text.Y+(detail is null?0:m.Gap+detailSize.Y));
        var actual=MaterialDraw.Size(size,Math.Max(m.Padding*2+text.X,180*m.Scale),height);
        ImGui.PushID(id);
        var clicked=MaterialDraw.Hit("##card",actual,disabled,out var state);
        MaterialDraw.Shape(state,selected?c.PrimaryContainer:c.SurfaceContainerLow,c.OnSurface,m.OuterRadius,true);
        if(state.Visible)
        {
            var dl=ImGui.GetWindowDrawList();
            dl.PushClipRect(state.Min+new Vector2(m.Padding),state.Max-new Vector2(m.Padding),true);
            MaterialText.AddText(dl,state.Min+new Vector2(m.Padding),MaterialDraw.Color(c.OnSurface,state.Alpha),title);
            if(detail is not null) MaterialText.AddText(dl,state.Min+new Vector2(m.Padding,m.Padding+titleHeight+m.Gap),MaterialDraw.Color(c.OnSurfaceVariant,state.Alpha),detail);
            dl.PopClipRect();
        }
        ImGui.PopID();
        return clicked;
    }

    public ref struct SurfaceScope
    {
        private MaterialStyleScope style;
        private bool active;
        public bool Visible { get; }
        internal SurfaceScope(string id,Vector2 size,MaterialElevation elevation,bool outline,float? padding,float? radius,ImGuiWindowFlags flags)
        {
            var m=MaterialTheme.Metrics;
            style=new();
            var material = MaterialSkin.HasMaterial;
            var rounding = radius.HasValue?radius.Value*m.Scale:m.OuterRadius;
            style.Color(ImGuiCol.ChildBg,material?Vector4.Zero:MaterialTheme.Current.SurfaceColor(Color(elevation)));
            style.Style(ImGuiStyleVar.WindowPadding,new Vector2(padding.HasValue?padding.Value*m.Scale:m.Padding));
            style.Style(ImGuiStyleVar.ChildRounding,rounding);
            style.Style(ImGuiStyleVar.ChildBorderSize,outline?m.Border:0);
            var ownerDrawList = ImGui.GetWindowDrawList();
            Visible=ImGui.BeginChild(id,m.Scaled(size),outline,flags|ImGuiWindowFlags.AlwaysUseWindowPadding);
            active=true;
            if (Visible && material)
            {
                MaterialSkin.PaintPanel(ImGui.GetWindowPos(),ImGui.GetWindowPos()+ImGui.GetWindowSize(),
                    elevation==MaterialElevation.Lowest,Color(elevation),rounding,ImGui.GetStyle().Alpha,ownerDrawList);
                if (outline)
                    ownerDrawList.AddRect(ImGui.GetWindowPos()+new Vector2(m.Border*.5f),
                        ImGui.GetWindowPos()+ImGui.GetWindowSize()-new Vector2(m.Border*.5f),
                        MaterialDraw.Color(MaterialTheme.Current.Colors.OutlineVariant,ImGui.GetStyle().Alpha),rounding,ImDrawFlags.None,m.Border);
            }
        }
        public void Dispose()
        {
            if(!active) return;
            ImGui.EndChild();
            style.Dispose();
            active=false;
        }
    }
}
