using Clovent.Modules.Identity.Application.Authentication.Services;

namespace Clovent.Modules.Identity.Infrastructure.Security;

public sealed class PasswordHasher : IPasswordHasher
{
    public string Hash(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool Verify(string password,string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password,hash);
    }
}
