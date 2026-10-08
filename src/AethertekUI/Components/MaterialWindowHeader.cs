using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Bindings.ImGui;

namespace AethertekUI;

public readonly record struct MaterialHeaderAction(string Id,MaterialIcon Icon,string? Tooltip=null);

public static class MaterialWindowHeader
{
    /// <summary>Paint after native End and motion restore, before complete-window opacity. The host owns the texture.</summary>
    public static unsafe void PaintTitle(ImGuiWindowPtr window, string visibleTitle,
        ImTextureID texture, Vector2 textureSize, float extraRightWidth = 0, bool showCloseButton = true)
    {
        if (window.IsNull || !window.Active || (window.Flags & ImGuiWindowFlags.NoTitleBar) != 0) return;
        var context = ImGui.GetCurrentContext();
        for (var i = 0; i < context.CurrentWindowStack.Size; i++)
            if (context.CurrentWindowStack.Data[i].Window == window.Handle)
                throw new InvalidOperationException("Paint the title after its final native End.");
        var style = ImGui.GetStyle();
        // Begin stores the combined title/menu height in InnerRect. Native title
        // helpers instead recalculate it from the current font, which a consumer
        // may have changed before this after-End repaint.
        var titleHeight = window.InnerRect.Min.Y - window.Pos.Y;
        if ((window.Flags & ImGuiWindowFlags.MenuBar) != 0)
            titleHeight = (titleHeight - window.DC.MenuBarOffset.Y) * .5f;
        var title = window.InnerRect;
        title.Min = window.Pos;
        title.Max = window.Pos + new Vector2(window.Size.X, titleHeight);
        var fontSize = titleHeight - 2 * style.FramePadding.Y;
        if (fontSize <= 0) return;
        var gap = style.ItemInnerSpacing.X;
        var collapse = (window.Flags & (ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.Modal)) == 0;
        var left = collapse && style.WindowMenuButtonPosition == ImGuiDir.Left;
        var right = collapse && style.WindowMenuButtonPosition == ImGuiDir.Right;
        var min = title.Min + new Vector2(window.WindowBorderSize + style.FramePadding.X
            + (left ? fontSize + gap : 0), 0);
        var max = new Vector2(title.Max.X - window.WindowBorderSize - style.FramePadding.X
            - ((showCloseButton ? 1 : 0) + (right ? 1 : 0)) * (fontSize + gap)
            - Math.Max(0, extraRightWidth), title.Max.Y);
        var list = window.DrawList;
        var nativeTitle = MaterialNativeCaption.Visible(Marshal.PtrToStringUTF8((nint)window.Name) ?? string.Empty);
        // Erase only native caption glyphs. Repainting a background would multiply alpha
        // and cover the retained rounded chrome or native/custom title controls.
        var paintFont = ImGui.GetFont();
        MaterialNativeCaption.HideGlyphs(list, 0, paintFont, nativeTitle, title.Min, title.Max);
        // A consumer may borrow a different body font after End. The original
        // native caption can still belong to the host's atlas/default font.
        var hostFonts = ImGui.GetIO().Fonts.Fonts;
        for (var i = 0; i < hostFonts.Size; i++)
            if (hostFonts[i] != paintFont)
                MaterialNativeCaption.HideGlyphs(list, 0, hostFonts[i], nativeTitle, title.Min, title.Max);
        if (max.X <= min.X) return;
        list.PushClipRect(min, max, false);
        try
        {
            var iconMin = new Vector2(min.X, title.Min.Y + (title.Max.Y - title.Min.Y - fontSize) * .5f);
            MaterialCanvas.DrawImage(list, texture, textureSize, iconMin, iconMin + new Vector2(fontSize));
            var text = MaterialNativeCaption.Visible(visibleTitle);
            var size = MaterialNativeCaption.IsShaped(text)
                ? MaterialTextRenderer.Current!.GetLayout(text, fontSize).Size
                : ImGui.CalcTextSize(text) * (fontSize / ImGui.GetFontSize());
            MaterialText.AddText(list, ImGui.GetFont(), fontSize,
                new Vector2(min.X + fontSize + gap, title.Min.Y + (title.Max.Y - title.Min.Y - size.Y) * .5f),
                ImGui.GetColorU32(ImGuiCol.Text), text);
        }
        finally { list.PopClipRect(); }
    }

    /// <returns>Index of the activated action, or -1.</returns>
    public static int Draw(string title,string? subtitle,ReadOnlySpan<MaterialHeaderAction> actions,bool compact=false)
    {
        var m=MaterialTheme.Metrics; var c=MaterialTheme.Current.Colors;
        var origin=ImGui.GetCursorScreenPos();
        var width=ImGui.GetContentRegionAvail().X;
        var text=MaterialText.Measure(title);
        var detail=subtitle is null?Vector2.Zero:MaterialText.Measure(subtitle);
        var titleHeight=MaterialNativeCaption.IsShaped(title)?text.Y:ImGui.GetTextLineHeight();
        var detailHeight=subtitle is not null && MaterialNativeCaption.IsShaped(subtitle)?detail.Y:ImGui.GetTextLineHeight();
        var height=compact||subtitle is null?m.ButtonHeight:Math.Max(m.ButtonHeight,titleHeight+detailHeight+m.Gap);
        if(MaterialNativeCaption.IsShaped(title)) height=Math.Max(height,text.Y);
        var actionWidth=actions.Length*(m.ButtonHeight+m.Gap);
        var dl=ImGui.GetWindowDrawList();
        dl.PushClipRect(origin,origin+new Vector2(Math.Max(0,width-actionWidth),height),true);
        MaterialText.AddText(dl,origin,MaterialDraw.Color(c.OnSurface),title);
        if(!compact&&subtitle is not null) MaterialText.AddText(dl,origin+new Vector2(0,titleHeight+m.Gap),MaterialDraw.Color(c.OnSurfaceVariant),subtitle);
        dl.PopClipRect();
        ImGui.BeginGroup();
        var result=-1;
        for(var i=0;i<actions.Length;i++)
        {
            ImGui.SetCursorScreenPos(origin+new Vector2(width-actionWidth+i*(m.ButtonHeight+m.Gap),0));
            if(MaterialButton.IconButton(actions[i].Id,actions[i].Icon)) result=i;
            if(actions[i].Tooltip is not null&&ImGui.IsItemHovered()) MaterialSemantic.DrawTooltip(actions[i].Tooltip!);
        }
        ImGui.SetCursorScreenPos(origin); ImGui.Dummy(new(width,height)); ImGui.EndGroup();
        return result;
    }
}
