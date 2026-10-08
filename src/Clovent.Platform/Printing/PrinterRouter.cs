namespace Clovent.Platform.Printing;

/// <summary>
/// Default implementation of <see cref="IPrinterRouter"/>.
/// Evaluates hierarchical scopes (Terminal -> Branch -> Organization -> Global Default)
/// while strictly preventing cross-organization confidential document leaks.
/// </summary>
public sealed class PrinterRouter : IPrinterRouter
{
    private readonly IPrinterConfigurationStore? _configStore;
    private readonly PrinterConfiguration? _staticConfiguration;

    /// <summary>Constructs a router backed by a persistent configuration store.</summary>
    public PrinterRouter(IPrinterConfigurationStore configStore)
    {
        _configStore = configStore ?? throw new ArgumentNullException(nameof(configStore));
    }

    /// <summary>Constructs a router backed by a static in-memory configuration (useful for unit tests).</summary>
    public PrinterRouter(PrinterConfiguration configuration)
    {
        _staticConfiguration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    private PrinterConfiguration GetConfiguration()
    {
        if (_staticConfiguration != null)
        {
            return _staticConfiguration;
        }

        return _configStore?.GetConfiguration() ?? new PrinterConfiguration();
    }

    /// <inheritdoc/>
    public PrinterProfile? ResolvePrinter(
        Guid? organizationId,
        Guid? branchId,
        Guid? terminalId,
        PrinterRole role = PrinterRole.Receipt)
    {
        var config = GetConfiguration();
        var assignments = config.Assignments;

        // 1. Terminal-level match (most specific)
        if (terminalId.HasValue)
        {
            var terminalMatch = assignments.FirstOrDefault(a =>
                a.TerminalId == terminalId.Value &&
                a.Role == role &&
                (a.OrganizationId == null || a.OrganizationId == organizationId));

            if (terminalMatch != null)
            {
                var profile = config.FindProfile(terminalMatch.PrinterProfileId);
                if (profile is { IsEnabled: true })
                {
                    return profile;
                }
            }
        }

        // 2. Branch-level match
        if (branchId.HasValue)
        {
            var branchMatch = assignments.FirstOrDefault(a =>
                a.BranchId == branchId.Value &&
                a.TerminalId == null &&
                a.Role == role &&
                (a.OrganizationId == null || a.OrganizationId == organizationId));

            if (branchMatch != null)
            {
                var profile = config.FindProfile(branchMatch.PrinterProfileId);
                if (profile is { IsEnabled: true })
                {
                    return profile;
                }
            }
        }

        // 3. Organization-level match
        if (organizationId.HasValue)
        {
            var orgMatch = assignments.FirstOrDefault(a =>
                a.OrganizationId == organizationId.Value &&
                a.BranchId == null &&
                a.TerminalId == null &&
                a.Role == role);

            if (orgMatch != null)
            {
                var profile = config.FindProfile(orgMatch.PrinterProfileId);
                if (profile is { IsEnabled: true })
                {
                    return profile;
                }
            }
        }

        // 4. Default Receipt Printer fallback (strictly for Receipt role or general fallback)
        if (role == PrinterRole.Receipt)
        {
            var defaultProfile = config.GetDefaultReceiptProfile();
            if (defaultProfile != null)
            {
                // Verify the default profile is not explicitly bound to a DIFFERENT organization
                var conflictingAssignment = assignments.FirstOrDefault(a =>
                    a.PrinterProfileId == defaultProfile.Id &&
                    a.OrganizationId.HasValue &&
                    a.OrganizationId != organizationId);

                if (conflictingAssignment == null)
                {
                    return defaultProfile;
                }
            }
        }

        // 5. Any active profile matching the role that is not bound to a different organization
        var anyRoleProfile = config.Profiles.FirstOrDefault(p =>
            p.Role == role &&
            p.IsEnabled &&
            !assignments.Any(a => a.PrinterProfileId == p.Id && a.OrganizationId.HasValue && a.OrganizationId != organizationId));

        return anyRoleProfile;
    }
}
