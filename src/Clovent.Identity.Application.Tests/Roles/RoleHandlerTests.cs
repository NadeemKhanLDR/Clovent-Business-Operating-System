using Clovent.Identity.Application.Roles.Commands;
using Clovent.Identity.Application.Roles.Queries;
using Clovent.Identity.Application.Tests.TestSupport;
using Clovent.Identity.Permissions;
using Clovent.Identity.Permissions.ValueObjects;
using Clovent.Identity.Roles;
using Clovent.Identity.Roles.ValueObjects;
using Xunit;

namespace Clovent.Identity.Application.Tests.Roles;

public class RoleHandlerTests
{
    [Fact]
    public async Task CreateRoleCommandHandler_ValidRequest_Persists()
    {
        var repository = new FakeRoleRepository();
        var handler = new CreateRoleCommandHandler(repository);

        var dto = await handler.Handle(new CreateRoleCommand("Cashier"), CancellationToken.None);

        Assert.Equal("Cashier", dto.Name);
        Assert.NotNull(await repository.GetByIdAsync(new RoleId(dto.RoleId)));
    }

    [Fact]
    public async Task CreateRoleCommandHandler_DuplicateName_Throws()
    {
        var repository = new FakeRoleRepository();
        repository.Add(Role.Create(RoleName.Create("Cashier")));
        var handler = new CreateRoleCommandHandler(repository);

        await Assert.ThrowsAsync<Clovent.Identity.IdentityDomainException>(() =>
            handler.Handle(new CreateRoleCommand("Cashier"), CancellationToken.None));
    }

    [Fact]
    public async Task RenameRoleCommandHandler_ExistingRole_UpdatesName()
    {
        var repository = new FakeRoleRepository();
        var role = Role.Create(RoleName.Create("Old Name"));
        repository.Add(role);
        var handler = new RenameRoleCommandHandler(repository);

        var dto = await handler.Handle(new RenameRoleCommand(role.Id.Value, "New Name"), CancellationToken.None);

        Assert.Equal("New Name", dto.Name);
    }

    [Fact]
    public async Task AssignAndRemovePermissionToRoleCommandHandlers_RoundTrip()
    {
        var roleRepository = new FakeRoleRepository();
        var permissionRepository = new FakePermissionRepository();
        var role = Role.Create(RoleName.Create("Cashier"));
        roleRepository.Add(role);
        var permission = Permission.Create(PermissionCode.Create("feature.pos.pay"), "Allows recording payments.");
        permissionRepository.Add(permission);

        var assigned = await new AssignPermissionToRoleCommandHandler(roleRepository, permissionRepository)
            .Handle(new AssignPermissionToRoleCommand(role.Id.Value, permission.Id.Value), CancellationToken.None);
        Assert.Contains(permission.Id.Value, assigned.PermissionIds);

        var removed = await new RemovePermissionFromRoleCommandHandler(roleRepository)
            .Handle(new RemovePermissionFromRoleCommand(role.Id.Value, permission.Id.Value), CancellationToken.None);
        Assert.DoesNotContain(permission.Id.Value, removed.PermissionIds);
    }

    [Fact]
    public async Task AssignPermissionToRoleCommandHandler_UnknownPermission_Throws()
    {
        var roleRepository = new FakeRoleRepository();
        var role = Role.Create(RoleName.Create("Cashier"));
        roleRepository.Add(role);
        var handler = new AssignPermissionToRoleCommandHandler(roleRepository, new FakePermissionRepository());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new AssignPermissionToRoleCommand(role.Id.Value, Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task ListRolesQueryHandler_ReturnsEveryRole()
    {
        var repository = new FakeRoleRepository();
        repository.Add(Role.Create(RoleName.Create("Cashier")));
        repository.Add(Role.Create(RoleName.Create("Manager")));
        var handler = new ListRolesQueryHandler(repository);

        var result = await handler.Handle(new ListRolesQuery(), CancellationToken.None);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetRoleByIdQueryHandler_UnknownRole_Throws()
    {
        var handler = new GetRoleByIdQueryHandler(new FakeRoleRepository());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetRoleByIdQuery(Guid.NewGuid()), CancellationToken.None));
    }
}
