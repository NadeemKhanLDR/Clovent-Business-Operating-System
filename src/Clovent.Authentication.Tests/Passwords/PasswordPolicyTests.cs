using Clovent.Authentication.Passwords;
using Xunit;

namespace Clovent.Authentication.Tests.Passwords;

public class PasswordPolicyTests
{
    [Fact]
    public void Default_AcceptsStrongCandidate()
    {
        var result = PasswordPolicy.Default.Evaluate("Str0ng!Pass");

        Assert.True(result.IsSatisfied);
        Assert.Empty(result.Violations);
    }

    [Fact]
    public void Default_TooShort_ViolatesMinLength()
    {
        var result = PasswordPolicy.Default.Evaluate("Sh0rt!");

        Assert.False(result.IsSatisfied);
        Assert.Contains(result.Violations, v => v.Contains("at least"));
    }

    [Fact]
    public void Default_MissingUppercase_Violates()
    {
        var result = PasswordPolicy.Default.Evaluate("weak1!password");

        Assert.Contains(result.Violations, v => v.Contains("uppercase"));
    }

    [Fact]
    public void Default_MissingDigit_Violates()
    {
        var result = PasswordPolicy.Default.Evaluate("NoDigits!Here");

        Assert.Contains(result.Violations, v => v.Contains("digit"));
    }

    [Fact]
    public void Default_MissingSpecialCharacter_Violates()
    {
        var result = PasswordPolicy.Default.Evaluate("NoSpecial1Chars");

        Assert.Contains(result.Violations, v => v.Contains("special character"));
    }

    [Fact]
    public void Evaluate_MultipleViolations_ListsAll()
    {
        var result = PasswordPolicy.Default.Evaluate("weak");

        Assert.True(result.Violations.Count > 1);
    }

    [Fact]
    public void Create_MinLengthNotPositive_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PasswordPolicy.Create(0, 10, true, true, true, true));
    }

    [Fact]
    public void Create_MaxLessThanMin_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PasswordPolicy.Create(10, 5, true, true, true, true));
    }

    [Fact]
    public void Equals_SameConfiguration_AreEqual()
    {
        var a = PasswordPolicy.Create(8, 64, true, true, true, false);
        var b = PasswordPolicy.Create(8, 64, true, true, true, false);

        Assert.Equal(a, b);
    }
}
