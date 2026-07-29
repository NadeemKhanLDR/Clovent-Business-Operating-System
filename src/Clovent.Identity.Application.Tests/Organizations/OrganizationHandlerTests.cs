using Clovent.Identity.Application.Organizations.Commands;
using Clovent.Identity.Application.Organizations.Queries;
using Clovent.Identity.Application.Tests.TestSupport;
using Clovent.Identity.Organizations;
using Clovent.Identity.Organizations.ValueObjects;
using Xunit;

namespace Clovent.Identity.Application.Tests.Organizations;

public class OrganizationHandlerTests
{
    [Fact]
    public async Task CreateOrganizationCommandHandler_ValidRequest_PersistsAndReturnsDto()
    {
        var repository = new FakeOrganizationRepository();
        var handler = new CreateOrganizationCommandHandler(repository);

        var dto = await handler.Handle(new CreateOrganizationCommand("Acme Corp", "TAX-1"), CancellationToken.None);

        Assert.Equal("Acme Corp", dto.Name);
        Assert.Equal("TAX-1", dto.TaxId);
        Assert.Equal("Active", dto.Status);
        Assert.NotNull(await repository.GetByIdAsync(new OrganizationId(dto.OrganizationId)));
    }

    [Fact]
    public async Task RenameOrganizationCommandHandler_ExistingOrganization_UpdatesName()
    {
        var repository = new FakeOrganizationRepository();
        var organization = Organization.Create(OrganizationName.Create("Old Name"));
        repository.Add(organization);
        var handler = new RenameOrganizationCommandHandler(repository);

        var dto = await handler.Handle(new RenameOrganizationCommand(organization.Id.Value, "New Name"), CancellationToken.None);

        Assert.Equal("New Name", dto.Name);
    }

    [Fact]
    public async Task RenameOrganizationCommandHandler_UnknownOrganization_Throws()
    {
        var handler = new RenameOrganizationCommandHandler(new FakeOrganizationRepository());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new RenameOrganizationCommand(Guid.NewGuid(), "New Name"), CancellationToken.None));
    }

    [Fact]
    public async Task SetOrganizationTaxIdCommandHandler_ClearsTaxId_WhenNull()
    {
        var repository = new FakeOrganizationRepository();
        var organization = Organization.Create(OrganizationName.Create("Acme Corp"), Clovent.Identity.Shared.ValueObjects.TaxId.Create("TAX-1"));
        repository.Add(organization);
        var handler = new SetOrganizationTaxIdCommandHandler(repository);

        var dto = await handler.Handle(new SetOrganizationTaxIdCommand(organization.Id.Value, null), CancellationToken.None);

        Assert.Null(dto.TaxId);
    }

    [Fact]
    public async Task ActivateOrganizationCommandHandler_ThenDeactivate_RoundTrips()
    {
        var repository = new FakeOrganizationRepository();
        var organization = Organization.Create(OrganizationName.Create("Acme Corp"));
        organization.Deactivate();
        repository.Add(organization);

        var activated = await new ActivateOrganizationCommandHandler(repository)
            .Handle(new ActivateOrganizationCommand(organization.Id.Value), CancellationToken.None);
        Assert.Equal("Active", activated.Status);

        var deactivated = await new DeactivateOrganizationCommandHandler(repository)
            .Handle(new DeactivateOrganizationCommand(organization.Id.Value), CancellationToken.None);
        Assert.Equal("Inactive", deactivated.Status);
    }

    [Fact]
    public async Task GetOrganizationByIdQueryHandler_UnknownOrganization_Throws()
    {
        var handler = new GetOrganizationByIdQueryHandler(new FakeOrganizationRepository());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetOrganizationByIdQuery(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task ListOrganizationsQueryHandler_ReturnsEveryOrganization()
    {
        var repository = new FakeOrganizationRepository();
        repository.Add(Organization.Create(OrganizationName.Create("Org One")));
        repository.Add(Organization.Create(OrganizationName.Create("Org Two")));
        var handler = new ListOrganizationsQueryHandler(repository);

        var result = await handler.Handle(new ListOrganizationsQuery(), CancellationToken.None);

        Assert.Equal(2, result.Count);
    }
}
