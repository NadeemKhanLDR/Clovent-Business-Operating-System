using Clovent.Desktop.MasterData;
using Xunit;

namespace Clovent.Desktop.Tests.MasterData;

public class MasterDataFilterTests
{
    private sealed record Item(string Name);

    [Fact]
    public void Apply_NullSearchText_ReturnsEveryItem()
    {
        IReadOnlyList<Item> items = [new Item("Alpha"), new Item("Beta")];

        var result = MasterDataFilter.Apply(items, null, i => i.Name);

        Assert.Equal(items, result);
    }

    [Fact]
    public void Apply_EmptySearchText_ReturnsEveryItem()
    {
        IReadOnlyList<Item> items = [new Item("Alpha"), new Item("Beta")];

        var result = MasterDataFilter.Apply(items, "   ", i => i.Name);

        Assert.Equal(items, result);
    }

    [Fact]
    public void Apply_NoSelector_ReturnsEveryItem()
    {
        IReadOnlyList<Item> items = [new Item("Alpha"), new Item("Beta")];

        var result = MasterDataFilter.Apply<Item>(items, "Alpha", null);

        Assert.Equal(items, result);
    }

    [Fact]
    public void Apply_MatchingSubstring_IsCaseInsensitive()
    {
        IReadOnlyList<Item> items = [new Item("Alpha Corp"), new Item("Beta Corp")];

        var result = MasterDataFilter.Apply(items, "alpha", i => i.Name);

        Assert.Equal([new Item("Alpha Corp")], result);
    }

    [Fact]
    public void Apply_NoMatch_ReturnsEmpty()
    {
        IReadOnlyList<Item> items = [new Item("Alpha"), new Item("Beta")];

        var result = MasterDataFilter.Apply(items, "gamma", i => i.Name);

        Assert.Empty(result);
    }

    [Theory]
    [InlineData(false, null, true, false)]
    [InlineData(true, null, false, false)]
    [InlineData(true, false, true, false)]
    [InlineData(true, null, true, true)]
    [InlineData(true, true, true, true)]
    public void CanEdit_EvaluatesAllGates(bool hasFocusedRow, bool? permitted, bool hasHandler, bool expected)
    {
        Assert.Equal(expected, MasterDataFilter.CanEdit(hasFocusedRow, permitted, hasHandler));
    }

    [Theory]
    [InlineData(true, null, "Inactive", true, true)]
    [InlineData(true, null, "PendingActivation", true, true)]
    [InlineData(true, null, "Active", true, false)]
    [InlineData(true, null, "Locked", true, false)]
    [InlineData(true, false, "Inactive", true, false)]
    [InlineData(true, null, "Inactive", false, false)]
    [InlineData(false, null, "Inactive", true, false)]
    public void CanActivate_EvaluatesAllGates(bool hasFocusedRow, bool? permitted, string status, bool hasHandler, bool expected)
    {
        Assert.Equal(expected, MasterDataFilter.CanActivate(hasFocusedRow, permitted, status, hasHandler));
    }

    [Theory]
    [InlineData(true, null, "Active", true, true)]
    [InlineData(true, null, "Inactive", true, false)]
    [InlineData(true, false, "Active", true, false)]
    [InlineData(true, null, "Active", false, false)]
    [InlineData(false, null, "Active", true, false)]
    public void CanDeactivate_EvaluatesAllGates(bool hasFocusedRow, bool? permitted, string status, bool hasHandler, bool expected)
    {
        Assert.Equal(expected, MasterDataFilter.CanDeactivate(hasFocusedRow, permitted, status, hasHandler));
    }
}
