using Clovent.Desktop.Notifications;
using Xunit;

namespace Clovent.Desktop.Tests.Notifications;

public class NotificationServiceTests
{
    [Fact]
    public void Add_InsertsMostRecentFirst()
    {
        var service = new NotificationService();

        service.Add("First", "message one");
        service.Add("Second", "message two");

        Assert.Equal("Second", service.Notifications[0].Title);
        Assert.Equal("First", service.Notifications[1].Title);
    }

    [Fact]
    public void Add_RaisesChanged()
    {
        var service = new NotificationService();
        var raised = false;
        service.Changed += (_, _) => raised = true;

        service.Add("Title", "Message");

        Assert.True(raised);
    }

    [Fact]
    public void Clear_RemovesEverythingAndRaisesChanged()
    {
        var service = new NotificationService();
        service.Add("Title", "Message");
        var raised = false;
        service.Changed += (_, _) => raised = true;

        service.Clear();

        Assert.Empty(service.Notifications);
        Assert.True(raised);
    }

    [Fact]
    public void Add_EmptyTitle_Throws()
    {
        var service = new NotificationService();

        Assert.Throws<ArgumentException>(() => service.Add("", "message"));
    }
}
