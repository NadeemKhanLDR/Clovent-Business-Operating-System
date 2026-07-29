using System.Text.RegularExpressions;
using Clovent.Domain;

namespace Clovent.Identity.Permissions.ValueObjects;

/// <summary>
/// A permission's stable machine identifier, dot-segmented from broad to
/// specific (e.g. <c>"identity.users.manage"</c>). This is what roles and
/// authorization checks reference - never the free-text description.
/// </summary>
public sealed partial class PermissionCode : ValueObject
{
    /// <summary>The code, always lowercase.</summary>
    public string Value { get; }

    private PermissionCode(string value) => Value = value;

    /// <summary>Validates and normalizes <paramref name="value"/> into a <see cref="PermissionCode"/>.</summary>
    /// <exception cref="ArgumentException"><paramref name="value"/> does not match the required shape.</exception>
    public static PermissionCode Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Permission code is required.", nameof(value));

        var normalized = value.Trim().ToLowerInvariant();

        if (!PermissionCodePattern().IsMatch(normalized))
            throw new ArgumentException(
                $"'{value}' is not a valid permission code. Expected 2-5 lowercase, dot-separated segments (e.g. 'identity.users.manage').",
                nameof(value));

        return new PermissionCode(normalized);
    }

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <inheritdoc/>
    public override string ToString() => Value;

    [GeneratedRegex(@"^[a-z][a-z0-9]*(\.[a-z][a-z0-9]*){1,4}$")]
    private static partial Regex PermissionCodePattern();
}
