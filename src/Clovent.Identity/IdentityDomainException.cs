using Clovent.Domain;
using Clovent.Identity.Branches;
using Clovent.Identity.Companies;
using Clovent.Identity.Organizations;
using Clovent.Identity.Permissions;
using Clovent.Identity.Roles;
using Clovent.Identity.Users;

namespace Clovent.Identity;

/// <summary>
/// Raised when an Identity Domain aggregate operation would violate one of
/// its invariants. A single type with named factory methods (one per rule)
/// rather than a subclass per rule - callers that only need to react to
/// "an identity invariant was violated" can catch this one type, while the
/// message still names exactly which rule and which aggregate.
/// </summary>
public sealed class IdentityDomainException : DomainException
{
    private IdentityDomainException(string message) : base(message)
    {
    }

    /// <summary>A user Activate() was attempted while already <see cref="UserStatus.Active"/>.</summary>
    public static IdentityDomainException UserAlreadyActive(UserId userId) =>
        new($"User '{userId}' is already active.");

    /// <summary>A user Deactivate()/Lock() was attempted while not <see cref="UserStatus.Active"/>.</summary>
    public static IdentityDomainException UserNotActive(UserId userId) =>
        new($"User '{userId}' is not active.");

    /// <summary>A user Unlock() was attempted while not <see cref="UserStatus.Locked"/>.</summary>
    public static IdentityDomainException UserNotLocked(UserId userId) =>
        new($"User '{userId}' is not locked.");

    /// <summary>A user Activate() was attempted while <see cref="UserStatus.Locked"/>.</summary>
    public static IdentityDomainException UserMustBeUnlockedBeforeActivation(UserId userId) =>
        new($"User '{userId}' is locked and must be unlocked before it can be activated.");

    /// <summary>A role was assigned to a user it is already assigned to.</summary>
    public static IdentityDomainException RoleAlreadyAssignedToUser(UserId userId, RoleId roleId) =>
        new($"Role '{roleId}' is already assigned to user '{userId}'.");

    /// <summary>A role was removed from a user it is not assigned to.</summary>
    public static IdentityDomainException RoleNotAssignedToUser(UserId userId, RoleId roleId) =>
        new($"Role '{roleId}' is not assigned to user '{userId}'.");

    /// <summary>A permission was granted to a role it is already granted to.</summary>
    public static IdentityDomainException PermissionAlreadyAssignedToRole(RoleId roleId, PermissionId permissionId) =>
        new($"Permission '{permissionId}' is already assigned to role '{roleId}'.");

    /// <summary>A permission was revoked from a role it is not granted to.</summary>
    public static IdentityDomainException PermissionNotAssignedToRole(RoleId roleId, PermissionId permissionId) =>
        new($"Permission '{permissionId}' is not assigned to role '{roleId}'.");

    /// <summary>A company was added to an organization it already belongs to.</summary>
    public static IdentityDomainException CompanyAlreadyBelongsToOrganization(OrganizationId organizationId, CompanyId companyId) =>
        new($"Company '{companyId}' already belongs to organization '{organizationId}'.");

    /// <summary>A company was removed from an organization it does not belong to.</summary>
    public static IdentityDomainException CompanyDoesNotBelongToOrganization(OrganizationId organizationId, CompanyId companyId) =>
        new($"Company '{companyId}' does not belong to organization '{organizationId}'.");

    /// <summary>A branch was added to a company it already belongs to.</summary>
    public static IdentityDomainException BranchAlreadyBelongsToCompany(CompanyId companyId, BranchId branchId) =>
        new($"Branch '{branchId}' already belongs to company '{companyId}'.");

    /// <summary>A branch was removed from a company it does not belong to.</summary>
    public static IdentityDomainException BranchDoesNotBelongToCompany(CompanyId companyId, BranchId branchId) =>
        new($"Branch '{branchId}' does not belong to company '{companyId}'.");

    /// <summary>An organization Activate() was attempted while already <see cref="Organizations.OrganizationStatus.Active"/>.</summary>
    public static IdentityDomainException OrganizationAlreadyActive(OrganizationId organizationId) =>
        new($"Organization '{organizationId}' is already active.");

    /// <summary>An organization Deactivate() was attempted while not <see cref="Organizations.OrganizationStatus.Active"/>.</summary>
    public static IdentityDomainException OrganizationNotActive(OrganizationId organizationId) =>
        new($"Organization '{organizationId}' is not active.");

    /// <summary>A company Activate() was attempted while already <see cref="Companies.CompanyStatus.Active"/>.</summary>
    public static IdentityDomainException CompanyAlreadyActive(CompanyId companyId) =>
        new($"Company '{companyId}' is already active.");

    /// <summary>A company Deactivate() was attempted while not <see cref="Companies.CompanyStatus.Active"/>.</summary>
    public static IdentityDomainException CompanyNotActive(CompanyId companyId) =>
        new($"Company '{companyId}' is not active.");

    /// <summary>A branch Activate() was attempted while already <see cref="Branches.BranchStatus.Active"/>.</summary>
    public static IdentityDomainException BranchAlreadyActive(BranchId branchId) =>
        new($"Branch '{branchId}' is already active.");

    /// <summary>A branch Deactivate() was attempted while not <see cref="Branches.BranchStatus.Active"/>.</summary>
    public static IdentityDomainException BranchNotActive(BranchId branchId) =>
        new($"Branch '{branchId}' is not active.");
}
