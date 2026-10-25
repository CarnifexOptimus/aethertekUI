using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace AethertekUI;

public static class MaterialSwitch
{
    /// <summary>Measure a retained switch row in pixels using its actual wrapped caption and trailing controls.</summary>
    public static Vector2 MeasureRow(string display, string onLabel, string offLabel, float widthPixels, string? help = null)
        => MeasureRowCore(display, onLabel, offLabel, widthPixels, help).Size;

    /// <summary>Wrapped label, optional help and trailing switch/state, retaining GetID(nativeLabel). Width is already-scaled pixels.</summary>
    public static unsafe bool Row(string nativeLabel, string display, ref bool value, string onLabel, string offLabel,
        float widthPixels, string? help = null, bool disabled = false)
    {
        ArgumentNullException.ThrowIfNull(nativeLabel);
        var c = MaterialTheme.Current.Colors; var controls = MaterialControls.Metrics;
        var (track, stateSize, helpWidth, labelWidth, text, size) = MeasureRowCore(display, onLabel, offLabel, widthPixels, help);
        var tailWidth = track.X + controls.Gap + stateSize.X;
        var height = size.Y;
        bool changed; MaterialState state;
        using (var caption = new MaterialStyleScope())
        {
            caption.Color(ImGuiCol.Text, Vector4.Zero);
            changed = MaterialDraw.Hit(nativeLabel, size, disabled, out state);
        }
        var helpMin = state.Min + new Vector2(labelWidth + controls.Gap, (height - ImGui.GetTextLineHeight()) * .5f);
        var helpMax = helpMin + new Vector2(helpWidth, ImGui.GetTextLineHeight());
        var rowHelpHovered = helpWidth > 0 && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled);
        var helpHovered = rowHelpHovered && ImGui.IsMouseHoveringRect(helpMin, helpMax);
        if (changed && helpHovered && ImGui.IsMouseReleased(0)) changed = false;
        if (changed) { value = !value; ImGuiP.MarkItemEdited(state.Id); }
        var t = MaterialDraw.Animate(state.Id, 2, value ? 1 : 0);
        if (state.Visible)
        {
            var list = ImGui.GetWindowDrawList();
            list.PushClipRect(state.Min, state.Max, true);
            try
            {
                MaterialText.AddText(list, state.Min + new Vector2(0, (height - text.Y) * .5f), MaterialDraw.Color(c.OnSurface, state.Alpha), display, labelWidth);
                if (helpWidth > 0) MaterialText.AddText(list, helpMin, MaterialDraw.Color(c.OnSurfaceVariant, state.Alpha), "(?)");
                var trackMin = state.Min + new Vector2(size.X - tailWidth, (height - track.Y) * .5f);
                PaintTrack(state, trackMin, track, value, t);
                var current = value ? onLabel : offLabel;
                var currentSize = MaterialText.Measure(current);
                MaterialText.AddText(list, state.Min + new Vector2(size.X - stateSize.X, (height - currentSize.Y) * .5f),
                    MaterialDraw.Color(value ? c.Primary : c.OnSurfaceVariant, state.Alpha), current);
            }
            finally { list.PopClipRect(); }
        }
        if (rowHelpHovered)
        {
            var context = ImGui.GetCurrentContext();
            var lastItem = context.LastItemData;
            try { MaterialText.SetTooltip(help!); }
            finally { context.Handle->LastItemData = lastItem; }
        }
        return changed;
    }

    private static (Vector2 Track, Vector2 State, float HelpWidth, float LabelWidth, Vector2 Text, Vector2 Size)
        MeasureRowCore(string display, string onLabel, string offLabel, float widthPixels, string? help)
    {
        ArgumentNullException.ThrowIfNull(display);
        ArgumentNullException.ThrowIfNull(onLabel); ArgumentNullException.ThrowIfNull(offLabel);
        if (!float.IsFinite(widthPixels) || widthPixels <= 0) throw new ArgumentOutOfRangeException(nameof(widthPixels));
        var m = MaterialTheme.Metrics; var controls = MaterialControls.Metrics;
        var track = m.SwitchSize * Math.Min(1, (controls.Height - 2 * m.Scale) / m.SwitchSize.Y);
        var state = new Vector2(Math.Max(MaterialText.Measure(onLabel).X, MaterialText.Measure(offLabel).X),
            Math.Max(MaterialText.Measure(onLabel).Y, MaterialText.Measure(offLabel).Y));
        var helpWidth = string.IsNullOrEmpty(help) ? 0 : MaterialText.Measure("(?)").X;
        var tailWidth = track.X + controls.Gap + state.X;
        var labelWidth = Math.Max(1, widthPixels - tailWidth - controls.Gap - (helpWidth == 0 ? 0 : helpWidth + controls.Gap));
        var text = MaterialText.Measure(display, wrapWidth: labelWidth);
        var height = Math.Max(controls.Height, Math.Max(text.Y, state.Y) + 2 * m.Scale);
        var size = new Vector2(Math.Max(widthPixels, tailWidth + controls.Gap + (helpWidth == 0 ? 0 : helpWidth + controls.Gap) + 1), height);
        return (track, state, helpWidth, labelWidth, text, size);
    }

    public static bool Draw(string id, string label, ref bool value, bool disabled = false)
        => DrawCore(id,label,ref value,null,disabled);

    /// <summary>Track dimensions in logical units; retains the native button and original ID.</summary>
    public static bool Draw(string id, string label, ref bool value, Vector2 trackSize, bool disabled = false)
    {
        if(!float.IsFinite(trackSize.X) || !float.IsFinite(trackSize.Y) || trackSize.Y <= 6 || trackSize.X < trackSize.Y)
            throw new ArgumentOutOfRangeException(nameof(trackSize));
        return DrawCore(id,label,ref value,trackSize,disabled);
    }

    private static bool DrawCore(string id, string label, ref bool value, Vector2? trackSize, bool disabled)
    {
        var m=MaterialTheme.Metrics; var c=MaterialTheme.Current.Colors;
        var controls=MaterialControls.Metrics;
        var height=Math.Max(controls.Height,(trackSize?.Y ?? 0)*m.Scale);
        var text=MaterialText.Measure(label);
        var lineHeight=MaterialNativeCaption.IsShaped(label)?text.Y:ImGui.GetTextLineHeight();
        if(MaterialNativeCaption.IsShaped(label)) height=Math.Max(height,text.Y+2*m.Scale);
        var track=trackSize.HasValue ? trackSize.Value*m.Scale : m.SwitchSize*Math.Min(1,(height-2*m.Scale)/m.SwitchSize.Y);
        var width=MaterialLayout.ResolveWidth(0,track.X+(label.Length==0?0:controls.Gap+text.X),
            ImGui.GetContentRegionAvail().X,m.Scale);
        ImGui.PushID(id);
        var changed=MaterialDraw.Hit("##switch",new(width,height),disabled,out var state);
        if(changed) value=!value;
        var t=MaterialDraw.Animate(state.Id,2,value?1:0);
        if(state.Visible)
        {
            ImGui.GetWindowDrawList().PushClipRect(state.Min,state.Max,true);
            var min=state.Min+new Vector2(0,(height-track.Y)*.5f);
            PaintTrack(state,min,track,value,t);
            MaterialText.AddText(ImGui.GetWindowDrawList(),state.Min+new Vector2(track.X+controls.Gap,(height-lineHeight)*.5f),
                MaterialDraw.Color(c.OnSurface,state.Alpha),label);
            ImGui.GetWindowDrawList().PopClipRect();
        }
        ImGui.PopID();
        return changed;
    }

    private static void PaintTrack(in MaterialState state, Vector2 min, Vector2 track, bool value, float t)
    {
        var m = MaterialTheme.Metrics; var c = MaterialTheme.Current.Colors;
        var trackState = state with { Min = min, Max = min + track };
        var bg = Vector4.Lerp(c.SurfaceContainerHighest, c.Primary, t);
        MaterialDraw.Shape(trackState, bg, c.OnSurface, track.Y * .5f, !value);
        var inset = 3 * m.Scale;
        var radius = (track.Y - 2 * inset) * .5f;
        var center = min + new Vector2(inset + radius + t * (track.X - 2 * (inset + radius)), track.Y * .5f);
        var thumb = Vector4.Lerp(c.Outline, c.OnPrimary, t);
        var list = ImGui.GetWindowDrawList();
        list.AddCircleFilled(center, radius, MaterialDraw.Color(thumb, state.Alpha), 24);
        if (state.Hovered)
            list.AddCircle(center, radius + inset, MaterialDraw.Color(MaterialColor.Alpha(c.OnSurface, .12f), state.Alpha), 24, m.Border * 2);
    }
}

