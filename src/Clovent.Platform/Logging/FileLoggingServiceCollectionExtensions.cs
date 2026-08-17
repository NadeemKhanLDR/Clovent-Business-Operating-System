using Clovent.Platform.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Clovent.Platform;

/// <summary>
/// Registers <see cref="FileLoggerProvider"/> on an
/// <see cref="ILoggingBuilder"/> - the persistent-log counterpart to the
/// console provider <see cref="Bootstrap.ApplicationBootstrapper"/> adds by
/// default, for hosts (like the Desktop application) that must leave a
/// diagnostic record surviving restarts.
/// </summary>
public static class FileLoggingServiceCollectionExtensions
{
    /// <summary>
    /// Adds the rolling file logger, bound from the optional
    /// <c>Logging:File</c> configuration section (see
    /// <see cref="FileLoggerOptions"/> - absent section means safe defaults).
    /// </summary>
    public static ILoggingBuilder AddFileLogger(this ILoggingBuilder builder, IConfiguration configuration)
    {
        var options = configuration.GetSection(FileLoggerOptions.SectionName).Get<FileLoggerOptions>()
            ?? new FileLoggerOptions();
        builder.AddProvider(new FileLoggerProvider(options));
        return builder;
    }
}
