using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Clovent.Modules.Identity.Persistence;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<IdentityDbContext>();

        builder.UseSqlServer(
            "Server=DESKTOP-BPP5KM1;Database=Clovent.Identity;Trusted_Connection=True;TrustServerCertificate=True");

        return new IdentityDbContext(builder.Options);
    }
}

