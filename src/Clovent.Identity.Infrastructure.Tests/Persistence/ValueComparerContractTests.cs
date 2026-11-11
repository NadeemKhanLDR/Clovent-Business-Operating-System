using Clovent.Identity.Branches;
using Clovent.Identity.Companies;
using Clovent.Identity.Companies.ValueObjects;
using Clovent.Identity.Infrastructure.Persistence;
using Clovent.Identity.Infrastructure.Tests.TestSupport;
using Clovent.Identity.Organizations;
using Clovent.Identity.Organizations.ValueObjects;
using Clovent.Identity.Permissions;
using Clovent.Identity.Roles;
using Clovent.Identity.Roles.ValueObjects;
using Clovent.Identity.Users;
using Clovent.Identity.Users.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Xunit;

namespace Clovent.Identity.Infrastructure.Tests.Persistence;

public sealed class ValueComparerContractTests : SqliteTestBase
{
    [Fact]
    public void WiredIdentityCollectionComparers_EquivalentSetsInsertedInDifferentOrders_ProduceEqualHashCodes()
    {
        using var db = CreateContext();

        AssertComparerOrderInvariance<User, RoleId>(
            db,
            nameof(User.RoleIds),
            new RoleId(Guid.Parse("00000000-0000-0000-0000-000000000002")),
            new RoleId(Guid.Parse("00000000-0000-0000-0000-000000000001")));

        AssertComparerOrderInvariance<Role, PermissionId>(
            db,
            nameof(Role.PermissionIds),
            new PermissionId(Guid.Parse("00000000-0000-0000-0000-000000000002")),
            new PermissionId(Guid.Parse("00000000-0000-0000-0000-000000000001")));

        AssertComparerOrderInvariance<Organization, CompanyId>(
            db,
            nameof(Organization.CompanyIds),
            new CompanyId(Guid.Parse("00000000-0000-0000-0000-000000000002")),
            new CompanyId(Guid.Parse("00000000-0000-0000-0000-000000000001")));

        AssertComparerOrderInvariance<Company, BranchId>(
            db,
            nameof(Company.BranchIds),
            new BranchId(Guid.Parse("00000000-0000-0000-0000-000000000002")),
            new BranchId(Guid.Parse("00000000-0000-0000-0000-000000000001")));
    }

    [Fact]
    public async Task IdentityCollectionComparers_RoundTripReloadAndInPlaceMutation_PreservesPersistedValuesAndChangeTracking()
    {
        var role1 = RoleId.New();
        var role2 = RoleId.New();
        var perm1 = PermissionId.New();
        var perm2 = PermissionId.New();
        var comp1 = CompanyId.New();
        var comp2 = CompanyId.New();
        var branch1 = BranchId.New();
        var branch2 = BranchId.New();

        var user = User.Create(
            Email.Create("comparer.test@cbos.local"),
            UserName.Create("comparer.test"),
            DisplayName.Create("Comparer Test"));
        user.AssignRole(role1);

        var role = Role.Create(RoleName.Create("CashierRole"));
        role.AddPermission(perm1);

        var org = Organization.Create(OrganizationName.Create("TestOrg"));
        org.AddCompany(comp1);

        var company = Company.Create(org.Id, CompanyName.Create("TestCo"));
        company.AddBranch(branch1);

        await using (var seedDb = CreateContext())
        {
            await seedDb.Users.AddAsync(user);
            await seedDb.Roles.AddAsync(role);
            await seedDb.Organizations.AddAsync(org);
            await seedDb.Companies.AddAsync(company);
            await seedDb.SaveChangesAsync();
        }

        await using (var editDb = CreateContext())
        {
            var loadedUser = await editDb.Users.SingleAsync(u => u.Id == user.Id);
            var loadedRole = await editDb.Roles.SingleAsync(r => r.Id == role.Id);
            var loadedOrg = await editDb.Organizations.SingleAsync(o => o.Id == org.Id);
            var loadedCompany = await editDb.Companies.SingleAsync(c => c.Id == company.Id);

            Assert.False(editDb.ChangeTracker.HasChanges());

            await editDb.Entry(loadedUser).ReloadAsync();
            await editDb.Entry(loadedRole).ReloadAsync();
            await editDb.Entry(loadedOrg).ReloadAsync();
            await editDb.Entry(loadedCompany).ReloadAsync();
            Assert.False(editDb.ChangeTracker.HasChanges());

            loadedUser.AssignRole(role2);
            loadedRole.AddPermission(perm2);
            loadedOrg.AddCompany(comp2);
            loadedCompany.AddBranch(branch2);

            editDb.ChangeTracker.DetectChanges();
            Assert.True(editDb.ChangeTracker.HasChanges());
            await editDb.SaveChangesAsync();
        }

        await using var verifyDb = CreateContext();
        var persistedUser = await verifyDb.Users.AsNoTracking().SingleAsync(u => u.Id == user.Id);
        var persistedRole = await verifyDb.Roles.AsNoTracking().SingleAsync(r => r.Id == role.Id);
        var persistedOrg = await verifyDb.Organizations.AsNoTracking().SingleAsync(o => o.Id == org.Id);
        var persistedCompany = await verifyDb.Companies.AsNoTracking().SingleAsync(c => c.Id == company.Id);

        Assert.Equal(2, persistedUser.RoleIds.Count);
        Assert.Contains(role1, persistedUser.RoleIds);
        Assert.Contains(role2, persistedUser.RoleIds);

        Assert.Equal(2, persistedRole.PermissionIds.Count);
        Assert.Contains(perm1, persistedRole.PermissionIds);
        Assert.Contains(perm2, persistedRole.PermissionIds);

        Assert.Equal(2, persistedOrg.CompanyIds.Count);
        Assert.Contains(comp1, persistedOrg.CompanyIds);
        Assert.Contains(comp2, persistedOrg.CompanyIds);

        Assert.Equal(2, persistedCompany.BranchIds.Count);
        Assert.Contains(branch1, persistedCompany.BranchIds);
        Assert.Contains(branch2, persistedCompany.BranchIds);
    }

    private static void AssertComparerOrderInvariance<TEntity, TElement>(
        IdentityDbContext db,
        string propertyName,
        TElement firstItem,
        TElement secondItem)
    {
        var property = db.Model.FindEntityType(typeof(TEntity))!.FindProperty(propertyName)!;
        var comparer = Assert.IsAssignableFrom<ValueComparer<IReadOnlyCollection<TElement>>>(property.GetValueComparer());

        var setA = new HashSet<TElement> { firstItem, secondItem };
        var setB = new HashSet<TElement> { secondItem, firstItem };

        Assert.False(setA.SequenceEqual(setB));
        Assert.True(comparer.Equals(setA, setB));
        Assert.Equal(comparer.GetHashCode(setA), comparer.GetHashCode(setB));
    }
}
