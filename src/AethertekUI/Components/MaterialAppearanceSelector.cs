using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace AethertekUI;

public readonly record struct MaterialAppearanceChange(bool AccentChanged, bool LanguageChanged);
public readonly record struct MaterialAppearanceLabels(string Color, string Language, string Teal, string Blue, string Pink, string Custom);

/// <summary>Consumer owns labels, language options, edit draft, persistence, and frame-boundary theme updates.</summary>
public static class MaterialAppearanceSelector
{
    public static MaterialAppearanceChange Draw(string id, ref Vector3 accent, ref string language,
        MaterialOptions<string> languages, MaterialAppearanceLabels labels, float languageWidth = 112)
    {
        var accentChanged=DrawAccent(id,ref accent,labels);
        ImGui.SameLine();
        var changed=DrawLanguage(id,ref language,languages,languageWidth);
        return new(accentChanged,changed);
    }

    /// <summary>Place the colour control separately while retaining the combined selector's native identity.</summary>
    public static bool DrawAccent(string id,ref Vector3 accent,MaterialAppearanceLabels labels)
        => DrawAccent(id,ref accent,labels,28);

    /// <summary>Consumer-selected swatch size in logical units; native identity and change semantics are retained.</summary>
    public static bool DrawAccent(string id,ref Vector3 accent,MaterialAppearanceLabels labels,float size)
    {
        if(!float.IsFinite(size) || size<=0) throw new ArgumentOutOfRangeException(nameof(size));
        ImGui.PushID(id);
        try { return DrawAccent(ref accent,labels,size); }
        finally { ImGui.PopID(); }
    }

    /// <summary>Rectangular colour field with palette, swatch and chevron; logical dimensions and the existing accent identity.</summary>
    public static bool DrawAccent(string id,ref Vector3 accent,MaterialAppearanceLabels labels,Vector2 size)
    {
        if(!float.IsFinite(size.X) || !float.IsFinite(size.Y) || size.X<=0 || size.Y<=0)
            throw new ArgumentOutOfRangeException(nameof(size));
        ImGui.PushID(id);
        try
        {
            var colors=MaterialTheme.Current.Colors;
            var scale=MaterialTheme.Metrics.Scale;
            ImGui.PushID("accent");
            bool pressed;
            try
            {
                pressed=MaterialDraw.Hit("##button",size*scale,false,out var state);
                MaterialDraw.Shape(state,colors.SurfaceContainerLowest,colors.OnSurface,Math.Min(4,Math.Min(size.X,size.Y)*.5f)*scale,true);
                if(state.Visible)
                {
                    var extent=state.Max-state.Min;
                    var center=(state.Min+state.Max)*.5f;
                    var inset=Math.Min(12*scale,extent.X*.12f);
                    var swatch=Math.Min(28*scale,Math.Min(extent.Y*.6f,extent.X*.24f));
                    var paletteSpace=extent.X*.5f-swatch*.5f-inset-Math.Min(6*scale,extent.X*.04f);
                    var icon=Math.Min(MaterialControls.Metrics.IconSize,Math.Min(extent.Y*.7f,Math.Max(0,paletteSpace)));
                    var chevron=Math.Min(16*scale,Math.Min(extent.Y*.35f,extent.X*.12f));
                    MaterialIcons.Draw(MaterialIcon.Palette,new(state.Min.X+inset,center.Y-icon*.5f),icon,colors.OnSurfaceVariant,state.Alpha);
                    var list=ImGui.GetWindowDrawList();
                    list.AddRectFilled(center-new Vector2(swatch*.5f),center+new Vector2(swatch*.5f),
                        MaterialDraw.Color(new(accent,1),state.Alpha),3*scale);
                    list.AddRect(center-new Vector2(swatch*.5f),center+new Vector2(swatch*.5f),
                        MaterialDraw.Color(colors.OutlineVariant,state.Alpha),3*scale);
                    MaterialIcons.Draw(MaterialIcon.ChevronDown,new(state.Max.X-inset-chevron,center.Y-chevron*.5f),
                        chevron,colors.OnSurfaceVariant,state.Alpha);
                }
            }
            finally { ImGui.PopID(); }
            if(pressed) ImGui.OpenPopup("color");
            if(ImGui.IsItemHovered()) ImGui.SetTooltip(labels.Color);
            return DrawColorPopup(ref accent,labels);
        }
        finally { ImGui.PopID(); }
    }

