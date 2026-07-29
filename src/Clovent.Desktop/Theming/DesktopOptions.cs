using System.ComponentModel.DataAnnotations;

namespace Clovent.Desktop.Theming;

/// <summary>
/// Desktop host-wide defaults, bound from the <c>Desktop</c> configuration
/// section via <c>Clovent.Platform.Configuration.OptionsRegistrationExtensions.AddValidatedOptions{T}</c>
/// - no hardcoded fallback values, matching <c>PlatformOptions</c>'s
/// fail-fast philosophy.
/// </summary>
public sealed class DesktopOptions
{
    /// <summary>The configuration section name this class binds to.</summary>
    public const string SectionName = "Desktop";

    /// <summary>The DevExpress skin name applied at startup (e.g. <c>"WXI"</c>) - see <see cref="ThemeInitializationStartupTask"/>.</summary>
    [Required]
    public required string DefaultSkin { get; init; }

    /// <summary>The default UI culture name (e.g. <c>"en-US"</c>) applied at startup before any user preference is known.</summary>
    [Required]
    public required string DefaultLanguage { get; init; }

    /// <summary>
    /// Whether <see cref="Seed.DevelopmentUserSeedStartupTask"/> should create a
    /// demo admin user/credentials if none exist. An explicit, auditable
    /// configuration value rather than an implicit "Development environment"
    /// check, so a production <c>appsettings.Production.json</c> must
    /// deliberately opt in rather than accidentally inherit a development
    /// default.
    /// </summary>
    [Required]
    public required bool SeedDevelopmentUser { get; init; }

    /// <summary>
    /// Whether <see cref="Seed.DevelopmentMasterDataSeedStartupTask"/> should
    /// create a demo Organization/Company/Branch hierarchy plus reference
    /// data (currencies, languages, time zones) if none exist. Same
    /// explicit-opt-in reasoning as <see cref="SeedDevelopmentUser"/>.
    /// </summary>
    [Required]
    public required bool SeedDevelopmentMasterData { get; init; }

    /// <summary>
    /// Whether <see cref="Seed.DevelopmentCatalogSeedStartupTask"/> should
    /// create demo Catalog/Inventory data (categories, units, a product with
    /// a variant, barcode, prices, and warehouse stock) if none exist. Same
    /// explicit-opt-in reasoning as <see cref="SeedDevelopmentUser"/>.
    /// </summary>
    [Required]
    public required bool SeedDevelopmentCatalogData { get; init; }

    /// <summary>
    /// Whether <see cref="Seed.DevelopmentRestaurantSeedStartupTask"/> should
    /// create demo Restaurant POS data (a dining area, tables, and payment
    /// methods) if none exist. Same explicit-opt-in reasoning as
    /// <see cref="SeedDevelopmentUser"/>.
    /// </summary>
    [Required]
    public required bool SeedDevelopmentRestaurantData { get; init; }
}
