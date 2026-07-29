using Clovent.Identity.Application.Companies.Commands;
using Clovent.Identity.Application.Companies.Queries;
using Clovent.Identity.Application.Tests.TestSupport;
using Clovent.Identity.Companies;
using Clovent.Identity.Companies.ValueObjects;
using Clovent.Identity.Organizations;
using Clovent.Identity.Organizations.ValueObjects;
using Xunit;

namespace Clovent.Identity.Application.Tests.Companies;

public class CompanyHandlerTests
{
    [Fact]
    public async Task CreateCompanyCommandHandler_ValidRequest_PersistsAndAddsToOrganization()
    {
        var organizationRepository = new FakeOrganizationRepository();
        var organization = Organization.Create(OrganizationName.Create("Acme Corp"));
        organizationRepository.Add(organization);
        var companyRepository = new FakeCompanyRepository();
        var handler = new CreateCompanyCommandHandler(organizationRepository, companyRepository);

        var dto = await handler.Handle(new CreateCompanyCommand(organization.Id.Value, "Acme Retail"), CancellationToken.None);

        Assert.Equal("Acme Retail", dto.Name);
        Assert.Contains(new CompanyId(dto.CompanyId), organization.CompanyIds);
        Assert.NotNull(await companyRepository.GetByIdAsync(new CompanyId(dto.CompanyId)));
    }

    [Fact]
    public async Task CreateCompanyCommandHandler_UnknownOrganization_Throws()
    {
        var handler = new CreateCompanyCommandHandler(new FakeOrganizationRepository(), new FakeCompanyRepository());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new CreateCompanyCommand(Guid.NewGuid(), "Acme Retail"), CancellationToken.None));
    }

    [Fact]
    public async Task RenameCompanyCommandHandler_ExistingCompany_UpdatesName()
    {
        var repository = new FakeCompanyRepository();
        var company = Company.Create(OrganizationId.New(), CompanyName.Create("Old Name"));
        repository.Add(company);
        var handler = new RenameCompanyCommandHandler(repository);

        var dto = await handler.Handle(new RenameCompanyCommand(company.Id.Value, "New Name"), CancellationToken.None);

        Assert.Equal("New Name", dto.Name);
    }

    [Fact]
    public async Task SetCompanyTaxIdCommandHandler_UnknownCompany_Throws()
    {
        var handler = new SetCompanyTaxIdCommandHandler(new FakeCompanyRepository());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new SetCompanyTaxIdCommand(Guid.NewGuid(), "TAX-1"), CancellationToken.None));
    }

    [Fact]
    public async Task ActivateAndDeactivateCompanyCommandHandlers_RoundTrip()
    {
        var repository = new FakeCompanyRepository();
        var company = Company.Create(OrganizationId.New(), CompanyName.Create("Acme Retail"));
        company.Deactivate();
        repository.Add(company);

        var activated = await new ActivateCompanyCommandHandler(repository)
            .Handle(new ActivateCompanyCommand(company.Id.Value), CancellationToken.None);
        Assert.Equal("Active", activated.Status);

        var deactivated = await new DeactivateCompanyCommandHandler(repository)
            .Handle(new DeactivateCompanyCommand(company.Id.Value), CancellationToken.None);
        Assert.Equal("Inactive", deactivated.Status);
    }

    [Fact]
    public async Task GetCompanyByIdQueryHandler_UnknownCompany_Throws()
    {
        var handler = new GetCompanyByIdQueryHandler(new FakeCompanyRepository());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetCompanyByIdQuery(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task ListCompaniesByOrganizationQueryHandler_FiltersToOwningOrganization()
    {
        var repository = new FakeCompanyRepository();
        var organizationId = OrganizationId.New();
        repository.Add(Company.Create(organizationId, CompanyName.Create("Company A")));
        repository.Add(Company.Create(OrganizationId.New(), CompanyName.Create("Company B")));
        var handler = new ListCompaniesByOrganizationQueryHandler(repository);

        var result = await handler.Handle(new ListCompaniesByOrganizationQuery(organizationId.Value), CancellationToken.None);

        Assert.Single(result);
    }
}
