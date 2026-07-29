using Clovent.MasterData.Application;
using Clovent.MasterData.Currencies;
using Clovent.MasterData.Departments;
using Clovent.MasterData.FiscalYears;
using Clovent.MasterData.Infrastructure.Persistence;
using Clovent.MasterData.Infrastructure.Repositories;
using Clovent.MasterData.Languages;
using Clovent.MasterData.Settings;
using Clovent.MasterData.Terminals;
using Clovent.MasterData.TimeZones;
using Clovent.MasterData.Warehouses;
using Clovent.Platform.Bootstrap;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Clovent.MasterData.Infrastructure.DependencyInjection;

/// <summary>
/// MasterData's own Persistence-layer registration, following the same
/// AddApplication()/AddInfrastructure()/AddPersistence() convention as every
/// other module - see <c>Clovent.Identity.Infrastructure.DependencyInjection.PersistenceServiceCollectionExtensions</c>
/// for the identical pattern.
/// </summary>
public static class PersistenceServiceCollectionExtensions
{
    /// <summary>The <c>ConnectionStrings</c> configuration key this module reads its SQL Server connection string from.</summary>
    public const string ConnectionStringName = "MasterData";

    /// <summary>
    /// Registers <see cref="MasterDataDbContext"/>, every repository
    /// implementation (Department/Warehouse/Terminal/FiscalYear/Currency/Language/TimeZone/BusinessSettings),
    /// the <see cref="IUnitOfWork"/> seam, and an <see cref="IPersistenceInitializer"/>
    /// that applies migrations.
    /// </summary>
    /// <exception cref="InvalidOperationException">No <c>ConnectionStrings:MasterData</c> value is configured.</exception>
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Missing required connection string 'ConnectionStrings:{ConnectionStringName}'.");

        services.AddDbContext<MasterDataDbContext>(options => options.UseSqlServer(connectionString));

        services.TryAddScoped<IDepartmentRepository, DepartmentRepository>();
        services.TryAddScoped<IWarehouseRepository, WarehouseRepository>();
        services.TryAddScoped<ITerminalRepository, TerminalRepository>();
        services.TryAddScoped<IFiscalYearRepository, FiscalYearRepository>();
        services.TryAddScoped<ICurrencyRepository, CurrencyRepository>();
        services.TryAddScoped<ILanguageRepository, LanguageRepository>();
        services.TryAddScoped<ITimeZoneRepository, TimeZoneRepository>();
        services.TryAddScoped<IBusinessSettingsRepository, BusinessSettingsRepository>();

        services.TryAddScoped<IUnitOfWork, UnitOfWork>();

        // Scoped, not Singleton - see Clovent.Identity.Infrastructure's
        // identical reasoning: MasterDataDbContext is Scoped, and
        // ApplicationBootstrapper.BuildAndInitializeAsync already resolves
        // every IPersistenceInitializer from a freshly-created scope.
        services.AddScoped<IPersistenceInitializer, MasterDataPersistenceInitializer>();

        return services;
    }
}
