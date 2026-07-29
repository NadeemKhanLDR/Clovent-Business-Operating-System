using Clovent.Identity.Users.ValueObjects;
using Xunit;

namespace Clovent.Identity.Tests.Users.ValueObjects;

public class PhoneNumberTests
{
    [Fact]
    public void Create_StripsSpacesAndDashes()
    {
        var phone = PhoneNumber.Create("+1 415-555-0100");

        Assert.Equal("+14155550100", phone.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-number")]
    [InlineData("0123456789")]
    [InlineData("123")]
    public void Create_Invalid_Throws(string value)
    {
        Assert.Throws<ArgumentException>(() => PhoneNumber.Create(value));
    }

    [Fact]
    public void Equals_SameValue_AreEqual()
    {
        Assert.Equal(PhoneNumber.Create("+14155550100"), PhoneNumber.Create("+1 415 555 0100"));
    }
}
