using System.Data.Common;
using System.Text.Json;

namespace Clovent.Platform.Configuration;

/// <summary>
/// Exception thrown before any shared filesystem, ACL, logging, licensing,
/// commissioning, or database activity when opt-in QA instance isolation is
/// incomplete or invalid.
/// </summary>
public sealed class InvalidQaConfigurationException : InvalidOperationException
{
    /// <summary>Initializes a new <see cref="InvalidQaConfigurationException"/>.</summary>
    public InvalidQaConfigurationException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Resolves instance-owned filesystem and connection settings for either an
/// ordinary production instance or an explicitly opted-in isolated QA instance.
/// </summary>
public sealed class CbosInstanceScope
{
    /// <summary>Environment variable enabling QA mode and defining the isolated instance root directory.</summary>
    public const string QaRootEnvironmentVariable = "CBOS_QA_INSTANCE_ROOT";

    /// <summary>Environment variable supplying the explicit SQL connection string in QA mode.</summary>
    public const string QaConnectionEnvironmentVariable = "CBOS_QA_CONNECTION_STRING";

    /// <summary>Command-line option enabling QA mode and defining the isolated instance root directory.</summary>
    public const string QaRootCliOption = "--qa-root";

    /// <summary>Command-line option supplying the explicit SQL connection string in QA mode.</summary>
    public const string QaConnectionCliOption = "--qa-connection";

    /// <summary>Short command-line flag asserting QA mode (requires an explicit root).</summary>
    public const string QaModeCliFlag = "--qa";

    /// <summary>Long command-line flag asserting QA mode (requires an explicit root).</summary>
    public const string QaModeLongCliFlag = "--qa-mode";

    /// <summary>Environment variable supplying an explicitly approved <c>Server|Database</c> target in QA mode.</summary>
    public const string QaApprovedTargetEnvironmentVariable = "CBOS_QA_APPROVED_TARGET";

    /// <summary>Command-line option supplying an explicitly approved <c>Server|Database</c> target in QA mode.</summary>
    public const string QaApprovedTargetCliOption = "--qa-approved-target";

    /// <summary>File name under <see cref="ConfigDirectory"/> recording an approved QA SQL target.</summary>
    public const string QaApprovedTargetFileName = "qa-target.approved.json";

    /// <summary>Default production database catalog name, forbidden in QA mode.</summary>
    public const string ForbiddenDefaultDatabaseName = "Clovent_BusinessOperatingSystem";

    private static readonly string[] AllowedQaDatabasePrefixes =
    [
        "CBOS_QA_",
        "CBOS_ManualQA_",
        "CBOS_Disposable_",
        "CBOS_Test_"
    ];

    private static readonly HashSet<string> ForbiddenExactCatalogNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ForbiddenDefaultDatabaseName,
        "master",
        "model",
        "msdb",
        "tempdb"
    };

    private static readonly object SyncLock = new();
    private static CbosInstanceScope _current = CreateProduction();

    private readonly string _commonAppDataCloventDir;
    private readonly string _localAppDataCloventDir;
    private readonly string? _resolvedQaInstanceRoot;
    private readonly string? _approvedServer;
    private readonly string? _approvedDatabase;

    private CbosInstanceScope(
        bool isQaMode,
        string? qaInstanceRoot,
        string? resolvedQaInstanceRoot,
        string? explicitConnectionString,
        string? dataSource,
        string? databaseName,
        string? approvedServer,
        string? approvedDatabase,
        string commonAppDataRoot,
        string localAppDataRoot)
    {
        IsQaMode = isQaMode;
        QaInstanceRoot = qaInstanceRoot;
        _resolvedQaInstanceRoot = resolvedQaInstanceRoot;
        ExplicitConnectionString = explicitConnectionString;
        DataSource = dataSource;
        DatabaseName = databaseName;
        _approvedServer = approvedServer;
        _approvedDatabase = approvedDatabase;
        _commonAppDataCloventDir = Path.Combine(commonAppDataRoot, "Clovent");
        _localAppDataCloventDir = Path.Combine(localAppDataRoot, "Clovent");
    }

    /// <summary>Gets the active process-wide instance scope.</summary>
    public static CbosInstanceScope Current
    {
        get
        {
            lock (SyncLock)
            {
                return _current;
            }
        }
    }

    /// <summary>Indicates whether the current process is running in explicit QA isolation mode.</summary>
    public bool IsQaMode { get; }

    /// <summary>Normalized full path of the QA instance root when <see cref="IsQaMode"/> is true; otherwise null.</summary>
    public string? QaInstanceRoot { get; }

    /// <summary>Explicitly resolved QA SQL connection string when provided at startup; otherwise null.</summary>
    public string? ExplicitConnectionString { get; }

    /// <summary>Validated SQL Server data source parsed in QA mode; otherwise null.</summary>
    public string? DataSource { get; }

    /// <summary>Validated SQL database catalog name parsed in QA mode; otherwise null.</summary>
    public string? DatabaseName { get; }

    /// <summary>Machine-level data root (<c>%ProgramData%\Clovent\BusinessOperatingSystem</c> in production; <see cref="QaInstanceRoot"/> in QA).</summary>
    public string MachineDataDirectory =>
        IsQaMode ? QaInstanceRoot! : Path.Combine(_commonAppDataCloventDir, "BusinessOperatingSystem");

