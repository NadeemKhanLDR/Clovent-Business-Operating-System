using Clovent.Identity.Application.Tests.TestSupport;
using Clovent.Identity.Application.Users.Commands;
using Clovent.Identity.Application.Users.Queries;
using Clovent.Identity.Branches;
using Clovent.Identity.Branches.ValueObjects;
using Clovent.Identity.Companies;
using Clovent.Identity.Companies.ValueObjects;
using Clovent.Identity.Organizations;
using Clovent.Identity.Roles;
using Clovent.Identity.Roles.ValueObjects;
using Clovent.Identity.Users;
using Clovent.Identity.Users.ValueObjects;
using Xunit;

namespace Clovent.Identity.Application.Tests.Users;

public class UserHandlerTests
{
    private static User CreateUser(string userName = "jdoe") =>
        User.Create(Email.Create($"{userName}@example.com"), UserName.Create(userName), DisplayName.Create("Jane Doe"));

    [Fact]
    public async Task CreateUserCommandHandler_ValidRequest_Persists()
    {
        var repository = new FakeUserRepository();
        var handler = new CreateUserCommandHandler(repository);

        var dto = await handler.Handle(new CreateUserCommand("new@example.com", "newuser", "New User"), CancellationToken.None);

        Assert.Equal("PendingActivation", dto.Status);
        Assert.NotNull(await repository.GetByIdAsync(new UserId(dto.UserId)));
    }

    [Fact]
    public async Task CreateUserCommandHandler_DuplicateEmail_Throws()
    {
        var repository = new FakeUserRepository();
        repository.Add(CreateUser());
        var handler = new CreateUserCommandHandler(repository);

        await Assert.ThrowsAsync<IdentityDomainException>(() =>
            handler.Handle(new CreateUserCommand("jdoe@example.com", "someoneelse", "Someone Else"), CancellationToken.None));
    }

    [Fact]
    public async Task CreateUserCommandHandler_DuplicateUserName_Throws()
    {
        var repository = new FakeUserRepository();
        repository.Add(CreateUser());
        var handler = new CreateUserCommandHandler(repository);

        await Assert.ThrowsAsync<IdentityDomainException>(() =>
            handler.Handle(new CreateUserCommand("other@example.com", "jdoe", "Other"), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateUserCommandHandler_ExistingUser_ChangesDisplayName()
    {
        var repository = new FakeUserRepository();
        var user = CreateUser();
        repository.Add(user);
        var handler = new UpdateUserCommandHandler(repository);

        var dto = await handler.Handle(new UpdateUserCommand(user.Id.Value, "New Display Name"), CancellationToken.None);

        Assert.Equal("New Display Name", dto.DisplayName);
    }

    [Fact]
    public async Task UpdateUserCommandHandler_UnknownUser_Throws()
    {
        var handler = new UpdateUserCommandHandler(new FakeUserRepository());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new UpdateUserCommand(Guid.NewGuid(), "New Name"), CancellationToken.None));
    }

    [Fact]
    public async Task ActivateAndDeactivateUserCommandHandlers_RoundTrip()
    {
        var repository = new FakeUserRepository();
        var user = CreateUser();
        repository.Add(user);

        var activated = await new ActivateUserCommandHandler(repository).Handle(new ActivateUserCommand(user.Id.Value), CancellationToken.None);
        Assert.Equal("Active", activated.Status);

        var deactivated = await new DeactivateUserCommandHandler(repository).Handle(new DeactivateUserCommand(user.Id.Value), CancellationToken.None);
        Assert.Equal("Inactive", deactivated.Status);
    }

    [Fact]
    public async Task AssignAndRemoveUserToRoleCommandHandlers_RoundTrip()
    {
        var userRepository = new FakeUserRepository();
        var roleRepository = new FakeRoleRepository();
        var user = CreateUser();
        userRepository.Add(user);
        var role = Role.Create(RoleName.Create("Cashier"));
        roleRepository.Add(role);

        var assigned = await new AssignUserToRoleCommandHandler(userRepository, roleRepository)
            .Handle(new AssignUserToRoleCommand(user.Id.Value, role.Id.Value), CancellationToken.None);
        Assert.Contains(role.Id.Value, assigned.RoleIds);

        var removed = await new RemoveUserFromRoleCommandHandler(userRepository)
            .Handle(new RemoveUserFromRoleCommand(user.Id.Value, role.Id.Value), CancellationToken.None);
        Assert.DoesNotContain(role.Id.Value, removed.RoleIds);
    }

    [Fact]
    public async Task AssignUserToRoleCommandHandler_UnknownRole_Throws()
    {
        var userRepository = new FakeUserRepository();
        var user = CreateUser();
        userRepository.Add(user);
        var handler = new AssignUserToRoleCommandHandler(userRepository, new FakeRoleRepository());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new AssignUserToRoleCommand(user.Id.Value, Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task AssignUserCompanyCommandHandler_ValidRequest_Assigns()
    {
        var userRepository = new FakeUserRepository();
        var companyRepository = new FakeCompanyRepository();
        var user = CreateUser();
        userRepository.Add(user);
        var company = Company.Create(OrganizationId.New(), CompanyName.Create("Acme Retail"));
        companyRepository.Add(company);
        var handler = new AssignUserCompanyCommandHandler(userRepository, companyRepository);

        var dto = await handler.Handle(new AssignUserCompanyCommand(user.Id.Value, company.Id.Value), CancellationToken.None);

        Assert.Equal(company.Id.Value, dto.CompanyId);
    }

    [Fact]
    public async Task AssignUserBranchCommandHandler_ValidRequest_Assigns()
    {
        var userRepository = new FakeUserRepository();
        var branchRepository = new FakeBranchRepository();
        var user = CreateUser();
        userRepository.Add(user);
        var branch = Branch.Create(CompanyId.New(), BranchName.Create("Main Branch"));
        branchRepository.Add(branch);
        var handler = new AssignUserBranchCommandHandler(userRepository, branchRepository);

        var dto = await handler.Handle(new AssignUserBranchCommand(user.Id.Value, branch.Id.Value), CancellationToken.None);

        Assert.Equal(branch.Id.Value, dto.BranchId);
    }

    [Fact]
    public async Task GetUserByIdQueryHandler_UnknownUser_Throws()
    {
        var handler = new GetUserByIdQueryHandler(new FakeUserRepository());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetUserByIdQuery(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task SearchUsersQueryHandler_FiltersBySearchTextAndStatus()
    {
        var repository = new FakeUserRepository();
        var active = CreateUser("active1");
        active.Activate();
        repository.Add(active);
        repository.Add(CreateUser("pending1"));
        var handler = new SearchUsersQueryHandler(repository);

        var activeResults = await handler.Handle(new SearchUsersQuery(Status: UserStatus.Active), CancellationToken.None);
        Assert.Single(activeResults);
        Assert.Equal("active1", activeResults.Single().UserName);

        var textResults = await handler.Handle(new SearchUsersQuery(SearchText: "pending1"), CancellationToken.None);
        Assert.Single(textResults);
    }
}
