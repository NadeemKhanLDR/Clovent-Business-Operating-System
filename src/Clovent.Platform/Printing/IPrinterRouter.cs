namespace Clovent.Platform.Printing;

/// <summary>
/// Resolves the target logical <see cref="PrinterProfile"/> for a print request
/// based on tenant, organization, branch, terminal, and document role.
/// Enforces multi-organization boundary isolation.
/// </summary>
public interface IPrinterRouter
{
    /// <summary>
    /// Resolves the appropriate printer profile for the specified execution scope and role.
    /// </summary>
    /// <param name="organizationId">Optional organization boundary.</param>
    /// <param name="branchId">Optional physical branch outlet boundary.</param>
    /// <param name="terminalId">Optional physical POS terminal register boundary.</param>
    /// <param name="role">The document role required (Receipt, Kitchen, Bar, Invoice, Label).</param>
    /// <returns>The resolved <see cref="PrinterProfile"/>, or null if no valid profile is configured.</returns>
    PrinterProfile? ResolvePrinter(
        Guid? organizationId,
        Guid? branchId,
        Guid? terminalId,
        PrinterRole role = PrinterRole.Receipt);
}
