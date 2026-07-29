using Clovent.MasterData.Application.Tests.TestSupport;
using Clovent.MasterData.Application.TimeZones.Commands;
using Clovent.MasterData.Application.TimeZones.Queries;
using Clovent.MasterData.TimeZones;
using Xunit;

namespace Clovent.MasterData.Application.Tests.TimeZones;

public class TimeZoneEntryHandlerTests
{
    [Fact]
    public async Task CreateTimeZoneEntryCommandHandler_ValidRequest_PersistsAndReturnsDto()
    {
        var repository = new FakeTimeZoneRepository();
        var handler = new CreateTimeZoneEntryCommandHandler(repository);

        var dto = await handler.Handle(new CreateTimeZoneEntryCommand("UTC", "(UTC) Coordinated Universal Time", 0), CancellationToken.None);

        Assert.Equal("UTC", dto.IanaId);
        Assert.Equal("Active", dto.Status);
        Assert.NotNull(await repository.GetByIdAsync(new TimeZoneEntryId(dto.TimeZoneEntryId)));
    }

    [Fact]
    public async Task ActivateAndDeactivateTimeZoneEntryCommandHandlers_RoundTrip()
    {
        var repository = new FakeTimeZoneRepository();
        var entry = TimeZoneEntry.Create(IanaId.Create("UTC"), "UTC", 0);
        entry.Deactivate();
        repository.Add(entry);

        var activated = await new ActivateTimeZoneEntryCommandHandler(repository)
            .Handle(new ActivateTimeZoneEntryCommand(entry.Id.Value), CancellationToken.None);
        Assert.Equal("Active", activated.Status);

        var deactivated = await new DeactivateTimeZoneEntryCommandHandler(repository)
            .Handle(new DeactivateTimeZoneEntryCommand(entry.Id.Value), CancellationToken.None);
        Assert.Equal("Inactive", deactivated.Status);
    }

    [Fact]
    public async Task GetTimeZoneEntryByIdQueryHandler_UnknownEntry_Throws()
    {
        var handler = new GetTimeZoneEntryByIdQueryHandler(new FakeTimeZoneRepository());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetTimeZoneEntryByIdQuery(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task ListTimeZoneEntriesQueryHandler_ReturnsEveryEntry()
    {
        var repository = new FakeTimeZoneRepository();
        repository.Add(TimeZoneEntry.Create(IanaId.Create("UTC"), "UTC", 0));
        repository.Add(TimeZoneEntry.Create(IanaId.Create("America/New_York"), "Eastern Time", -300));
        var handler = new ListTimeZoneEntriesQueryHandler(repository);

        var result = await handler.Handle(new ListTimeZoneEntriesQuery(), CancellationToken.None);

        Assert.Equal(2, result.Count);
    }
}
