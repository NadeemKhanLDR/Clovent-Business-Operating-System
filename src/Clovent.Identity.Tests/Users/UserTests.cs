using Clovent.Identity;
using Clovent.Identity.Roles;
using Clovent.Identity.Users;
using Clovent.Identity.Users.Events;
using Clovent.Identity.Users.ValueObjects;
using Xunit;

namespace Clovent.Identity.Tests.Users;

public class UserTests
{
    private static User CreateUser() =>
        User.Create(
            Email.Create("ada@example.com"),
            UserName.Create("ada"),
            DisplayName.Create("Ada Lovelace"));

    [Fact]
    public void Create_StartsPendingActivation_AndRaisesUserCreated()
    {
        var user = CreateUser();

        Assert.Equal(UserStatus.PendingActivation, user.Status);
        var raised = Assert.Single(user.DomainEvents);
        Assert.IsType<UserCreated>(raised);
    }

    [Fact]
    public void Activate_FromPendingActivation_Succeeds()
    {
        var user = CreateUser();
        user.ClearDomainEvents();

        user.Activate();

        Assert.Equal(UserStatus.Active, user.Status);
        Assert.IsType<UserActivated>(Assert.Single(user.DomainEvents));
    }

    [Fact]
    public void Activate_WhenAlreadyActive_Throws()
    {
        var user = CreateUser();
        user.Activate();

        var ex = Assert.Throws<IdentityDomainException>(() => user.Activate());
        Assert.Contains(user.Id.ToString(), ex.Message);
    }

    [Fact]
    public void Activate_WhenLocked_ThrowsAndRequiresUnlockFirst()
    {
        var user = CreateUser();
        user.Activate();
        user.Lock();

        Assert.Throws<IdentityDomainException>(() => user.Activate());
    }

    [Fact]
    public void Deactivate_FromActive_Succeeds()
    {
        var user = CreateUser();
        user.Activate();
        user.ClearDomainEvents();

        user.Deactivate();

        Assert.Equal(UserStatus.Inactive, user.Status);
        Assert.IsType<UserDeactivated>(Assert.Single(user.DomainEvents));
    }

    [Fact]
    public void Deactivate_WhenNotActive_Throws()
    {
        var user = CreateUser();

        Assert.Throws<IdentityDomainException>(() => user.Deactivate());
    }

    [Fact]
    public void Lock_FromActive_Succeeds()
    {
        var user = CreateUser();
        user.Activate();
        user.ClearDomainEvents();

        user.Lock();

        Assert.Equal(UserStatus.Locked, user.Status);
        Assert.IsType<UserLocked>(Assert.Single(user.DomainEvents));
    }

    [Fact]
    public void Lock_WhenNotActive_Throws()
    {
        var user = CreateUser();

        Assert.Throws<IdentityDomainException>(() => user.Lock());
    }

    [Fact]
    public void Unlock_FromLocked_ReturnsToActive()
    {
        var user = CreateUser();
        user.Activate();
        user.Lock();
        user.ClearDomainEvents();

        user.Unlock();

        Assert.Equal(UserStatus.Active, user.Status);
        Assert.IsType<UserUnlocked>(Assert.Single(user.DomainEvents));
    }

    [Fact]
    public void Unlock_WhenNotLocked_Throws()
    {
        var user = CreateUser();

        Assert.Throws<IdentityDomainException>(() => user.Unlock());
    }

    [Fact]
    public void AssignRole_NewRole_Succeeds()
    {
        var user = CreateUser();
        var roleId = RoleId.New();

        user.AssignRole(roleId);

        Assert.Contains(roleId, user.RoleIds);
        Assert.IsType<UserRoleAssigned>(user.DomainEvents.Last());
    }

    [Fact]
    public void AssignRole_AlreadyAssigned_Throws()
    {
        var user = CreateUser();
        var roleId = RoleId.New();
        user.AssignRole(roleId);

        Assert.Throws<IdentityDomainException>(() => user.AssignRole(roleId));
    }

    [Fact]
    public void RemoveRole_Assigned_Succeeds()
    {
        var user = CreateUser();
        var roleId = RoleId.New();
        user.AssignRole(roleId);

        user.RemoveRole(roleId);

        Assert.DoesNotContain(roleId, user.RoleIds);
        Assert.IsType<UserRoleRemoved>(user.DomainEvents.Last());
    }

    [Fact]
    public void RemoveRole_NotAssigned_Throws()
    {
        var user = CreateUser();

        Assert.Throws<IdentityDomainException>(() => user.RemoveRole(RoleId.New()));
    }

    [Fact]
    public void ChangeDisplayName_DifferentValue_UpdatesAndRaisesEvent()
    {
        var user = CreateUser();
        user.ClearDomainEvents();
        var newName = DisplayName.Create("A. Lovelace");

        user.ChangeDisplayName(newName);

        Assert.Equal(newName, user.DisplayName);
        Assert.IsType<UserDisplayNameChanged>(Assert.Single(user.DomainEvents));
    }

    [Fact]
    public void ChangeDisplayName_SameValue_IsNoOp()
    {
        var user = CreateUser();
        user.ClearDomainEvents();

        user.ChangeDisplayName(DisplayName.Create("Ada Lovelace"));

        Assert.Empty(user.DomainEvents);
    }
}
