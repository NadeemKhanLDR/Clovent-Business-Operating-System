using Clovent.Authentication.Application.Sessions.Commands;
using Clovent.Authentication.Application.Tests.TestSupport;
using Xunit;

namespace Clovent.Authentication.Application.Tests.Sessions;

public class StartSessionCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_ValidRequest_PersistsAndReturnsSessionDto()
    {
        var repository = new FakeSessionRepository();
        var handler = new StartSessionCommandHandler(repository, new FakeTimeProvider(Now));
        var userId = Guid.NewGuid();

        var dto = await handler.Handle(new StartSessionCommand(userId), CancellationToken.None);

        Assert.Equal(userId, dto.UserId);
        Assert.Equal("Active", dto.Status);
        var stored = await repository.GetByIdAsync(new Clovent.Authentication.Sessions.SessionId(dto.SessionId));
        Assert.NotNull(stored);
    }

    [Fact]
    public async Task Handle_NoIdleTimeoutSpecified_UsesDefault()
    {
        var handler = new StartSessionCommandHandler(new FakeSessionRepository(), new FakeTimeProvider(Now));

        var dto = await handler.Handle(new StartSessionCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(Now.AddMinutes(30), dto.ExpiresAtUtc);
    }

    [Fact]
    public async Task Handle_CustomIdleTimeout_IsHonored()
    {
        var handler = new StartSessionCommandHandler(new FakeSessionRepository(), new FakeTimeProvider(Now));

        var dto = await handler.Handle(new StartSessionCommand(Guid.NewGuid(), TimeSpan.FromHours(2)), CancellationToken.None);

        Assert.Equal(Now.AddHours(2), dto.ExpiresAtUtc);
    }

    [Fact]
    public async Task Handle_InvalidIpAddress_Throws()
    {
        var handler = new StartSessionCommandHandler(new FakeSessionRepository(), new FakeTimeProvider(Now));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.Handle(new StartSessionCommand(Guid.NewGuid(), IpAddress: "not-an-ip"), CancellationToken.None));
    }
}
