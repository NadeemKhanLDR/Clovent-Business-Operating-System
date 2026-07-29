using Clovent.Identity.Branches.ValueObjects;
using Xunit;

namespace Clovent.Identity.Tests.Branches;

public class BranchNameTests
{
    [Fact]
    public void Create_Valid_Succeeds()
    {
        Assert.Equal("Downtown", BranchName.Create("Downtown").Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    public void Create_TooShort_Throws(string value)
    {
        Assert.Throws<ArgumentException>(() => BranchName.Create(value));
    }

    [Fact]
    public void Create_TooLong_Throws()
    {
        Assert.Throws<ArgumentException>(() => BranchName.Create(new string('a', 201)));
    }
}
