namespace Clovent.Generator.Tests;

using Clovent.Core.Models;
using Clovent.Documents;
using Clovent.Generator.Services.Module;
using Clovent.Templates;
using Xunit;

public class ModuleGeneratorTests
{
    [Fact]
    public void Generate_ShouldCreateDirectoryStructureAndReadme()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "CloventTests_" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(tempDir);
            var templateEngine = new TemplateEngine();
            var vaultManager = new ObsidianVaultManager();
            var docGen = new DocumentGenerator(vaultManager);
            var sut = new ModuleGenerator(templateEngine, docGen);

            var options = new ModuleGenerationOptions
            {
                Name = "Inventory",
                OutputPath = tempDir,
                IncludeDocumentation = true
            };

            var result = sut.Generate(options);

            Assert.True(result.Success);
            var moduleDir = Path.Combine(tempDir, "Inventory");
            Assert.True(Directory.Exists(moduleDir));
            Assert.True(Directory.Exists(Path.Combine(moduleDir, "Domain")));
            Assert.True(Directory.Exists(Path.Combine(moduleDir, "Application")));
            Assert.True(Directory.Exists(Path.Combine(moduleDir, "Infrastructure")));
            Assert.True(File.Exists(Path.Combine(moduleDir, "README.md")));
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }
}
