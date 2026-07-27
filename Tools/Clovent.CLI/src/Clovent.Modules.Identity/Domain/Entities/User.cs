namespace Clovent.Modules.Identity.Domain.Entities;

public sealed class User
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid CompanyId { get; private set; }

    public Guid BranchId { get; private set; }

    public string Username { get; private set; } = string.Empty;

    public string FirstName { get; private set; } = string.Empty;

    public string LastName { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public bool IsActive { get; private set; } = true;

    public bool IsLocked { get; private set; }

    public DateTime CreatedOn { get; private set; } = DateTime.UtcNow;

    private User()
    {
    }

    public User(
        Guid companyId,
        Guid branchId,
        string username,
        string firstName,
        string lastName,
        string email,
        string passwordHash)
    {
        CompanyId = companyId;
        BranchId = branchId;
        Username = username;
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        PasswordHash = passwordHash;
    }

    public void UpdateProfile(string firstName, string lastName, string email)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
    }

    public void ChangePassword(string passwordHash)
    {
        PasswordHash = passwordHash;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public void Lock() => IsLocked = true;

    public void Unlock() => IsLocked = false;
}
