using System.Text.Json;
using AethertekUI.Dalamud;
using Xunit;

namespace AethertekUI.Tests;

public sealed class XaItemOwnershipTests
{
    [Fact]
    public void UnrecordedItemsAreMissingWhileKnownCopiesAreOwned()
    {
        var states = XaItemOwnership.QueryStates(_ => "{\"version\":2,\"ready\":true,\"character\":{\"contentId\":42},\"rows\":[]}",
            _ => "{}", new uint[] { 100, 101, 0, 100 }, 42, Now);
        Assert.Equal(2, states.Count);
        Assert.All(states.Values, state => Assert.Equal(XaItemOwnershipState.Missing, state));
        states = XaItemOwnership.QueryStates(_ => Items(2), _ => "{}", new uint[] { 100, 101 }, 42, Now);
        Assert.Equal(XaItemOwnershipState.Owned, states[100]);
        Assert.Equal(XaItemOwnershipState.Missing, states[101]);
    }

    [Fact]
    public void UnavailableStorageIsMissingWithoutRetriesButCharacterIdentityRemainsRequired()
    {
        var calls = 0;
        var states = XaItemOwnership.QueryStates(_ => { calls++; throw new InvalidOperationException(); },
            _ => { calls++; return "{}"; }, Requested, 42, Now);
        Assert.Equal(XaItemOwnershipState.Missing, states[100]);
        Assert.Equal(2, calls);
        states = XaItemOwnership.QueryStates(_ => throw new Exception(), _ => throw new Exception(), Requested, 0, Now);
        Assert.Equal(XaItemOwnershipState.Unknown, states[100]);
    }

    [Theory]
    [InlineData(true, false, XaItemOwnershipState.Unknown, XaItemOwnershipState.Owned)]
    [InlineData(false, false, XaItemOwnershipState.Owned, XaItemOwnershipState.Owned)]
    [InlineData(false, true, XaItemOwnershipState.Unknown, XaItemOwnershipState.Unknown)]
    [InlineData(false, false, XaItemOwnershipState.Missing, XaItemOwnershipState.Missing)]
    [InlineData(false, true, XaItemOwnershipState.Missing, XaItemOwnershipState.Missing)]
    [InlineData(true, true, XaItemOwnershipState.Missing, XaItemOwnershipState.Owned)]
    public void NativeOrRecordedCopiesWinOverAssumedMissing(bool nativeOwned, bool nativeAbsenceKnown,
        XaItemOwnershipState storage, XaItemOwnershipState expected)
        => Assert.Equal(expected, XaItemOwnership.Resolve(nativeOwned, nativeAbsenceKnown, storage));

    private static readonly DateTimeOffset Now = new(2026, 10, 10, 3, 0, 0, TimeSpan.Zero);
    private static readonly HashSet<uint> Requested = [100];

    [Theory]
    [InlineData(1, "inventory", "Fresh")]
    [InlineData(2, "retainer", "Partial")]
    [InlineData(2, "armoire", "Persisted")]
    [InlineData(1, "glamourDresser", "Fresh")]
    public void SupportedCurrentCharacterRowsEstablishOwnership(int version, string source, string quality)
        => Assert.Contains(100u, XaItemOwnership.ReadItems(Items(version, source: source, quality: quality), Requested, 42, Now));

    [Theory]
    [InlineData(3, 42, 42, 1, "retainer", "Fresh", -1)]
    [InlineData(2, 7, 7, 1, "retainer", "Fresh", -1)]
    [InlineData(2, 42, 7, 1, "retainer", "Fresh", -1)]
    [InlineData(2, 42, 42, 0, "retainer", "Fresh", -1)]
    [InlineData(2, 42, 42, -1, "retainer", "Fresh", -1)]
    [InlineData(2, 42, 42, 1, "housing", "Fresh", -1)]
    public void InvalidIdentityQuantitySourceOrVersionCannotEstablishOwnership(int version, int owner, int rowOwner, int quantity,
        string source, string quality, int minutes)
        => Assert.Empty(XaItemOwnership.ReadItems(Items(version, owner, rowOwner, quantity, source, quality, minutes), Requested, 42, Now));

    [Theory]
    [InlineData("inventory", "Fresh", -1)]
    [InlineData("retainer", "Stale", -1440)]
    [InlineData("armoire", "Persisted", -10080)]
    [InlineData("glamourDresser", "Degraded", -43200)]
    public void RecordedCopiesRemainOwnedUntilTheUserRefreshesStorage(string source, string quality, int minutes)
        => Assert.Contains(100u, XaItemOwnership.ReadItems(Items(2, source: source, quality: quality, minutes: minutes), Requested, 42, Now));

