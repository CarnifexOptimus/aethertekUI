using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace CombatReborn.MaterialUI;

public static class MaterialTable
{
    public static MaterialStyleScope PushStyle(bool alternateRows=true)
    {
        var c=MaterialTheme.Current.Colors;
        var style=new MaterialStyleScope();
        style.Style(ImGuiStyleVar.CellPadding,MaterialTheme.Metrics.CellPadding);
        style.Color(ImGuiCol.TableHeaderBg,c.SurfaceContainerHigh);
        style.Color(ImGuiCol.TableRowBg,Vector4.Zero);
        style.Color(ImGuiCol.TableRowBgAlt,alternateRows?MaterialColor.Alpha(c.OnSurface,.025f):Vector4.Zero);
        style.Color(ImGuiCol.TableBorderLight,MaterialColor.Alpha(c.OutlineVariant,.5f));
        style.Color(ImGuiCol.TableBorderStrong,c.OutlineVariant);
        return style;
    }
    /// <summary>Call within the row after a native Selectable. Does not own data or table flags.</summary>
    public static void RowState(bool selected,bool hovered,Vector4? semanticBackground=null)
    {
        var c=MaterialTheme.Current.Colors;
        if(semanticBackground.HasValue||selected||hovered)
            ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0,MaterialDraw.Color(
                semanticBackground??(selected?c.SecondaryContainer:MaterialColor.Alpha(c.OnSurface,.06f))));
    }
}
