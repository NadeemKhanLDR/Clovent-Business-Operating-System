using Microsoft.Extensions.Logging;

namespace Clovent.Platform.Logging;

/// <summary>
/// The per-category logger handed out by <see cref="FileLoggerProvider"/>.
/// Level filtering happens upstream (standard <c>Logging:LogLevel</c>
/// configuration), so every call that reaches
/// <see cref="Log{TState}"/> is written.
/// </summary>
public sealed class FileLogger(string categoryName, FileLoggerProvider provider) : ILogger
{
    /// <inheritdoc/>
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    /// <inheritdoc/>
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        provider.Write(categoryName, logLevel, formatter(state, exception), exception);
    }
}
