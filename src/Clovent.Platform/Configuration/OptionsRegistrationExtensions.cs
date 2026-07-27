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
    /// <summary>
    /// Binds <typeparamref name="TOptions"/> to <paramref name="sectionName"/>
    /// and validates it via its DataAnnotations attributes (e.g. <c>[Required]</c>)
    /// at host startup, before the host finishes starting - not lazily, the
    /// first time some unrelated code happens to read the option. Use this
    /// for every strongly-typed Options class, in Platform Foundation and in
    /// future modules alike, rather than a bare <c>services.Configure&lt;TOptions&gt;(...)</c>.
    /// </summary>
    /// <typeparam name="TOptions">The Options class to bind and validate.</typeparam>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">The root configuration to read <paramref name="sectionName"/> from.</param>
    /// <param name="sectionName">The configuration section (e.g. <c>"Platform"</c>) to bind <typeparamref name="TOptions"/> from.</param>
    /// <returns><paramref name="services"/>, for chaining.</returns>
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
