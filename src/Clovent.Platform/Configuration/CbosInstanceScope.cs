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

    /// <summary>Default production database catalog name, forbidden in QA mode.</summary>
    public const string ForbiddenDefaultDatabaseName = "Clovent_BusinessOperatingSystem";

    private static readonly object SyncLock = new();
    private static CbosInstanceScope _current = CreateProduction();

    private readonly string _commonAppDataCloventDir;
    private readonly string _localAppDataCloventDir;

    private CbosInstanceScope(
        bool isQaMode,
        string? qaInstanceRoot,
        string? explicitConnectionString,
        string? dataSource,
        string? databaseName,
        string commonAppDataRoot,
        string localAppDataRoot)
    {
        IsQaMode = isQaMode;
        QaInstanceRoot = qaInstanceRoot;
        ExplicitConnectionString = explicitConnectionString;
        DataSource = dataSource;
        DatabaseName = databaseName;
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

        if (string.IsNullOrWhiteSpace(configuredDirectory))
        {
            return UserLogsDirectory;
        }

        if (!IsPathInsideQaRoot(configuredDirectory))
        {
            throw new InvalidQaConfigurationException(
                $"Configured log directory '{configuredDirectory}' escapes the QA instance root '{QaInstanceRoot}'.");
        }

        return Path.GetFullPath(configuredDirectory);
    }

    /// <summary>Checks whether a candidate path is equal to or nested beneath <see cref="QaInstanceRoot"/>.</summary>
    public bool IsPathInsideQaRoot(string candidatePath)
    {
        if (!IsQaMode || string.IsNullOrWhiteSpace(QaInstanceRoot) || string.IsNullOrWhiteSpace(candidatePath))
        {
            return false;
        }

        try
        {
            var normalizedCandidate = NormalizeDirectoryPath(candidatePath);
            return string.Equals(normalizedCandidate, QaInstanceRoot, StringComparison.OrdinalIgnoreCase)
                || normalizedCandidate.StartsWith(QaInstanceRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Rejects the default production catalog name when running in QA mode.</summary>
    public void ValidateTargetDatabaseName(string? databaseName)
    {
        if (!IsQaMode)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(databaseName))
        {
            throw new InvalidQaConfigurationException("QA mode requires a non-empty SQL Database / Initial Catalog name.");
        }

        if (string.Equals(databaseName.Trim(), ForbiddenDefaultDatabaseName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidQaConfigurationException(
                $"QA mode refuses to target the default production database name '{ForbiddenDefaultDatabaseName}'.");
        }
    }

    /// <summary>Creates the default production instance scope.</summary>
    public static CbosInstanceScope CreateProduction()
    {
        var commonAppData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return new CbosInstanceScope(
            isQaMode: false,
            qaInstanceRoot: null,
            explicitConnectionString: null,
            dataSource: null,
            databaseName: null,
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
    public static IDisposable EnterQaScopeForTesting(string qaInstanceRoot, string qaConnectionString)
    {
        var commonAppData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        var scope = CreateValidatedQaScope(qaInstanceRoot, qaConnectionString, hasExplicitConnectionInput: true, commonAppData, localAppData);
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
            }
        }

        var envRootRaw = getEnvironmentVariable(QaRootEnvironmentVariable);
        var hasEnvRoot = envRootRaw is not null;
        var envConnRaw = getEnvironmentVariable(QaConnectionEnvironmentVariable);
        var hasEnvConn = envConnRaw is not null;

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

        bool anyQaSignal = qaFlagRequested || hasCliRoot || hasEnvRoot || hasCliConnection || hasEnvConn;
        if (!anyQaSignal)
        {
            return new CbosInstanceScope(false, null, null, null, null, commonAppData, localAppData);
        }

        var effectiveRoot = !string.IsNullOrWhiteSpace(cliRoot) ? cliRoot : envRootRaw;
        if (string.IsNullOrWhiteSpace(effectiveRoot))
        {
            throw new InvalidQaConfigurationException(
                $"QA mode was requested without an instance root. Supply '{QaRootCliOption} <directory>' or '{QaRootEnvironmentVariable}'.");
        }

        bool hasExplicitConnectionInput = hasCliConnection || hasEnvConn;
        var effectiveConnection = hasCliConnection ? cliConnection : envConnRaw;

        return CreateValidatedQaScope(
            effectiveRoot,
            effectiveConnection,
            hasExplicitConnectionInput,
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

        var pathRoot = Path.GetPathRoot(fullRoot);
        if (!string.IsNullOrWhiteSpace(pathRoot) &&
            string.Equals(fullRoot, NormalizeDirectoryPath(pathRoot), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidQaConfigurationException(
                $"QA instance root '{fullRoot}' cannot be a drive or filesystem root.");
        }

        if (File.Exists(fullRoot))
        {
            throw new InvalidQaConfigurationException(
                $"QA instance root '{fullRoot}' points to an existing file rather than a directory.");
        }

        RejectForbiddenSharedOverlap(fullRoot, commonAppData, localAppData);

        if (hasExplicitConnectionInput)
        {
            var (dataSource, databaseName) = ParseAndValidateConnectionString(rawConnectionString);
            return new CbosInstanceScope(true, fullRoot, rawConnectionString!.Trim(), dataSource, databaseName, commonAppData, localAppData);
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
        return new CbosInstanceScope(true, fullRoot, null, savedServer, savedDb, commonAppData, localAppData);
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

            if (string.IsNullOrWhiteSpace(database) ||
                string.Equals(database.Trim(), ForbiddenDefaultDatabaseName, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidQaConfigurationException(
                    $"QA database config '{qaConfigFile}' targets missing or forbidden database '{database}'.");
            }

            return (server.Trim(), database.Trim());
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

    /// <summary>Validates that a QA SQL Server connection string is well-formed and does not target the default production database.</summary>
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

        if (string.IsNullOrWhiteSpace(database))
        {
            throw new InvalidQaConfigurationException("QA SQL connection string must specify an explicit Database / Initial Catalog.");
        }

        if (string.Equals(database.Trim(), ForbiddenDefaultDatabaseName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidQaConfigurationException(
                $"QA mode refuses to target the default production database '{ForbiddenDefaultDatabaseName}'.");
        }

        return (server.Trim(), database.Trim());
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

    private static void RejectForbiddenSharedOverlap(string fullRoot, string commonAppData, string localAppData)
    {
        var forbiddenPrefixes = new List<string>();
        if (!string.IsNullOrWhiteSpace(commonAppData))
        {
            forbiddenPrefixes.Add(NormalizeDirectoryPath(Path.Combine(commonAppData, "Clovent")));
        }
        if (!string.IsNullOrWhiteSpace(localAppData))
        {
            forbiddenPrefixes.Add(NormalizeDirectoryPath(Path.Combine(localAppData, "Clovent")));
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