public static class MaterialSelection
{
    /// <summary>Wrap a retained native checkbox caption within a pixel width, preserving GetID(nativeLabel).</summary>
    /// <remarks>No component ID is added. The caller retains the value, change handler and existing parent ID scope.</remarks>
    public static bool Checkbox(string nativeLabel, string display, ref bool value, float widthPixels, bool disabled = false)
    {
        var (side, text, size, wrap) = RetainedSize(display, widthPixels);
        var changed = RetainedHit(nativeLabel, size, disabled, out var state);
        if (changed) { value = !value; ImGuiP.MarkItemEdited(state.Id); }
        if (state.Visible)
        {
            var m = MaterialTheme.Metrics; var c = MaterialTheme.Current.Colors;
            var box = state with { Min = state.Min + new Vector2(0, (size.Y - side) * .5f),
                Max = state.Min + new Vector2(side, (size.Y + side) * .5f) };
            var list = ImGui.GetWindowDrawList();
            list.PushClipRect(state.Min, state.Max, true);
            try
            {
                MaterialDraw.Shape(box, value ? c.Primary : c.SurfaceContainer, c.OnSurface, m.Border * 3, !value);
                if (value) MaterialIcons.Draw(MaterialIcon.Check, box.Min, side, c.OnPrimary, state.Alpha);
                MaterialText.AddText(list, state.Min + new Vector2(side + MaterialControls.Metrics.Gap, (size.Y - text.Y) * .5f),
                    MaterialDraw.Color(c.OnSurface, state.Alpha), display, wrap);
            }
            finally { list.PopClipRect(); }
        }
        return changed;
    }

