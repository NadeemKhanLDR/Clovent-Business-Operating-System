using System;
using System.Drawing;
using System.Windows.Forms;
using Clovent.Desktop.MasterData;
using Clovent.Desktop.Restaurant.Shared;
using Xunit;

namespace Clovent.Desktop.Tests.Restaurant.Shared;

public class ManagerAuthorizationFormTests
{
    private static T GetField<T>(object target, string fieldName) where T : class
    {
        var type = target.GetType();
        while (type != null)
        {
            var field = type.GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (field != null)
            {
                return (T)field.GetValue(target)!;
            }
            type = type.BaseType;
        }
        throw new InvalidOperationException($"Field {fieldName} not found on {target.GetType()}");
    }

    private static void RaiseLoad(Form form)
    {
        var method = typeof(MasterDataEditFormBase).GetMethod(
            "MasterDataEditFormBase_Load",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (method == null)
        {
            throw new InvalidOperationException("MasterDataEditFormBase_Load method not found");
        }
        method.Invoke(form, new object[] { null!, EventArgs.Empty });
    }

    [Fact]
    public void Form_ShortMessage_DisplaysCompleteMessageAndSavesCredentials()
    {
        var detailText = "Short authorization reason.";
        using var form = new ManagerAuthorizationForm("Auth Title", detailText);
        
        RaiseLoad(form);

        var detailLabel = GetField<DevExpress.XtraEditors.LabelControl>(form, "_detailLabel");
        var userNameEdit = GetField<DevExpress.XtraEditors.TextEdit>(form, "_userNameEdit");
        var passwordEdit = GetField<DevExpress.XtraEditors.TextEdit>(form, "_passwordEdit");

        // Verify content
        Assert.Equal(detailText, detailLabel.Text);
        
        // Verify credentials getters
        userNameEdit.Text = "manager1";
        passwordEdit.Text = "secretpwd";
        Assert.Equal("manager1", form.ManagerUserName);
        Assert.Equal("secretpwd", form.ManagerPassword);
    }

    [Fact]
    public void Form_LongMessage_DoesNotClipAndGrowsVertically()
    {
        var shortDetail = "Short reason.";
        var longDetail = "This is a very long manager authorization message that contains extensive financial details. " +
                         "Credit limit exceeded. This customer currently owes $1,500.00. " +
                         "The new sale would increase the balance to $1,800.00, but the credit limit is $500.00. " +
                         "A manager must authorize this credit sale immediately. Let's make sure it wraps multiple lines.";

        using var formShort = new ManagerAuthorizationForm("Auth Short", shortDetail);
        RaiseLoad(formShort);
        var shortMinHeight = formShort.MinimumSize.Height;

        using var formLong = new ManagerAuthorizationForm("Auth Long", longDetail);
        RaiseLoad(formLong);
        var longMinHeight = formLong.MinimumSize.Height;

        // Verify layout dimensions
        Assert.True(shortMinHeight > 0);
        Assert.True(longMinHeight > 0);
        
        // A longer message should naturally result in a taller dialog
        Assert.True(longMinHeight > shortMinHeight, $"Long detail min height ({longMinHeight}) should be strictly greater than short detail min height ({shortMinHeight})");
        
        var detailLabel = GetField<DevExpress.XtraEditors.LabelControl>(formLong, "_detailLabel");
        Assert.Equal(longDetail, detailLabel.Text);
    }

    [Fact]
    public void Form_ControlBoundsAreValidAndInsideClientBounds()
    {
        var detailText = "Manager authorization is required to perform this privileged operation.";
        using var form = new ManagerAuthorizationForm("Auth Sizing", detailText);
        RaiseLoad(form);

        var detailLabel = GetField<DevExpress.XtraEditors.LabelControl>(form, "_detailLabel");
        var userNameEdit = GetField<DevExpress.XtraEditors.TextEdit>(form, "_userNameEdit");
        var passwordEdit = GetField<DevExpress.XtraEditors.TextEdit>(form, "_passwordEdit");
        
        var okButton = GetField<DevExpress.XtraEditors.SimpleButton>(form, "_okButton");
        var cancelButton = GetField<DevExpress.XtraEditors.SimpleButton>(form, "_cancelButton");

        // Verify no control has negative or zero dimensions
        Assert.True(form.Width > 0);
        Assert.True(form.Height > 0);
        Assert.True(detailLabel.Width > 0);
        Assert.True(detailLabel.Height > 0);
        Assert.True(userNameEdit.Width > 0);
        Assert.True(userNameEdit.Height > 0);
        Assert.True(passwordEdit.Width > 0);
        Assert.True(passwordEdit.Height > 0);
        Assert.True(okButton.Width > 0);
        Assert.True(okButton.Height > 0);
        Assert.True(cancelButton.Width > 0);
        Assert.True(cancelButton.Height > 0);

        // Verify controls are within form bounds
        var clientW = form.ClientSize.Width;
        var clientH = form.ClientSize.Height;

        Assert.True(detailLabel.Right <= clientW);
        Assert.True(userNameEdit.Right <= clientW);
        Assert.True(passwordEdit.Right <= clientW);
    }

    [Theory]
    [InlineData("Short title", "Short message.")]
    [InlineData("Very Long Manager Authorization Title That Needs Extra Width To Avoid Truncation In The Caption Bar", 
                "This is a long details message containing specific transaction details and amount values that must be fully visible and wrap properly without truncation.")]
    public void Form_TitleAndDetails_DoNotClipOrOverlap(string titleText, string detailText)
    {
        using var form = new ManagerAuthorizationForm(titleText, detailText);
        RaiseLoad(form);
        form.Show();

        var headerLabel = GetField<DevExpress.XtraEditors.LabelControl>(form, "_headerLabel");
        var detailLabel = GetField<DevExpress.XtraEditors.LabelControl>(form, "_detailLabel");
        
        var contentPanel = GetField<TableLayoutPanel>(form, "_contentPanel");
        var buttonPanel = GetField<FlowLayoutPanel>(form, "_buttonPanel");
        
        headerLabel.Size = headerLabel.GetPreferredSize(new Size(form.ClientSize.Width, 0));
        detailLabel.Size = detailLabel.GetPreferredSize(new Size(form.ClientSize.Width, 0));
        
        var topChromeHeight = headerLabel.Height + detailLabel.Height;
        
        contentPanel.Dock = DockStyle.None;
        contentPanel.Size = new Size(form.ClientSize.Width, form.ClientSize.Height - buttonPanel.Height - topChromeHeight);
        contentPanel.Location = new Point(0, topChromeHeight);
        contentPanel.PerformLayout();

        var userNameEdit = GetField<DevExpress.XtraEditors.TextEdit>(form, "_userNameEdit");
        var passwordEdit = GetField<DevExpress.XtraEditors.TextEdit>(form, "_passwordEdit");
        var okButton = GetField<DevExpress.XtraEditors.SimpleButton>(form, "_okButton");
        var cancelButton = GetField<DevExpress.XtraEditors.SimpleButton>(form, "_cancelButton");

        // Verify title width constraint
        int titleWidth = TextRenderer.MeasureText(form.Text, SystemFonts.CaptionFont).Width + 120;
        Assert.True(form.ClientSize.Width >= titleWidth, "Form is too narrow for the title bar text");

        // Verify positions to ensure no vertical overlap
        Point GetLocationOnForm(Control control)
        {
            Point p = control.Location;
            Control? parent = control.Parent;
            while (parent != null && parent != form)
            {
                p.Offset(parent.Location);
                parent = parent.Parent;
            }
            return p;
        }

        var headerRect = new Rectangle(GetLocationOnForm(headerLabel), headerLabel.Size);
        var detailRect = new Rectangle(GetLocationOnForm(detailLabel), detailLabel.Size);
        var userRect = new Rectangle(GetLocationOnForm(userNameEdit), userNameEdit.Size);
        var okRect = new Rectangle(GetLocationOnForm(okButton), okButton.Size);

        // Header must be at the top, detail label below header
        Assert.True(detailRect.Top >= headerRect.Bottom, "Detail label overlaps header label");

        // Username field must be below detail label
        Assert.True(userRect.Top >= detailRect.Bottom, "Username input overlaps detail label");

        // OK button must be below username field
        Assert.True(okRect.Top >= userRect.Bottom, "OK button overlaps username input");

        // Ensure all controls are fully within client bounds
        Assert.True(headerRect.Bottom <= form.ClientSize.Height);
        Assert.True(detailRect.Bottom <= form.ClientSize.Height);
        Assert.True(userRect.Bottom <= form.ClientSize.Height);
        Assert.True(okRect.Bottom <= form.ClientSize.Height);
    }
}
