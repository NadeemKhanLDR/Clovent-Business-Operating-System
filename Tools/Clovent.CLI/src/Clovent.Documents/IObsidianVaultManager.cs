namespace Clovent.Documents;

public interface IObsidianVaultManager
{
    bool IsVaultInitialized(string vaultPath);
    void EnsureVaultStructure(string vaultPath);
    void WriteNote(string vaultPath, string category, string fileName, string content);
}
