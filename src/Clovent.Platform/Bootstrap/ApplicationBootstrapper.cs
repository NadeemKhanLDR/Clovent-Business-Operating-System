using Clovent.Platform.Configuration;
using Clovent.Platform.DependencyInjection;
using Clovent.Platform.Modules;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Clovent.Platform.Bootstrap;

/// <summary>
/// Single entry point responsible for loading configuration, building the
/// DI container, registering logging and modules, and initializing
/// persistence - the one place every future host (desktop, web, or
/// otherwise) starts from. Built on Microsoft.Extensions.Hosting's
/// generic host (not Microsoft.AspNetCore.*), so it is not coupled to
/// ASP.NET Core.
/// </summary>
public sealed class ApplicationBootstrapper
{
    private readonly HostApplicationBuilder _builder;

    private ApplicationBootstrapper(HostApplicationBuilder builder)
    {
        _builder = builder;
    }

    /// <summary>
    /// Loads configuration for the host: appsettings.json, then
    /// appsettings.{environmentName}.json, then environment variables,
    /// then command-line arguments.
    /// </summary>
    public static ApplicationBootstrapper Create(
        string[]? args = null,
        string? basePath = null,
        string? environmentName = null)
    {
        var settings = new HostApplicationBuilderSettings
        {
            Args = args,
            ContentRootPath = basePath,
            EnvironmentName = environmentName,
        };

        var builder = Host.CreateApplicationBuilder(settings);

        // Replace the generic host's implicit configuration sources with
        // the explicit, documented precedence, rather than relying on
        // undocumented defaults.
        builder.Configuration.Sources.Clear();
        PlatformConfiguration.Configure(
            builder.Configuration,
            builder.Environment.ContentRootPath,
            builder.Environment.EnvironmentName,
            args);

        return new ApplicationBootstrapper(builder);
    }

    public IConfiguration Configuration => _builder.Configuration;

    public IServiceCollection Services => _builder.Services;

    public IHostEnvironment Environment => _builder.Environment;

    /// <summary>
    /// Registers logging. Providers beyond the console default can be
    /// added by future hosts via <paramref name="configureLogging"/>
    /// without this class needing to know about them.
    /// </summary>
    public ApplicationBootstrapper WithLogging(Action<ILoggingBuilder>? configureLogging = null)
    {
        _builder.Logging.ClearProviders();
        _builder.Logging.AddConfiguration(_builder.Configuration.GetSection("Logging"));
        _builder.Logging.AddConsole();
        configureLogging?.Invoke(_builder.Logging);

        return this;
    }

    /// <summary>
    /// Registers Platform Foundation itself (Application + Infrastructure
    /// + Persistence).
    /// </summary>
    public ApplicationBootstrapper WithPlatform()
    {
        _builder.Services.AddPlatform(_builder.Configuration);
        return this;
    }

    /// <summary>
    /// Registers a module. This is the only call a future module needs -
    /// no switch statement, no manually maintained list.
    /// </summary>
    public ApplicationBootstrapper WithModule<TModule>() where TModule : IModule, new()
    {
        _builder.Services.AddModule<TModule>(_builder.Configuration);
        return this;
    }

    /// <summary>
    /// Builds the DI container without running any startup pipeline.
    /// </summary>
    public IHost Build() => _builder.Build();

    /// <summary>
    /// Builds the DI container and then runs the persistence
    /// initialization pipeline followed by the future-startup-task
    /// pipeline - every <see cref="IPersistenceInitializer"/> and
    /// <see cref="IStartupTask"/> registered by any module, discovered
    /// automatically via the container, with no switch statement.
    /// </summary>
    public async Task<IHost> BuildAndInitializeAsync(CancellationToken cancellationToken = default)
    {
        var host = _builder.Build();

        using (var scope = host.Services.CreateScope())
        {
            var provider = scope.ServiceProvider;

            foreach (var initializer in provider.GetServices<IPersistenceInitializer>())
            {
                await initializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            }

            foreach (var startupTask in provider.GetServices<IStartupTask>())
            {
                await startupTask.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        return host;
    }
}
