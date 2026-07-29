using Clovent.Authentication.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Authentication.Infrastructure.Tests.TestSupport;

/// <summary>
/// Backs a test with EF Core's InMemory provider, which evaluates every
/// query client-side (full LINQ-to-Objects semantics). Used only for
/// verifying repository query *logic* (filtering, ordering) where the
/// SQLite provider used by <see cref="SqliteTestBase"/> cannot translate the
/// query server-side - specifically, range comparisons on
/// <see cref="DateTimeOffset"/> columns, a documented SQLite provider
/// limitation that does not apply to SQL Server, the real target of this
/// project. Structural/mapping concerns (value converters, column types,
/// unique indexes, constructor binding) stay covered by
/// <see cref="SqliteTestBase"/>, which exercises a real relational engine.
/// </summary>
public abstract class InMemoryTestBase
{
    private readonly string _databaseName = Guid.NewGuid().ToString();

    /// <summary>Creates a fresh <see cref="AuthenticationDbContext"/> against this test's shared InMemory database.</summary>
    protected AuthenticationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AuthenticationDbContext>()
            .UseInMemoryDatabase(_databaseName)
            .Options;

        return new AuthenticationDbContext(options);
    }
}