    /// <summary>Wrap a retained native radio caption within a pixel width, preserving GetID(nativeLabel).</summary>
    public static bool Radio<T>(string nativeLabel, string display, T option, ref T value, float widthPixels, bool disabled = false)
    {
        var (side, text, size, wrap) = RetainedSize(display, widthPixels);
        var clicked = RetainedHit(nativeLabel, size, disabled, out var state);
        var selected = EqualityComparer<T>.Default.Equals(value, option);
        var changed = clicked && !selected;
        if (changed) { value = option; ImGuiP.MarkItemEdited(state.Id); }
        if (state.Visible)
        {
            var m = MaterialTheme.Metrics; var c = MaterialTheme.Current.Colors;
            var center = state.Min + new Vector2(side * .5f, size.Y * .5f);
            var list = ImGui.GetWindowDrawList();
            list.PushClipRect(state.Min, state.Max, true);
            try
            {
                list.AddCircle(center, side * .45f, MaterialDraw.Color(selected || changed || state.Focused ? c.Primary : c.Outline, state.Alpha),
                    20, m.Border * (state.Focused ? 2 : 1));
                if (selected || changed) list.AddCircleFilled(center, side * .25f, MaterialDraw.Color(c.Primary, state.Alpha), 16);
                MaterialText.AddText(list, state.Min + new Vector2(side + MaterialControls.Metrics.Gap, (size.Y - text.Y) * .5f),
                    MaterialDraw.Color(c.OnSurface, state.Alpha), display, wrap);
            }
            finally { list.PopClipRect(); }
        }
        return changed;
    }

    private static (float Side, Vector2 Text, Vector2 Size, float Wrap) RetainedSize(string display, float widthPixels)
    {
        ArgumentNullException.ThrowIfNull(display);
        if (!float.IsFinite(widthPixels) || widthPixels <= 0) throw new ArgumentOutOfRangeException(nameof(widthPixels));
        var m = MaterialTheme.Metrics; var controls = MaterialControls.Metrics;
        var side = Math.Min(controls.Height - 2 * m.Scale, Math.Max(controls.IconSize, ImGui.GetTextLineHeight()));
        var gap = display.Length == 0 ? 0 : controls.Gap;
        var width = Math.Max(side + gap + (display.Length == 0 ? 0 : 1), widthPixels);
        var wrap = Math.Max(1, width - side - gap);
        var text = MaterialText.Measure(display, wrapWidth: wrap);
        return (side, text, new(width, Math.Max(controls.Height, text.Y + 2 * m.Scale)), wrap);
    }

