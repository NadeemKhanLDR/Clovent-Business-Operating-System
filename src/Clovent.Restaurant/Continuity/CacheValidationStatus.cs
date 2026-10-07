namespace Clovent.Restaurant.Continuity;

/// <summary>
/// Status of the local operational cache during integrity, freshness, and terminal validation.
/// </summary>
public enum CacheValidationStatus
{
    /// <summary>Cache is fresh, authentic, matching terminal and branch, and fully valid for offline selling.</summary>
    Valid = 0,

    /// <summary>Cache is older than the warning threshold but still within maximum permitted policy age.</summary>
    StaleWithinPolicy = 1,

    /// <summary>Cache age exceeds the maximum allowable policy limit. Continuity selling is prohibited.</summary>
    TooStale = 2,

    /// <summary>Cache payload is corrupted, unreadable, or failed basic checksum validation.</summary>
    Corrupt = 3,

    /// <summary>Cache cryptographic HMAC authentication signature failed. Tampering detected.</summary>
    Tampered = 4,

    /// <summary>Cache was generated for a different POS terminal. Multi-terminal crossover prohibited.</summary>
    WrongTerminal = 5,

    /// <summary>Cache was generated for a different branch. Multi-branch crossover prohibited.</summary>
    WrongBranch = 6,

    /// <summary>Cache schema version is incompatible with current POS client binary.</summary>
    IncompatibleVersion = 7,

    /// <summary>No operational cache exists on this terminal.</summary>
    Missing = 8
}
