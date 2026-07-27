using Clovent.Modules.Identity.Application.Authentication.Requests;
using Clovent.Modules.Identity.Application.Authentication.Responses;

namespace Clovent.Modules.Identity.Application.Authentication.Services;

public interface IAuthenticationService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
}
