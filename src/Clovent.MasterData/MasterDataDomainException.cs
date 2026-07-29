using Clovent.Domain;
using Clovent.MasterData.Currencies;
using Clovent.MasterData.Departments;
using Clovent.MasterData.FiscalYears;
using Clovent.MasterData.Languages;
using Clovent.MasterData.Terminals;
using Clovent.MasterData.TimeZones;
using Clovent.MasterData.Warehouses;

namespace Clovent.MasterData;

/// <summary>
/// Raised when a Master Data aggregate operation would violate one of its
/// invariants - mirrors <c>Clovent.Identity.IdentityDomainException</c> and
/// <c>Clovent.Authentication.AuthenticationDomainException</c> exactly: one
/// sealed type, one static factory method per rule.
/// </summary>
public sealed class MasterDataDomainException : DomainException
{
    private MasterDataDomainException(string message) : base(message)
    {
    }

    /// <summary>A department Activate() was attempted while already active.</summary>
    public static MasterDataDomainException DepartmentAlreadyActive(DepartmentId departmentId) =>
        new($"Department '{departmentId}' is already active.");

    /// <summary>A department Deactivate() was attempted while not active.</summary>
    public static MasterDataDomainException DepartmentNotActive(DepartmentId departmentId) =>
        new($"Department '{departmentId}' is not active.");

    /// <summary>A warehouse Activate() was attempted while already active.</summary>
    public static MasterDataDomainException WarehouseAlreadyActive(WarehouseId warehouseId) =>
        new($"Warehouse '{warehouseId}' is already active.");

    /// <summary>A warehouse Deactivate() was attempted while not active.</summary>
    public static MasterDataDomainException WarehouseNotActive(WarehouseId warehouseId) =>
        new($"Warehouse '{warehouseId}' is not active.");

    /// <summary>A terminal Activate() was attempted while already active.</summary>
    public static MasterDataDomainException TerminalAlreadyActive(TerminalId terminalId) =>
        new($"Terminal '{terminalId}' is already active.");

    /// <summary>A terminal Deactivate() was attempted while not active.</summary>
    public static MasterDataDomainException TerminalNotActive(TerminalId terminalId) =>
        new($"Terminal '{terminalId}' is not active.");

    /// <summary>A currency Activate() was attempted while already active.</summary>
    public static MasterDataDomainException CurrencyAlreadyActive(CurrencyId currencyId) =>
        new($"Currency '{currencyId}' is already active.");

    /// <summary>A currency Deactivate() was attempted while not active.</summary>
    public static MasterDataDomainException CurrencyNotActive(CurrencyId currencyId) =>
        new($"Currency '{currencyId}' is not active.");

    /// <summary>A language Activate() was attempted while already active.</summary>
    public static MasterDataDomainException LanguageAlreadyActive(LanguageId languageId) =>
        new($"Language '{languageId}' is already active.");

    /// <summary>A language Deactivate() was attempted while not active.</summary>
    public static MasterDataDomainException LanguageNotActive(LanguageId languageId) =>
        new($"Language '{languageId}' is not active.");

    /// <summary>A time zone entry Activate() was attempted while already active.</summary>
    public static MasterDataDomainException TimeZoneEntryAlreadyActive(TimeZoneEntryId timeZoneEntryId) =>
        new($"Time zone entry '{timeZoneEntryId}' is already active.");

    /// <summary>A time zone entry Deactivate() was attempted while not active.</summary>
    public static MasterDataDomainException TimeZoneEntryNotActive(TimeZoneEntryId timeZoneEntryId) =>
        new($"Time zone entry '{timeZoneEntryId}' is not active.");

    /// <summary>A FiscalYear was created/reconstructed with an end date not after its start date.</summary>
    public static MasterDataDomainException FiscalYearEndBeforeStart(DateOnly startDate, DateOnly endDate) =>
        new($"Fiscal year end date '{endDate}' must be after start date '{startDate}'.");

    /// <summary>A FiscalYear Close() was attempted while already closed.</summary>
    public static MasterDataDomainException FiscalYearAlreadyClosed(FiscalYearId fiscalYearId) =>
        new($"Fiscal year '{fiscalYearId}' is already closed.");
}
