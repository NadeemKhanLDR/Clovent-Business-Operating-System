using Clovent.Platform.Modules;

namespace Clovent.Desktop.Modules;

/// <summary>
/// The ordered list of <see cref="IModule"/> types the desktop host loads at
/// startup via <see cref="DesktopModuleLoader.LoadModules"/>. Empty as of
/// Milestone 7 - no business module has an <see cref="IModule"/>
/// implementation yet (Authentication's Application/Infrastructure layers
/// are registered directly in <see cref="Program"/> for now). Milestone 9
/// ("Authentication Integration") is expected to add an Authentication
/// module here once one exists, and later milestones/business modules
/// follow the same pattern - append a type, nothing else changes.
/// </summary>
public static class DesktopModuleCatalog
{
    /// <summary>Every module type to load, in the order they should be registered.</summary>
    public static IReadOnlyList<Type> ModuleTypes { get; } = [];
}
