using Clovent.Domain;
using Clovent.Identity.Permissions.Events;
using Clovent.Identity.Permissions.ValueObjects;

namespace Clovent.Identity.Permissions;

/// <summary>
/// A single grantable capability, referenced by <see cref="Roles.Role"/> via
/// its <see cref="Code"/>. Permissions are catalog entries - their identity
/// and code never change once created.
/// </summary>
public sealed class Permission : AggregateRoot<PermissionId>
{
    private const int MaxDescriptionLength = 500;

    /// <summary>The stable machine identifier for this permission.</summary>
    public PermissionCode Code { get; }

    /// <summary>Human-readable explanation of what this permission grants.</summary>
    public string Description { get; }

    /// <summary>UTC instant this permission was created.</summary>
    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>
    /// Takes <paramref name="createdAtUtc"/> explicitly - see <see cref="Roles.Role"/>'s
    /// identical constructor doc comment for why.
    /// </summary>
    private Permission(PermissionId id, PermissionCode code, string description, DateTimeOffset createdAtUtc)
    {
        Id = id;
        Code = code;
        Description = description;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>Creates a new permission catalog entry.</summary>
    /// <exception cref="ArgumentException"><paramref name="description"/> is empty or exceeds <c>500</c> characters.</exception>
    public static Permission Create(PermissionCode code, string description)
    {
        ArgumentNullException.ThrowIfNull(code);

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Permission description is required.", nameof(description));

        description = description.Trim();

        if (description.Length > MaxDescriptionLength)
            throw new ArgumentException($"Permission description cannot exceed {MaxDescriptionLength} characters.", nameof(description));

        var now = DateTimeOffset.UtcNow;
        var permission = new Permission(PermissionId.New(), code, description, now);
        permission.AddDomainEvent(new PermissionCreated(permission.Id, permission.Code, now));
        return permission;
    }
}
