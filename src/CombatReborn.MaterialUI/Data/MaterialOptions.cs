namespace CombatReborn.MaterialUI;

/// <summary>Key must be unique and stable across reordering; it is independent of the visible label.</summary>
public readonly record struct MaterialOption<T>(string Key, T Value, string Label, bool Disabled = false);

/// <summary>Owned by the consumer. Caches filtered indices only when the query/options change.</summary>
public sealed class MaterialOptions<T>
{
    private MaterialOption<T>[] options;
    private string[] nativeLabels = [];
    private readonly List<int> filtered = new();
    private string query = string.Empty;
    public string Query => query;
    public int Count => filtered.Count;
    public MaterialOption<T> this[int filteredIndex] => options[filtered[filteredIndex]];
    internal string NativeLabel(int filteredIndex) => nativeLabels[filtered[filteredIndex]];
    public MaterialOptions(ReadOnlySpan<MaterialOption<T>> options) { this.options = []; Replace(options); }

    public void Replace(ReadOnlySpan<MaterialOption<T>> values)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var option in values)
            if (string.IsNullOrEmpty(option.Key) || !keys.Add(option.Key))
                throw new ArgumentException("Options need distinct, nonempty keys.", nameof(values));
        options = values.ToArray();
        nativeLabels = new string[options.Length];
        for (var i = 0; i < options.Length; i++) nativeLabels[i] = options[i].Label + "###option";
        Refilter();
    }

    public bool SetQuery(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value == query) return false;
        query = value;
        Refilter();
        return true;
    }

    public string LabelFor(T value, string fallback = "Select...")
    {
        foreach (var option in options)
            if (EqualityComparer<T>.Default.Equals(option.Value, value)) return option.Label;
        return fallback;
    }

    private void Refilter()
    {
        filtered.Clear();
        for (var i = 0; i < options.Length; i++)
            if (query.Length == 0 || options[i].Label.Contains(query, StringComparison.OrdinalIgnoreCase))
                filtered.Add(i);
    }
}
