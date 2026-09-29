using System;
using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Clovent.Desktop.Authorization;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Forms.Base.Appearance;
using Clovent.Desktop.Restaurant.Shared;
using DevExpress.XtraEditors;
using Xunit;

namespace Clovent.Desktop.Tests.Restaurant.Shared;

public sealed class ManagerAuthorizationFormTests
{
    private static T GetField<T>(object target, string fieldName) where T : class
    {
        var type = target.GetType();
        while (type != null)
        {
            var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (field != null)
            {
                return (T)field.GetValue(target)!;
            }
            type = type.BaseType;
        }
        throw new InvalidOperationException($"Field {fieldName} not found on {target.GetType()}");
    }

    private sealed class FakeManagerAuthorizationService : IManagerAuthorizationService
    {
        public Func<string, string, string, ManagerAuthorizationResult>? AuthorizeHandler { get; set; }

        public Task<ManagerAuthorizationResult> AuthorizeAsync(
            string userName,
            string password,
            string featureCode,
            CancellationToken cancellationToken = default)
        {
            if (AuthorizeHandler != null)
            {
                return Task.FromResult(AuthorizeHandler(userName, password, featureCode));
            }

            if (userName == "manager" && password == "validpass")
            {
                return Task.FromResult(ManagerAuthorizationResult.Approved(Guid.NewGuid(), "Store Manager"));
            }

            if (userName == "unauthorized_user" && password == "validpass")
            {
                return Task.FromResult(ManagerAuthorizationResult.Denied("User 'Cashier John' is not authorized to approve this action."));
            }

            return Task.FromResult(ManagerAuthorizationResult.Denied("Invalid manager username or password."));
        }
    }

    [Fact]
    public void ManagerAuthorizationDialog_HasUsernameEditor()
    {
        using var form = new ManagerAuthorizationForm("Auth Title", "Auth Detail");
        form.Show();

        var userNameEdit = GetField<TextEdit>(form, "_userNameEdit");
        var passwordEdit = GetField<TextEdit>(form, "_passwordEdit");

        Assert.NotNull(userNameEdit);
        Assert.True(userNameEdit.Visible);
        Assert.True(userNameEdit.Enabled);

        // Username editor must be positioned above password editor
        var userPos = userNameEdit.PointToScreen(Point.Empty);
        var passPos = passwordEdit.PointToScreen(Point.Empty);
        Assert.True(userPos.Y <= passPos.Y, "Username editor should be positioned at or above password editor");

        form.Close();
    }

    [Fact]
    public void ManagerAuthorizationDialog_HasPasswordEditor()
    {
        using var form = new ManagerAuthorizationForm("Auth Title", "Auth Detail");
        form.Show();

        var passwordEdit = GetField<TextEdit>(form, "_passwordEdit");

        Assert.NotNull(passwordEdit);
        Assert.True(passwordEdit.Visible);
        Assert.True(passwordEdit.Enabled);

        form.Close();
    }

    [Fact]
    public void ManagerAuthorizationDialog_PasswordIsMasked()
    {
        using var form = new ManagerAuthorizationForm("Auth Title", "Auth Detail");
        form.CreateControl();

        var passwordEdit = GetField<TextEdit>(form, "_passwordEdit");

        Assert.True(passwordEdit.Properties.UseSystemPasswordChar, "Password field must mask characters via UseSystemPasswordChar");
    }