    [Fact]
    public void QueryRequestsAllSupportedStorageSourcesAndKeepsOtherCharactersOut()
    {
        string? capturedRequest = null;
        var states = XaItemOwnership.QueryStates(request =>
        {
            capturedRequest = request;
            return Items(2, owner: 7, rowOwner: 7);
        }, _ => "{}", Requested, 42, Now);
        Assert.NotNull(capturedRequest);
        using var document = JsonDocument.Parse(capturedRequest);
        Assert.Equal(new[] { "inventory", "retainer", "armoire", "glamourDresser" },
            document.RootElement.GetProperty("sources").EnumerateArray().Select(source => source.GetString()));
        Assert.Equal(XaItemOwnershipState.Missing, states[100]);
    }

    [Theory]
    [InlineData("{}")] [InlineData("[]")] [InlineData("null")] [InlineData("{")]
    [InlineData("{\"version\":2,\"ready\":true,\"character\":{\"contentId\":42},\"rows\":[]}")]
    [InlineData("{\"version\":2,\"ready\":true,\"character\":[],\"rows\":[]}")]
    public void EmptyOrMalformedResponsesProvideNoOwnedCopy(string response)
        => Assert.Empty(XaItemOwnership.ReadItems(response, Requested, 42, Now));

    [Theory]
    [InlineData(true, true, true, 42, -1, true)]
    [InlineData(false, true, true, 42, -1, false)]
    [InlineData(true, false, true, 42, -1, false)]
    [InlineData(true, true, false, 42, -1, false)]
    [InlineData(true, true, true, 7, -1, false)]
    [InlineData(true, true, true, 42, -10080, true)]
    public void StorageRequiresCurrentSuccessfulRecordedSection(bool success, bool requested, bool recorded, int owner, int minutes, bool expected)
    {
        var response = JsonSerializer.Serialize(new
        {
            version = 1, success, contentId = owner, snapshotUpdatedUtc = Now.AddMinutes(minutes).ToString("O"),
            armoire = new { requested, recorded, items = new[] { new { itemId = 100, quantity = 1 } } },
        });
        Assert.Equal(expected, XaItemOwnership.ReadStorage(response, Requested, 42, Now).Contains(100));
    }

    [Fact]
    public void UnavailableChannelDoesNotDiscardValidOtherEvidenceOrRetry()
    {
        var itemCalls = 0; var storageCalls = 0;
        var result = XaItemOwnership.Query(_ => { itemCalls++; return Items(2); },
            _ => { storageCalls++; throw new InvalidOperationException(); }, Requested, 42, Now);
        Assert.Contains(100u, result);
        Assert.Equal(1, itemCalls); Assert.Equal(1, storageCalls);
        Assert.Empty(XaItemOwnership.Query(_ => throw new Exception(), _ => throw new Exception(), Requested, 0, Now));
    }

    [Fact]
    public void StockQuantitiesIncludeEveryInventoryContainerAndRemoteSource()
    {
        string[] containers = ["Inventory 1", "Inventory 2", "Inventory 3", "Inventory 4", "Equipped", "Crystals",
            "Armoury - Main Hand", "Armoury - Off Hand", "Armoury - Head", "Armoury - Body", "Armoury - Hands",
            "Armoury - Legs", "Armoury - Feet", "Armoury - Earring", "Armoury - Necklace", "Armoury - Bracelet",
            "Armoury - Ring", "Armoury - Soul Crystal", "Saddlebag 1", "Saddlebag 2",
            "Premium Saddlebag 1", "Premium Saddlebag 2"];
        var rows = containers.Select(container => StockRow("inventory", container, 2)).ToList();
        rows.Add(StockRow("retainer", "Retainer: First", 3));
        rows.Add(StockRow("retainer", "Retainer: Second", 4));
        rows.Add(StockRow("armoire", "Armoire", 1));
        rows.Add(StockRow("glamourDresser", "Glamour Dresser", 2));

        var stocks = XaItemOwnership.QueryStocks(_ => StockItems(rows.ToArray()), _ => "{}", [100, 101, 100, 0], 42, Now);
        Assert.Equal(2, stocks.Count);
        Assert.Equal(containers.Length, stocks[100].InventoryCounts.Count);
        Assert.Equal(10, stocks[100].StorageCount);
        Assert.Equal(containers.Length * 2 + 10, stocks[100].CountWithLiveInventory(new Dictionary<string, int>()));
        Assert.Equal(0, stocks[101].CountWithLiveInventory(new Dictionary<string, int>()));
    }

