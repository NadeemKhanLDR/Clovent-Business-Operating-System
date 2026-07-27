namespace Clovent.Documents;

using Clovent.Core.Models;

public interface IDocumentGenerator
{
    void GenerateModuleDocumentation(string vaultPath, ModuleGenerationOptions options, string docContent);
    void GenerateEntityDocumentation(string vaultPath, EntityGenerationOptions options, string docContent);
}
