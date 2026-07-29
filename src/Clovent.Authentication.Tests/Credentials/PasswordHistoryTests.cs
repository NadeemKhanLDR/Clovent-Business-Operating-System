using Clovent.Authentication.Credentials;
using Xunit;

namespace Clovent.Authentication.Tests.Credentials;

public class PasswordHistoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Empty_HasNoEntriesAndNoLastChangedAt()
    {
        Assert.Empty(PasswordHistory.Empty.Entries);
        Assert.Null(PasswordHistory.Empty.LastChangedAtUtc);
    }

    [Fact]
    public void WithNewPassword_AddsMostRecentEntryFirst()
    {
        var first = PasswordHistory.Empty.WithNewPassword(PasswordHash.Create("hash1"), Now);
        var second = first.WithNewPassword(PasswordHash.Create("hash2"), Now.AddDays(1));

        Assert.Equal(2, second.Entries.Count);
        Assert.Equal("hash2", second.Entries[0].Hash.Value);
        Assert.Equal("hash1", second.Entries[1].Hash.Value);
    }

    [Fact]
    public void WithNewPassword_UpdatesLastChangedAtUtc()
    {
        var history = PasswordHistory.Empty.WithNewPassword(PasswordHash.Create("hash1"), Now);

        Assert.Equal(Now, history.LastChangedAtUtc);
    }

    [Fact]
    public void WithNewPassword_TrimsToMaxSize()
    {
        var history = PasswordHistory.Empty;
        for (var i = 0; i < 10; i++)
        {
            history = history.WithNewPassword(PasswordHash.Create($"hash{i}"), Now.AddDays(i), maxSize: 3);
        }

        Assert.Equal(3, history.Entries.Count);
        Assert.Equal("hash9", history.Entries[0].Hash.Value);
        Assert.Equal("hash8", history.Entries[1].Hash.Value);
        Assert.Equal("hash7", history.Entries[2].Hash.Value);
    }

    [Fact]
    public void WithNewPassword_DoesNotMutateOriginal()
    {
        var original = PasswordHistory.Empty.WithNewPassword(PasswordHash.Create("hash1"), Now);

        original.WithNewPassword(PasswordHash.Create("hash2"), Now.AddDays(1));

        Assert.Single(original.Entries);
    }

    [Fact]
    public void WithNewPassword_NonPositiveMaxSize_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PasswordHistory.Empty.WithNewPassword(PasswordHash.Create("hash1"), Now, maxSize: 0));
    }

    [Fact]
    public void Contains_MatchingHash_ReturnsTrue()
    {
        var hash = PasswordHash.Create("hash1");
        var history = PasswordHistory.Empty.WithNewPassword(hash, Now);

        Assert.True(history.Contains(PasswordHash.Create("hash1")));
    }

    [Fact]
    public void Contains_NonMatchingHash_ReturnsFalse()
    {
        var history = PasswordHistory.Empty.WithNewPassword(PasswordHash.Create("hash1"), Now);

        Assert.False(history.Contains(PasswordHash.Create("different")));
    }

    [Fact]
    public void Equals_SameEntries_AreEqual()
    {
        var a = PasswordHistory.Empty.WithNewPassword(PasswordHash.Create("hash1"), Now);
        var b = PasswordHistory.Empty.WithNewPassword(PasswordHash.Create("hash1"), Now);

        Assert.Equal(a, b);
    }

    [Fact]
    public void Equals_DifferentEntries_AreNotEqual()
    {
        var a = PasswordHistory.Empty.WithNewPassword(PasswordHash.Create("hash1"), Now);
        var b = PasswordHistory.Empty.WithNewPassword(PasswordHash.Create("hash2"), Now);

        Assert.NotEqual(a, b);
    }
}