    [Fact]
    public void IdenticalStacksAreSummedInsteadOfDeduplicated()
    {
        var stocks = XaItemOwnership.QueryStocks(_ => StockItems(
            StockRow("inventory", "Inventory 1", 4), StockRow("inventory", "Inventory 1", 4),
            StockRow("retainer", "Retainer: First", 5), StockRow("retainer", "Retainer: First", 5)),
            _ => "{}", Requested, 42, Now);
        Assert.Equal(8, stocks[100].InventoryCounts["Inventory 1"]);
        Assert.Equal(10, stocks[100].StorageCount);
        Assert.Equal(18, stocks[100].CountWithLiveInventory(new Dictionary<string, int>()));
    }

    [Fact]
    public void PartialLiveOverlayReplacesContainersIncludingZeroAndPreservesClosedStorage()
    {
        var stock = XaItemOwnership.QueryStocks(_ => StockItems(
            StockRow("inventory", "Inventory 1", 3), StockRow("inventory", "Inventory 2", 2),
            StockRow("inventory", "Armoury - Head", 1), StockRow("inventory", "Saddlebag 1", 4),
            StockRow("inventory", "Saddlebag 2", 5), StockRow("inventory", "Premium Saddlebag 1", 6),
            StockRow("inventory", "Premium Saddlebag 2", 7), StockRow("retainer", "Retainer: First", 8)),
            _ => "{}", Requested, 42, Now)[100];
        var live = new Dictionary<string, int>
        {
            ["Inventory 1"] = 0, ["Inventory 2"] = 1, ["Inventory 3"] = 0, ["Inventory 4"] = 0,
            ["armoury - head"] = 2,
        };
        Assert.Equal(33, stock.CountWithLiveInventory(live));
        live["Saddlebag 1"] = 0;
        live["Saddlebag 2"] = 0;
        Assert.Equal(24, stock.CountWithLiveInventory(live));
        Assert.Equal(3, stock.InventoryCounts["Inventory 1"]);
    }

    [Fact]
    public void ArmouryReceiptRefreshSatisfiesTargetWithoutAddingRecordedCopyOrSpendingAgain()
    {
        var stock = XaItemOwnership.QueryStocks(_ => StockItems(StockRow("inventory", "Armoury - Head", 1),
            StockRow("armoire", "Armoire", 1)), _ => "{}", Requested, 42, Now)[100];
        var live = new Dictionary<string, int> { ["Armoury - Head"] = 1 };
        const int target = 3;
        Assert.Equal(1, Math.Max(0, target - stock.CountWithLiveInventory(live)));
        live["Armoury - Head"] = 2;
        Assert.Equal(3, stock.CountWithLiveInventory(live));
        Assert.Equal(0, Math.Max(0, target - stock.CountWithLiveInventory(live)));
        Assert.Equal(0, Math.Max(0, target - stock.CountWithLiveInventory(live)));
    }

    [Theory]
    [InlineData(null)] [InlineData("")] [InlineData("Unrecognized location")]
    public void LegacyInventoryAggregateIsALowerBoundRatherThanAnAdditionalNativeCopy(string? container)
    {
        var stock = XaItemOwnership.QueryStocks(_ => StockItems(StockRow("inventory", container, 5),
            StockRow("inventory", "Inventory 1", 2), StockRow("retainer", "Retainer: First", 1)),
            _ => "{}", Requested, 42, Now)[100];
        Assert.Equal(6, stock.CountWithLiveInventory(new Dictionary<string, int> { ["Inventory 1"] = 3 }));
        Assert.Equal(8, stock.CountWithLiveInventory(new Dictionary<string, int> { ["Inventory 1"] = 7 }));
    }

    [Fact]
    public void OptionalStorageEndpointReplacesOverlappingSourceTotalsWithoutDoubleCounting()
    {
        var storage = JsonSerializer.Serialize(new
        {
            version = 1, success = true, contentId = 42,
            armoire = new { requested = true, recorded = true, items = new[] { new { itemId = 100, quantity = 1 } } },
            glamourDresser = new { requested = true, recorded = true, items = new[] { new { itemId = 100, quantity = 1 }, new { itemId = 100, quantity = 2 } } },
        });
        var stock = XaItemOwnership.QueryStocks(_ => StockItems(StockRow("armoire", "Armoire", 2),
            StockRow("glamourDresser", "Glamour Dresser", 2), StockRow("retainer", "Retainer: First", 4)),
            _ => storage, Requested, 42, Now)[100];
        Assert.Equal(9, stock.StorageCount);
    }

