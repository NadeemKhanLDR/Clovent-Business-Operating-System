namespace Clovent.Platform.Printing;

/// <summary>
/// Associates a <see cref="PrinterProfile"/> with a specific organizational,
/// branch, terminal, and document role scope.
/// </summary>
public sealed class PrinterAssignment
{
    /// <summary>Unique and stable assignment identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Optional organization scope. If set, this assignment applies to this organization only.</summary>
    public Guid? OrganizationId { get; set; }

    /// <summary>Optional branch scope. If set, this assignment applies to this physical store branch only.</summary>
    public Guid? BranchId { get; set; }

    /// <summary>Optional terminal scope. If set, this assignment applies to this physical register only.</summary>
    public Guid? TerminalId { get; set; }

    /// <summary>The role/document type this printer handles within this scope.</summary>
    public PrinterRole Role { get; set; } = PrinterRole.Receipt;

    /// <summary>The assigned <see cref="PrinterProfile"/> identifier.</summary>
    public Guid PrinterProfileId { get; set; }

    /// <summary>Whether this profile acts as the primary receipt printer for this scope.</summary>
    public bool IsDefaultReceiptPrinter { get; set; }

    /// <summary>Timestamp when this assignment was created.</summary>
    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
