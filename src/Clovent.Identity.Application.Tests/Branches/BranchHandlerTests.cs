using Clovent.Identity.Application.Branches.Commands;
using Clovent.Identity.Application.Branches.Queries;
using Clovent.Identity.Application.Tests.TestSupport;
using Clovent.Identity.Branches;
using Clovent.Identity.Branches.ValueObjects;
using Clovent.Identity.Companies;
using Xunit;

namespace Clovent.Identity.Application.Tests.Branches;

public class BranchHandlerTests
{
    [Fact]
    public async Task CreateBranchCommandHandler_ValidRequest_PersistsAndAddsToCompany()
    {
        var companyRepository = new FakeCompanyRepository();
        var company = Company.Create(Clovent.Identity.Organizations.OrganizationId.New(), Clovent.Identity.Companies.ValueObjects.CompanyName.Create("Acme Retail"));
        companyRepository.Add(company);
        var branchRepository = new FakeBranchRepository();
        var handler = new CreateBranchCommandHandler(companyRepository, branchRepository);

        var dto = await handler.Handle(new CreateBranchCommand(company.Id.Value, "Main Branch", "123 Main St", "Springfield", "IL", "62704", "USA"), CancellationToken.None);

        Assert.Equal("Main Branch", dto.Name);
        Assert.Equal("Springfield", dto.City);
        Assert.Contains(new BranchId(dto.BranchId), company.BranchIds);
    }

    [Fact]
    public async Task CreateBranchCommandHandler_NoAddressFields_LeavesAddressNull()
    {
        var companyRepository = new FakeCompanyRepository();
        var company = Company.Create(Clovent.Identity.Organizations.OrganizationId.New(), Clovent.Identity.Companies.ValueObjects.CompanyName.Create("Acme Retail"));
        companyRepository.Add(company);
        var handler = new CreateBranchCommandHandler(companyRepository, new FakeBranchRepository());

        var dto = await handler.Handle(new CreateBranchCommand(company.Id.Value, "Main Branch"), CancellationToken.None);

        Assert.Null(dto.Street);
        Assert.Null(dto.City);
    }

    [Fact]
    public async Task CreateBranchCommandHandler_UnknownCompany_Throws()
    {
        var handler = new CreateBranchCommandHandler(new FakeCompanyRepository(), new FakeBranchRepository());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new CreateBranchCommand(Guid.NewGuid(), "Main Branch"), CancellationToken.None));
    }

    [Fact]
    public async Task RenameBranchCommandHandler_ExistingBranch_UpdatesName()
    {
        var repository = new FakeBranchRepository();
        var branch = Branch.Create(CompanyId.New(), BranchName.Create("Old Name"));
        repository.Add(branch);
        var handler = new RenameBranchCommandHandler(repository);

        var dto = await handler.Handle(new RenameBranchCommand(branch.Id.Value, "New Name"), CancellationToken.None);

        Assert.Equal("New Name", dto.Name);
    }

    [Fact]
    public async Task SetBranchAddressCommandHandler_AllFieldsNull_ClearsAddress()
    {
        var repository = new FakeBranchRepository();
        var address = Clovent.Identity.Shared.ValueObjects.Address.Create("123 Main St", "Springfield", "IL", "62704", "USA");
        var branch = Branch.Create(CompanyId.New(), BranchName.Create("Main Branch"), address);
        repository.Add(branch);
        var handler = new SetBranchAddressCommandHandler(repository);

        var dto = await handler.Handle(new SetBranchAddressCommand(branch.Id.Value, null, null, null, null, null), CancellationToken.None);

        Assert.Null(dto.Street);
        Assert.Null(dto.City);
    }

    [Fact]
    public async Task ActivateAndDeactivateBranchCommandHandlers_RoundTrip()
    {
        var repository = new FakeBranchRepository();
        var branch = Branch.Create(CompanyId.New(), BranchName.Create("Main Branch"));
        branch.Deactivate();
        repository.Add(branch);

        var activated = await new ActivateBranchCommandHandler(repository)
            .Handle(new ActivateBranchCommand(branch.Id.Value), CancellationToken.None);
        Assert.Equal("Active", activated.Status);

        var deactivated = await new DeactivateBranchCommandHandler(repository)
            .Handle(new DeactivateBranchCommand(branch.Id.Value), CancellationToken.None);
        Assert.Equal("Inactive", deactivated.Status);
    }

    [Fact]
    public async Task GetBranchByIdQueryHandler_UnknownBranch_Throws()
    {
        var handler = new GetBranchByIdQueryHandler(new FakeBranchRepository());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetBranchByIdQuery(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task ListBranchesByCompanyQueryHandler_FiltersToOwningCompany()
    {
        var repository = new FakeBranchRepository();
        var companyId = CompanyId.New();
        repository.Add(Branch.Create(companyId, BranchName.Create("Branch A")));
        repository.Add(Branch.Create(CompanyId.New(), BranchName.Create("Branch B")));
        var handler = new ListBranchesByCompanyQueryHandler(repository);

        var result = await handler.Handle(new ListBranchesByCompanyQuery(companyId.Value), CancellationToken.None);

        Assert.Single(result);
    }
}
