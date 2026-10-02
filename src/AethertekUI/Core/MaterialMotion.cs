namespace AethertekUI;

/// <summary>UI-thread animation cache with a hard capacity and periodic stale-entry pruning.</summary>
public sealed class MaterialMotion(int capacity = 4096, int staleFrames = 180)
{
    private readonly Dictionary<ulong, Entry> states = new();
    private readonly List<ulong> expired = new();
    private int lastSweep = -1;
    private readonly record struct Entry(float Value, int Frame);
    public int Count => states.Count;
    public bool Enabled { get; set; } = true;

    public float Sample(ulong id, float target, float deltaTime, int frame)
    {
        if (capacity <= 0) return target;
        if (lastSweep < 0 || frame - lastSweep >= 60 || frame < lastSweep) { Prune(frame); lastSweep = frame; }
        if (!states.TryGetValue(id, out var entry) || frame - entry.Frame > staleFrames || frame < entry.Frame)
            entry = new(target, frame);
        var value = !Enabled ? target : entry.Frame == frame ? entry.Value
            : entry.Value + (target-entry.Value)*(1-MathF.Exp(-18*Math.Clamp(deltaTime,0,.1f)));
        if (MathF.Abs(value-target) < .001f) value = target;
        if (states.Count >= capacity && !states.ContainsKey(id))
        {
            // Capacity pressure is rare; evict the oldest without allocating a sort.
            var oldest = ulong.MaxValue;
            var oldestFrame = int.MaxValue;
            foreach (var pair in states) if (pair.Value.Frame < oldestFrame) { oldest = pair.Key; oldestFrame = pair.Value.Frame; }
            states.Remove(oldest);
        }
        states[id] = new(value, frame);
        return value;
    }
    public void Prune(int frame)
    {
        expired.Clear();
        foreach (var pair in states) if (frame < pair.Value.Frame || frame-pair.Value.Frame > staleFrames) expired.Add(pair.Key);
        foreach (var id in expired) states.Remove(id);
    }
    public void Clear() => states.Clear();
}
