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

    /// <summary>Per-terminal receipt copy and layout settings keyed by terminal ID string.</summary>
    public Dictionary<string, TerminalReceiptSettings> TerminalReceiptSettings { get; set; } = new();

    /// <summary>Global fallback receipt copy and layout settings.</summary>
    public TerminalReceiptSettings DefaultReceiptSettings { get; set; } = new();

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

    /// <summary>Gets receipt copy and layout settings for a specific terminal, falling back to default.</summary>
    public TerminalReceiptSettings GetSettingsForTerminal(Guid? terminalId)
    {
        if (terminalId.HasValue && TerminalReceiptSettings.TryGetValue(terminalId.Value.ToString(), out var settings))
        {
            return settings;
        }

        return DefaultReceiptSettings ?? new TerminalReceiptSettings();
    }

    /// <summary>Sets or updates receipt copy and layout settings for a specific terminal.</summary>
    public void SetSettingsForTerminal(Guid terminalId, TerminalReceiptSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.TerminalId = terminalId;
        TerminalReceiptSettings[terminalId.ToString()] = settings;
    }
}
