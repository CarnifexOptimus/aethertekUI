using System.Text.Json;
using AethertekUI.Dalamud;
using Xunit;

namespace AethertekUI.Tests;

public sealed class XaItemOwnershipTests
{
    [Fact]
    public void QueryStatesNeverConvertsEmptyStorageToMissing()
    {
        var states = XaItemOwnership.QueryStates(_ => "{\"version\":2,\"ready\":true,\"character\":{\"contentId\":42},\"rows\":[]}",
            _ => "{}", new uint[] { 100, 101, 0, 100 }, 42, Now);
        Assert.Equal(2, states.Count);
        Assert.All(states.Values, state => Assert.Equal(XaItemOwnershipState.Unknown, state));
        states = XaItemOwnership.QueryStates(_ => Items(2), _ => "{}", new uint[] { 100, 101 }, 42, Now);
        Assert.Equal(XaItemOwnershipState.Owned, states[100]);
        Assert.Equal(XaItemOwnershipState.Unknown, states[101]);
    }

    [Theory]
    [InlineData(true, false, XaItemOwnershipState.Unknown, XaItemOwnershipState.Owned)]
    [InlineData(false, false, XaItemOwnershipState.Owned, XaItemOwnershipState.Owned)]
    [InlineData(false, true, XaItemOwnershipState.Unknown, XaItemOwnershipState.Unknown)]
    [InlineData(false, false, XaItemOwnershipState.Missing, XaItemOwnershipState.Unknown)]
    [InlineData(false, true, XaItemOwnershipState.Missing, XaItemOwnershipState.Missing)]
    [InlineData(true, true, XaItemOwnershipState.Missing, XaItemOwnershipState.Owned)]
    public void AbsenceRequiresBothTruthSources(bool nativeOwned, bool nativeAbsenceKnown,
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
    [InlineData(2, 42, 42, 1, "retainer", "Stale", -1)]
    [InlineData(2, 42, 42, 1, "retainer", "Degraded", -1)]
    [InlineData(2, 42, 42, 1, "retainer", "Fresh", -60)]
    [InlineData(2, 42, 42, 1, "retainer", "Fresh", 1)]
    public void InvalidOrStaleEvidenceCannotEstablishOwnership(int version, int owner, int rowOwner, int quantity,
        string source, string quality, int minutes)
        => Assert.Empty(XaItemOwnership.ReadItems(Items(version, owner, rowOwner, quantity, source, quality, minutes), Requested, 42, Now));

    [Theory]
    [InlineData("{}")] [InlineData("[]")] [InlineData("null")] [InlineData("{")]
    [InlineData("{\"version\":2,\"ready\":true,\"character\":{\"contentId\":42},\"rows\":[]}")]
    [InlineData("{\"version\":2,\"ready\":true,\"character\":[],\"rows\":[]}")]
    public void EmptyOrMalformedResponsesProvideNoOwnedOrMissingConclusion(string response)
        => Assert.Empty(XaItemOwnership.ReadItems(response, Requested, 42, Now));

    [Theory]
    [InlineData(true, true, true, 42, -1, true)]
    [InlineData(false, true, true, 42, -1, false)]
    [InlineData(true, false, true, 42, -1, false)]
    [InlineData(true, true, false, 42, -1, false)]
    [InlineData(true, true, true, 7, -1, false)]
    [InlineData(true, true, true, 42, -60, false)]
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

    private static string Items(int version, int owner = 42, int rowOwner = 42, int quantity = 1,
        string source = "inventory", string quality = "Fresh", int minutes = -1)
        => JsonSerializer.Serialize(new
        {
            version, ready = true, character = new { contentId = owner },
            rows = new[] { new { itemId = 100, ownerContentId = rowOwner, quantity, source,
                snapshotQuality = quality, lastSeenUtc = Now.AddMinutes(minutes).ToString("O") } },
        });
}
