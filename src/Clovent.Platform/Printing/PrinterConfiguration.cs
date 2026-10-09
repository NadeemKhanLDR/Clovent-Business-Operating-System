namespace Clovent.Platform.Printing;

/// <summary>
/// Root persistent configuration containing all configured printer profiles
/// and scope assignments. Persisted to printers.json.
/// </summary>
public sealed class PrinterConfiguration
{
    /// <summary>Schema version for backwards compatibility.</summary>
    public int Version { get; set; } = 1;

    /// <summary>All registered printer profiles.</summary>
    public List<PrinterProfile> Profiles { get; set; } = [];

    /// <summary>All scope-to-profile routing assignments.</summary>
    public List<PrinterAssignment> Assignments { get; set; } = [];

    /// <summary>Global default receipt printer profile identifier if no scope match is found.</summary>
    public Guid? DefaultReceiptProfileId { get; set; }

    /// <summary>Finds a profile by its unique identifier.</summary>
    public PrinterProfile? FindProfile(Guid profileId) =>
        Profiles.FirstOrDefault(p => p.Id == profileId);

    /// <summary>Finds the default receipt printer profile.</summary>
    public PrinterProfile? GetDefaultReceiptProfile()
    {
        if (DefaultReceiptProfileId.HasValue)
        {
            var def = FindProfile(DefaultReceiptProfileId.Value);
            if (def is { IsEnabled: true })
            {
                return def;
            }
        }

        return Profiles.FirstOrDefault(p => p.Role == PrinterRole.Receipt && p.IsEnabled)
            ?? Profiles.FirstOrDefault(p => p.IsEnabled);
    }
}
