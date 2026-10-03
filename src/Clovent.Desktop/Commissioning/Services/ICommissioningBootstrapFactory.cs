using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.Commissioning.Services;

/// <summary>
/// Factory interface for constructing the isolated DI container required during
/// first-run commissioning before the main application host is created.
/// </summary>
public interface ICommissioningBootstrapFactory
{
    /// <summary>
    /// Creates and configures a standalone ServiceProvider containing the DbContexts and services
    /// required to provision initial master data and administrator accounts.
    /// </summary>
    /// <param name="connectionString">The active database connection string.</param>
    /// <returns>A ServiceProvider implementing IDisposable.</returns>
    ServiceProvider CreateBootstrapServiceProvider(string connectionString);
}
