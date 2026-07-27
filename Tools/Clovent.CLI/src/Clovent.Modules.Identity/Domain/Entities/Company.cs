namespace Clovent.Modules.Identity.Domain.Entities;

public sealed class Company
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public bool IsActive { get; private set; } = true;

    public DateTime CreatedOn { get; private set; } = DateTime.UtcNow;

    private Company()
    {
    }

    public Company(string code, string name)
    {
        Code = code;
        Name = name;
    }

    public void Update(string code, string name)
    {
        Code = code;
        Name = name;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}
