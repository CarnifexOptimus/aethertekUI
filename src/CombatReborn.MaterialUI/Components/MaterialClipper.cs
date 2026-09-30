using Dalamud.Bindings.ImGui;

namespace CombatReborn.MaterialUI;

/// <summary>Native clipper scope for uniformly spaced rows. Do not use for variable-height trees.</summary>
public ref struct MaterialClipper
{
    private ImGuiListClipperPtr clipper;
    private bool active;
    private readonly int count;
    public int Start => clipper.DisplayStart;
    public int End => clipper.DisplayEnd;
    public MaterialClipper(int count, float rowHeight = -1)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
        if (!float.IsFinite(rowHeight) || (rowHeight <= 0 && rowHeight != -1))
            throw new ArgumentOutOfRangeException(nameof(rowHeight));
        clipper = ImGui.ImGuiListClipper();
        this.count = count;
        active = true;
        clipper.Begin(count, rowHeight);
    }
    public bool Step() => active && clipper.Step();
    /// <summary>Call before Step to include a selected/focused row outside the visible range.</summary>
    public void Include(int index)
    {
        if (!active || index < 0 || index >= count) throw new ArgumentOutOfRangeException(nameof(index));
        clipper.ForceDisplayRangeByIndices(index,index+1);
    }
    public void Dispose()
    {
        if (!active) return;
        clipper.End();
        clipper.Destroy();
        active = false;
    }
}