    /// <summary>Place the language field separately while retaining its native identity.</summary>
    public static bool DrawLanguage(string id,ref string language,MaterialOptions<string> languages,float languageWidth=112)
    {
        ImGui.PushID(id);
        try { return DrawLanguage(ref language,languages,languageWidth); }
        finally { ImGui.PopID(); }
    }

    private static bool DrawLanguage(ref string language,MaterialOptions<string> languages,float languageWidth)
    {
        var colors=MaterialTheme.Current.Colors;
        using var controls=MaterialControls.Push();
        var scale=MaterialTheme.Metrics.Scale;
        var iconSize=Math.Min(MaterialControls.Metrics.IconSize,MaterialControls.Metrics.Height);
        var minimum=MathF.Ceiling(ImGui.CalcTextSize(languages.LabelFor(language,"Select...")).X
            + MaterialControls.Metrics.Height + 3*MaterialControls.Metrics.Gap + iconSize);
        var pixels=MaterialLayout.FitNextItemWidth(languageWidth<0?languageWidth:languageWidth*scale,minimum);
        var changed=MaterialCombo.Draw("language",languages,ref language,
            MaterialComboLayout.Default with { ConstrainFieldWidth=false,LeadingIcon=MaterialIcon.Globe },
            new MaterialControlAppearance(colors.SurfaceContainerLowest,colors.OnSurface,colors.OutlineVariant),pixels/scale,searchable:false);
        return changed;
    }

    /// <summary>Draw the consumer's existing native language widget in its original ID scope.</summary>
    public static MaterialAppearanceChange Draw(string id,ref Vector3 accent,MaterialAppearanceLabels labels,Func<bool> drawLanguage)
    {
        ArgumentNullException.ThrowIfNull(drawLanguage);
        ImGui.PushID(id);
        bool accentChanged;
        try
        {
            accentChanged=DrawAccent(ref accent,labels);
            DrawGlobe(labels.Language);
        }
        finally { ImGui.PopID(); }
        ImGui.SameLine();
        return new(accentChanged,drawLanguage());
    }

    private static bool DrawAccent(ref Vector3 accent,MaterialAppearanceLabels labels,float size=28)
    {
        var colors=MaterialTheme.Current.Colors;
        var swatch=new MaterialControlAppearance(new(accent,1),colors.OnSurface,colors.OutlineVariant);
        if(MaterialButton.Draw("accent", "", swatch, new(size,size))) ImGui.OpenPopup("color");
        if(ImGui.IsItemHovered()) ImGui.SetTooltip(labels.Color);
        return DrawColorPopup(ref accent,labels);
    }
    private static bool DrawColorPopup(ref Vector3 accent,MaterialAppearanceLabels labels)
    {
        var accentChanged=false;
        var colors=MaterialTheme.Current.Colors;
        if(ImGui.BeginPopup("color"))
        {
            (string Id,string Label,Vector3 Color)[] presets=[("teal",labels.Teal,new(28/255f,201/255f,230/255f)),
                ("blue",labels.Blue,new(66/255f,133/255f,244/255f)),("pink",labels.Pink,new(236/255f,92/255f,160/255f))];
            foreach(var p in presets)
                if(MaterialButton.Draw(p.Id,p.Label,new MaterialControlAppearance(new(p.Color,1),new(0,0,0,1),colors.OutlineVariant)))
                { accent=p.Color; accentChanged=true; }
            ImGui.TextUnformatted(labels.Custom);
            ImGui.ColorPicker3("##rgb",ref accent,ImGuiColorEditFlags.NoAlpha);
            accentChanged |= ImGui.IsItemDeactivatedAfterEdit();
            ImGui.EndPopup();
        }
        return accentChanged;
    }
    private static void DrawGlobe(string label)
    {
        ImGui.SameLine();
        MaterialButton.IconButton("globe",MaterialIcon.Globe,disabled:true,size:28);
        if(ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) ImGui.SetTooltip(label);
    }
}
