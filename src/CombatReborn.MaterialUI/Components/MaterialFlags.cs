using Dalamud.Bindings.ImGui;

namespace CombatReborn.MaterialUI;

public readonly record struct MaterialBitOption(string Key, string Label, ulong Mask, bool Disabled = false);

public static class MaterialFlags
{
    public static bool IsSet(ulong value, ulong mask) => mask != 0 && (value & mask) == mask;
    public static ulong Set(ulong value, ulong mask, bool selected) => selected ? value | mask : value & ~mask;
    public static bool Draw(string id, ReadOnlySpan<MaterialBitOption> options, ref ulong value, bool disabled = false)
    {
        var changed=false;
        ImGui.PushID(id);
        foreach(var option in options)
        {
            var selected=IsSet(value,option.Mask);
            if(MaterialSelection.Checkbox(option.Key,option.Label,ref selected,disabled||option.Disabled||option.Mask==0))
            { value=Set(value,option.Mask,selected); changed=true; }
        }
        ImGui.PopID();
        return changed;
    }
}

public static class MaterialOverride
{
    private static readonly MaterialSegment<bool?>[] Choices =
        [new(null,"Inherit",Key:"inherit"),new(true,"On",Key:"on"),new(false,"Off",Key:"off")];
    public static bool Switch(string id, ref bool? value, float width = -1, bool disabled = false)
        => MaterialSegmented.Draw<bool?>(id,Choices,ref value,width,disabled);
}
