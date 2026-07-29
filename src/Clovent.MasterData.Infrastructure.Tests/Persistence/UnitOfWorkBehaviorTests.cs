using Clovent.MasterData.Application;
using Clovent.MasterData.Infrastructure.Persistence;
using Xunit;

namespace Clovent.MasterData.Infrastructure.Tests.Persistence;

public class UnitOfWorkBehaviorTests
{
    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveChangesCallCount { get; private set; }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed record SampleRequest;

    [Fact]
    public async Task Handle_CallsNextThenSavesChangesOnce()
    {
        var unitOfWork = new FakeUnitOfWork();
        var behavior = new UnitOfWorkBehavior<SampleRequest, string>(unitOfWork);
        var nextCalled = false;

        var result = await behavior.Handle(
            new SampleRequest(),
            () =>
            {
                nextCalled = true;
                Assert.Equal(0, unitOfWork.SaveChangesCallCount);
                return Task.FromResult("response");
            },
            CancellationToken.None);

        Assert.True(nextCalled);
        Assert.Equal("response", result);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_WhenNextThrows_DoesNotSaveChanges()
    {
        var unitOfWork = new FakeUnitOfWork();
        var behavior = new UnitOfWorkBehavior<SampleRequest, string>(unitOfWork);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.Handle(
                new SampleRequest(),
                () => throw new InvalidOperationException("handler failed"),
                CancellationToken.None));

        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }
}