    private static bool RetainedHit(string nativeLabel, Vector2 size, bool disabled, out MaterialState state)
    {
        ArgumentNullException.ThrowIfNull(nativeLabel);
        using var caption = new MaterialStyleScope();
        caption.Color(ImGuiCol.Text, Vector4.Zero);
        return MaterialDraw.Hit(nativeLabel, size, disabled, out state);
    }

    public static bool Checkbox(string id, string label, ref bool value, bool disabled = false)
    {
        var m=MaterialTheme.Metrics; var c=MaterialTheme.Current.Colors;
        var controls=MaterialControls.Metrics;
        var text=MaterialText.Measure(label);
        var lineHeight=MaterialNativeCaption.IsShaped(label)?text.Y:ImGui.GetTextLineHeight();
        var side=Math.Min(controls.Height-2*m.Scale,Math.Max(controls.IconSize,ImGui.GetTextLineHeight()));
        var height=MaterialNativeCaption.IsShaped(label)?Math.Max(controls.Height,text.Y+2*m.Scale):controls.Height;
        var size=new Vector2(MaterialLayout.ResolveWidth(0,side+controls.Gap+text.X,
            ImGui.GetContentRegionAvail().X,m.Scale),height);
        ImGui.PushID(id);
        var changed=MaterialDraw.Hit("##checkbox",size,disabled,out var state);
        if(changed) value=!value;
        var box=state with { Min=state.Min+new Vector2(0,(size.Y-side)*.5f),Max=state.Min+new Vector2(side,(size.Y+side)*.5f) };
        if(state.Visible)
        {
            ImGui.GetWindowDrawList().PushClipRect(state.Min,state.Max,true);
            MaterialDraw.Shape(box,value?c.Primary:c.SurfaceContainer,c.OnSurface,m.Border*3,!value);
            if(value) MaterialIcons.Draw(MaterialIcon.Check,box.Min,side,c.OnPrimary,state.Alpha);
            MaterialText.AddText(ImGui.GetWindowDrawList(),state.Min+new Vector2(side+controls.Gap,(size.Y-lineHeight)*.5f),MaterialDraw.Color(c.OnSurface,state.Alpha),label);
            ImGui.GetWindowDrawList().PopClipRect();
        }
        ImGui.PopID();
        return changed;
    }
    public static bool Radio<T>(string id, string label, T option, ref T value, bool disabled = false)
    {
        var m=MaterialTheme.Metrics; var c=MaterialTheme.Current.Colors;
        var controls=MaterialControls.Metrics;
        var text=MaterialText.Measure(label);
        var lineHeight=MaterialNativeCaption.IsShaped(label)?text.Y:ImGui.GetTextLineHeight();
        var side=Math.Min(controls.Height-2*m.Scale,Math.Max(controls.IconSize,ImGui.GetTextLineHeight()));
        var height=MaterialNativeCaption.IsShaped(label)?Math.Max(controls.Height,text.Y+2*m.Scale):controls.Height;
        var size=new Vector2(MaterialLayout.ResolveWidth(0,side+controls.Gap+text.X,
            ImGui.GetContentRegionAvail().X,m.Scale),height);
        ImGui.PushID(id);
        var clicked=MaterialDraw.Hit("##radio",size,disabled,out var state);
        var selected=EqualityComparer<T>.Default.Equals(value,option);
        var changed=clicked&&!selected;
        if(changed) value=option;
        if(state.Visible)
        {
            var center=state.Min+new Vector2(side*.5f,size.Y*.5f);
            var dl=ImGui.GetWindowDrawList();
            dl.PushClipRect(state.Min,state.Max,true);
            dl.AddCircle(center,side*.45f,MaterialDraw.Color(selected||changed||state.Focused?c.Primary:c.Outline,state.Alpha),20,m.Border*(state.Focused?2:1));
            if(selected||changed) dl.AddCircleFilled(center,side*.25f,MaterialDraw.Color(c.Primary,state.Alpha),16);
            MaterialText.AddText(dl,state.Min+new Vector2(side+controls.Gap,(size.Y-lineHeight)*.5f),MaterialDraw.Color(c.OnSurface,state.Alpha),label);
            dl.PopClipRect();
        }
        ImGui.PopID();
        return changed;
    }
}
