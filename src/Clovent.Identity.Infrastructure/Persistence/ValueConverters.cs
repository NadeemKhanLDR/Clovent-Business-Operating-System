using System.Text.Json;
using Clovent.Identity.Branches;
using Clovent.Identity.Branches.ValueObjects;
using Clovent.Identity.Companies;
using Clovent.Identity.Companies.ValueObjects;
using Clovent.Identity.Organizations;
using Clovent.Identity.Organizations.ValueObjects;
using Clovent.Identity.Permissions;
using Clovent.Identity.Permissions.ValueObjects;
using Clovent.Identity.Roles;
using Clovent.Identity.Roles.ValueObjects;
using Clovent.Identity.Shared.ValueObjects;
using Clovent.Identity.Users;
using Clovent.Identity.Users.ValueObjects;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Clovent.Identity.Infrastructure.Persistence;

/// <summary>
/// EF Core <see cref="ValueConverter{TModel,TProvider}"/>s shared across
/// this project's entity type configurations - see
/// <c>Clovent.Authentication.Infrastructure.Persistence.ValueConverters</c>
/// for the identical pattern and reasoning (every conversion goes through
/// the value object's own public factory, no Domain-layer changes needed).
/// </summary>
internal static class ValueConverters
{
    /// <summary><see cref="UserId"/> &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<UserId, Guid> UserIdConverter =
        new(id => id.Value, value => new UserId(value));

    /// <summary><see cref="RoleId"/> &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<RoleId, Guid> RoleIdConverter =
        new(id => id.Value, value => new RoleId(value));

    /// <summary><see cref="PermissionId"/> &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<PermissionId, Guid> PermissionIdConverter =
        new(id => id.Value, value => new PermissionId(value));

    /// <summary><see cref="Email"/> &lt;-&gt; normalized address string.</summary>
    public static readonly ValueConverter<Email, string> EmailConverter =
        new(v => v.Value, v => Email.Create(v));

    /// <summary><see cref="UserName"/> &lt;-&gt; handle string.</summary>
    public static readonly ValueConverter<UserName, string> UserNameConverter =
        new(v => v.Value, v => UserName.Create(v));

    /// <summary><see cref="DisplayName"/> &lt;-&gt; display text.</summary>
    public static readonly ValueConverter<DisplayName, string> DisplayNameConverter =
        new(v => v.Value, v => DisplayName.Create(v));

    /// <summary><see cref="RoleName"/> &lt;-&gt; name text.</summary>
    public static readonly ValueConverter<RoleName, string> RoleNameConverter =
        new(v => v.Value, v => RoleName.Create(v));

    /// <summary><see cref="PermissionCode"/> &lt;-&gt; code text.</summary>
    public static readonly ValueConverter<PermissionCode, string> PermissionCodeConverter =
        new(v => v.Value, v => PermissionCode.Create(v));

    /// <summary>
    /// <see cref="User.RoleIds"/> &lt;-&gt; a JSON array of role id GUIDs. Same
    /// reasoning as <c>Clovent.Authentication.Infrastructure.Persistence.ValueConverters.PasswordHistoryConverter</c>:
    /// a small, capped-in-practice set with no independent identity, never
    /// queried apart from its owning aggregate - a single column avoids an
    /// owned-collection table for a property with no public setter and no
    /// settable backing field of a matching type. Reconstruction feeds
    /// straight into <c>User</c>'s constructor (see its doc comment), not a
    /// public mutator, since assigning roles one at a time would spuriously
    /// raise domain events during materialization.
    /// </summary>
    public static readonly ValueConverter<IReadOnlyCollection<RoleId>, string> RoleIdsConverter = new(
        v => JsonSerializer.Serialize(v.Select(r => r.Value), (JsonSerializerOptions?)null),
        v => (JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>())
            .Select(g => new RoleId(g))
            .ToList());

    /// <summary>
    /// Without this, EF Core's default change tracking for a converted
    /// property uses reference equality - <see cref="User.RoleIds"/> always
    /// returns the *same* backing <c>HashSet&lt;RoleId&gt;</c> reference, so
    /// <c>AssignRole</c>/<c>RemoveRole</c> mutating it in place would never be
    /// detected as a change (same reference before and after). This compares
    /// contents (<see cref="Enumerable.SequenceEqual{TSource}(IEnumerable{TSource}, IEnumerable{TSource})"/>
    /// order-independent enough for a set-like collection in practice) and,
    /// critically, snapshots via <c>ToList()</c> - a genuine copy, so the
    /// stored "original value" doesn't itself mutate when the live set does.
    /// </summary>
    public static readonly ValueComparer<IReadOnlyCollection<RoleId>> RoleIdsComparer = new(
        (a, b) => (a ?? new List<RoleId>()).OrderBy(r => r.Value).SequenceEqual((b ?? new List<RoleId>()).OrderBy(r => r.Value)),
        v => v.Aggregate(0, (hash, id) => HashCode.Combine(hash, id)),
        v => v.ToList());

    /// <summary><see cref="Role.PermissionIds"/>'s <see cref="PermissionId"/> counterpart to <see cref="RoleIdsConverter"/> - identical reasoning.</summary>
    public static readonly ValueConverter<IReadOnlyCollection<PermissionId>, string> PermissionIdsConverter = new(
        v => JsonSerializer.Serialize(v.Select(p => p.Value), (JsonSerializerOptions?)null),
        v => (JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>())
            .Select(g => new PermissionId(g))
            .ToList());

