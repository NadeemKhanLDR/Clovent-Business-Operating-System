using Clovent.Platform.Logging;
using Clovent.Platform.Tests.TestSupport;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Clovent.Platform.Tests.Logging;

public sealed class FileLoggerProviderTests
{
    private sealed class FakeTimeProvider : TimeProvider
    {
        public DateTimeOffset Now { get; set; } =
            new DateTimeOffset(2026, 8, 17, 10, 0, 0, TimeZoneInfo.Local.GetUtcOffset(DateTimeOffset.Now));

        public override DateTimeOffset GetUtcNow() => Now.ToUniversalTime();
    }

    private static string[] ReadLines(string directory, string fileName)
        => File.ReadAllLines(Path.Combine(directory, fileName));

    [Fact]
    public void Write_FormatsTimestampLevelCategoryAndMessage()
    {
        using var temp = new TempDirectory();
        string fileName;
        using (var provider = new FileLoggerProvider(
            new FileLoggerOptions { Directory = temp.Path }, TimeProvider.System))
        {
            provider.CreateLogger("Clovent.Test.Category")
                .LogError(new InvalidOperationException("boom"), "Something failed: {Detail}", "x");
            fileName = "clovent-" + DateTime.Now.ToString("yyyy-MM-dd") + ".log";
        }

        var lines = ReadLines(temp.Path, fileName);
        var entry = Assert.Single(lines, l => l.Contains("Something failed"));
        Assert.Contains("[ERR]", entry);
        Assert.Contains("Clovent.Test.Category: Something failed: x", entry);
        // Timestamp: yyyy-MM-dd HH:mm:ss.fff
        Assert.Matches(@"\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3}", entry);
        Assert.Contains("System.InvalidOperationException: boom", lines[Array.IndexOf(lines, entry) + 1]);
    }

    [Fact]
    public void Write_AfterProviderRecreated_AppendsToSameDayFile()
    {
        using var temp = new TempDirectory();
        var options = new FileLoggerOptions { Directory = temp.Path };

        using (var first = new FileLoggerProvider(options, TimeProvider.System))
        {
            first.CreateLogger("A").LogInformation("first run");
        }

        using (var second = new FileLoggerProvider(options, TimeProvider.System))
        {
            second.CreateLogger("B").LogInformation("second run");
        }

        var fileName = "clovent-" + DateTime.Now.ToString("yyyy-MM-dd") + ".log";
        var lines = ReadLines(temp.Path, fileName);
        Assert.Contains(lines, l => l.EndsWith("A: first run"));
        Assert.Contains(lines, l => l.EndsWith("B: second run"));
    }

    [Fact]
    public void Write_NewDay_RollsToNewFileAndPrunesBeyondRetentionLimit()
    {
        using var temp = new TempDirectory();
        var time = new FakeTimeProvider();
        var options = new FileLoggerOptions { Directory = temp.Path, RetainedFileCountLimit = 2 };

        for (var day = 0; day < 4; day++)
        {
            time.Now = time.Now.AddDays(1);
            using var provider = new FileLoggerProvider(options, time);
            provider.CreateLogger("Roll").LogInformation("day {Day}", day);
        }

        var files = Directory.GetFiles(temp.Path, "clovent-*.log");
        Assert.Equal(2, files.Length);

        var allLines = files.SelectMany(File.ReadAllLines).ToArray();
        Assert.Contains(allLines, l => l.EndsWith("day 3")); // newest survives
        Assert.DoesNotContain(allLines, l => l.EndsWith("day 0")); // oldest pruned
    }

    [Fact]
    public void Write_LevelFilteredUpstream_DoesNotReachProvider()
    {
        using var temp = new TempDirectory();
        string fileName;
        var provider = new FileLoggerProvider(
            new FileLoggerOptions { Directory = temp.Path }, TimeProvider.System);
        using (var factory = LoggerFactory.Create(builder =>
        {
            builder.AddProvider(provider);
            builder.AddFilter("Clovent.Noisy", LogLevel.Warning);
        }))
        {
            var logger = factory.CreateLogger("Clovent.Noisy");
            logger.LogInformation("per-refresh noise");
            logger.LogWarning("kept warning");
            fileName = "clovent-" + DateTime.Now.ToString("yyyy-MM-dd") + ".log";
        }

        provider.Dispose();
        var lines = ReadLines(temp.Path, fileName);
        Assert.DoesNotContain(lines, l => l.Contains("per-refresh noise"));
        Assert.Contains(lines, l => l.Contains("kept warning"));
    }
}
