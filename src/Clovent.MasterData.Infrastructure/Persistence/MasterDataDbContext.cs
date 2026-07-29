using Clovent.MasterData.Currencies;
using Clovent.MasterData.Departments;
using Clovent.MasterData.FiscalYears;
using Clovent.MasterData.Languages;
using Clovent.MasterData.Settings;
using Clovent.MasterData.Terminals;
using Clovent.MasterData.TimeZones;
using Clovent.MasterData.Warehouses;
using Microsoft.EntityFrameworkCore;

namespace Clovent.MasterData.Infrastructure.Persistence;

/// <summary>
/// EF Core persistence context for the MasterData bounded context
/// (Milestone 13, "Organization &amp; Master Data Foundation"). Tables live
/// under the <c>MasterData</c> schema, mirroring how <c>IdentityDbContext</c>
/// uses the <c>Identity</c> schema.
/// </summary>
public sealed class MasterDataDbContext(DbContextOptions<MasterDataDbContext> options) : DbContext(options)
{
    /// <summary>Department aggregates.</summary>
    public DbSet<Department> Departments => Set<Department>();

    /// <summary>Warehouse aggregates.</summary>
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();

    /// <summary>Terminal aggregates.</summary>
    public DbSet<Terminal> Terminals => Set<Terminal>();

    /// <summary>FiscalYear aggregates.</summary>
    public DbSet<FiscalYear> FiscalYears => Set<FiscalYear>();

    /// <summary>Currency aggregates.</summary>
    public DbSet<Currency> Currencies => Set<Currency>();

    /// <summary>Language aggregates.</summary>
    public DbSet<Language> Languages => Set<Language>();

    /// <summary>TimeZoneEntry aggregates.</summary>
    public DbSet<TimeZoneEntry> TimeZoneEntries => Set<TimeZoneEntry>();

    /// <summary>BusinessSettings aggregates.</summary>
    public DbSet<BusinessSettings> BusinessSettings => Set<BusinessSettings>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MasterDataDbContext).Assembly);
    }
}