    /// <summary>Instance configuration directory.</summary>
    public string ConfigDirectory => Path.Combine(MachineDataDirectory, "Config");

    /// <summary>Instance license and trial state directory.</summary>
    public string LicenseDirectory => Path.Combine(MachineDataDirectory, "License");

    /// <summary>Instance runtime state directory.</summary>
    public string StateDirectory => Path.Combine(MachineDataDirectory, "State");

    /// <summary>Machine-level logs directory.</summary>
    public string ProgramDataLogsDirectory => Path.Combine(MachineDataDirectory, "Logs");

    /// <summary>Offline continuity journal directory.</summary>
    public string ContinuityJournalDirectory => Path.Combine(MachineDataDirectory, "ContinuityJournal");

    /// <summary>Offline operational cache directory.</summary>
    public string OperationalCacheDirectory => Path.Combine(MachineDataDirectory, "OperationalCache");

    /// <summary>Active POS cart checkpoint directory.</summary>
    public string CartCheckpointsDirectory => Path.Combine(MachineDataDirectory, "CartCheckpoints");

    /// <summary>Durable print job tracking directory.</summary>
    public string PrintJobsDirectory => Path.Combine(MachineDataDirectory, "PrintJobs");

    /// <summary>Per-user Clovent root (<c>%LocalAppData%\Clovent</c> in production; <c>&lt;QaRoot&gt;\User</c> in QA).</summary>
    public string UserRootDirectory =>
        IsQaMode ? Path.Combine(QaInstanceRoot!, "User") : _localAppDataCloventDir;

    /// <summary>Per-user application storage directory (<c>%LocalAppData%\Clovent\Clovent.BusinessOperatingSystem</c> in production; <c>&lt;QaRoot&gt;\User\App</c> in QA).</summary>
    public string UserAppDirectory =>
        IsQaMode ? Path.Combine(UserRootDirectory, "App") : Path.Combine(_localAppDataCloventDir, "Clovent.BusinessOperatingSystem");

    /// <summary>Per-user rolling application log directory (<c>%LocalAppData%\Clovent\Logs</c> in production; <c>&lt;QaRoot&gt;\Logs</c> in QA).</summary>
    public string UserLogsDirectory =>
        IsQaMode ? Path.Combine(QaInstanceRoot!, "Logs") : Path.Combine(_localAppDataCloventDir, "Logs");

    /// <summary>Per-user continuity/designer diagnostic log directory.</summary>
    public string DiagnosticLogsDirectory =>
        IsQaMode ? Path.Combine(QaInstanceRoot!, "Logs") : Path.Combine(UserAppDirectory, "Logs");

    /// <summary>Default database backup fallback directory.</summary>
    public string BackupsDirectory =>
        IsQaMode ? Path.Combine(QaInstanceRoot!, "Backups") : Path.Combine(_commonAppDataCloventDir, "Backups");

    /// <summary>Protected single-payment recovery attempt journal directory.</summary>
    public string PaymentAttemptJournalDirectory =>
        IsQaMode ? Path.Combine(QaInstanceRoot!, "PaymentAttempts-v1") : Path.Combine(UserAppDirectory, "PaymentAttempts-v1");

    /// <summary>Machine-level database connection configuration file path.</summary>
    public string MachineDatabaseConfigFilePath =>
        IsQaMode ? Path.Combine(ConfigDirectory, "database.config.json") : Path.Combine(MachineDataDirectory, "database.config.json");

    /// <summary>User-level database connection configuration file path (maps inside <see cref="ConfigDirectory"/> in QA mode).</summary>
    public string UserDatabaseConfigFilePath =>
        IsQaMode ? Path.Combine(ConfigDirectory, "database.config.json") : Path.Combine(UserAppDirectory, "database.config.json");

    /// <summary>Primary commissioning marker file path.</summary>
    public string PrimaryCommissioningMarkerPath => Path.Combine(ConfigDirectory, "commissioning.json");

    /// <summary>Fallback commissioning marker file path (maps to <see cref="PrimaryCommissioningMarkerPath"/> in QA mode).</summary>
    public string FallbackCommissioningMarkerPath =>
        IsQaMode ? PrimaryCommissioningMarkerPath : Path.Combine(UserAppDirectory, "commissioning.json");

    /// <summary>Primary terminal association file path.</summary>
    public string MachineTerminalFilePath => Path.Combine(ConfigDirectory, "terminal.json");

    /// <summary>User-level terminal association file path (maps to <see cref="MachineTerminalFilePath"/> in QA mode).</summary>
    public string UserTerminalFilePath =>
        IsQaMode ? MachineTerminalFilePath : Path.Combine(_localAppDataCloventDir, "BusinessOperatingSystem", "Config", "terminal.json");

    /// <summary>Primary software license file path.</summary>
    public string PrimaryLicenseFilePath => Path.Combine(LicenseDirectory, "clovent.lic");

    /// <summary>Fallback software license file path (maps to <see cref="PrimaryLicenseFilePath"/> in QA mode).</summary>
    public string FallbackLicenseFilePath =>
        IsQaMode ? PrimaryLicenseFilePath : Path.Combine(UserAppDirectory, "clovent.lic");

