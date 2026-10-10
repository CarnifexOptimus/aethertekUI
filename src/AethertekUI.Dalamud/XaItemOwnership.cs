using System.Globalization;
using System.Text.Json;

namespace AethertekUI.Dalamud;

public enum XaItemOwnershipState { Unknown, Owned, Missing }

/// <summary>
/// Reads positive, current-character XADB ownership evidence. An absent result is
/// unknown: the item API does not certify that all storage has been recorded.
/// Consumers own IPC subscriptions, native inventory and registration checks.
/// </summary>
public static class XaItemOwnership
{
    private const int MaximumResponseLength = 4 * 1024 * 1024;
    // XADB's current SnapshotQualityResolver considers snapshots stale at 60 minutes.
    private static readonly TimeSpan SnapshotLifetime = TimeSpan.FromMinutes(60);

    public static Dictionary<uint, XaItemOwnershipState> QueryStates(Func<string, string> searchItems,
        Func<string, string> searchStorage, IEnumerable<uint> itemIds, ulong currentContentId, DateTimeOffset now)
    {
        var requested = itemIds.Where(id => id != 0).ToHashSet();
        var owned = Query(searchItems, searchStorage, requested, currentContentId, now);
        // The supported producer contracts certify positives, never complete absence.
        return requested.ToDictionary(id => id, id => owned.Contains(id) ? XaItemOwnershipState.Owned : XaItemOwnershipState.Unknown);
    }

    public static XaItemOwnershipState Resolve(bool nativeOwned, bool nativeAbsenceKnown, XaItemOwnershipState storage)
        => nativeOwned || storage == XaItemOwnershipState.Owned ? XaItemOwnershipState.Owned
            : nativeAbsenceKnown && storage == XaItemOwnershipState.Missing ? XaItemOwnershipState.Missing
            : XaItemOwnershipState.Unknown;

    public static HashSet<uint> Query(Func<string, string> searchItems, Func<string, string> searchStorage,
        IEnumerable<uint> itemIds, ulong currentContentId, DateTimeOffset now)
    {
        var requested = itemIds.Where(id => id != 0).ToHashSet();
        var owned = new HashSet<uint>();
        if (currentContentId == 0 || requested.Count == 0) return owned;
        var request = JsonSerializer.Serialize(new
        {
            version = 1, itemIds = requested,
            sources = new[] { "inventory", "retainer", "armoire", "glamourDresser" },
            includeZeroQuantity = false,
        });
        try { owned.UnionWith(ReadItems(searchItems(request), requested, currentContentId, now)); }
        catch { /* Missing/unavailable IPC supplies no evidence; do not retry. */ }
        var storageRequest = JsonSerializer.Serialize(new
        {
            version = 1, contentId = 0, sources = new[] { "armoire", "glamourDresser" },
        });
        try { owned.UnionWith(ReadStorage(searchStorage(storageRequest), requested, currentContentId, now)); }
        catch { /* This channel is absent from some XADB builds. */ }
        return owned;
    }

    public static HashSet<uint> ReadItems(string? json, IReadOnlySet<uint> requested,
        ulong currentContentId, DateTimeOffset now)
    {
        var owned = new HashSet<uint>();
        if (currentContentId == 0 || string.IsNullOrWhiteSpace(json) || json.Length > MaximumResponseLength) return owned;
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (!Number(root, "version", out var version) || version is not (1 or 2) || !True(root, "ready") ||
                !root.TryGetProperty("character", out var character) || !Number(character, "contentId", out var owner) ||
                owner != currentContentId || !root.TryGetProperty("rows", out var rows) || rows.ValueKind != JsonValueKind.Array)
                return owned;
            foreach (var row in rows.EnumerateArray())
            {
                if (!Number(row, "itemId", out var itemId) || itemId > uint.MaxValue || !requested.Contains((uint)itemId) ||
                    !Number(row, "ownerContentId", out var rowOwner) || rowOwner != currentContentId ||
                    !Number(row, "quantity", out var quantity) || quantity == 0 ||
                    Text(row, "source") is not ("inventory" or "retainer" or "armoire" or "glamourDresser") ||
                    Text(row, "snapshotQuality") is not ("Fresh" or "Persisted" or "Partial") ||
                    !Fresh(Text(row, "lastSeenUtc"), now)) continue;
                owned.Add((uint)itemId);
            }
        }
        catch (JsonException) { owned.Clear(); }
        return owned;
    }

    public static HashSet<uint> ReadStorage(string? json, IReadOnlySet<uint> requested,
        ulong currentContentId, DateTimeOffset now)
    {
        var owned = new HashSet<uint>();
        if (currentContentId == 0 || string.IsNullOrWhiteSpace(json) || json.Length > MaximumResponseLength) return owned;
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (!Number(root, "version", out var version) || version != 1 || !True(root, "success") ||
                !Number(root, "contentId", out var owner) || owner != currentContentId ||
                !Fresh(Text(root, "snapshotUpdatedUtc"), now)) return owned;
            foreach (var source in new[] { "armoire", "glamourDresser" })
            {
                if (!root.TryGetProperty(source, out var section) || !True(section, "requested") || !True(section, "recorded") ||
                    !section.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array) continue;
                var captured = Text(section, "capturedUtc");
                if (!string.IsNullOrEmpty(captured) && !Fresh(captured, now)) continue;
                foreach (var row in items.EnumerateArray())
                    if (Number(row, "itemId", out var itemId) && itemId <= uint.MaxValue && requested.Contains((uint)itemId) &&
                        Number(row, "quantity", out var quantity) && quantity > 0) owned.Add((uint)itemId);
            }
        }
        catch (JsonException) { owned.Clear(); }
        return owned;
    }

    private static bool Fresh(string? value, DateTimeOffset now)
        => DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var captured) &&
            captured <= now && now - captured < SnapshotLifetime;

    private static bool Number(JsonElement element, string name, out ulong value)
    {
        value = 0;
        return element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var property) &&
            property.ValueKind == JsonValueKind.Number && property.TryGetUInt64(out value);
    }
    private static bool True(JsonElement element, string name)
        => element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.True;
    private static string? Text(JsonElement element, string name)
        => element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString() : null;
}
