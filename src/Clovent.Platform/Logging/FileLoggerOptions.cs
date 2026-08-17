namespace Clovent.Platform.Logging;

/// <summary>
/// Options for <see cref="FileLoggerProvider"/>, bound from the
/// <c>Logging:File</c> configuration section. Unlike most Clovent options
/// (which are required and fail fast), these deliberately default: file
/// logging must work without per-installation configuration, and its
/// defaults (per-user directory, daily rolling, bounded retention) are safe
/// for production.
/// </summary>
public sealed class FileLoggerOptions
{
    /// <summary>The configuration section name these options bind from.</summary>
    public const string SectionName = "Logging:File";

    /// <summary>
    /// The log file name pattern is <c>clovent-{yyyy-MM-dd}.log</c>;
    /// this option only controls the directory. When null (the default), the
    /// provider writes to the per-user
    /// <c>%LOCALAPPDATA%\Clovent\Logs</c> directory - writable without
    /// administrator privileges and never shared between Windows users.
    /// </summary>
    public string? Directory { get; init; }

    /// <summary>
    /// Maximum number of daily log files kept before the oldest are deleted
    /// during a roll. Bounded so a long-lived POS installation cannot fill
    /// the user's profile disk.
    /// </summary>
    public int RetainedFileCountLimit { get; init; } = 31;
}
