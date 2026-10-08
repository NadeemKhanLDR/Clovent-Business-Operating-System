using Clovent.Platform.Configuration;
using Clovent.Platform.Tests.TestSupport;
using Xunit;

namespace Clovent.Platform.Tests.Configuration;

public class AtomicFileWriterTests
{
    private sealed record TestPayload(string Name, int Value);

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
}
