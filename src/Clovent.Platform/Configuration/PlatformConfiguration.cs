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
    public static IConfiguration Build(string basePath, string? environmentName = null, string[]? commandLineArgs = null)
    {
        var builder = new ConfigurationBuilder();
        Configure(builder, basePath, environmentName, commandLineArgs);
        return builder.Build();
    }

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
