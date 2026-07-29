using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Clovent.MasterData.Infrastructure.Persistence;

/// <summary>Design-time-only factory the <c>dotnet ef</c> tooling uses to create a <see cref="MasterDataDbContext"/> - see the identical <c>IdentityDbContextFactory</c> for the full reasoning.</summary>
public sealed class MasterDataDbContextFactory : IDesignTimeDbContextFactory<MasterDataDbContext>
{
    /// <inheritdoc/>
    public MasterDataDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MasterDataDbContext>();
        optionsBuilder.UseSqlServer("Server=.;Database=Clovent_MasterData_DesignTime;Trusted_Connection=True;TrustServerCertificate=True;");

        return new MasterDataDbContext(optionsBuilder.Options);
    }
}
