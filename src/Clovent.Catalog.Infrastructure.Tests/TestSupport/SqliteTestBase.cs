using Clovent.Catalog.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Catalog.Infrastructure.Tests.TestSupport;

/// <summary>Backs each test with a real relational engine (SQLite, in-memory) - see the identical <c>Clovent.Identity.Infrastructure.Tests.TestSupport.SqliteTestBase</c> for the full reasoning.</summary>
public abstract class SqliteTestBase : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<CatalogDbContext> _options;

    protected SqliteTestBase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    /// <summary>Creates a fresh <see cref="CatalogDbContext"/> against the shared in-memory database.</summary>
    protected CatalogDbContext CreateContext() => new(_options);

    /// <inheritdoc/>
    public void Dispose()
    {
        _connection.Dispose();
    }
}