    /// <summary><see cref="Role.PermissionIds"/>'s counterpart to <see cref="RoleIdsComparer"/> - identical reasoning.</summary>
    public static readonly ValueComparer<IReadOnlyCollection<PermissionId>> PermissionIdsComparer = new(
        (a, b) => (a ?? new List<PermissionId>()).OrderBy(p => p.Value).SequenceEqual((b ?? new List<PermissionId>()).OrderBy(p => p.Value)),
        v => v.Aggregate(0, (hash, id) => HashCode.Combine(hash, id)),
        v => v.ToList());

    /// <summary><see cref="OrganizationId"/> &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<OrganizationId, Guid> OrganizationIdConverter =
        new(id => id.Value, value => new OrganizationId(value));

    /// <summary><see cref="CompanyId"/> &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<CompanyId, Guid> CompanyIdConverter =
        new(id => id.Value, value => new CompanyId(value));

    /// <summary><see cref="BranchId"/> &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<BranchId, Guid> BranchIdConverter =
        new(id => id.Value, value => new BranchId(value));

    /// <summary><see cref="OrganizationName"/> &lt;-&gt; name text.</summary>
    public static readonly ValueConverter<OrganizationName, string> OrganizationNameConverter =
        new(v => v.Value, v => OrganizationName.Create(v));

    /// <summary><see cref="CompanyName"/> &lt;-&gt; name text.</summary>
    public static readonly ValueConverter<CompanyName, string> CompanyNameConverter =
        new(v => v.Value, v => CompanyName.Create(v));

    /// <summary><see cref="BranchName"/> &lt;-&gt; name text.</summary>
    public static readonly ValueConverter<BranchName, string> BranchNameConverter =
        new(v => v.Value, v => BranchName.Create(v));

    /// <summary><see cref="TaxId"/> &lt;-&gt; nullable text.</summary>
    public static readonly ValueConverter<TaxId?, string?> TaxIdConverter =
        new(v => v == null ? null : v.Value, v => v == null ? null : TaxId.Create(v));

    /// <summary>
    /// <see cref="Branch.Address"/> &lt;-&gt; a nullable JSON object column. A
    /// converter rather than an EF Core owned type deliberately: owned-type
    /// navigations cannot be bound through an aggregate's constructor (EF
    /// Core's constructor-binding only handles scalar/converted properties),
    /// which would break the "every persisted field explicit in the private
    /// constructor" convention this solution applies to every aggregate - see
    /// <c>Organization</c>'s identical constructor doc comment. A single JSON
    /// column keeps <see cref="Address"/> a plain converted property like
    /// every other value object here.
    /// </summary>
    public static readonly ValueConverter<Address?, string?> AddressConverter = new(
        v => v == null ? null : JsonSerializer.Serialize(new AddressJson(v.Street, v.City, v.State, v.PostalCode, v.Country), (JsonSerializerOptions?)null),
        v => v == null ? null : Deserialize(v));

    private static Address Deserialize(string json)
    {
        var dto = JsonSerializer.Deserialize<AddressJson>(json, (JsonSerializerOptions?)null)!;
        return Address.Create(dto.Street, dto.City, dto.State, dto.PostalCode, dto.Country);
    }

    private sealed record AddressJson(string Street, string City, string State, string PostalCode, string Country);

    /// <summary>
    /// <see cref="Organizations.Organization.CompanyIds"/> &lt;-&gt; a JSON array
    /// of company id GUIDs - identical reasoning to <see cref="RoleIdsConverter"/>.
    /// </summary>
    public static readonly ValueConverter<IReadOnlyCollection<CompanyId>, string> CompanyIdsConverter = new(
        v => JsonSerializer.Serialize(v.Select(c => c.Value), (JsonSerializerOptions?)null),
        v => (JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>())
            .Select(g => new CompanyId(g))
            .ToList());

    /// <summary><see cref="Organizations.Organization.CompanyIds"/>'s counterpart to <see cref="RoleIdsComparer"/> - identical reasoning.</summary>
    public static readonly ValueComparer<IReadOnlyCollection<CompanyId>> CompanyIdsComparer = new(
        (a, b) => (a ?? new List<CompanyId>()).OrderBy(c => c.Value).SequenceEqual((b ?? new List<CompanyId>()).OrderBy(c => c.Value)),
        v => v.Aggregate(0, (hash, id) => HashCode.Combine(hash, id)),
        v => v.ToList());

    /// <summary>
    /// <see cref="Companies.Company.BranchIds"/> &lt;-&gt; a JSON array of branch
    /// id GUIDs - identical reasoning to <see cref="RoleIdsConverter"/>.
    /// </summary>
    public static readonly ValueConverter<IReadOnlyCollection<BranchId>, string> BranchIdsConverter = new(
        v => JsonSerializer.Serialize(v.Select(b => b.Value), (JsonSerializerOptions?)null),
        v => (JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>())
            .Select(g => new BranchId(g))
            .ToList());

    /// <summary><see cref="Companies.Company.BranchIds"/>'s counterpart to <see cref="RoleIdsComparer"/> - identical reasoning.</summary>
    public static readonly ValueComparer<IReadOnlyCollection<BranchId>> BranchIdsComparer = new(
        (a, b) => (a ?? new List<BranchId>()).OrderBy(b => b.Value).SequenceEqual((b ?? new List<BranchId>()).OrderBy(b => b.Value)),
        v => v.Aggregate(0, (hash, id) => HashCode.Combine(hash, id)),
        v => v.ToList());
}
