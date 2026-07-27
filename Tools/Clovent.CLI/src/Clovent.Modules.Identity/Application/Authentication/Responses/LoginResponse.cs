namespace Clovent.Modules.Identity.Application.Authentication.Responses;

public sealed class LoginResponse
{
    public bool Success { get; init; }

    public string Message { get; init; } = string.Empty;

    public Guid? UserId { get; init; }
}
