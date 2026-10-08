using System.Text.Json;
using System.Text.Json.Serialization;
using Clovent.Platform.Configuration;
using Clovent.Platform.Tests.TestSupport;
using Xunit;

namespace Clovent.Platform.Tests.Configuration;

public class AtomicFileWriterTests
{
    private sealed record TestPayload(string Name, int Value);

    private sealed class BrokenPayload
    {
        [JsonIgnore]
        public string Safe { get; set; } = "OK";

        public string ExplodingProperty => throw new InvalidOperationException("Serialization deliberately failed.");
    }

    [Fact]
    public void WriteJsonAtomic_CreatesValidJsonFile()
    {
        using var tempDir = new TempDirectory();
        var targetFile = Path.Combine(tempDir.Path, "config.json");
        var payload = new TestPayload("Alpha", 42);

        // Act
        AtomicFileWriter.WriteJsonAtomic(targetFile, payload);

        // Assert
        Assert.True(File.Exists(targetFile));
        var content = File.ReadAllText(targetFile);
        Assert.Contains("\"Name\": \"Alpha\"", content);
        Assert.Contains("\"Value\": 42", content);
    }

    [Fact]
    public void WriteJsonAtomic_OverwritesExistingFileSafely()
    {
        using var tempDir = new TempDirectory();
        var targetFile = Path.Combine(tempDir.Path, "config.json");
        AtomicFileWriter.WriteJsonAtomic(targetFile, new TestPayload("Old", 1));

        // Act: overwrite
        AtomicFileWriter.WriteJsonAtomic(targetFile, new TestPayload("New", 2));

        // Assert
        Assert.True(File.Exists(targetFile));
        var content = File.ReadAllText(targetFile);
        Assert.Contains("\"Name\": \"New\"", content);
        Assert.Contains("\"Value\": 2", content);
    }

    [Fact]
    public void WriteStringAtomic_CreatesMissingParentDirectories()
    {
        using var tempDir = new TempDirectory();
        var nestedPath = Path.Combine(tempDir.Path, "sub1", "sub2", "test.txt");

        // Act
        AtomicFileWriter.WriteStringAtomic(nestedPath, "Hello World");

        // Assert
        Assert.True(File.Exists(nestedPath));
        Assert.Equal("Hello World", File.ReadAllText(nestedPath));
    }

    [Fact]
    public void WriteJsonAtomic_WhenSerializationThrows_PreservesExistingFileUntouched()
    {
        using var tempDir = new TempDirectory();
        var targetFile = Path.Combine(tempDir.Path, "valid_config.json");
        var original = new TestPayload("OriginalValid", 100);
        AtomicFileWriter.WriteJsonAtomic(targetFile, original);

        var originalContent = File.ReadAllText(targetFile);

        // Act & Assert: Serialization error must throw and NOT touch existing file
        Assert.Throws<InvalidOperationException>(() =>
            AtomicFileWriter.WriteJsonAtomic(targetFile, new BrokenPayload()));

        var afterContent = File.ReadAllText(targetFile);
        Assert.Equal(originalContent, afterContent);
        Assert.Contains("\"OriginalValid\"", afterContent);
    }

    [Fact]
    public void WriteJsonAtomic_ConcurrentWriters_PreservesIntegrityAndProducesValidJson()
    {
        using var tempDir = new TempDirectory();
        var targetFile = Path.Combine(tempDir.Path, "concurrent.json");

        // Act: 10 parallel tasks writing concurrently
        Parallel.For(0, 20, i =>
        {
            var payload = new TestPayload($"Writer-{i}", i);
            AtomicFileWriter.WriteJsonAtomic(targetFile, payload);
        });

        // Assert: Target file exists, is non-empty, and parses as completely valid JSON
        Assert.True(File.Exists(targetFile));
        var content = File.ReadAllText(targetFile);
        var parsed = JsonSerializer.Deserialize<TestPayload>(content);
        Assert.NotNull(parsed);
        Assert.StartsWith("Writer-", parsed.Name);
    }
}
