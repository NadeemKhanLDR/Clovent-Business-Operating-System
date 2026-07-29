using Clovent.Identity.Application.Permissions.Queries;
using Clovent.Identity.Application.Tests.TestSupport;
using Clovent.Identity.Permissions;
using Clovent.Identity.Permissions.ValueObjects;
using Xunit;

namespace Clovent.Identity.Application.Tests.Permissions;

public class PermissionHandlerTests
{
    [Fact]
    public async Task ListPermissionsQueryHandler_ReturnsEveryPermission()
    {
        var repository = new FakePermissionRepository();
        repository.Add(Permission.Create(PermissionCode.Create("feature.pos.pay"), "Allows recording payments."));
        repository.Add(Permission.Create(PermissionCode.Create("feature.users.create"), "Allows creating users."));
        var handler = new ListPermissionsQueryHandler(repository);

        var result = await handler.Handle(new ListPermissionsQuery(), CancellationToken.None);

        Assert.Equal(2, result.Count);
    }
}
