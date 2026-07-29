namespace Clovent.Identity.Application.Authorization;

/// <summary>
/// The "Application policies" deliverable: a named, reusable rule combining
/// one or more permission codes a user must hold ALL of - the same shape as
/// ASP.NET Core's policy-based authorization, without the HTTP-specific
/// requirements/handler pipeline that framework builds around it, since
/// this is a desktop host with no request context to hang that on.
/// </summary>
public sealed record AuthorizationPolicy(string Name, IReadOnlyList<string> RequiredPermissionCodes);
