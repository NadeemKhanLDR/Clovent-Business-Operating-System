using Clovent.Restaurant.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Clovent.Restaurant.Infrastructure.Tests.TestSupport;

/// <summary>Backs each test with a real relational engine (SQLite, in-memory) - see the identical <c>Clovent.Identity.Infrastructure.Tests.TestSupport.SqliteTestBase</c> for the full reasoning.</summary>
public abstract class SqliteTestBase : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<RestaurantDbContext> _options;

    private sealed class SqliteDateTimeOffsetCustomizer(ModelCustomizerDependencies dependencies) : ModelCustomizer(dependencies)
    {
        public override void Customize(ModelBuilder modelBuilder, DbContext context)
        {
            base.Customize(modelBuilder, context);
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entity.GetProperties())
                {
                    if (property.ClrType == typeof(DateTimeOffset))
                    {
                        property.SetValueConverter(new ValueConverter<DateTimeOffset, long>(
                            v => v.UtcTicks,
                            v => new DateTimeOffset(v, TimeSpan.Zero)));
                    }
                    else if (property.ClrType == typeof(DateTimeOffset?))
                    {
                        property.SetValueConverter(new ValueConverter<DateTimeOffset?, long?>(
                            v => v.HasValue ? v.Value.UtcTicks : null,
                            v => v.HasValue ? new DateTimeOffset(v.Value, TimeSpan.Zero) : null));
                    }
                }
            }
        }
    }

    protected SqliteTestBase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<RestaurantDbContext>()
            .UseSqlite(_connection)
            .ReplaceService<IModelCustomizer, SqliteDateTimeOffsetCustomizer>()
            .Options;

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    /// <summary>Creates a fresh <see cref="RestaurantDbContext"/> against the shared in-memory database.</summary>
    protected RestaurantDbContext CreateContext() => new(_options);

    /// <inheritdoc/>
    public virtual void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
