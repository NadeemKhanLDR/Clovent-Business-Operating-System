using Clovent.Desktop.Login;
using Xunit;

namespace Clovent.Desktop.Tests.Login;

public class LoginResultTests
{
    [Fact]
    public void Success_HasNoErrorMessage()
    {
        var result = LoginResult.Success();

        Assert.True(result.Succeeded);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void Failure_CarriesTheGivenMessage()
    {
        var result = LoginResult.Failure("bad credentials");

        Assert.False(result.Succeeded);
        Assert.Equal("bad credentials", result.ErrorMessage);
    }
}
