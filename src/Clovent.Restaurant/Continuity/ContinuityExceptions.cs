namespace Clovent.Restaurant.Continuity;

/// <summary>
/// Exception thrown when cryptographic tampering, payload corruption, sequence violation,
/// or broken hash chaining is detected in emergency continuity transactions.
/// </summary>
public sealed class ContinuityTamperException : InvalidOperationException
{
    /// <summary>Initializes a new instance of <see cref="ContinuityTamperException"/>.</summary>
    public ContinuityTamperException(string message) : base(message) { }

    /// <summary>Initializes a new instance of <see cref="ContinuityTamperException"/> with an inner exception.</summary>
    public ContinuityTamperException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when cryptographic data protection (DPAPI) cannot be established
/// at the required machine level without an explicit configured policy.
/// </summary>
public sealed class ContinuitySecurityException : InvalidOperationException
{
    /// <summary>Initializes a new instance of <see cref="ContinuitySecurityException"/>.</summary>
    public ContinuitySecurityException(string message) : base(message) { }

    /// <summary>Initializes a new instance of <see cref="ContinuitySecurityException"/> with an inner exception.</summary>
    public ContinuitySecurityException(string message, Exception innerException) : base(message, innerException) { }
}
