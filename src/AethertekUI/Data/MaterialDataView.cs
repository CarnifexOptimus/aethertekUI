namespace AethertekUI;

/// <summary>Host-owned cached view. Rebuild explicitly when data, query or sort changes; no per-frame filtering.</summary>
public sealed class MaterialDataView<T>
{
    private readonly List<T> rows = new();
    public IReadOnlyList<T> Rows => rows;
    public int Count => rows.Count;
    public T this[int index] => rows[index];
    public int Revision { get; private set; }

    public void Rebuild(IReadOnlyList<T> source, Predicate<T>? filter = null, IComparer<T>? comparer = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        // Keep the old view valid if a consumer filter/comparer throws.
        var next = new List<T>(source.Count);
        for (var i = 0; i < source.Count; i++)
            if (filter is null || filter(source[i])) next.Add(source[i]);
        if (comparer is not null) next.Sort(comparer);
        rows.Clear();
        rows.AddRange(next);
        Revision++;
    }
}

/// <summary>Selection uses domain keys, so sorting and filtering never silently retarget it.</summary>
public sealed class MaterialSelectionSet<TKey> where TKey : notnull
{
    private readonly HashSet<TKey> keys = new();
    public int Count => keys.Count;
    public bool Contains(TKey key) => keys.Contains(key);
    public void Clear() => keys.Clear();
    public bool Toggle(TKey key) => keys.Remove(key) || keys.Add(key);
    public void SelectOnly(TKey key) { keys.Clear(); keys.Add(key); }
    public void Set(TKey key, bool selected) { if (selected) keys.Add(key); else keys.Remove(key); }
    public void Prune(ISet<TKey> existingKeys) => keys.RemoveWhere(key => !existingKeys.Contains(key));
}

/// <summary>Explicit host-driven navigation, without stealing keyboard/gamepad input.</summary>
public static class MaterialNavigationMath
{
    public static int NextEnabled(ReadOnlySpan<bool> enabled, int current, int direction, bool wrap = true)
    {
        if (direction is not (-1 or 1)) throw new ArgumentOutOfRangeException(nameof(direction));
        if (enabled.IsEmpty) return -1;
        var index = Math.Clamp(current, -1, enabled.Length);
        for (var visited = 0; visited < enabled.Length; visited++)
        {
            index += direction;
            if (index < 0 || index >= enabled.Length)
            {
                if (!wrap) return current;
                index = direction > 0 ? 0 : enabled.Length - 1;
            }
            if (enabled[index]) return index;
        }
        return current;
    }
}
