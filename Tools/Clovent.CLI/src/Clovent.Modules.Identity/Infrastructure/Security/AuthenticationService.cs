using Clovent.Modules.Identity.Application.Authentication.Requests;
using Clovent.Modules.Identity.Application.Authentication.Responses;
using Clovent.Modules.Identity.Application.Authentication.Services;

namespace Clovent.Modules.Identity.Infrastructure.Security;

public sealed class AuthenticationService : IAuthenticationService
{
    public Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        return Task.FromResult(new LoginResponse
        {
            Success = false,
            Message = "Authentication service not implemented."
        });
    }
}
