using Clovent.Desktop.Sessions;
using Xunit;

namespace Clovent.Desktop.Tests.Sessions;

public class CurrentSessionTests
{
    [Fact]
    public void Initially_IsNotAuthenticated()
    {
        var session = new CurrentSession();

        Assert.False(session.IsAuthenticated);
        Assert.Null(session.UserId);
        Assert.Null(session.SessionId);
    }

    [Fact]
    public void SignIn_SetsAuthenticatedStateAndRaisesChanged()
    {
        var session = new CurrentSession();
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var changedRaised = false;
        session.Changed += (_, _) => changedRaised = true;

        session.SignIn(userId, sessionId, "Alice Example");

        Assert.True(session.IsAuthenticated);
        Assert.Equal(userId, session.UserId);
        Assert.Equal(sessionId, session.SessionId);
        Assert.Equal("Alice Example", session.DisplayName);
        Assert.True(changedRaised);
    }

    [Fact]
    public void SignOut_ClearsAuthenticatedStateAndRaisesChanged()
    {
        var session = new CurrentSession();
        session.SignIn(Guid.NewGuid(), Guid.NewGuid(), "Alice Example");
        var changedRaised = false;
        session.Changed += (_, _) => changedRaised = true;

        session.SignOut();

        Assert.False(session.IsAuthenticated);
        Assert.Null(session.UserId);
        Assert.Null(session.SessionId);
        Assert.Null(session.DisplayName);
        Assert.True(changedRaised);
    }
}
