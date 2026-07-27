namespace Clovent.Modules.Identity.Application.Authentication.Requests;

public sealed record LoginRequest(
    string UserName,
    string Password);