    [Fact]
    public void StockCountsKeepCurrentCharacterIsolationAndIgnoreUnrequestedOrUnsupportedRows()
    {
        var stock = XaItemOwnership.QueryStocks(_ => StockItems(
            StockRow("retainer", "Retainer: First", 50, owner: 7),
            StockRow("armoire", "Armoire", 50, itemId: 101), StockRow("housing", "House", 50),
            StockRow("inventory", "Inventory 1", 2)),
            _ => "{\"version\":1,\"success\":true,\"contentId\":7,\"armoire\":{\"requested\":true,\"recorded\":true,\"items\":[{\"itemId\":100,\"quantity\":50}]}}",
            Requested, 42, Now)[100];
        Assert.Equal(2, stock.CountWithLiveInventory(new Dictionary<string, int>()));
        Assert.Equal(0, XaItemOwnership.QueryStocks(_ => Items(2, owner: 7, rowOwner: 7), _ => "{}", Requested, 42, Now)[100].StorageCount);
    }

    [Theory]
    [InlineData("null")] [InlineData("[]")] [InlineData("{")]
    [InlineData("{\"version\":2,\"ready\":true,\"character\":{\"contentId\":42},\"rows\":[null,[],\"bad\"]}")]
    [InlineData("{\"version\":1,\"success\":true,\"contentId\":42,\"armoire\":[],\"glamourDresser\":null}")]
    public void InvalidStockResponsesDoNotThrowOrInventQuantities(string response)
    {
        var stocks = XaItemOwnership.QueryStocks(_ => response, _ => response, Requested, 42, Now);
        Assert.Equal(0, stocks[100].CountWithLiveInventory(new Dictionary<string, int>()));
    }

    [Fact]
    public void QuantitiesAndTotalsSaturateWithoutOverflowWhileInvalidNumbersAreIgnored()
    {
        var response = StockItems(
            new { source = "inventory", containerName = "Inventory 1", itemId = 100, ownerContentId = 42, quantity = ulong.MaxValue },
            StockRow("inventory", "Inventory 1", 1), StockRow("retainer", "Retainer: First", int.MaxValue),
            StockRow("armoire", "Armoire", -1), StockRow("glamourDresser", "Glamour Dresser", 0));
        var stock = XaItemOwnership.QueryStocks(_ => response, _ => "{}", Requested, 42, Now)[100];
        Assert.Equal(int.MaxValue, stock.InventoryCounts["Inventory 1"]);
        Assert.Equal(int.MaxValue, stock.StorageCount);
        Assert.Equal(int.MaxValue, stock.CountWithLiveInventory(new Dictionary<string, int>()));
        Assert.Equal(int.MaxValue, stock.CountWithLiveInventory(new Dictionary<string, int> { ["Inventory 1"] = -1 }));
    }

    [Fact]
    public void MissingCharacterIdentityReturnsZeroStocksWithoutCallingIpc()
    {
        var stocks = XaItemOwnership.QueryStocks(_ => throw new Exception(), _ => throw new Exception(), [100, 101, 0], 0, Now);
        Assert.Equal(2, stocks.Count);
        Assert.All(stocks.Values, stock => Assert.Equal(0, stock.CountWithLiveInventory(new Dictionary<string, int>())));
    }

    private static object StockRow(string source, string? container, int quantity, int itemId = 100, int owner = 42)
        => new { source, containerName = container, itemId, ownerContentId = owner, quantity };

    private static string StockItems(params object[] rows)
        => JsonSerializer.Serialize(new { version = 2, ready = true, character = new { contentId = 42 }, rows });

    private static string Items(int version, int owner = 42, int rowOwner = 42, int quantity = 1,
        string source = "inventory", string quality = "Fresh", int minutes = -1)
        => JsonSerializer.Serialize(new
        {
            version, ready = true, character = new { contentId = owner },
            rows = new[] { new { itemId = 100, ownerContentId = rowOwner, quantity, source,
                snapshotQuality = quality, lastSeenUtc = Now.AddMinutes(minutes).ToString("O") } },
        });
}
