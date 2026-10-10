namespace Clovent.Platform.Sync;

/// <summary>
/// Exception thrown when a delta-sync dispatch is attempted while network connectivity is inactive,
/// signaling the outbox processor to defer retry without penalizing attempt counts or dead-lettering.
/// </summary>
public sealed class NetworkOfflineException : Exception
{
    /// <summary>Creates a new instance of <see cref="NetworkOfflineException"/>.</summary>
    public NetworkOfflineException(string message) : base(message)
    {
    }

    /// <summary>Creates a new instance of <see cref="NetworkOfflineException"/> with an inner exception.</summary>
    public NetworkOfflineException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
