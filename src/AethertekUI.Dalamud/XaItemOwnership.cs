using System.Text.Json;

namespace AethertekUI.Dalamud;

public enum XaItemOwnershipState { Unknown, Owned, Missing }

/// <summary>Recorded character inventory and remote storage quantities for one item.</summary>
public sealed record XaItemStock(IReadOnlyDictionary<string, int> InventoryCounts, int StorageCount)
{
    /// <summary>Replaces readable recorded containers with live counts, including empty containers.</summary>
    public int CountWithLiveInventory(IReadOnlyDictionary<string, int> live)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var unlabelled = 0;
        foreach (var (container, quantity) in InventoryCounts)
        {
            if (string.IsNullOrEmpty(container)) unlabelled = XaItemOwnership.AddCount(unlabelled, Math.Max(0, quantity));
            else counts[container] = Math.Max(0, quantity);
        }
        foreach (var (container, quantity) in live)
            if (!string.IsNullOrEmpty(container)) counts[container] = Math.Max(0, quantity);

        var inventory = counts.Values.Aggregate(0, XaItemOwnership.AddCount);
        // Legacy rows have no usable location. They may already include the live
        // inventory, so retain their aggregate as a lower bound rather than add it.
        return XaItemOwnership.AddCount(Math.Max(unlabelled, inventory), Math.Max(0, StorageCount));
    }
}

/// <summary>
/// Reads current-character XADB ownership from the last recorded storage data.
/// Items without a recorded or native copy are treated as missing. Operators
/// are responsible for opening storage and refreshing its recorded contents.
/// Consumers own IPC subscriptions, native inventory and registration checks.
/// </summary>
public static class XaItemOwnership
{
    private const int MaximumResponseLength = 4 * 1024 * 1024;
    public static Dictionary<uint, XaItemOwnershipState> QueryStates(Func<string, string> searchItems,
        Func<string, string> searchStorage, IEnumerable<uint> itemIds, ulong currentContentId, DateTimeOffset now)
    {
        var requested = itemIds.Where(id => id != 0).ToHashSet();
        var owned = Query(searchItems, searchStorage, requested, currentContentId, now);
        return requested.ToDictionary(id => id, id => owned.Contains(id) ? XaItemOwnershipState.Owned
            : currentContentId != 0 ? XaItemOwnershipState.Missing : XaItemOwnershipState.Unknown);
    }

    public static XaItemOwnershipState Resolve(bool nativeOwned, bool nativeAbsenceKnown, XaItemOwnershipState storage)
        => nativeOwned || storage == XaItemOwnershipState.Owned ? XaItemOwnershipState.Owned
            : storage == XaItemOwnershipState.Missing ? XaItemOwnershipState.Missing
            : XaItemOwnershipState.Unknown;

    public static HashSet<uint> Query(Func<string, string> searchItems, Func<string, string> searchStorage,
        IEnumerable<uint> itemIds, ulong currentContentId, DateTimeOffset now)
        => QueryStocks(searchItems, searchStorage, itemIds, currentContentId, now)
            .Where(entry => entry.Value.StorageCount > 0 || entry.Value.InventoryCounts.Values.Any(quantity => quantity > 0))
            .Select(entry => entry.Key).ToHashSet();

    public static Dictionary<uint, XaItemStock> QueryStocks(Func<string, string> searchItems, Func<string, string> searchStorage,
        IEnumerable<uint> itemIds, ulong currentContentId, DateTimeOffset now)
    {
        var requested = itemIds.Where(id => id != 0).ToHashSet();
        var result = requested.ToDictionary(id => id,
            _ => new XaItemStock(new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase), 0));
        if (currentContentId == 0 || requested.Count == 0) return result;
        var rows = new Dictionary<(uint ItemId, string Source, string Container), int>();
        var storage = new Dictionary<(uint ItemId, string Source), int>();
        var request = JsonSerializer.Serialize(new
        {
            version = 1, itemIds = requested,
            sources = new[] { "inventory", "retainer", "armoire", "glamourDresser" },
            includeZeroQuantity = false,
        });
        try { rows = ReadItemQuantities(searchItems(request), requested, currentContentId); }
        catch { /* Missing/unavailable IPC supplies no evidence; do not retry. */ }
        var storageRequest = JsonSerializer.Serialize(new
        {
            version = 1, contentId = 0, sources = new[] { "armoire", "glamourDresser" },
        });
        try { storage = ReadStorageQuantities(searchStorage(storageRequest), requested, currentContentId); }
        catch { /* This channel is absent from some XADB builds. */ }

