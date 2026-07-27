namespace Clovent.Modules.Identity.Persistence.Seeder;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IdentityDbContext context)
    {
        await context.Database.EnsureCreatedAsync();
    }
}
