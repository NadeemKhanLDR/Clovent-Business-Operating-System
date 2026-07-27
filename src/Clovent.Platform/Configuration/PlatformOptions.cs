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
    public const string SectionName = "Platform";

    [Required]
    public required string EnvironmentName { get; init; }

    [Required]
    public required string DefaultCulture { get; init; }

    [Required]
    public required string DefaultTimeZone { get; init; }

    [Required]
    public required string DefaultCurrency { get; init; }
}
