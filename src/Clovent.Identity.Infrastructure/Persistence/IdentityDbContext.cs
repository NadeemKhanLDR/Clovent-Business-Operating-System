using Clovent.Identity.Branches;
using Clovent.Identity.Companies;
using Clovent.Identity.Organizations;
using Clovent.Identity.Permissions;
using Clovent.Identity.Roles;
using Clovent.Identity.Users;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Identity.Infrastructure.Persistence;

/// <summary>
/// EF Core persistence context for the Identity bounded context. Milestone 9
/// ("Authentication Integration") added <see cref="User"/> persistence;
/// Milestone 10 ("Authorization") added <see cref="Role"/> and
/// <see cref="Permission"/>; Milestone 13 ("Organization &amp; Master Data
/// Foundation") added <see cref="Organization"/>/<see cref="Company"/>/<see cref="Branch"/>.
/// Tables live under the <c>Identity</c> schema, mirroring how
/// <c>AuthenticationDbContext</c> uses the <c>Authentication</c> schema.
/// </summary>
public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options)
{
    /// <summary>User aggregates.</summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>Role aggregates.</summary>
    public DbSet<Role> Roles => Set<Role>();

    /// <summary>Permission aggregates.</summary>
    public DbSet<Permission> Permissions => Set<Permission>();

    /// <summary>Organization aggregates.</summary>
    public DbSet<Organization> Organizations => Set<Organization>();

    /// <summary>Company aggregates.</summary>
    public DbSet<Company> Companies => Set<Company>();

    /// <summary>Branch aggregates.</summary>
    public DbSet<Branch> Branches => Set<Branch>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
    }
}
