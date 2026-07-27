using System.ComponentModel.DataAnnotations;

namespace Clovent.Platform.Configuration;

/// <summary>
/// Platform-wide defaults every host must supply via configuration
/// (appsettings.json, environment variables, or command line) - there
/// are deliberately no hardcoded fallback values here. Startup fails
/// (see <see cref="OptionsRegistrationExtensions.AddValidatedOptions{TOptions}"/>)
/// if the "Platform" configuration section is missing or incomplete.
/// </summary>
public sealed class PlatformOptions
{
    /// <summary>The configuration section name this class binds to (<c>"Platform"</c>), used with <see cref="OptionsRegistrationExtensions.AddValidatedOptions{TOptions}"/>.</summary>
    public const string SectionName = "Platform";

    /// <summary>
    /// The name of the environment the host is running in (e.g. "Development",
    /// "Production"). Distinct from <see cref="Microsoft.Extensions.Hosting.IHostEnvironment.EnvironmentName"/>
    /// only in that this one is validated as a required, explicit
    /// configuration value rather than inferred by the host.
    /// </summary>
    [Required]
    public required string EnvironmentName { get; init; }

    /// <summary>The default culture (e.g. "en-US") new <see cref="Execution.IExecutionContext"/> instances should fall back to when a caller hasn't specified one.</summary>
    [Required]
    public required string DefaultCulture { get; init; }

    /// <summary>The default IANA/Windows time zone id (e.g. "UTC") new <see cref="Execution.IExecutionContext"/> instances should fall back to when a caller hasn't specified one.</summary>
    [Required]
    public required string DefaultTimeZone { get; init; }

    /// <summary>The default ISO 4217 currency code (e.g. "USD") new <see cref="Execution.IExecutionContext"/> instances should fall back to when a caller hasn't specified one.</summary>
    [Required]
    public required string DefaultCurrency { get; init; }
}
