namespace Clovent.Modules.Identity.Domain.Entities;

public sealed class Branch
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid CompanyId { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public bool IsActive { get; private set; } = true;

    public DateTime CreatedOn { get; private set; } = DateTime.UtcNow;

    private Branch()
    {
    }

    public Branch(Guid companyId, string code, string name)
    {
        CompanyId = companyId;
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
