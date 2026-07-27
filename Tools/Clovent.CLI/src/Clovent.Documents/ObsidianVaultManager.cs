namespace Clovent.Documents;

public sealed class ObsidianVaultManager : IObsidianVaultManager
{
    private static readonly string[] VaultCategories = new[]
    {
        "00 Vision",
        "01 Product Strategy",
        "02 Business Analysis",
        "03 SDLC",
        "04 UI UX Standards",
        "05 Software Architecture",
        "06 Coding Standards",
        "07 Domain Driven Design",
        "08 Database Design",
        "09 Security",
        "10 AI Architecture",
        "11 Platform Services",
        "12 Restaurant POS",
        "13 ADR"
    };

    public bool IsVaultInitialized(string vaultPath)
    {
        var obsidianDir = Path.Combine(vaultPath, ".obsidian");
        return Directory.Exists(obsidianDir);
    }

    public void EnsureVaultStructure(string vaultPath)
    {
        var obsidianDir = Path.Combine(vaultPath, ".obsidian");
        if (!Directory.Exists(obsidianDir))
        {
            Directory.CreateDirectory(obsidianDir);
        }

        foreach (var category in VaultCategories)
        {
            var categoryPath = Path.Combine(vaultPath, category);
            if (!Directory.Exists(categoryPath))
            {
                Directory.CreateDirectory(categoryPath);
            }
        }
    }

    public void WriteNote(string vaultPath, string category, string fileName, string content)
    {
        EnsureVaultStructure(vaultPath);
        var targetDir = Path.Combine(vaultPath, category);
        var fullPath = Path.Combine(targetDir, fileName.EndsWith(".md") ? fileName : $"{fileName}.md");
        File.WriteAllText(fullPath, content);
    }
}
