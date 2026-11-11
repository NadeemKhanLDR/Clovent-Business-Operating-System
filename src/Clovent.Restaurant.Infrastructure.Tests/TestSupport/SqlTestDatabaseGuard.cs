using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Clovent.Restaurant.Infrastructure.Tests.TestSupport;

internal static class SqlTestDatabaseGuard
{
    private static readonly object EvidenceGate = new();

    internal static string ConnectionString(string database)
    {
        var value = $"Server=(localdb)\\MSSQLLocalDB;Database={database};Integrated Security=true;TrustServerCertificate=true;Connection Timeout=15";
        Validate(value);
        return value;
    }

    internal static SqlConnectionStringBuilder Validate(string value)
    {
        var target = new SqlConnectionStringBuilder(value);
        Assert.Equal("(localdb)\\MSSQLLocalDB", target.DataSource, ignoreCase: true);
        Assert.True(target.IntegratedSecurity);
        Assert.True(string.IsNullOrEmpty(target.Password) && string.IsNullOrEmpty(target.UserID));
        Assert.Matches("^CBOS_(InvConsistency|OutboxClaimConcurrency|RefundConcurrency|Recovery|CrossProcess)_[a-f0-9]{32}$", target.InitialCatalog);
        return target;
    }

    internal static async Task CreatedAsync(DbContext db)
    {
        Assert.Equal("Microsoft.EntityFrameworkCore.SqlServer", db.Database.ProviderName);
        var target = Validate(db.Database.GetConnectionString()!);
        await using var connection = new SqlConnection(target.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT DB_NAME()";
        Assert.Equal(target.InitialCatalog, (string?)await command.ExecuteScalarAsync());
        Record(new { stage = "created", database = target.InitialCatalog, provider = db.Database.ProviderName,
            dataSource = target.DataSource, serverVersion = connection.ServerVersion, credentials = "Windows integrated; omitted" });
    }

    internal static async Task DeleteAndVerifyAsync(DbContext db)
    {
        var target = Validate(db.Database.GetConnectionString()!);
        await db.Database.EnsureDeletedAsync();
        var database = target.InitialCatalog;
        target.InitialCatalog = "master";
        await using var connection = new SqlConnection(target.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT DB_ID(@database)";
        command.Parameters.AddWithValue("@database", database);
        Assert.Equal(DBNull.Value, await command.ExecuteScalarAsync());
        Record(new { stage = "cleanup-verified", database, provider = db.Database.ProviderName, dataSource = target.DataSource });
    }

    private static void Record(object item)
    {
        var path = Environment.GetEnvironmentVariable("CBOS_SQL_TEST_EVIDENCE");
        if (string.IsNullOrWhiteSpace(path)) return;
        lock (EvidenceGate) File.AppendAllText(path, JsonSerializer.Serialize(item) + Environment.NewLine);
    }
}
