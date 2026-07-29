using Clovent.Identity.Branches;
using Clovent.Identity.Organizations;
using Clovent.MasterData.Currencies;
using Clovent.MasterData.Departments;
using Clovent.MasterData.Departments.ValueObjects;
using Clovent.MasterData.FiscalYears;
using Clovent.MasterData.FiscalYears.ValueObjects;
using Clovent.MasterData.Languages;
using Clovent.MasterData.Settings;
using Clovent.MasterData.Shared.ValueObjects;
using Clovent.MasterData.Terminals;
using Clovent.MasterData.Terminals.ValueObjects;
using Clovent.MasterData.TimeZones;
using Clovent.MasterData.Warehouses;
using Clovent.MasterData.Warehouses.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Clovent.MasterData.Infrastructure.Persistence;

/// <summary>
/// EF Core <see cref="ValueConverter{TModel,TProvider}"/>s shared across
/// this project's entity type configurations - see
/// <c>Clovent.Identity.Infrastructure.Persistence.ValueConverters</c> for
/// the identical pattern and reasoning (every conversion goes through the
/// value object's own public factory, no Domain-layer changes needed).
/// </summary>
internal static class ValueConverters
{
    /// <summary><see cref="BranchId"/> (from <c>Clovent.Identity</c>) &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<BranchId, Guid> BranchIdConverter =
        new(id => id.Value, value => new BranchId(value));

    /// <summary><see cref="OrganizationId"/> (from <c>Clovent.Identity</c>) &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<OrganizationId, Guid> OrganizationIdConverter =
        new(id => id.Value, value => new OrganizationId(value));

    /// <summary><see cref="DepartmentId"/> &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<DepartmentId, Guid> DepartmentIdConverter =
        new(id => id.Value, value => new DepartmentId(value));

    /// <summary><see cref="WarehouseId"/> &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<WarehouseId, Guid> WarehouseIdConverter =
        new(id => id.Value, value => new WarehouseId(value));

    /// <summary><see cref="TerminalId"/> &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<TerminalId, Guid> TerminalIdConverter =
        new(id => id.Value, value => new TerminalId(value));

    /// <summary><see cref="FiscalYearId"/> &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<FiscalYearId, Guid> FiscalYearIdConverter =
        new(id => id.Value, value => new FiscalYearId(value));

    /// <summary>Nullable <see cref="FiscalYearId"/> &lt;-&gt; nullable <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<FiscalYearId?, Guid?> NullableFiscalYearIdConverter =
        new(id => id == null ? null : id.Value.Value, value => value == null ? null : new FiscalYearId(value.Value));

    /// <summary><see cref="CurrencyId"/> &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<CurrencyId, Guid> CurrencyIdConverter =
        new(id => id.Value, value => new CurrencyId(value));

    /// <summary><see cref="LanguageId"/> &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<LanguageId, Guid> LanguageIdConverter =
        new(id => id.Value, value => new LanguageId(value));

    /// <summary><see cref="TimeZoneEntryId"/> &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<TimeZoneEntryId, Guid> TimeZoneEntryIdConverter =
        new(id => id.Value, value => new TimeZoneEntryId(value));

    /// <summary><see cref="BusinessSettingsId"/> &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<BusinessSettingsId, Guid> BusinessSettingsIdConverter =
        new(id => id.Value, value => new BusinessSettingsId(value));

    /// <summary><see cref="DepartmentName"/> &lt;-&gt; name text.</summary>
    public static readonly ValueConverter<DepartmentName, string> DepartmentNameConverter =
        new(v => v.Value, v => DepartmentName.Create(v));

    /// <summary><see cref="WarehouseName"/> &lt;-&gt; name text.</summary>
    public static readonly ValueConverter<WarehouseName, string> WarehouseNameConverter =
        new(v => v.Value, v => WarehouseName.Create(v));

    /// <summary><see cref="TerminalName"/> &lt;-&gt; name text.</summary>
    public static readonly ValueConverter<TerminalName, string> TerminalNameConverter =
        new(v => v.Value, v => TerminalName.Create(v));

    /// <summary><see cref="FiscalYearName"/> &lt;-&gt; name text.</summary>
    public static readonly ValueConverter<FiscalYearName, string> FiscalYearNameConverter =
        new(v => v.Value, v => FiscalYearName.Create(v));

    /// <summary><see cref="EntityCode"/> &lt;-&gt; code text (Warehouse/Terminal).</summary>
    public static readonly ValueConverter<EntityCode, string> EntityCodeConverter =
        new(v => v.Value, v => EntityCode.Create(v));

    /// <summary><see cref="CurrencyCode"/> &lt;-&gt; ISO 4217 code text.</summary>
    public static readonly ValueConverter<CurrencyCode, string> CurrencyCodeConverter =
        new(v => v.Value, v => CurrencyCode.Create(v));

    /// <summary><see cref="LanguageCode"/> &lt;-&gt; ISO 639-1 code text.</summary>
    public static readonly ValueConverter<LanguageCode, string> LanguageCodeConverter =
        new(v => v.Value, v => LanguageCode.Create(v));

    /// <summary><see cref="IanaId"/> &lt;-&gt; IANA identifier text.</summary>
    public static readonly ValueConverter<IanaId, string> IanaIdConverter =
        new(v => v.Value, v => IanaId.Create(v));
}