    /// <summary>Clock-rollback guard file path.</summary>
    public string LicenseGuardFilePath =>
        IsQaMode ? Path.Combine(StateDirectory, "license_guard.dat") : Path.Combine(UserAppDirectory, "license_guard.dat");

    /// <summary>Primary evaluation trial state file path.</summary>
    public string PrimaryTrialStateFilePath => Path.Combine(LicenseDirectory, "trial.state");

    /// <summary>Fallback evaluation trial state file path (maps to <see cref="PrimaryTrialStateFilePath"/> in QA mode).</summary>
    public string FallbackTrialStateFilePath =>
        IsQaMode ? PrimaryTrialStateFilePath : Path.Combine(UserAppDirectory, "trial.state");

    /// <summary>Primary Windows printer configuration file path.</summary>
    public string PrimaryPrinterConfigFilePath => Path.Combine(ConfigDirectory, "printers.json");

    /// <summary>Fallback Windows printer configuration file path (maps to <see cref="PrimaryPrinterConfigFilePath"/> in QA mode).</summary>
    public string FallbackPrinterConfigFilePath =>
        IsQaMode ? PrimaryPrinterConfigFilePath : Path.Combine(UserAppDirectory, "Config", "printers.json");

    /// <summary>Primary quarantined print jobs directory.</summary>
    public string PrimaryPrintJobQuarantineDirectory => Path.Combine(PrintJobsDirectory, "quarantined");

    /// <summary>Fallback quarantined print jobs directory (maps to <see cref="PrimaryPrintJobQuarantineDirectory"/> in QA mode).</summary>
    public string FallbackPrintJobQuarantineDirectory =>
        IsQaMode ? PrimaryPrintJobQuarantineDirectory : Path.Combine(UserAppDirectory, "PrintJobs", "quarantined");

    /// <summary>Primary encrypted continuity journal file path.</summary>
    public string PrimaryContinuityJournalFilePath => Path.Combine(ContinuityJournalDirectory, "journal.dat");

    /// <summary>Fallback encrypted continuity journal file path (maps to <see cref="PrimaryContinuityJournalFilePath"/> in QA mode).</summary>
    public string FallbackContinuityJournalFilePath =>
        IsQaMode ? PrimaryContinuityJournalFilePath : Path.Combine(UserAppDirectory, "ContinuityJournal", "journal.dat");

    /// <summary>Fallback operational cache directory (maps to <see cref="OperationalCacheDirectory"/> in QA mode).</summary>
    public string FallbackOperationalCacheDirectory =>
        IsQaMode ? OperationalCacheDirectory : Path.Combine(UserAppDirectory, "OperationalCache");

    /// <summary>Fallback active cart checkpoints directory (maps to <see cref="CartCheckpointsDirectory"/> in QA mode).</summary>
    public string FallbackCartCheckpointsDirectory =>
        IsQaMode ? CartCheckpointsDirectory : Path.Combine(UserAppDirectory, "CartCheckpoints");

