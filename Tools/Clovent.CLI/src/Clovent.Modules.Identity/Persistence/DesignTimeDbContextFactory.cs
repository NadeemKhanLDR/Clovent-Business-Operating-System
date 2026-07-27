using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Clovent.Modules.Identity.Persistence;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("IdentityDb")
            ?? throw new InvalidOperationException(
                "Connection string 'IdentityDb' was not found. Provide it via appsettings.json " +
                "(ConnectionStrings:IdentityDb) or the ConnectionStrings__IdentityDb environment variable.");

        var builder = new DbContextOptionsBuilder<IdentityDbContext>();
        builder.UseSqlServer(connectionString);

        return new IdentityDbContext(builder.Options);
    }
}

