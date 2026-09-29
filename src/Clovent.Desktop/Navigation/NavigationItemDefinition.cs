namespace Clovent.Desktop.Navigation;

/// <summary>
/// Authoritative metadata definition for a navigation item in CBOS.
/// Maps a navigation key directly to its ribbon presentation, authorization permission,
/// and behavioral characteristics.
/// </summary>
public sealed record NavigationItemDefinition(
    string Key,
    string Caption,
    string RibbonPage,
    string RibbonGroup,
    string IconUri,
    string Permission,
    int Order = 0,
    bool IsPrimaryAction = false,
    bool IsShortcut = false,
    string? Description = null);
