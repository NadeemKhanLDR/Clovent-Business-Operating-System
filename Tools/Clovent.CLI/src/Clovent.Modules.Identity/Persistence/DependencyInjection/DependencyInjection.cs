using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Modules.Identity.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityPersistence(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<IdentityDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });

        return services;
    }
}
