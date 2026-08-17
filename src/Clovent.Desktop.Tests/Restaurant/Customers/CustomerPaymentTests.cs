using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Restaurant.Customers;
using Clovent.Restaurant.Application.Customers.Dtos;
using Xunit;

namespace Clovent.Desktop.Tests.Restaurant.Customers;

/// <summary>
/// Covers defect D9 (the payment-method list was hardcoded and did not match
/// the configured methods) and defect D11 ("Outstanding outstanding", persisted
/// to the audit log).
/// </summary>
public class CustomerPaymentTests
{
    private static CustomerDto Customer(decimal outstanding = 272.50m) => new(
        CustomerId: Guid.NewGuid(),
        Code: "C001",
        Name: "John Smith",
        MobileNumber: "03001234567",
        Address: "Main Street",
        Email: null,
        OpeningBalance: 0m,
        CreditLimit: 500m,
        OutstandingBalance: outstanding,
        IsActive: true,
        Notes: null,
        CreatedAtUtc: DateTimeOffset.UtcNow,
        UpdatedAtUtc: DateTimeOffset.UtcNow);

    /// <summary>
    /// D9: whatever the caller read from the configured payment methods is what
    /// the dialog offers - the design-surface placeholders must not survive
    /// into a running dialog.
    /// </summary>
    [Fact]
    public void PaymentForm_OffersExactlyTheSuppliedPaymentMethods()
    {
        string[] configured = ["Cash", "Credit", "Credit Card", "Easy Paisa"];

        using var form = new CustomerPaymentForm(Customer(), configured);

        Assert.Equal(configured, ComboItems(form));
    }

    /// <summary>The first configured method is preselected, so a payment always has one.</summary>
    [Fact]
    public void PaymentForm_PreselectsTheFirstConfiguredMethod()
    {
        using var form = new CustomerPaymentForm(Customer(), ["Easy Paisa", "Cash"]);

        Assert.Equal("Easy Paisa", form.PaymentMethod);
    }

    /// <summary>
    /// The selected entry maps straight back out as the name the command is
    /// given, so what is recorded is a configured method and not a synonym.
    /// </summary>
    [Fact]
    public void PaymentForm_ReturnsTheSelectedMethodName()
    {
        using var form = new CustomerPaymentForm(Customer(), ["Cash", "Credit", "Easy Paisa"]);

        var combo = (DevExpress.XtraEditors.ComboBoxEdit)typeof(CustomerPaymentForm)
            .GetField("_comboPaymentMethod", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(form)!;
        combo.SelectedIndex = 2;

        Assert.Equal("Easy Paisa", form.PaymentMethod);
    }

    [Fact]
    public void PaymentForm_DefaultsTheAmountToTheOutstandingBalance()
    {
        using var form = new CustomerPaymentForm(Customer(272.50m), ["Cash"]);

        Assert.Equal(272.50m, form.Amount);
    }

    /// <summary>D11: the word "outstanding" appears once, not twice.</summary>
    [Fact]
    public void PaymentActivityDetail_DoesNotRepeatOutstanding()
    {
        var detail = CustomersView.ComposePaymentActivityDetail(
            amount: 200m,
            paymentMethod: "Cash",
            customerName: "QA-CUSTOMER-01",
            customerCode: "QACUST01",
            outstandingAfter: 317.50m,
            changeAmount: 0m);

        Assert.DoesNotContain("Outstanding outstanding", detail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains($"Outstanding: {CurrencyDisplay.Format(317.50m)}", detail);
        Assert.DoesNotContain("Change handed back", detail);
    }

    [Fact]
    public void PaymentActivityDetail_ReportsChangeOnlyWhenSomeWasHandedBack()
    {
        var detail = CustomersView.ComposePaymentActivityDetail(
            amount: 100m,
            paymentMethod: "Cash",
            customerName: "QA-CUSTOMER-01",
            customerCode: "QACUST01",
            outstandingAfter: 0m,
            changeAmount: 100m);

        Assert.DoesNotContain("Outstanding outstanding", detail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains($"Change handed back: {CurrencyDisplay.Format(100m)}", detail);
    }

    private static List<string> ComboItems(CustomerPaymentForm form)
    {
        var combo = (DevExpress.XtraEditors.ComboBoxEdit)typeof(CustomerPaymentForm)
            .GetField("_comboPaymentMethod", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(form)!;

        return [.. combo.Properties.Items.Cast<object>().Select(i => i.ToString()!)];
    }
}