        foreach (var itemId in requested)
        {
            var inventory = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var remote = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var (key, quantity) in rows)
            {
                if (key.ItemId != itemId) continue;
                if (key.Source == "inventory") inventory[key.Container] = AddCount(inventory.GetValueOrDefault(key.Container), quantity);
                else remote[key.Source] = AddCount(remote.GetValueOrDefault(key.Source), quantity);
            }
            foreach (var (key, quantity) in storage)
                if (key.ItemId == itemId) remote[key.Source] = Math.Max(remote.GetValueOrDefault(key.Source), quantity);
            result[itemId] = new XaItemStock(inventory, remote.Values.Aggregate(0, AddCount));
        }
        return result;
    }

    public static HashSet<uint> ReadItems(string? json, IReadOnlySet<uint> requested,
        ulong currentContentId, DateTimeOffset now)
        => ReadItemQuantities(json, requested, currentContentId).Keys.Select(key => key.ItemId).ToHashSet();

    private static Dictionary<(uint ItemId, string Source, string Container), int> ReadItemQuantities(
        string? json, IReadOnlySet<uint> requested, ulong currentContentId)
    {
        var counts = new Dictionary<(uint ItemId, string Source, string Container), int>();
        if (currentContentId == 0 || string.IsNullOrWhiteSpace(json) || json.Length > MaximumResponseLength) return counts;
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (!Number(root, "version", out var version) || version is not (1 or 2) || !True(root, "ready") ||
                !root.TryGetProperty("character", out var character) || !Number(character, "contentId", out var owner) ||
                owner != currentContentId || !root.TryGetProperty("rows", out var rows) || rows.ValueKind != JsonValueKind.Array)
                return counts;
            foreach (var row in rows.EnumerateArray())
            {
                if (!Number(row, "itemId", out var itemId) || itemId > uint.MaxValue || !requested.Contains((uint)itemId) ||
                    !Number(row, "ownerContentId", out var rowOwner) || rowOwner != currentContentId ||
                    !Number(row, "quantity", out var quantity) || quantity == 0 ||
                    Text(row, "source") is not ("inventory" or "retainer" or "armoire" or "glamourDresser")) continue;
                var source = Text(row, "source")!;
                var container = source == "inventory" ? InventoryContainer(Text(row, "containerName")) : string.Empty;
                var key = ((uint)itemId, source, container);
                counts[key] = AddCount(counts.GetValueOrDefault(key), (int)Math.Min(quantity, int.MaxValue));
            }
        }
        catch (JsonException) { counts.Clear(); }
        return counts;
    }

    public static HashSet<uint> ReadStorage(string? json, IReadOnlySet<uint> requested,
        ulong currentContentId, DateTimeOffset now)
        => ReadStorageQuantities(json, requested, currentContentId).Keys.Select(key => key.ItemId).ToHashSet();

    private static Dictionary<(uint ItemId, string Source), int> ReadStorageQuantities(
        string? json, IReadOnlySet<uint> requested, ulong currentContentId)
    {
        var counts = new Dictionary<(uint ItemId, string Source), int>();
        if (currentContentId == 0 || string.IsNullOrWhiteSpace(json) || json.Length > MaximumResponseLength) return counts;
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (!Number(root, "version", out var version) || version != 1 || !True(root, "success") ||
                !Number(root, "contentId", out var owner) || owner != currentContentId) return counts;
            foreach (var source in new[] { "armoire", "glamourDresser" })
            {
                if (!root.TryGetProperty(source, out var section) || !True(section, "requested") || !True(section, "recorded") ||
                    !section.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array) continue;
                foreach (var row in items.EnumerateArray())
                    if (Number(row, "itemId", out var itemId) && itemId <= uint.MaxValue && requested.Contains((uint)itemId) &&
                        Number(row, "quantity", out var quantity) && quantity > 0)
                    {
                        var key = ((uint)itemId, source);
                        counts[key] = AddCount(counts.GetValueOrDefault(key), (int)Math.Min(quantity, int.MaxValue));
                    }
            }
        }
        catch (JsonException) { counts.Clear(); }
        return counts;
    }

    internal static int AddCount(int first, int second) => (int)Math.Min(int.MaxValue, (long)first + second);

    private static string InventoryContainer(string? name)
    {
        var trimmed = name?.Trim() ?? string.Empty;
        return trimmed.ToLowerInvariant() switch
        {
            "inventory 1" or "inventory 2" or "inventory 3" or "inventory 4" or "equipped" or "crystals" or
            "armoury - main hand" or "armoury - off hand" or "armoury - head" or "armoury - body" or
            "armoury - hands" or "armoury - legs" or "armoury - feet" or "armoury - earring" or
            "armoury - necklace" or "armoury - bracelet" or "armoury - ring" or "armoury - soul crystal" or
            "saddlebag 1" or "saddlebag 2" or "premium saddlebag 1" or "premium saddlebag 2" => trimmed.ToLowerInvariant(),
            _ => string.Empty,
        };
    }

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
