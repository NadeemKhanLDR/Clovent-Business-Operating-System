using Clovent.Desktop.Commissioning.Security;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.Authorization;

/// <summary>
/// Verifies whether the active session holder possesses administrative privileges.
/// Enforces Requirements 12 and 13 to prevent unauthorized privilege escalation
/// in database configuration and software license activation.
/// </summary>
public static class AdministrativePrivilegeChecker
{
    /// <summary>
    /// Checks whether an authenticated user session is currently active in the application.
    /// </summary>
    public static bool IsSessionActive(IServiceProvider? services = null)
    {
        services ??= Program.Services;
        if (services == null)
        {
            return false;
        }

        var session = services.GetService<ICurrentSession>();
        return session != null && session.IsAuthenticated && session.UserId.HasValue;
    }

    /// <summary>
    /// Verifies whether the current user has administrative privileges.
    /// If no user session is active (e.g. initial setup / pre-login bootstrapping), checks for Windows Administrator elevation.
    /// If a session is active, verifies administrator role or administrative permissions.
    /// </summary>
    public static bool HasAdministrativePrivileges(IServiceProvider? services = null)
    {
        services ??= Program.Services;
        if (services == null)
        {
            return WindowsCommissioningSecurity.IsRunningAsAdministrator();
        }

        var session = services.GetService<ICurrentSession>();
        if (session == null || !session.IsAuthenticated || !session.UserId.HasValue)
        {
            // Pre-login (cold-start / commissioning): require Windows Administrator elevation
            return WindowsCommissioningSecurity.IsRunningAsAdministrator();
        }

        var userId = session.UserId.Value;
        var userName = session.UserName;

        // 1. Fast-path check on system administrator username
        if (!string.IsNullOrWhiteSpace(userName) &&
            (string.Equals(userName, "admin", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(userName, "administrator", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        // 2. Evaluated check via Identity AuthorizationService
        try
        {
            using var scope = services.CreateScope();
            var authService = scope.ServiceProvider.GetService<IAuthorizationService>();
            if (authService != null)
            {
                if (authService.HasRoleAsync(userId, "Administrator").GetAwaiter().GetResult() ||
                    authService.HasRoleAsync(userId, "Admin").GetAwaiter().GetResult() ||
                    authService.HasRoleAsync(userId, "SuperAdmin").GetAwaiter().GetResult())
                {
                    return true;
                }

                if (authService.HasPermissionAsync(userId, "feature.businesssettings.edit").GetAwaiter().GetResult() ||
                    authService.HasPermissionAsync(userId, "feature.users.create").GetAwaiter().GetResult())
                {
                    return true;
                }
            }
        }
        catch
        {
            // In case of transient evaluation failures, default to denying elevation
            return false;
        }

        return false;
    }
}
