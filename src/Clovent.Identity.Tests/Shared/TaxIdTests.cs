using Clovent.Identity.Shared.ValueObjects;
using Xunit;

namespace Clovent.Identity.Tests.Shared;

public class TaxIdTests
{
    [Fact]
    public void Create_Valid_TrimsAndStores()
    {
        var taxId = TaxId.Create("  12-3456789  ");

        Assert.Equal("12-3456789", taxId.Value);
    }

    [Fact]
    public void Create_Empty_Throws()
    {
        Assert.Throws<ArgumentException>(() => TaxId.Create(""));
    }

    [Fact]
    public void Create_TooLong_Throws()
    {
        Assert.Throws<ArgumentException>(() => TaxId.Create(new string('9', 51)));
    }

    [Fact]
    public void Equals_SameValue_AreEqual()
    {
        Assert.Equal(TaxId.Create("ABC123"), TaxId.Create("ABC123"));
    }
}
