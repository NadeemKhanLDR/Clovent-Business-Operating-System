namespace Clovent.Modules.Identity.Application.Authentication.Services;

public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password,string hash);
}
