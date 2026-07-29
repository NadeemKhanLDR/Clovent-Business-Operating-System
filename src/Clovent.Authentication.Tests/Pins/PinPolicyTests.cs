using Clovent.Authentication.Pins;
using Xunit;

namespace Clovent.Authentication.Tests.Pins;

public class PinPolicyTests
{
    [Fact]
    public void Default_AcceptsValidCandidate()
    {
        var result = PinPolicy.Default.Evaluate("7392");

        Assert.True(result.IsSatisfied);
    }

    [Theory]
    [InlineData("12")]
    [InlineData("1234567")]
    public void Default_WrongLength_Violates(string candidate)
    {
        var result = PinPolicy.Default.Evaluate(candidate);

        Assert.False(result.IsSatisfied);
    }

    [Fact]
    public void Default_NonDigits_Violates()
    {
        var result = PinPolicy.Default.Evaluate("12a4");

        Assert.Contains(result.Violations, v => v.Contains("digits only"));
    }

    [Theory]
    [InlineData("1111")]
    [InlineData("99999")]
    public void Default_RepeatedDigit_Violates(string candidate)
    {
        var result = PinPolicy.Default.Evaluate(candidate);

        Assert.Contains(result.Violations, v => v.Contains("repeated"));
    }

    [Theory]
    [InlineData("1234")]
    [InlineData("4321")]
    [InlineData("6789")]
    public void Default_SequentialDigits_Violates(string candidate)
    {
        var result = PinPolicy.Default.Evaluate(candidate);

        Assert.Contains(result.Violations, v => v.Contains("sequential"));
    }

    [Fact]
    public void Create_MinLengthNotPositive_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PinPolicy.Create(0, 6, true, true));
    }

    [Fact]
    public void Create_MaxLessThanMin_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PinPolicy.Create(6, 4, true, true));
    }
}
