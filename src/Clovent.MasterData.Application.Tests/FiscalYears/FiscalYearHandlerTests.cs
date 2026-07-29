using Clovent.Identity.Organizations;
using Clovent.MasterData.Application.FiscalYears.Commands;
using Clovent.MasterData.Application.FiscalYears.Queries;
using Clovent.MasterData.Application.Tests.TestSupport;
using Clovent.MasterData.FiscalYears;
using Clovent.MasterData.FiscalYears.ValueObjects;
using Xunit;

namespace Clovent.MasterData.Application.Tests.FiscalYears;

public class FiscalYearHandlerTests
{
    private static readonly DateOnly StartDate = new(2026, 1, 1);
    private static readonly DateOnly EndDate = new(2026, 12, 31);

    [Fact]
    public async Task CreateFiscalYearCommandHandler_ValidRequest_PersistsAndReturnsDto()
    {
        var repository = new FakeFiscalYearRepository();
        var handler = new CreateFiscalYearCommandHandler(repository);

        var dto = await handler.Handle(new CreateFiscalYearCommand(OrganizationId.New().Value, "FY2026", StartDate, EndDate), CancellationToken.None);

        Assert.Equal("FY2026", dto.Name);
        Assert.Equal("Open", dto.Status);
        Assert.NotNull(await repository.GetByIdAsync(new FiscalYearId(dto.FiscalYearId)));
    }

    [Fact]
    public async Task CreateFiscalYearCommandHandler_EndBeforeStart_Throws()
    {
        var handler = new CreateFiscalYearCommandHandler(new FakeFiscalYearRepository());

        await Assert.ThrowsAsync<MasterDataDomainException>(() =>
            handler.Handle(new CreateFiscalYearCommand(OrganizationId.New().Value, "FY2026", EndDate, StartDate), CancellationToken.None));
    }

    [Fact]
    public async Task RenameFiscalYearCommandHandler_UnknownFiscalYear_Throws()
    {
        var handler = new RenameFiscalYearCommandHandler(new FakeFiscalYearRepository());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new RenameFiscalYearCommand(Guid.NewGuid(), "New Name"), CancellationToken.None));
    }

    [Fact]
    public async Task CloseFiscalYearCommandHandler_ThenCloseAgain_Throws()
    {
        var repository = new FakeFiscalYearRepository();
        var fiscalYear = FiscalYear.Create(OrganizationId.New(), FiscalYearName.Create("FY2026"), StartDate, EndDate);
        repository.Add(fiscalYear);
        var handler = new CloseFiscalYearCommandHandler(repository);

        var closed = await handler.Handle(new CloseFiscalYearCommand(fiscalYear.Id.Value), CancellationToken.None);
        Assert.Equal("Closed", closed.Status);

        await Assert.ThrowsAsync<MasterDataDomainException>(() =>
            handler.Handle(new CloseFiscalYearCommand(fiscalYear.Id.Value), CancellationToken.None));
    }

    [Fact]
    public async Task GetFiscalYearByIdQueryHandler_UnknownFiscalYear_Throws()
    {
        var handler = new GetFiscalYearByIdQueryHandler(new FakeFiscalYearRepository());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetFiscalYearByIdQuery(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task ListFiscalYearsByOrganizationQueryHandler_FiltersToOwningOrganization()
    {
        var repository = new FakeFiscalYearRepository();
        var organizationId = OrganizationId.New();
        repository.Add(FiscalYear.Create(organizationId, FiscalYearName.Create("FY A"), StartDate, EndDate));
        repository.Add(FiscalYear.Create(OrganizationId.New(), FiscalYearName.Create("FY B"), StartDate, EndDate));
        var handler = new ListFiscalYearsByOrganizationQueryHandler(repository);

        var result = await handler.Handle(new ListFiscalYearsByOrganizationQuery(organizationId.Value), CancellationToken.None);

        Assert.Single(result);
    }
}
