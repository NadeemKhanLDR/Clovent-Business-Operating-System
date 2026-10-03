using Clovent.Authentication.Application;
using Clovent.Authentication.Credentials;
using Clovent.Authentication.Infrastructure.Persistence;
using Clovent.Authentication.Infrastructure.Security;
using Clovent.Identity.Branches;
using Clovent.Identity.Companies;
using Clovent.Identity.Infrastructure.Persistence;
using Clovent.Identity.Permissions;
using Clovent.Identity.Permissions.ValueObjects;
using Clovent.Identity.Roles;
using Clovent.Identity.Roles.ValueObjects;
using Clovent.Identity.Users;
using Clovent.Identity.Users.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Clovent.Desktop.Commissioning.Services;

/// <summary>
/// Data transfer request for provisioning the foundational first system administrator account.
/// </summary>
public sealed class FirstAdminProvisioningRequest
{
    /// <summary>
    /// Desired unique login username (e.g. "admin" or "sysadmin").
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Administrator full display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Administrator email address for notifications and account recovery.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Plaintext password meeting the enterprise complexity policy.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Confirmation password for cross-verification.
    /// </summary>
    public string? ConfirmPassword { get; set; }

    /// <summary>
    /// Optional company assignment ID.
    /// </summary>
    public Guid? CompanyId { get; set; }

    /// <summary>
    /// Optional branch assignment ID.
    /// </summary>
    public Guid? BranchId { get; set; }
}

/// <summary>
/// Contract for first administrator account provisioning.
/// </summary>
public interface IFirstAdminProvisioningService
{
    /// <summary>
    /// Checks whether any active administrator already exists in the system.
    /// Returns false if an active administrator exists (disabling bootstrap provisioning).
    /// </summary>
    Task<bool> CanProvisionFirstAdminAsync(IdentityDbContext identityDbContext, CancellationToken ct = default);

    /// <summary>
    /// Enforces the one-time bootstrap rule, validates complexity, and creates the administrator account.
    /// </summary>
    Task<(bool Success, string Message, Guid? UserId)> ProvisionFirstAdminAsync(
        IServiceProvider services,
        FirstAdminProvisioningRequest request,
        CancellationToken ct = default);
}

