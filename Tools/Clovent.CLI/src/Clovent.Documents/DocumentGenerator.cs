namespace Clovent.Documents;

using Clovent.Core.Models;

public sealed class DocumentGenerator : IDocumentGenerator
{
    private readonly IObsidianVaultManager _vaultManager;

    public DocumentGenerator(IObsidianVaultManager vaultManager)
    {
        _vaultManager = vaultManager;
    }

    public void GenerateModuleDocumentation(string vaultPath, ModuleGenerationOptions options, string docContent)
    {
        _vaultManager.WriteNote(vaultPath, "05 Software Architecture", $"{options.Name} Module.md", docContent);
    }

    public void GenerateEntityDocumentation(string vaultPath, EntityGenerationOptions options, string docContent)
    {
        _vaultManager.WriteNote(vaultPath, "07 Domain Driven Design", $"{options.ModuleName} - {options.EntityName}.md", docContent);
    }
}
