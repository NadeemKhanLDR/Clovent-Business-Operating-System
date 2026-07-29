using Clovent.Authentication.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Authentication.Infrastructure.Tests.TestSupport;

/// <summary>
/// Backs each test with a real relational engine (SQLite, in-memory) rather
/// than EF Core's InMemory provider - the InMemory provider ignores most of
/// what <see cref="Configurations"/> configures (value converters, column
/// types, unique indexes), so it would not actually exercise the mappings
/// this milestone is about. The connection is kept open for the test's
/// lifetime and shared across multiple <see cref="AuthenticationDbContext"/>
/// instances so a test can create a second context against the same
/// database to verify data survived a round trip through SQL, not just
/// EF Core's own change tracker.
/// </summary>
public abstract class SqliteTestBase : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AuthenticationDbContext> _options;

    protected SqliteTestBase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<AuthenticationDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    /// <summary>Creates a fresh <see cref="AuthenticationDbContext"/> against the shared in-memory database.</summary>
    protected AuthenticationDbContext CreateContext() => new(_options);

    /// <inheritdoc/>
    public void Dispose()
    {
        _connection.Dispose();
    }
}
