namespace Clovent.Generator.Tests;

using Clovent.Core.Models;
using Clovent.Documents;
using Clovent.Generator.Services.Entity;
using Clovent.Templates;
using Xunit;

public class EntityGeneratorTests
{
    [Fact]
    public void Generate_ShouldCreateDomainApplicationAndInfrastructureFiles()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "CloventEntityTests_" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(tempDir);
            var templateEngine = new TemplateEngine();
            var vaultManager = new ObsidianVaultManager();
            var docGen = new DocumentGenerator(vaultManager);
            var sut = new EntityGenerator(templateEngine, docGen);

            var options = new EntityGenerationOptions
            {
                ModuleName = "Catalog",
                EntityName = "Product",
                OutputPath = tempDir,
                Properties = new()
                {
                    new EntityPropertyDefinition { Name = "Sku", Type = "string", IsRequired = true, MaxLength = 50 },
                    new EntityPropertyDefinition { Name = "Price", Type = "decimal", IsRequired = true }
                }
            };

            var result = sut.Generate(options);

            Assert.True(result.Success);
            var moduleDir = Path.Combine(tempDir, "Catalog");
            Assert.True(File.Exists(Path.Combine(moduleDir, "Domain", "Entities", "Product.cs")));
            Assert.True(File.Exists(Path.Combine(moduleDir, "Domain", "ValueObjects", "ProductId.cs")));
            Assert.True(File.Exists(Path.Combine(moduleDir, "Application", "Commands", "CreateProductCommand.cs")));
            Assert.True(File.Exists(Path.Combine(moduleDir, "Infrastructure", "Persistence", "Configurations", "ProductConfiguration.cs")));
            Assert.True(File.Exists(Path.Combine(moduleDir, "UI", "Views", "ProductView.cs")));
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
