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
    /// Starts a new bootstrap sequence and loads configuration for the host:
    /// appsettings.json, then appsettings.{environmentName}.json, then
    /// environment variables, then command-line arguments (see
    /// <see cref="PlatformConfiguration"/> for the precedence rules). This is
    /// always the first call in a bootstrap chain, followed by whichever
    /// <c>With*</c> methods the host needs, ending in <see cref="Build"/> or
    /// <see cref="BuildAndInitializeAsync"/>.
    /// </summary>
    /// <param name="args">Command-line arguments, if the host has any (e.g. from <c>Main(string[] args)</c>).</param>
    /// <param name="basePath">Directory to resolve appsettings.json files from. Defaults to the host's content root when omitted.</param>
    /// <param name="environmentName">
    /// Environment name (e.g. "Development", "Production") used to select the
    /// appsettings.{environmentName}.json overlay. Defaults to whatever the
    /// generic host resolves from its own environment-detection conventions
    /// when omitted.
    /// </param>
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

    /// <summary>
    /// The configuration resolved by <see cref="Create"/>. Exposed so a host
    /// can read additional settings, or a module's <c>AddApplication</c>/
    /// <c>AddInfrastructure</c>/<c>AddPersistence</c> can be called directly
    /// against it before <see cref="Build"/>.
    /// </summary>
    public IConfiguration Configuration => _builder.Configuration;

    /// <summary>
    /// The DI service collection being assembled. Exposed so a host can add
    /// services beyond what <see cref="WithPlatform"/>/<see cref="WithModule{TModule}"/>
    /// register, before <see cref="Build"/> finalizes the container.
    /// </summary>
    public IServiceCollection Services => _builder.Services;

    /// <summary>
    /// The resolved host environment (application name, environment name,
    /// content root) that configuration and logging were built against.
    /// </summary>
    public IHostEnvironment Environment => _builder.Environment;

    /// <summary>
    /// Registers logging: clears any providers the generic host added by
    /// default, applies the "Logging" configuration section, and adds a
    /// console provider. Providers beyond the console default (files,
    /// telemetry, etc.) can be added by future hosts via
    /// <paramref name="configureLogging"/> without this class needing to
    /// know about them.
    /// </summary>
    /// <param name="configureLogging">Optional callback for host-specific logging providers/filters, applied after the console default.</param>
    /// <returns>This bootstrapper, for chaining.</returns>
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
    /// + Persistence, via <see cref="PlatformServiceCollectionExtensions.AddPlatform"/>).
    /// Call once per host, before any <see cref="WithModule{TModule}"/> calls
    /// that depend on Platform services (e.g. <c>ModuleRegistry</c>).
    /// </summary>
    /// <returns>This bootstrapper, for chaining.</returns>
    public ApplicationBootstrapper WithPlatform()
    {
        _builder.Services.AddPlatform(_builder.Configuration);
        return this;
    }

    /// <summary>
    /// Registers a module. This is the only call a future module needs -
    /// no switch statement, no manually maintained list. Call once per
    /// module the host wants active.
    /// </summary>
    /// <typeparam name="TModule">
    /// The module to register. Must have a public parameterless constructor;
    /// an instance is created immediately and used both to register the
    /// module's services and to appear in <c>ModuleRegistry</c>.
    /// </typeparam>
    /// <returns>This bootstrapper, for chaining.</returns>
    public ApplicationBootstrapper WithModule<TModule>() where TModule : IModule, new()
    {
        _builder.Services.AddModule<TModule>(_builder.Configuration);
        return this;
    }

    /// <summary>
    /// Finalizes the DI container without running any startup pipeline
    /// (no persistence initialization, no startup tasks). Prefer
    /// <see cref="BuildAndInitializeAsync"/> for a host that has registered
    /// any <see cref="IPersistenceInitializer"/> or <see cref="IStartupTask"/>.
    /// </summary>
    /// <returns>The built, ready-to-run host.</returns>
    public IHost Build() => _builder.Build();

    /// <summary>
    /// Finalizes the DI container and then runs the persistence
    /// initialization pipeline followed by the future-startup-task
    /// pipeline - every <see cref="IPersistenceInitializer"/> (run first, in
    /// registration order) and then every <see cref="IStartupTask"/> (run
    /// second, in registration order) registered by any module, discovered
    /// automatically via the container, with no switch statement.
    /// </summary>
    /// <param name="cancellationToken">Propagated to every initializer/startup task.</param>
    /// <returns>The built, initialized, ready-to-run host.</returns>
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
