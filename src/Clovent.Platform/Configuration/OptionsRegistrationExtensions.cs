using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Platform.Configuration;

/// <summary>
/// Generic strongly-typed Options registration, shared by every project
/// (Platform Foundation and future modules alike). Binds a configuration
/// section, validates it via DataAnnotations, and fails fast during host
/// startup if the section is missing or invalid - instead of failing
/// later, wherever the option happens to first be read.
/// </summary>
public static class OptionsRegistrationExtensions
{
    public static IServiceCollection AddValidatedOptions<TOptions>(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName)
        where TOptions : class
    {
        services
            .AddOptions<TOptions>()
            .Bind(configuration.GetSection(sectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }
}
