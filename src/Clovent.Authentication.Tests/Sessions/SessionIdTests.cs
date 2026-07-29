using Clovent.Authentication.Sessions;
using Xunit;

namespace Clovent.Authentication.Tests.Sessions;

public class SessionIdTests
{
    [Fact]
    public void New_IsNotEmpty()
    {
        Assert.NotEqual(Guid.Empty, SessionId.New().Value);
    }

    [Fact]
    public void Constructor_EmptyGuid_Throws()
    {
        Assert.Throws<ArgumentException>(() => new SessionId(Guid.Empty));
    }
}
