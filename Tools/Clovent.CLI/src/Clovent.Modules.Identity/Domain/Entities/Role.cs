namespace Clovent.Modules.Identity.Domain.Entities;

public sealed class Role
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid CompanyId { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public bool IsSystem { get; private set; }

    public bool IsActive { get; private set; } = true;

    public DateTime CreatedOn { get; private set; } = DateTime.UtcNow;

    private Role()
    {
    }

    public Role(
        Guid companyId,
        string code,
        string name,
        string description,
        bool isSystem = false)
    {
        CompanyId = companyId;
        Code = code;
        Name = name;
        Description = description;
        IsSystem = isSystem;
    }

    public void Update(string name, string description)
    {
        Name = name;
        Description = description;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}
