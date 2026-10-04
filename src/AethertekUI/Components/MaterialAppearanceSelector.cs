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
    {
        ImGui.PushID(id);
        try { return DrawAccent(ref accent,labels); }
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

    private static bool DrawAccent(ref Vector3 accent,MaterialAppearanceLabels labels)
    {
        var accentChanged=false;
        var colors=MaterialTheme.Current.Colors;
        var swatch=new MaterialControlAppearance(new(accent,1),colors.OnSurface,colors.OutlineVariant);
        if(MaterialButton.Draw("accent", "", swatch, new(28,28))) ImGui.OpenPopup("color");
        if(ImGui.IsItemHovered()) ImGui.SetTooltip(labels.Color);
        if(ImGui.BeginPopup("color"))
        {
            (string Id,string Label,Vector3 Color)[] presets=[("teal",labels.Teal,new(28/255f,201/255f,230/255f)),
                ("blue",labels.Blue,new(66/255f,133/255f,244/255f)),("pink",labels.Pink,new(236/255f,92/255f,160/255f))];
            foreach(var p in presets)
                if(MaterialButton.Draw(p.Id,p.Label,new MaterialControlAppearance(new(p.Color,1),colors.OnSurface,colors.OutlineVariant)))
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
