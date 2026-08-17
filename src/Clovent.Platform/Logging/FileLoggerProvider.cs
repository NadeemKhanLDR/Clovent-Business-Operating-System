using System.Text;
using Microsoft.Extensions.Logging;

namespace Clovent.Platform.Logging;

/// <summary>
/// A minimal, dependency-free <see cref="ILoggerProvider"/> that appends to
/// a daily-rolling log file - one file per local day
/// (<c>clovent-{yyyy-MM-dd}.log</c>), with bounded retention. Built on the
/// solution's existing Microsoft.Extensions.Logging pipeline (no Serilog/NLog
/// dependency): standard <c>Logging:LogLevel</c> configuration filtering
/// still applies upstream of this provider, so verbose categories
/// (<c>Microsoft.*</c>, per-POS-refresh noise, etc.) never reach the file.
/// </summary>
/// <remarks>
/// Writes are synchronous and serialized under a lock - acceptable for the
/// desktop host's low log volume, and guarantees ordering without a
/// background queue that could lose the final entries of a crashing process
/// (the exact entries a post-mortem investigation needs; see the T-01
/// incident). Entry format:
/// <c>yyyy-MM-dd HH:mm:ss.fff [LEVEL] category: message</c>, with logged
/// exceptions appended on subsequent indented lines.
/// </remarks>
public sealed class FileLoggerProvider : ILoggerProvider
{
    private const string FileNamePrefix = "clovent-";
    private const string FileNameSuffix = ".log";

    private readonly string _directory;
    private readonly int _retainedFileCountLimit;
    private readonly TimeProvider _timeProvider;
    private readonly object _sync = new();
    private readonly List<FileLogger> _loggers = [];
    private StreamWriter? _writer;
    private DateOnly _currentDate;

    /// <summary>Creates a provider writing into the directory resolved from <paramref name="options"/>.</summary>
    public FileLoggerProvider(FileLoggerOptions options)
        : this(options, TimeProvider.System)
    {
    }

    /// <summary>Creates a provider with an injectable clock (tests/rolling simulation).</summary>
    public FileLoggerProvider(FileLoggerOptions options, TimeProvider timeProvider)
    {
        _directory = options.Directory
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Clovent",
                "Logs");
        _retainedFileCountLimit = options.RetainedFileCountLimit;
        _timeProvider = timeProvider;
        Directory.CreateDirectory(_directory);
    }

    /// <summary>The directory log files are written to (exposed for diagnostics/tests).</summary>
    public string LogDirectory => _directory;

    /// <inheritdoc/>
    public ILogger CreateLogger(string categoryName)
    {
        lock (_sync)
        {
            var logger = new FileLogger(categoryName, this);
            _loggers.Add(logger);
            return logger;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        lock (_sync)
        {
            _writer?.Dispose();
            _writer = null;
            _loggers.Clear();
        }
    }

    internal void Write(string categoryName, LogLevel logLevel, string message, Exception? exception)
    {
        var now = _timeProvider.GetLocalNow();
        var line = FormatEntry(now, categoryName, logLevel, message, exception);

        lock (_sync)
        {
            try
            {
                EnsureWriter(DateOnly.FromDateTime(now.DateTime));
                _writer!.WriteLine(line);
            }
            catch (IOException)
            {
                // A locked/half-written log must never crash the application
                // it is trying to diagnose; drop the entry and continue.
            }
            catch (UnauthorizedAccessException)
            {
                // Ditto for directory-permission problems.
            }
        }
    }

    private static string FormatEntry(
        DateTimeOffset now, string categoryName, LogLevel logLevel, string message, Exception? exception)
    {
        var builder = new StringBuilder(128)
            .Append(now.ToString("yyyy-MM-dd HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture))
            .Append(" [").Append(logLevel switch
            {
                LogLevel.Trace => "TRC",
                LogLevel.Debug => "DBG",
                LogLevel.Information => "INF",
                LogLevel.Warning => "WRN",
                LogLevel.Error => "ERR",
                LogLevel.Critical => "CRT",
                _ => "???",
            })
            .Append("] ")
            .Append(categoryName)
            .Append(": ")
            .Append(message);

        for (var current = exception; current is not null; current = current.InnerException)
        {
            builder.AppendLine()
                .Append("    ")
                .Append(current.GetType().FullName)
                .Append(": ")
                .Append(current.Message);
        }

        return builder.ToString();
    }

    private void EnsureWriter(DateOnly date)
    {
        if (_writer is not null && date == _currentDate)
        {
            return;
        }

        _writer?.Dispose();
        _currentDate = date;

        PruneOldFiles();

        var stream = new FileStream(
            Path.Combine(_directory, $"{FileNamePrefix}{date:yyyy-MM-dd}{FileNameSuffix}"),
            FileMode.Append,
            FileAccess.Write,
            FileShare.Read);
        _writer = new StreamWriter(stream, Encoding.UTF8);
    }

    private void PruneOldFiles()
    {
        var logFiles = Directory.GetFiles(_directory, $"{FileNamePrefix}*{FileNameSuffix}");
        if (logFiles.Length < _retainedFileCountLimit)
        {
            return;
        }

        // Runs before the new day's file is created, so keep one slot free.
        foreach (var stale in logFiles
            .OrderByDescending(GetFileDate)
            .ThenBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Skip(_retainedFileCountLimit - 1))
        {
            try
            {
                File.Delete(stale);
            }
            catch (IOException)
            {
                // Locked file (another process inspecting it) - try again on
                // the next roll rather than failing startup.
            }
        }
    }

    private static DateOnly? GetFileDate(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        return DateOnly.TryParseExact(
            name.StartsWith(FileNamePrefix, StringComparison.Ordinal) ? name[FileNamePrefix.Length..] : name,
            "yyyy-MM-dd",
            out var date)
            ? date
            : null;
    }
}