    [Fact]
    public void ManagerAuthorizationDialog_HasAuthorizeAndCancelButtons()
    {
        using var form = new ManagerAuthorizationForm("Auth Title", "Auth Detail");
        form.CreateControl();

        var authorizeBtn = GetField<SimpleButton>(form, "_btnAuthorize");
        var cancelBtn = GetField<SimpleButton>(form, "_btnCancel");

        Assert.NotNull(authorizeBtn);
        Assert.NotNull(cancelBtn);

        Assert.Contains("Authorize", authorizeBtn.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Cancel", cancelBtn.Text, StringComparison.OrdinalIgnoreCase);

        // Must NOT be generic OK
        Assert.DoesNotContain("OK", authorizeBtn.Text, StringComparison.OrdinalIgnoreCase);

        Assert.Equal(authorizeBtn, form.AcceptButton);
        Assert.Equal(cancelBtn, form.CancelButton);
    }

    [Fact]
    public async Task ManagerAuthorizationDialog_UsernameValidationMatchesUI()
    {
        using var form = new ManagerAuthorizationForm("Auth Title", "Auth Detail");
        form.CreateControl();

        var userNameEdit = GetField<TextEdit>(form, "_userNameEdit");
        var passwordEdit = GetField<TextEdit>(form, "_passwordEdit");

        userNameEdit.Text = "   ";
        passwordEdit.Text = "any_password";

        var result = await form.PerformAuthorizeAsync();

        Assert.False(result);
        Assert.True(form.InlineErrorVisible);
        Assert.NotNull(form.CurrentInlineError);
        Assert.Contains("username", form.CurrentInlineError, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(DialogResult.None, form.DialogResult);
    }

    [Fact]
    public async Task ManagerAuthorizationDialog_PasswordValidationMatchesUI()
    {
        using var form = new ManagerAuthorizationForm("Auth Title", "Auth Detail");
        form.CreateControl();

        var userNameEdit = GetField<TextEdit>(form, "_userNameEdit");
        var passwordEdit = GetField<TextEdit>(form, "_passwordEdit");

        userNameEdit.Text = "manager1";
        passwordEdit.Text = "";

        var result = await form.PerformAuthorizeAsync();

        Assert.False(result);
        Assert.True(form.InlineErrorVisible);
        Assert.NotNull(form.CurrentInlineError);
        Assert.Contains("password", form.CurrentInlineError, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(DialogResult.None, form.DialogResult);
    }

    [Fact]
    public async Task ManagerAuthorizationDialog_RejectsInvalidCredentials()
    {
        var fakeAuth = new FakeManagerAuthorizationService();
        using var form = new ManagerAuthorizationForm("Auth Title", "Auth Detail", fakeAuth);
        form.CreateControl();

        var userNameEdit = GetField<TextEdit>(form, "_userNameEdit");
        var passwordEdit = GetField<TextEdit>(form, "_passwordEdit");

        userNameEdit.Text = "manager";
        passwordEdit.Text = "wrong_password";

        var result = await form.PerformAuthorizeAsync();

        Assert.False(result);
        Assert.True(form.InlineErrorVisible);
        Assert.NotNull(form.CurrentInlineError);
        Assert.Contains("Invalid manager username or password", form.CurrentInlineError);

        // Security check: password must be cleared, username retained
        Assert.Equal(string.Empty, form.ManagerPassword);
        Assert.Equal("manager", form.ManagerUserName);
        Assert.Equal(DialogResult.None, form.DialogResult);
    }

    [Fact]
    public async Task ManagerAuthorizationDialog_RejectsUnauthorizedManager()
    {
        var fakeAuth = new FakeManagerAuthorizationService();
        using var form = new ManagerAuthorizationForm("Auth Title", "Auth Detail", fakeAuth);
        form.CreateControl();

        var userNameEdit = GetField<TextEdit>(form, "_userNameEdit");
        var passwordEdit = GetField<TextEdit>(form, "_passwordEdit");

        userNameEdit.Text = "unauthorized_user";
        passwordEdit.Text = "validpass";

        var result = await form.PerformAuthorizeAsync();

        Assert.False(result);
        Assert.True(form.InlineErrorVisible);
        Assert.NotNull(form.CurrentInlineError);
        Assert.Contains("not authorized", form.CurrentInlineError, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(DialogResult.None, form.DialogResult);
    }

    [Fact]
    public async Task ManagerAuthorizationDialog_AcceptsAuthorizedManager()
    {
        var fakeAuth = new FakeManagerAuthorizationService();
        using var form = new ManagerAuthorizationForm("Auth Title", "Auth Detail", fakeAuth);
        form.CreateControl();

        var userNameEdit = GetField<TextEdit>(form, "_userNameEdit");
        var passwordEdit = GetField<TextEdit>(form, "_passwordEdit");

        userNameEdit.Text = "manager";
        passwordEdit.Text = "validpass";

        var result = await form.PerformAuthorizeAsync();

        Assert.True(result);
        Assert.False(form.InlineErrorVisible);
        Assert.Equal(DialogResult.OK, form.DialogResult);
        Assert.NotNull(form.AuthorizationResult);
        Assert.True(form.AuthorizationResult.Succeeded);
        Assert.Equal("Store Manager", form.AuthorizationResult.ManagerDisplayName);
    }

    [Fact]
    public void ManagerAuthorizationDialog_PrepopulatesUsernameWhenProvided()
    {
        using var form = new ManagerAuthorizationForm(
            "Auth Title",
            "Auth Detail",
            currentUserName: "prefilled_manager");
        form.CreateControl();

        Assert.Equal("prefilled_manager", form.ManagerUserName);
    }

    [Fact]
    public void ManagerAuthorizationDialog_CreditLimitContext_DisplaysFinancialBreakdown()
    {
        var context = new CreditLimitOverrideContext(
            CustomerName: "ABC Trading Corp",
            CurrentOutstanding: 20000.00m,
            CreditLimit: 0.00m,
            SaleAmount: 2130.00m);

        using var form = ManagerAuthorizationForm.ForCreditLimit(
            context.CustomerName,
            context.CurrentOutstanding,
            context.CreditLimit,
            context.SaleAmount);
        form.CreateControl();

        Assert.NotNull(form.Context);
        Assert.Equal("ABC Trading Corp", form.Context.CustomerName);
        Assert.Equal(20000.00m, form.Context.CurrentOutstanding);
        Assert.Equal(0.00m, form.Context.CreditLimit);
        Assert.Equal(2130.00m, form.Context.SaleAmount);
        Assert.Equal(22130.00m, form.Context.BalanceAfterSale);
        Assert.Equal(22130.00m, form.Context.ExceededBy);

        var lblCustomerVal = GetField<LabelControl>(form, "_lblCustomerVal");
        var lblOutstandingVal = GetField<LabelControl>(form, "_lblOutstandingVal");
        var lblLimitVal = GetField<LabelControl>(form, "_lblLimitVal");
        var lblSaleVal = GetField<LabelControl>(form, "_lblSaleVal");
        var lblNewBalanceVal = GetField<LabelControl>(form, "_lblNewBalanceVal");

        Assert.Equal("ABC Trading Corp", lblCustomerVal.Text);
        Assert.Contains("20,000", lblOutstandingVal.Text);
        Assert.Contains("0", lblLimitVal.Text);
        Assert.Contains("2,130", lblSaleVal.Text);
        Assert.Contains("22,130", lblNewBalanceVal.Text);
    }

    [Theory]
    [InlineData(96)]
    [InlineData(144)]
    [InlineData(192)]
    [InlineData(240)]
    public void ManagerAuthorizationDialog_LayoutScalesWithoutClipping(int dpi)
    {
        var workingArea = new Rectangle(0, 0, 1920, 1080 - 40); // 40px taskbar
        int targetW = DesktopDpi.Scale(540, dpi);
        int targetH = DesktopDpi.Scale(450, dpi);
        int minW = DesktopDpi.Scale(480, dpi);
        int minH = DesktopDpi.Scale(380, dpi);

        int maxAllowedW = Math.Max(minW, (int)(workingArea.Width * 0.96));
        int maxAllowedH = Math.Max(minH, (int)(workingArea.Height * 0.96));

        int finalW = Math.Clamp(targetW, minW, maxAllowedW);
        int finalH = Math.Clamp(targetH, minH, maxAllowedH);

        // Clamped dialog must fit comfortably within working area
        Assert.True(finalW <= workingArea.Width);
        Assert.True(finalH <= workingArea.Height);

        // Minimum bounds must be respected
        Assert.True(finalW >= minW);
        Assert.True(finalH >= minH);

        // Editor column and button widths must scale
        int labelColW = DesktopDpi.Scale(150, dpi);
        Assert.True(labelColW >= 150);
        int btnW = DesktopDpi.Scale(110, dpi);
        Assert.True(btnW >= 110);
    }

    [Theory]
    [InlineData(96)]
    [InlineData(144)]
    [InlineData(192)]
    [InlineData(240)]
    public void ManagerAuthorizationDialog_RuntimeLayoutMeetsAcceptanceCriteria(int dpi)
    {
        var context = new CreditLimitOverrideContext(
            CustomerName: "John Smith",
            CurrentOutstanding: 0.00m,
            CreditLimit: 0.00m,
            SaleAmount: 587.50m);

        using var form = ManagerAuthorizationForm.ForCreditLimit(
            context.CustomerName,
            context.CurrentOutstanding,
            context.CreditLimit,
            context.SaleAmount,
            currentUserName: "admin");

        form.ScaleLayoutForDpi(dpi);
        form.Show();

        var root = GetField<TableLayoutPanel>(form, "_rootLayout");
        var card = GetField<PanelControl>(form, "_cardPanel");
        var summaryTable = GetField<TableLayoutPanel>(form, "_summaryTable");
        var finGrid = GetField<TableLayoutPanel>(form, "_financialsGrid");
        var credPanel = GetField<TableLayoutPanel>(form, "_credentialsPanel");
        var userEdit = GetField<TextEdit>(form, "_userNameEdit");
        var passEdit = GetField<TextEdit>(form, "_passwordEdit");
        var lblUser = GetField<LabelControl>(form, "_lblUserName");
        var lblPass = GetField<LabelControl>(form, "_lblPassword");
        var btnAuth = GetField<SimpleButton>(form, "_btnAuthorize");
        var btnCancel = GetField<SimpleButton>(form, "_btnCancel");
        var lblNotice = GetField<LabelControl>(form, "_lblNotice");

        // 1. Username editor has positive width and height
        Assert.True(userEdit.Width > 0, "Username editor width must be positive");
        Assert.True(userEdit.Height > 0, "Username editor height must be positive");

        // 2. Password editor has positive width and height
        Assert.True(passEdit.Width > 0, "Password editor width must be positive");
        Assert.True(passEdit.Height > 0, "Password editor height must be positive");

        // 3. Username and password editors do not overlap
        var userScreenBounds = userEdit.RectangleToScreen(userEdit.ClientRectangle);
        var passScreenBounds = passEdit.RectangleToScreen(passEdit.ClientRectangle);
        Assert.False(userScreenBounds.IntersectsWith(passScreenBounds),
            $"Username editor {userScreenBounds} and password editor {passScreenBounds} must not overlap");
        Assert.True(passScreenBounds.Top >= userScreenBounds.Bottom,
            $"Password editor (top {passScreenBounds.Top}) must sit below username editor (bottom {userScreenBounds.Bottom})");

        // 4. Username and password labels do not overlap
        var lblUserScreen = lblUser.RectangleToScreen(lblUser.ClientRectangle);
        var lblPassScreen = lblPass.RectangleToScreen(lblPass.ClientRectangle);
        Assert.False(lblUserScreen.IntersectsWith(lblPassScreen),
            $"Username label {lblUserScreen} and password label {lblPassScreen} must not overlap");
        Assert.True(lblPassScreen.Top >= lblUserScreen.Bottom,
            $"Password label (top {lblPassScreen.Top}) must sit below username label (bottom {lblUserScreen.Bottom})");

        // 5. Username label aligns with username editor vertically
        Assert.True(Math.Abs(lblUserScreen.Top - userScreenBounds.Top) <= DesktopDpi.Scale(10, dpi),
            "Username label should vertically align with username editor");

        // 6. Password label aligns with password editor vertically
        Assert.True(Math.Abs(lblPassScreen.Top - passScreenBounds.Top) <= DesktopDpi.Scale(10, dpi),
            "Password label should vertically align with password editor");

        // 7. Financial summary rows do not intersect
        var finRows = new[]
        {
            GetField<LabelControl>(form, "_lblCustomerTitle"),
            GetField<LabelControl>(form, "_lblOutstandingTitle"),
            GetField<LabelControl>(form, "_lblLimitTitle"),
            GetField<LabelControl>(form, "_lblSaleTitle"),
            GetField<LabelControl>(form, "_lblNewBalanceTitle")
        };
        for (int i = 0; i < finRows.Length - 1; i++)
        {
            var r1 = finRows[i].RectangleToScreen(finRows[i].ClientRectangle);
            var r2 = finRows[i + 1].RectangleToScreen(finRows[i + 1].ClientRectangle);
            Assert.False(r1.IntersectsWith(r2),
                $"Financial row {i} {r1} intersects with row {i + 1} {r2}");
            Assert.True(r2.Top >= r1.Bottom,
                $"Financial row {i + 1} (top {r2.Top}) must sit below row {i} (bottom {r1.Bottom})");
        }

        // 8. Warning ("Credit limit exceeded.") does not overlap financial rows
        var noticeScreen = lblNotice.RectangleToScreen(lblNotice.ClientRectangle);
        var lastFinRowScreen = finRows[^1].RectangleToScreen(finRows[^1].ClientRectangle);
        Assert.False(noticeScreen.IntersectsWith(lastFinRowScreen),
            "Notice must not overlap the last financial row");
        Assert.True(noticeScreen.Top >= lastFinRowScreen.Bottom,
            $"Notice (top {noticeScreen.Top}) must sit below the last financial row (bottom {lastFinRowScreen.Bottom})");

        // 9. Action buttons are inside client bounds
        var clientBounds = form.ClientRectangle;
        var authBtnBounds = form.RectangleToClient(btnAuth.RectangleToScreen(btnAuth.ClientRectangle));
        var cancelBtnBounds = form.RectangleToClient(btnCancel.RectangleToScreen(btnCancel.ClientRectangle));
        Assert.True(clientBounds.Contains(authBtnBounds), "Authorize button must be within form client bounds");
        Assert.True(clientBounds.Contains(cancelBtnBounds), "Cancel button must be within form client bounds");

        // 10. Credential controls are inside client bounds
        var userEditClient = form.RectangleToClient(userScreenBounds);
        var passEditClient = form.RectangleToClient(passScreenBounds);
        Assert.True(clientBounds.Contains(userEditClient), "Username editor must be within form client bounds");
        Assert.True(clientBounds.Contains(passEditClient), "Password editor must be within form client bounds");

        // 11. Form remains inside a realistic 1920x1080 working area
        var maxWorkingArea = new Rectangle(0, 0, 1920, 1040);
        Assert.True(form.Width <= maxWorkingArea.Width, "Form width must fit within 1920 monitor");
        Assert.True(form.Height <= maxWorkingArea.Height, "Form height must fit within 1040 working area");

        form.Close();
    }
}
