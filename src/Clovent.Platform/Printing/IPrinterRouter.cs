namespace Clovent.Platform.Printing;

/// <summary>
/// Explicit outcome of printer routing resolution.
/// </summary>
public enum PrinterResolutionOutcome
{
    /// <summary>Profile successfully resolved within authorized scope and requested role.</summary>
    Success = 0,

    /// <summary>No profile is assigned or configured for the specified role and scope.</summary>
    NoAssignmentFound = 1,

    /// <summary>A profile was resolved for the scope, but is currently disabled.</summary>
    ProfileDisabled = 2,

    /// <summary>Requested profile is assigned to a different organization (negative isolation boundary enforced).</summary>
    CrossOrganizationDenied = 3
}

/// <summary>
/// Detailed result of printer routing resolution with explicit outcome diagnostics.
/// </summary>
public sealed record PrinterResolutionResult(
    PrinterResolutionOutcome Outcome,
    PrinterProfile? Profile,
    string? Message)
{
    /// <summary>Gets a value indicating whether a valid, enabled profile was successfully resolved.</summary>
    public bool IsResolved => Outcome == PrinterResolutionOutcome.Success && Profile != null;

    /// <summary>Creates a successful resolution result.</summary>
    public static PrinterResolutionResult Success(PrinterProfile profile) =>
        new(PrinterResolutionOutcome.Success, profile, null);

    /// <summary>Creates a profile disabled resolution result.</summary>
    public static PrinterResolutionResult Disabled(PrinterProfile profile) =>
        new(PrinterResolutionOutcome.ProfileDisabled, profile, $"Printer profile '{profile.ProfileName}' is disabled.");

    /// <summary>Creates a no-assignment resolution result.</summary>
    public static PrinterResolutionResult NoAssignment(string message) =>
        new(PrinterResolutionOutcome.NoAssignmentFound, null, message);

    /// <summary>Creates a cross-organization denied resolution result.</summary>
    public static PrinterResolutionResult CrossOrgDenied(string message) =>
        new(PrinterResolutionOutcome.CrossOrganizationDenied, null, message);
}

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

    /// <summary>
    /// Resolves the printer profile with detailed, explicit outcome diagnostics.
    /// </summary>
    PrinterResolutionResult ResolvePrinterDetailed(
        Guid? organizationId,
        Guid? branchId,
        Guid? terminalId,
        PrinterRole role = PrinterRole.Receipt);
}
