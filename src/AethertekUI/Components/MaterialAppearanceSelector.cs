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
        ImGui.PushID(id);
        var accentChanged=DrawAccent(ref accent,labels);
        var colors=MaterialTheme.Current.Colors;
        DrawGlobe(labels.Language);
        ImGui.SameLine();
        var changed=MaterialCombo.Draw("language",languages,ref language,
            new MaterialControlAppearance(colors.SurfaceContainerLowest,colors.OnSurface,colors.OutlineVariant),languageWidth,searchable:false);
        ImGui.PopID();
        return new(accentChanged,changed);
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