    /// <summary>Gets a per-user preference file or subdirectory path routed under <see cref="UserRootDirectory"/>.</summary>
    public string GetUserSettingPath(string relativeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativeName);
        return Path.Combine(UserRootDirectory, relativeName);
    }

    /// <summary>Resolves SmartPOS layout telemetry directory cleanly under <see cref="QaInstanceRoot"/> when in QA mode.</summary>
    public string GetSmartPosTelemetryDirectory(string productionDefaultDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productionDefaultDirectory);
        return IsQaMode ? Path.Combine(QaInstanceRoot!, "Telemetry", "runtime_layout") : productionDefaultDirectory;
    }

    /// <summary>Resolves and validates a log directory, enforcing confinement inside <see cref="QaInstanceRoot"/> in QA mode.</summary>
    public string ResolveLogDirectory(string? configuredDirectory)
    {
        if (!IsQaMode)
        {
            return string.IsNullOrWhiteSpace(configuredDirectory) ? UserLogsDirectory : configuredDirectory;
        }

        var candidate = string.IsNullOrWhiteSpace(configuredDirectory) ? UserLogsDirectory : configuredDirectory;
        return EnsurePathInsideQaRoot(candidate, "log directory");
    }

    /// <summary>
    /// Validates that <paramref name="candidatePath"/> (and its resolved reparse-point/junction target)
    /// stays strictly confined inside <see cref="QaInstanceRoot"/> when running in QA mode.
    /// </summary>
    public string EnsurePathInsideQaRoot(string candidatePath, string purpose)
    {
        if (!IsQaMode)
        {
            return Path.GetFullPath(candidatePath);
        }

        if (string.IsNullOrWhiteSpace(candidatePath))
        {
            throw new InvalidQaConfigurationException($"QA {purpose} path cannot be empty.");
        }

        string fullCandidate;
        try
        {
            fullCandidate = NormalizeDirectoryPath(candidatePath);
        }
        catch (Exception ex)
        {
            throw new InvalidQaConfigurationException(
                $"QA {purpose} path '{candidatePath}' is invalid: {ex.Message}", ex);
        }

        if (!IsSyntacticallyInsideRoot(fullCandidate, QaInstanceRoot!))
        {
            throw new InvalidQaConfigurationException(
                $"Configured {purpose} '{candidatePath}' escapes the QA instance root '{QaInstanceRoot}'.");
        }

        var resolvedCandidate = ResolveRealPathIncludingReparseAncestors(fullCandidate, purpose);
        var resolvedRoot = _resolvedQaInstanceRoot ?? ResolveRealPathIncludingReparseAncestors(QaInstanceRoot!, "instance root");
        if (!IsSyntacticallyInsideRoot(resolvedCandidate, resolvedRoot))
        {
            throw new InvalidQaConfigurationException(
                $"Configured {purpose} '{candidatePath}' resolves through a reparse point/junction to '{resolvedCandidate}', escaping the QA instance root '{resolvedRoot}'.");
        }

        RejectForbiddenSharedOverlap(resolvedCandidate, _commonAppDataCloventDir, _localAppDataCloventDir, isCloventDirsAlready: true);
        return fullCandidate;
    }

    /// <summary>Checks whether a candidate path (and any reparse-point target) is equal to or nested beneath <see cref="QaInstanceRoot"/>.</summary>
    public bool IsPathInsideQaRoot(string candidatePath)
    {
        if (!IsQaMode || string.IsNullOrWhiteSpace(QaInstanceRoot) || string.IsNullOrWhiteSpace(candidatePath))
        {
            return false;
        }

        try
        {
            var normalizedCandidate = NormalizeDirectoryPath(candidatePath);
            if (!IsSyntacticallyInsideRoot(normalizedCandidate, QaInstanceRoot))
            {
                return false;
            }

            var resolvedCandidate = ResolveRealPathIncludingReparseAncestors(normalizedCandidate, "candidate path");
            var resolvedRoot = _resolvedQaInstanceRoot ?? ResolveRealPathIncludingReparseAncestors(QaInstanceRoot, "instance root");
            return IsSyntacticallyInsideRoot(resolvedCandidate, resolvedRoot);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Rejects forbidden, production, or non-QA catalog names when running in QA mode.</summary>
    public void ValidateTargetDatabaseName(string? databaseName)
    {
        if (!IsQaMode)
        {
            return;
        }

        ValidateQaCatalogName(databaseName);
    }

    /// <summary>
    /// Validates that the supplied SQL Server and database satisfy QA target identification and
    /// explicit target approval rules when running in QA mode.
    /// </summary>
    public void ValidateTargetApproval(string? dataSource, string? databaseName)
    {
        if (!IsQaMode)
        {
            return;
        }

        ValidateQaCatalogName(databaseName);

        if (string.IsNullOrWhiteSpace(dataSource))
        {
            throw new InvalidQaConfigurationException("QA mode requires a non-empty SQL Server / Data Source.");
        }

        ValidateTargetApprovalCore(dataSource.Trim(), databaseName!.Trim(), _approvedServer, _approvedDatabase);
    }

    private static bool IsSyntacticallyInsideRoot(string normalizedPath, string normalizedRoot) =>
        string.Equals(normalizedPath, normalizedRoot, StringComparison.OrdinalIgnoreCase) ||
        normalizedPath.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

    /// <summary>Creates the default production instance scope.</summary>
    public static CbosInstanceScope CreateProduction()
    {
        var commonAppData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return new CbosInstanceScope(
            isQaMode: false,
            qaInstanceRoot: null,
            resolvedQaInstanceRoot: null,
            explicitConnectionString: null,
            dataSource: null,
            databaseName: null,
            approvedServer: null,
            approvedDatabase: null,
            commonAppDataRoot: commonAppData,
            localAppDataRoot: localAppData);
    }

    /// <summary>
    /// Evaluates startup arguments and environment variables before any filesystem, ACL,
    /// logging, licensing, commissioning, or database call.
    /// Ordinary startup without QA opt-in flags or environment variables preserves production behavior.
    /// </summary>
    public static CbosInstanceScope InitializeFromStartup(
        string[]? args = null,
        Func<string, string?>? getEnvironmentVariable = null)
    {
        getEnvironmentVariable ??= Environment.GetEnvironmentVariable;
        var commonAppData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        var scope = EvaluateStartupCore(args, getEnvironmentVariable, commonAppData, localAppData);
        lock (SyncLock)
        {
            _current = scope;
        }
        return scope;
    }

    /// <summary>Resets <see cref="Current"/> back to default production scope (test cleanup helper).</summary>
    public static void ResetToProduction()
    {
        lock (SyncLock)
        {
            _current = CreateProduction();
        }
    }

    /// <summary>Enters an explicit validated QA scope for isolated automated tests and restores production scope on dispose.</summary>
    public static IDisposable EnterQaScopeForTesting(string qaInstanceRoot, string qaConnectionString, string? approvedTarget = null)
    {
        var commonAppData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        var scope = CreateValidatedQaScope(
            qaInstanceRoot,
            qaConnectionString,
            hasExplicitConnectionInput: true,
            hasExplicitApprovedTargetInput: !string.IsNullOrWhiteSpace(approvedTarget),
            rawApprovedTarget: approvedTarget,
            commonAppData,
            localAppData);
        CbosInstanceScope previous;
        lock (SyncLock)
        {
            previous = _current;
            _current = scope;
        }

        return new ScopeRestoreHandle(previous);
    }

    private sealed class ScopeRestoreHandle(CbosInstanceScope previous) : IDisposable
    {
        private int _disposed;
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                lock (SyncLock)
                {
                    _current = previous;
                }
            }
        }
    }

    private static CbosInstanceScope EvaluateStartupCore(
        string[]? args,
        Func<string, string?> getEnvironmentVariable,
        string commonAppData,
        string localAppData)
    {
        bool qaFlagRequested = false;
        bool hasCliRoot = false;
        string? cliRoot = null;
        bool hasCliConnection = false;
        string? cliConnection = null;
        bool hasCliApprovedTarget = false;
        string? cliApprovedTarget = null;

        if (args is { Length: > 0 })
        {
            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (string.Equals(arg, QaModeCliFlag, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(arg, QaModeLongCliFlag, StringComparison.OrdinalIgnoreCase))
                {
                    qaFlagRequested = true;
                }
                else if (TryReadCliOption(args, ref i, QaRootCliOption, out var rootValue))
                {
                    hasCliRoot = true;
                    cliRoot = rootValue;
                }
                else if (TryReadCliOption(args, ref i, QaConnectionCliOption, out var connValue))
                {
                    hasCliConnection = true;
                    cliConnection = connValue;
                }
                else if (TryReadCliOption(args, ref i, QaApprovedTargetCliOption, out var approvedTargetValue))
                {
                    hasCliApprovedTarget = true;
                    cliApprovedTarget = approvedTargetValue;
                }
            }
        }

        var envRootRaw = getEnvironmentVariable(QaRootEnvironmentVariable);
        var hasEnvRoot = envRootRaw is not null;
        var envConnRaw = getEnvironmentVariable(QaConnectionEnvironmentVariable);
        var hasEnvConn = envConnRaw is not null;
        var envApprovedRaw = getEnvironmentVariable(QaApprovedTargetEnvironmentVariable);
        var hasEnvApproved = envApprovedRaw is not null;

        // Validate explicit flags with missing/empty values before falling back.
        if (hasCliRoot && string.IsNullOrWhiteSpace(cliRoot))
        {
            throw new InvalidQaConfigurationException(
                $"CLI option '{QaRootCliOption}' was specified with a missing or empty directory path.");
        }

        if (hasCliConnection && string.IsNullOrWhiteSpace(cliConnection))
        {
            throw new InvalidQaConfigurationException(
                $"CLI option '{QaConnectionCliOption}' was specified with a missing or empty connection string.");
        }

        if (hasCliApprovedTarget && string.IsNullOrWhiteSpace(cliApprovedTarget))
        {
            throw new InvalidQaConfigurationException(
                $"CLI option '{QaApprovedTargetCliOption}' was specified with a missing or empty Server|Database pair.");
        }

        if (hasEnvRoot && string.IsNullOrWhiteSpace(envRootRaw))
        {
            throw new InvalidQaConfigurationException(
                $"Environment variable '{QaRootEnvironmentVariable}' is present but empty.");
        }

        if (hasEnvConn && string.IsNullOrWhiteSpace(envConnRaw))
        {
            throw new InvalidQaConfigurationException(
                $"Environment variable '{QaConnectionEnvironmentVariable}' is present but empty.");
        }

        if (hasEnvApproved && string.IsNullOrWhiteSpace(envApprovedRaw))
        {
            throw new InvalidQaConfigurationException(
                $"Environment variable '{QaApprovedTargetEnvironmentVariable}' is present but empty.");
        }

        bool anyQaSignal = qaFlagRequested || hasCliRoot || hasEnvRoot || hasCliConnection || hasEnvConn || hasCliApprovedTarget || hasEnvApproved;
        if (!anyQaSignal)
        {
            return new CbosInstanceScope(false, null, null, null, null, null, null, null, commonAppData, localAppData);
        }

        var effectiveRoot = !string.IsNullOrWhiteSpace(cliRoot) ? cliRoot : envRootRaw;
        if (string.IsNullOrWhiteSpace(effectiveRoot))
        {
            throw new InvalidQaConfigurationException(
                $"QA mode was requested without an instance root. Supply '{QaRootCliOption} <directory>' or '{QaRootEnvironmentVariable}'.");
        }

        bool hasExplicitConnectionInput = hasCliConnection || hasEnvConn;
        var effectiveConnection = hasCliConnection ? cliConnection : envConnRaw;
        bool hasExplicitApprovedTargetInput = hasCliApprovedTarget || hasEnvApproved;
        var effectiveApprovedTarget = hasCliApprovedTarget ? cliApprovedTarget : envApprovedRaw;

        return CreateValidatedQaScope(
            effectiveRoot,
            effectiveConnection,
            hasExplicitConnectionInput,
            hasExplicitApprovedTargetInput,
            effectiveApprovedTarget,
            commonAppData,
            localAppData);
    }

    private static bool TryReadCliOption(string[] args, ref int index, string optionName, out string? value)
    {
        var arg = args[index];
        if (string.Equals(arg, optionName, StringComparison.OrdinalIgnoreCase))
        {
            if (index + 1 < args.Length)
            {
                var next = args[index + 1];
                if (!next.StartsWith("--", StringComparison.Ordinal))
                {
                    index++;
                    value = next;
                    return true;
                }
            }

            value = string.Empty;
            return true;
        }

        var prefix = optionName + "=";
        if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            value = arg[prefix.Length..];
            return true;
        }

        value = null;
        return false;
    }

    private static CbosInstanceScope CreateValidatedQaScope(
        string rawRoot,
        string? rawConnectionString,
        bool hasExplicitConnectionInput,
        bool hasExplicitApprovedTargetInput,
        string? rawApprovedTarget,
        string commonAppData,
        string localAppData)
    {
        if (string.IsNullOrWhiteSpace(rawRoot))
        {
            throw new InvalidQaConfigurationException("QA instance root directory cannot be empty.");
        }

        var trimmedRoot = rawRoot.Trim().Trim('"');
        if (!Path.IsPathFullyQualified(trimmedRoot))
        {
            throw new InvalidQaConfigurationException(
                $"QA instance root '{trimmedRoot}' must be a fully-qualified absolute directory path.");
        }

        string fullRoot;
        try
        {
            fullRoot = NormalizeDirectoryPath(trimmedRoot);
        }
        catch (Exception ex)
        {
            throw new InvalidQaConfigurationException(
                $"QA instance root '{trimmedRoot}' is not a valid path: {ex.Message}", ex);
        }

        RejectDriveRoot(fullRoot, rawRoot);

        if (File.Exists(fullRoot))
        {
            throw new InvalidQaConfigurationException(
                $"QA instance root '{fullRoot}' points to an existing file rather than a directory.");
        }

        RejectForbiddenSharedOverlap(fullRoot, commonAppData, localAppData, isCloventDirsAlready: false);

        var resolvedRoot = ResolveRealPathIncludingReparseAncestors(fullRoot, "instance root");
        RejectDriveRoot(resolvedRoot, rawRoot);
        if (File.Exists(resolvedRoot))
        {
            throw new InvalidQaConfigurationException(
                $"QA instance root '{fullRoot}' resolves to an existing file '{resolvedRoot}' rather than a directory.");
        }

        RejectForbiddenSharedOverlap(resolvedRoot, commonAppData, localAppData, isCloventDirsAlready: false);

        string? approvedServer = null;
        string? approvedDatabase = null;
        if (hasExplicitApprovedTargetInput)
        {
            (approvedServer, approvedDatabase) = ParseApprovedTargetPair(rawApprovedTarget!);
        }
        else
        {
            var approvedFile = Path.Combine(fullRoot, "Config", QaApprovedTargetFileName);
            if (File.Exists(approvedFile))
            {
                (approvedServer, approvedDatabase) = ReadApprovedTargetFile(approvedFile);
            }
        }

        if (hasExplicitConnectionInput)
        {
            var (dataSource, databaseName) = ParseAndValidateConnectionString(rawConnectionString);
            ValidateTargetApprovalCore(dataSource, databaseName, approvedServer, approvedDatabase);
            return new CbosInstanceScope(
                isQaMode: true,
                qaInstanceRoot: fullRoot,
                resolvedQaInstanceRoot: resolvedRoot,
                explicitConnectionString: rawConnectionString!.Trim(),
                dataSource: dataSource,
                databaseName: databaseName,
                approvedServer: approvedServer,
                approvedDatabase: approvedDatabase,
                commonAppDataRoot: commonAppData,
                localAppDataRoot: localAppData);
        }

        // Support restarting an already-configured QA root ONLY when its own <QaRoot>\Config\database.config.json
        // exists and names an isolated non-default database. Never read shared %ProgramData%, %LocalAppData%,
        // appsettings.json, or ConnectionStrings__Default in QA mode.
        var qaConfigFile = Path.Combine(fullRoot, "Config", "database.config.json");
        if (!File.Exists(qaConfigFile))
        {
            throw new InvalidQaConfigurationException(
                $"QA mode requires an explicit SQL connection target via '{QaConnectionCliOption}', '{QaConnectionEnvironmentVariable}', or an existing QA config at '{qaConfigFile}'. Refusing to fall back to production or default configuration.");
        }

        var (savedServer, savedDb) = ValidateSavedQaConfigFile(qaConfigFile);
        ValidateTargetApprovalCore(savedServer, savedDb, approvedServer, approvedDatabase);
        return new CbosInstanceScope(
            isQaMode: true,
            qaInstanceRoot: fullRoot,
            resolvedQaInstanceRoot: resolvedRoot,
            explicitConnectionString: null,
            dataSource: savedServer,
            databaseName: savedDb,
            approvedServer: approvedServer,
            approvedDatabase: approvedDatabase,
            commonAppDataRoot: commonAppData,
            localAppDataRoot: localAppData);
    }

    private static void RejectDriveRoot(string normalizedPath, string rawInput)
    {
        var pathRoot = Path.GetPathRoot(normalizedPath);
        if (!string.IsNullOrWhiteSpace(pathRoot) &&
            string.Equals(normalizedPath, NormalizeDirectoryPath(pathRoot), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidQaConfigurationException(
                $"QA instance root '{rawInput}' (resolved '{normalizedPath}') cannot be a drive or filesystem root.");
        }
    }

    private static (string Server, string Database) ParseApprovedTargetPair(string rawPair)
    {
        if (string.IsNullOrWhiteSpace(rawPair))
        {
            throw new InvalidQaConfigurationException(
                $"Approved QA target cannot be empty. Expected '<Server>|<Database>'.");
        }

        var parts = rawPair.Split('|', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
        {
            throw new InvalidQaConfigurationException(
                $"Approved QA target '{rawPair}' must use the format '<Server>|<Database>'.");
        }

        ValidateQaCatalogName(parts[1]);
        return (parts[0], parts[1]);
    }

    private static (string Server, string Database) ReadApprovedTargetFile(string approvedFilePath)
    {
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(approvedFilePath));
            var root = doc.RootElement;
            string? server = root.TryGetProperty("Server", out var serverProp) ? serverProp.GetString() : null;
            string? database = root.TryGetProperty("Database", out var dbProp) ? dbProp.GetString() : null;

            if (string.IsNullOrWhiteSpace(server))
            {
                throw new InvalidQaConfigurationException(
                    $"Approved QA target file '{approvedFilePath}' must specify a non-empty 'Server'.");
            }

            ValidateQaCatalogName(database);
            return (server.Trim(), database!.Trim());
        }
        catch (InvalidQaConfigurationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidQaConfigurationException(
                $"Approved QA target file '{approvedFilePath}' is unreadable or invalid JSON: {ex.Message}", ex);
        }
    }

    private static bool IsLocalDbDataSource(string dataSource) =>
        dataSource.StartsWith("(localdb)\\", StringComparison.OrdinalIgnoreCase);

    private static void ValidateTargetApprovalCore(
        string dataSource,
        string databaseName,
        string? approvedServer,
        string? approvedDatabase)
    {
        bool hasExplicitApproval = !string.IsNullOrWhiteSpace(approvedServer) && !string.IsNullOrWhiteSpace(approvedDatabase);
        if (hasExplicitApproval)
        {
            if (!string.Equals(dataSource, approvedServer, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(databaseName, approvedDatabase, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidQaConfigurationException(
                    $"QA SQL target '{dataSource}|{databaseName}' does not match the approved target '{approvedServer}|{approvedDatabase}'.");
            }

            return;
        }

        if (!IsLocalDbDataSource(dataSource))
        {
            throw new InvalidQaConfigurationException(
                $"QA SQL target '{dataSource}|{databaseName}' uses a non-LocalDB server without explicit approval. " +
                $"Approve the target via '{QaApprovedTargetCliOption} \"{dataSource}|{databaseName}\"', " +
                $"'{QaApprovedTargetEnvironmentVariable}', or '<QaRoot>\\Config\\{QaApprovedTargetFileName}'.");
        }
    }

    private static (string DataSource, string DatabaseName) ValidateSavedQaConfigFile(string qaConfigFile)
    {
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(qaConfigFile));
            var root = doc.RootElement;
            string? server = root.TryGetProperty("Server", out var serverProp) ? serverProp.GetString() : null;
            string? database = root.TryGetProperty("Database", out var dbProp) ? dbProp.GetString() : null;

            if (string.IsNullOrWhiteSpace(server))
            {
                throw new InvalidQaConfigurationException(
                    $"QA database config '{qaConfigFile}' does not contain a valid Server.");
            }

            ValidateQaCatalogName(database);
            return (server.Trim(), database!.Trim());
        }
        catch (InvalidQaConfigurationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidQaConfigurationException(
                $"QA database config '{qaConfigFile}' is unreadable or invalid JSON: {ex.Message}", ex);
        }
    }

    /// <summary>Validates that a QA SQL Server connection string is well-formed and targets an allowed QA database.</summary>
    public static (string DataSource, string DatabaseName) ParseAndValidateConnectionString(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidQaConfigurationException("QA SQL connection string cannot be empty.");
        }

        DbConnectionStringBuilder builder;
        try
        {
            builder = new DbConnectionStringBuilder { ConnectionString = connectionString.Trim() };
        }
        catch (Exception ex)
        {
            throw new InvalidQaConfigurationException($"QA SQL connection string is malformed: {ex.Message}", ex);
        }

        var server = ExtractValue(builder, "Server", "Data Source", "Address", "Addr", "Network Address");
        var database = ExtractValue(builder, "Database", "Initial Catalog");

        if (string.IsNullOrWhiteSpace(server))
        {
            throw new InvalidQaConfigurationException("QA SQL connection string must specify a Server / Data Source.");
        }

        ValidateQaCatalogName(database);
        return (server.Trim(), database!.Trim());
    }

    private static void ValidateQaCatalogName(string? databaseName)
    {
        if (string.IsNullOrWhiteSpace(databaseName))
        {
            throw new InvalidQaConfigurationException("QA mode requires an explicit non-empty SQL Database / Initial Catalog name.");
        }

        var trimmed = databaseName.Trim();
        if (ForbiddenExactCatalogNames.Contains(trimmed) ||
            trimmed.StartsWith(ForbiddenDefaultDatabaseName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidQaConfigurationException(
                $"QA mode refuses to target forbidden or production database catalog '{trimmed}'.");
        }

        var tokens = trimmed.Split(['_', '-', '.'], StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Any(t =>
                string.Equals(t, "prod", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(t, "production", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(t, "live", StringComparison.OrdinalIgnoreCase)) ||
            trimmed.Contains("production", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidQaConfigurationException(
                $"QA mode refuses to target production-named database catalog '{trimmed}'.");
        }

        bool hasAllowedPrefix = AllowedQaDatabasePrefixes.Any(
            prefix => trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && trimmed.Length > prefix.Length);

        if (!hasAllowedPrefix)
        {
            throw new InvalidQaConfigurationException(
                $"QA database catalog '{trimmed}' must start with an explicit QA prefix ({string.Join(", ", AllowedQaDatabasePrefixes)}) so arbitrary or production databases cannot be opened implicitly.");
        }
    }

    private static string? ExtractValue(DbConnectionStringBuilder builder, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (builder.TryGetValue(key, out var raw) && raw is not null)
            {
                var str = raw.ToString();
                if (!string.IsNullOrWhiteSpace(str))
                {
                    return str;
                }
            }
        }
        return null;
    }

    private static void RejectForbiddenSharedOverlap(
        string fullRoot,
        string commonAppDataOrClovent,
        string localAppDataOrClovent,
        bool isCloventDirsAlready)
    {
        var forbiddenPrefixes = new List<string>();
        if (!string.IsNullOrWhiteSpace(commonAppDataOrClovent))
        {
            var dir = isCloventDirsAlready ? commonAppDataOrClovent : Path.Combine(commonAppDataOrClovent, "Clovent");
            forbiddenPrefixes.Add(NormalizeDirectoryPath(dir));
        }
        if (!string.IsNullOrWhiteSpace(localAppDataOrClovent))
        {
            var dir = isCloventDirsAlready ? localAppDataOrClovent : Path.Combine(localAppDataOrClovent, "Clovent");
            forbiddenPrefixes.Add(NormalizeDirectoryPath(dir));
        }

        var winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        if (!string.IsNullOrWhiteSpace(winDir))
        {
            forbiddenPrefixes.Add(NormalizeDirectoryPath(winDir));
        }

        foreach (var forbidden in forbiddenPrefixes)
        {
            bool insideOrEqual =
                string.Equals(fullRoot, forbidden, StringComparison.OrdinalIgnoreCase) ||
                fullRoot.StartsWith(forbidden + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

            bool parentOfForbidden =
                forbidden.StartsWith(fullRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

            if (insideOrEqual || parentOfForbidden)
            {
                throw new InvalidQaConfigurationException(
                    $"QA instance root '{fullRoot}' overlaps shared system/production directory '{forbidden}'. Choose a dedicated isolated directory.");
            }
        }
    }

    private static string ResolveRealPathIncludingReparseAncestors(string candidatePath, string purpose)
    {
        var currentPath = NormalizeDirectoryPath(candidatePath);

        for (int hops = 0; hops < 8; hops++)
        {
            var pathRoot = Path.GetPathRoot(currentPath);
            if (string.IsNullOrWhiteSpace(pathRoot))
            {
                throw new InvalidQaConfigurationException($"QA {purpose} path '{candidatePath}' has no root.");
            }

            var relative = Path.GetRelativePath(pathRoot, currentPath);
            if (relative == ".")
            {
                return currentPath;
            }

            var segments = relative.Split(
                [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
                StringSplitOptions.RemoveEmptyEntries);

            var probe = pathRoot;
            bool redirected = false;

            for (int i = 0; i < segments.Length; i++)
            {
                probe = Path.Combine(probe, segments[i]);

                FileAttributes attrs;
                try
                {
                    attrs = File.GetAttributes(probe);
                }
                catch (FileNotFoundException)
                {
                    break;
                }
                catch (DirectoryNotFoundException)
                {
                    break;
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
                {
                    throw new InvalidQaConfigurationException(
                        $"QA {purpose} path '{candidatePath}' failed filesystem attribute inspection at '{probe}': {ex.Message}",
                        ex);
                }

                if ((attrs & FileAttributes.ReparsePoint) != 0)
                {
                    FileSystemInfo info = (attrs & FileAttributes.Directory) != 0
                        ? new DirectoryInfo(probe)
                        : new FileInfo(probe);

                    string? targetPath = null;
                    try
                    {
                        targetPath = info.ResolveLinkTarget(returnFinalTarget: true)?.FullName;
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FileNotFoundException or DirectoryNotFoundException)
                    {
                        targetPath = null;
                    }

                    if (string.IsNullOrWhiteSpace(targetPath) && !string.IsNullOrWhiteSpace(info.LinkTarget))
                    {
                        var parent = Path.GetDirectoryName(probe) ?? pathRoot;
                        targetPath = Path.IsPathFullyQualified(info.LinkTarget)
                            ? info.LinkTarget
                            : Path.Combine(parent, info.LinkTarget);
                    }

                    if (string.IsNullOrWhiteSpace(targetPath))
                    {
                        throw new InvalidQaConfigurationException(
                            $"QA {purpose} path '{candidatePath}' contains a reparse point at '{probe}' whose target could not be resolved.");
                    }

                    var remaining = segments.Skip(i + 1).ToArray();
                    currentPath = NormalizeDirectoryPath(
                        remaining.Length == 0
                            ? targetPath
                            : Path.Combine([targetPath, .. remaining]));
                    redirected = true;
                    break;
                }
            }

            if (!redirected)
            {
                return currentPath;
            }
        }

        throw new InvalidQaConfigurationException(
            $"QA {purpose} path '{candidatePath}' exceeded reparse-point resolution depth.");
    }

    private static string NormalizeDirectoryPath(string path)
    {
        var full = Path.GetFullPath(path);
        var root = Path.GetPathRoot(full);
        if (!string.IsNullOrEmpty(root) && string.Equals(full, root, StringComparison.OrdinalIgnoreCase))
        {
            return full;
        }
        return full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}