/// <summary>
/// Implements Requirements 11 and 12: First administrator account provisioning and bootstrap protection.
/// Ensures an administrator cannot be provisioned once one already exists, prevents backdoor passwords,
/// and assigns full administrative permissions without ever logging sensitive credentials.
/// </summary>
public sealed class FirstAdminProvisioningService(ILogger<FirstAdminProvisioningService>? logger = null)
    : IFirstAdminProvisioningService
{
    private const string DisabledProvisioningMessage = "Administrator account already exists. Self-provisioning is disabled.";

    private static readonly HashSet<string> AdminRoleNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Administrator",
        "Admin",
        "SuperAdmin"
    };

    private static readonly string[] MenuKeys =
    [
        "dashboard", "users", "roles", "organizations", "companies", "branches", "departments",
        "warehouses", "terminals", "fiscalyears", "currencies", "businesssettings",
        "categories", "brands", "units", "products", "variants", "barcodes", "prices",
        "warehousestocks", "stockadjustments", "stocktransfers", "inventorytransactions",
        "diningareas", "tables", "pos", "runningorders", "holdorders", "kitchentickets",
        "orderhistory", "endofday", "menuitems", "restaurantsetup", "paymentmethods",
        "activitylog", "appearance", "customers", "customerreceivables", "shifts",
        "attendance", "recommendationrules", "smartcombos", "quickordertemplates",
        "upsellperformance"
    ];

    private static readonly (string Feature, string[] Operations)[] FeatureOperations =
    [
        ("users", ["create", "edit", "activate", "deactivate", "resetpassword", "setpin", "unlock", "assignrole", "assigncompany", "assignbranch"]),
        ("roles", ["create", "edit", "assignpermission"]),
        ("organizations", ["create", "edit", "activate", "deactivate"]),
        ("companies", ["create", "edit", "activate", "deactivate"]),
        ("branches", ["create", "edit", "activate", "deactivate"]),
        ("departments", ["create", "edit", "activate", "deactivate"]),
        ("warehouses", ["create", "edit", "activate", "deactivate"]),
        ("terminals", ["create", "edit", "activate", "deactivate"]),
        ("fiscalyears", ["create", "edit", "deactivate"]),
        ("currencies", ["create", "activate", "deactivate"]),
        ("businesssettings", ["edit"]),
        ("categories", ["create", "edit", "activate", "deactivate"]),
        ("brands", ["create", "edit", "activate", "deactivate"]),
        ("units", ["create", "edit", "activate", "deactivate"]),
        ("products", ["create", "edit", "activate", "deactivate"]),
        ("variants", ["create", "edit", "activate", "deactivate"]),
        ("barcodes", ["create", "activate", "deactivate", "markprimary"]),
        ("prices", ["create", "edit", "activate", "deactivate"]),
        ("warehousestocks", ["create", "edit", "receive", "issue", "reserve", "release"]),
        ("stockadjustments", ["create", "apply", "cancel"]),
        ("stocktransfers", ["create", "complete", "cancel"]),
        ("inventorytransactions", ["view"]),
        ("diningareas", ["create", "edit", "activate", "deactivate"]),
        ("tables", ["create", "edit", "activate", "deactivate", "occupy", "vacate", "reserve", "outofservice", "returntoservice"]),
        ("pos", ["create", "hold", "resume", "void", "cancel", "reopen", "sendtokitchen", "complete", "pay",
            "transfertable", "mergetables", "splitbill", "notes", "discount", "servicecharge", "additem", "editline",
            "priceoverride", "creditsale", "exceedcreditlimit",
            "smartinsights", "quickorders", "restaurantpulse", "rushmode", "printlastreceipt"]),
        ("kitchentickets", ["start", "markready", "serve", "cancel"]),
        ("endofday", ["view", "close"]),
        ("menuitems", ["create", "edit", "activate", "deactivate", "createcategory"]),
        ("restaurantsetup", ["edit"]),
        ("paymentmethods", ["create", "edit", "activate", "deactivate"]),
        ("activitylog", ["view"]),
        ("appearance", ["create", "edit", "delete"]),
        ("customers", ["create", "edit", "activate", "deactivate", "viewledger", "payment"]),
        ("customerreceivables", ["view", "export"]),
        ("shifts", ["open", "close", "cashmovement", "history", "viewdetails", "overridevariance"]),
        ("attendance", ["punchself", "viewself", "manage"]),
        ("recommendationrules", ["create", "edit", "deactivate"]),
        ("upsellperformance", ["view"]),
        ("smartcombos", ["analyze", "create", "dismiss"]),
        ("quickordertemplates", ["create", "edit", "deactivate"])
    ];

    /// <inheritdoc/>
    public async Task<bool> CanProvisionFirstAdminAsync(IdentityDbContext identityDbContext, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(identityDbContext);

        try
        {
            var roles = await identityDbContext.Roles.AsNoTracking().ToListAsync(ct).ConfigureAwait(false);
            var adminRoleIds = roles
                .Where(r => AdminRoleNames.Contains(r.Name.Value))
                .Select(r => r.Id)
                .ToHashSet();

            var activeUsers = await identityDbContext.Users
                .AsNoTracking()
                .Where(u => u.Status == UserStatus.Active)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            if (adminRoleIds.Count > 0)
            {
                bool anyAdminExists = activeUsers.Any(u =>
                    u.RoleIds.Any(r => adminRoleIds.Contains(r)) ||
                    string.Equals(u.UserName.Value, "admin", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(u.UserName.Value, "administrator", StringComparison.OrdinalIgnoreCase));

                return !anyAdminExists;
            }

            // If no admin role exists yet, check if any active user exists with username "admin" or "administrator"
            bool hasAdminUser = activeUsers.Any(u =>
                string.Equals(u.UserName.Value, "admin", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(u.UserName.Value, "administrator", StringComparison.OrdinalIgnoreCase));

            return !hasAdminUser;
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Failed to query IdentityDbContext for administrator check; allowing provisioning check.");
            // If the schema or tables do not exist yet, no admin exists
            return true;
        }
    }

    /// <inheritdoc/>
    public async Task<(bool Success, string Message, Guid? UserId)> ProvisionFirstAdminAsync(
        IServiceProvider services,
        FirstAdminProvisioningRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(request);

        using var scope = services.CreateScope();
        var identityDbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        // 1. Enforce one-time bootstrap rule: If CanProvisionFirstAdminAsync is false, IMMEDIATELY FAIL
        bool canProvision = await CanProvisionFirstAdminAsync(identityDbContext, ct).ConfigureAwait(false);
        if (!canProvision)
        {
            logger?.LogWarning("Attempted first administrator provisioning when an administrator already exists.");
            return (false, DisabledProvisioningMessage, null);
        }

        // 2. Validate Password Policy
        var passwordValidationError = ValidatePasswordPolicy(request.Password, request.ConfirmPassword, request.UserName);
        if (passwordValidationError != null)
        {
            return (false, passwordValidationError, null);
        }

        // 3. Validate Username
        if (string.IsNullOrWhiteSpace(request.UserName))
        {
            return (false, "Username is required.", null);
        }

        UserName userNameVo;
        try
        {
            userNameVo = UserName.Create(request.UserName);
        }
        catch (Exception ex)
        {
            return (false, ex.Message, null);
        }

        bool userNameTaken = await identityDbContext.Users
            .AnyAsync(u => u.UserName == userNameVo, ct)
            .ConfigureAwait(false);
        if (userNameTaken)
        {
            return (false, $"Username '{request.UserName}' is already taken.", null);
        }

        // 4. Validate Email
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return (false, "Email address is required.", null);
        }

        Email emailVo;
        try
        {
            emailVo = Email.Create(request.Email);
        }
        catch (Exception ex)
        {
            return (false, ex.Message, null);
        }

        bool emailTaken = await identityDbContext.Users
            .AnyAsync(u => u.Email == emailVo, ct)
            .ConfigureAwait(false);
        if (emailTaken)
        {
            return (false, $"Email '{request.Email}' is already registered.", null);
        }

        // 5. Validate Display Name
        DisplayName displayNameVo;
        try
        {
            var displayName = string.IsNullOrWhiteSpace(request.DisplayName) ? request.UserName : request.DisplayName;
            displayNameVo = DisplayName.Create(displayName);
        }
        catch (Exception ex)
        {
            return (false, ex.Message, null);
        }

        // 6. Ensure Administrator Role & Standard Permissions
        var adminRoleName = RoleName.Create("Administrator");
        var role = await identityDbContext.Roles.FirstOrDefaultAsync(r => r.Name == adminRoleName, ct).ConfigureAwait(false);
        bool isNewRole = false;
        if (role == null)
        {
            role = Role.Create(adminRoleName);
            isNewRole = true;
        }

        var allPermissionCodes = BuildStandardPermissionCodes();
        var existingPermissions = await identityDbContext.Permissions.ToListAsync(ct).ConfigureAwait(false);
        var existingByCode = existingPermissions.ToDictionary(p => p.Code.Value, StringComparer.OrdinalIgnoreCase);

        foreach (var code in allPermissionCodes)
        {
            if (!existingByCode.TryGetValue(code, out var perm))
            {
                var permCode = PermissionCode.Create(code);
                perm = Permission.Create(permCode, $"Grants {code}.");
                await identityDbContext.Permissions.AddAsync(perm, ct).ConfigureAwait(false);
                existingByCode[code] = perm;
            }

            if (!role.PermissionIds.Contains(perm.Id))
            {
                role.AddPermission(perm.Id);
            }
        }

        if (isNewRole)
        {
            await identityDbContext.Roles.AddAsync(role, ct).ConfigureAwait(false);
        }

        // 7. Create User aggregate in IdentityDbContext
        var user = User.Create(emailVo, userNameVo, displayNameVo);
        user.Activate();
        user.AssignRole(role.Id);

        if (request.CompanyId.HasValue && request.CompanyId.Value != Guid.Empty)
        {
            user.AssignCompany(new CompanyId(request.CompanyId.Value));
        }

        if (request.BranchId.HasValue && request.BranchId.Value != Guid.Empty)
        {
            user.AssignBranch(new BranchId(request.BranchId.Value));
        }

        await identityDbContext.Users.AddAsync(user, ct).ConfigureAwait(false);
        await identityDbContext.SaveChangesAsync(ct).ConfigureAwait(false);

        // 8. Create UserCredentials aggregate in AuthenticationDbContext
        var authDbContext = scope.ServiceProvider.GetRequiredService<AuthenticationDbContext>();
        var passwordHasher = scope.ServiceProvider.GetService<IPasswordHasher>() ?? new Pbkdf2PasswordHasher();
        var timeProvider = scope.ServiceProvider.GetService<TimeProvider>() ?? TimeProvider.System;

        var now = timeProvider.GetUtcNow();
        var credentials = UserCredentials.Create(user.Id, now);
        var hashedPassword = passwordHasher.Hash(request.Password);
        credentials.SetPassword(PasswordHash.Create(hashedPassword), now);

        await authDbContext.UserCredentials.AddAsync(credentials, ct).ConfigureAwait(false);
        await authDbContext.SaveChangesAsync(ct).ConfigureAwait(false);

        // Security requirement: Never log passwords
        logger?.LogInformation(
            "Successfully provisioned first system administrator user '{UserName}' with UserId '{UserId}'.",
            userNameVo.Value,
            user.Id.Value);

        return (true, "First administrator account successfully provisioned.", user.Id.Value);
    }

    /// <summary>
    /// Validates that a password satisfies all enterprise security policies:
    /// - Minimum 8 characters
    /// - At least 1 uppercase letter
    /// - At least 1 lowercase letter
    /// - At least 1 digit
    /// - At least 1 special character
    /// - Not equal to 'Admin123!' or username
    /// - Matches confirm password (if supplied)
    /// </summary>
    public static string? ValidatePasswordPolicy(string password, string? confirmPassword, string? userName)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return "Password is required.";
        }

        if (password.Length < 8)
        {
            return "Password must be at least 8 characters long.";
        }

        if (!password.Any(char.IsUpper))
        {
            return "Password must contain at least one uppercase letter.";
        }

        if (!password.Any(char.IsLower))
        {
            return "Password must contain at least one lowercase letter.";
        }

        if (!password.Any(char.IsDigit))
        {
            return "Password must contain at least one digit.";
        }

        if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
        {
            return "Password must contain at least one special character.";
        }

        if (string.Equals(password, "Admin123!", StringComparison.OrdinalIgnoreCase))
        {
            return "Prohibited password: 'Admin123!' is a known insecure default.";
        }

        if (!string.IsNullOrWhiteSpace(userName) &&
            string.Equals(password, userName, StringComparison.OrdinalIgnoreCase))
        {
            return "Password cannot be identical to the username.";
        }

        if (!string.IsNullOrEmpty(confirmPassword) &&
            !string.Equals(password, confirmPassword, StringComparison.Ordinal))
        {
            return "Passwords do not match.";
        }

        return null;
    }

    private static List<string> BuildStandardPermissionCodes()
    {
        var codes = new List<string>();

        foreach (var key in MenuKeys)
        {
            codes.Add($"menu.{key}");
        }

        foreach (var (feature, operations) in FeatureOperations)
        {
            foreach (var operation in operations)
            {
                codes.Add($"feature.{feature}.{operation}");
            }
        }

        return codes;
    }
}
