namespace Clovent.Desktop.Commissioning.Database;

/// <summary>
/// Status of database schema compatibility relative to the running application version.
/// </summary>
public enum SchemaCompatibilityStatus
{
    /// <summary>
    /// All migrations in code have been applied to the database, and no unknown migrations exist.
    /// Safe for normal application and POS startup.
    /// </summary>
    Compatible,

    /// <summary>
    /// The application contains pending migrations that have not yet been applied to the database.
    /// Requires an explicit update or migration phase before startup.
    /// </summary>
    DatabaseTooOld,

    /// <summary>
    /// The database contains migrations unknown to this version of the application.
    /// Indicates the database was upgraded by a newer release.
    /// </summary>
    DatabaseNewer,

    /// <summary>
    /// Connection to the database could not be established or the database does not exist.
    /// </summary>
    ConnectionFailed
}

/// <summary>
/// Result of the database schema compatibility evaluation.
/// </summary>
public sealed class SchemaCompatibilityResult
{
    /// <summary>
    /// Overall compatibility status.
    /// </summary>
    public SchemaCompatibilityStatus Status { get; }

    /// <summary>
    /// User-friendly non-technical message describing the compatibility state and recommended action.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Detailed technical explanation or masked exception message if connection or inspection failed.
    /// </summary>
    public string? Detail { get; }

    /// <summary>
    /// List of migration identifiers defined in code but not yet applied to the database.
    /// </summary>
    public IReadOnlyList<string> PendingMigrations { get; }

    /// <summary>
    /// List of migration identifiers present in the database but unknown to this application version.
    /// </summary>
    public IReadOnlyList<string> UnknownMigrations { get; }

    /// <summary>
    /// Breakdown of pending migrations mapped by bounded context DbContext name.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> PendingByContext { get; }

    /// <summary>
    /// Breakdown of unknown migrations mapped by bounded context DbContext name.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> UnknownByContext { get; }

    /// <summary>
    /// True if the database schema is fully compatible and safe for standard POS startup.
    /// </summary>
    public bool IsCompatible => Status == SchemaCompatibilityStatus.Compatible;

    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaCompatibilityResult"/> class.
    /// </summary>
    /// <param name="status">The overall compatibility status.</param>
    /// <param name="message">User-friendly non-technical message describing the compatibility state.</param>
    /// <param name="detail">Detailed technical explanation or masked error message.</param>
    /// <param name="pendingMigrations">Pending migrations defined in code but not yet applied.</param>
    /// <param name="unknownMigrations">Unknown migrations in the database not defined in code.</param>
    /// <param name="pendingByContext">Pending migrations grouped by bounded context.</param>
    /// <param name="unknownByContext">Unknown migrations grouped by bounded context.</param>
    public SchemaCompatibilityResult(
        SchemaCompatibilityStatus status,
        string message,
        string? detail = null,
        IReadOnlyList<string>? pendingMigrations = null,
        IReadOnlyList<string>? unknownMigrations = null,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? pendingByContext = null,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? unknownByContext = null)
    {
        Status = status;
        Message = message;
        Detail = detail;
        PendingMigrations = pendingMigrations ?? Array.Empty<string>();
        UnknownMigrations = unknownMigrations ?? Array.Empty<string>();
        PendingByContext = pendingByContext ?? new Dictionary<string, IReadOnlyList<string>>();
        UnknownByContext = unknownByContext ?? new Dictionary<string, IReadOnlyList<string>>();
    }
}

/// <summary>
/// Validates EF Core migration compatibility across all bounded contexts without applying migrations.
/// Implements Requirement 8: Automatic EF migration safety for normal POS startup.
/// </summary>
public interface IDatabaseSchemaCompatibilityValidator
{
    /// <summary>
    /// Evaluates whether the target database matches the expected migration state.
    /// </summary>
    Task<SchemaCompatibilityResult> ValidateCompatibilityAsync(string connectionString, CancellationToken ct = default);
}
