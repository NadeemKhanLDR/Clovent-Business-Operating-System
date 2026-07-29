using System.Net;
using Clovent.Domain;

namespace Clovent.Authentication.Shared.ValueObjects;

/// <summary>A validated IPv4 or IPv6 address, recorded against sessions and login attempts for audit purposes.</summary>
public sealed class IpAddress : ValueObject
{
    /// <summary>The normalized address text.</summary>
    public string Value { get; }

    private IpAddress(string value) => Value = value;

    /// <summary>Validates and normalizes <paramref name="value"/> into an <see cref="IpAddress"/>.</summary>
    /// <exception cref="ArgumentException"><paramref name="value"/> is not a valid IPv4 or IPv6 address.</exception>
    public static IpAddress Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("IP address is required.", nameof(value));

        if (!IPAddress.TryParse(value.Trim(), out var parsed))
            throw new ArgumentException($"'{value}' is not a valid IP address.", nameof(value));

        return new IpAddress(parsed.ToString());
    }

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <inheritdoc/>
    public override string ToString() => Value;
}
