using Microsoft.Extensions.Configuration;

namespace Clovent.Platform.Configuration;

/// <summary>
/// Centralizes configuration source precedence for every CBOS host
/// (desktop, web, or otherwise): appsettings.json, then
/// appsettings.{environment}.json, then environment variables, then
/// command-line arguments - each source overriding the ones before it.
/// </summary>
public static class PlatformConfiguration
{
    /// <summary>
    /// Builds a standalone <see cref="IConfiguration"/> with the documented
    /// source precedence. Prefer this when configuration is needed outside
    /// of a full <see cref="Bootstrap.ApplicationBootstrapper"/> sequence
    /// (e.g. a design-time tool); <see cref="Bootstrap.ApplicationBootstrapper.Create"/>
    /// calls <see cref="Configure"/> directly on its own builder instead of
    /// this method, to configure a host's <see cref="IConfigurationBuilder"/> in place.
    /// </summary>
    /// <param name="basePath">Directory to resolve appsettings.json files from.</param>
    /// <param name="environmentName">Environment name for the appsettings.{environmentName}.json overlay; omit to skip that source.</param>
    /// <param name="commandLineArgs">Command-line arguments; omit or pass an empty array to skip that source.</param>
    /// <returns>The resolved, merged configuration.</returns>
    public static IConfiguration Build(string basePath, string? environmentName = null, string[]? commandLineArgs = null)
    {
        var builder = new ConfigurationBuilder();
        Configure(builder, basePath, environmentName, commandLineArgs);
        return builder.Build();
    }

    /// <summary>
    /// Applies the documented source precedence to an existing
    /// <paramref name="builder"/> in place: appsettings.json, then
    /// appsettings.{<paramref name="environmentName"/>}.json (if supplied),
    /// then environment variables, then <paramref name="commandLineArgs"/>
    /// (if supplied) - each source added later overrides the same key from
    /// a source added earlier, per <see cref="IConfigurationBuilder"/>'s
    /// normal behavior.
    /// </summary>
    /// <param name="builder">The configuration builder to add sources to.</param>
    /// <param name="basePath">Directory to resolve appsettings.json files from.</param>
    /// <param name="environmentName">Environment name for the appsettings.{environmentName}.json overlay; omit to skip that source.</param>
    /// <param name="commandLineArgs">Command-line arguments; omit or pass an empty array to skip that source.</param>
    public static void Configure(
        IConfigurationBuilder builder,
        string basePath,
        string? environmentName = null,
        string[]? commandLineArgs = null)
    {
        builder
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);

        if (!string.IsNullOrWhiteSpace(environmentName))
        {
            builder.AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: false);
        }

        builder.AddEnvironmentVariables();

        if (commandLineArgs is { Length: > 0 })
        {
            builder.AddCommandLine(commandLineArgs);
        }
    }
}
