using Clovent.Domain;
using Clovent.MasterData.Shared.ValueObjects;

namespace Clovent.Restaurant.Customers;

/// <summary>
/// A customer account within the Restaurant system. Manages outstanding balances,
/// credit limits, status, and contact information.
/// </summary>
public sealed class Customer : AggregateRoot<CustomerId>
{
    /// <summary>Unique customer code (e.g. "CUST-001").</summary>
    public EntityCode Code { get; private set; }

    /// <summary>Full name of the customer.</summary>
    public string Name { get; private set; }

    /// <summary>Primary mobile phone number.</summary>
    public string MobileNumber { get; private set; }

    /// <summary>Billing/delivery address.</summary>
    public string Address { get; private set; }

    /// <summary>Email address, optional.</summary>
    public string? Email { get; private set; }

    /// <summary>Opening balance when the account was registered.</summary>
    public decimal OpeningBalance { get; private set; }

    /// <summary>Maximum amount of credit allowed for this customer.</summary>
    public decimal CreditLimit { get; private set; }

    /// <summary>Current outstanding amount owed to the restaurant.</summary>
    public decimal OutstandingBalance { get; private set; }

    /// <summary>Whether this customer is active and can receive credit sales.</summary>
    public bool IsActive { get; private set; }

    /// <summary>General notes about the customer.</summary>
    public string? Notes { get; private set; }

    /// <summary>UTC instant this customer was created.</summary>
    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>UTC instant this customer was last changed.</summary>
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    /// <summary>Takes every persisted field explicitly so this is the single, unambiguous constructor an EF Core Infrastructure implementation can bind to.</summary>
    private Customer(
        CustomerId id,
        EntityCode code,
        string name,
        string mobileNumber,
        string address,
        string? email,
        decimal openingBalance,
        decimal creditLimit,
        decimal outstandingBalance,
        bool isActive,
        string? notes,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        Code = code;
        Name = name;
        MobileNumber = mobileNumber;
        Address = address;
        Email = email;
        OpeningBalance = openingBalance;
        CreditLimit = creditLimit;
        OutstandingBalance = outstandingBalance;
        IsActive = isActive;
        Notes = notes;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    /// <summary>Creates a new Customer aggregate.</summary>
    public static Customer Create(
        EntityCode code,
        string name,
        string mobileNumber,
        string address,
        string? email,
        decimal openingBalance,
        decimal creditLimit,
        string? notes)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Customer name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(mobileNumber))
            throw new ArgumentException("Mobile number is required.", nameof(mobileNumber));
        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException("Address is required.", nameof(address));
        if (openingBalance < 0)
            throw new ArgumentException("Opening balance cannot be negative.", nameof(openingBalance));
        if (creditLimit < 0)
            throw new ArgumentException("Credit limit cannot be negative.", nameof(creditLimit));

        var now = DateTimeOffset.UtcNow;
        return new Customer(
            CustomerId.New(),
            code,
            name.Trim(),
            mobileNumber.Trim(),
            address.Trim(),
            email?.Trim(),
            openingBalance,
            creditLimit,
            openingBalance, // Initial outstanding starts at opening balance
            true,
            notes?.Trim(),
            now,
            now);
    }

    /// <summary>Updates customer information.</summary>
    public void Update(
        string name,
        string mobileNumber,
        string address,
        string? email,
        decimal creditLimit,
        string? notes)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Customer name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(mobileNumber))
            throw new ArgumentException("Mobile number is required.", nameof(mobileNumber));
        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException("Address is required.", nameof(address));
        if (creditLimit < 0)
            throw new ArgumentException("Credit limit cannot be negative.", nameof(creditLimit));

        Name = name.Trim();
        MobileNumber = mobileNumber.Trim();
        Address = address.Trim();
        Email = email?.Trim();
        CreditLimit = creditLimit;
        Notes = notes?.Trim();
        Touch();
    }

    /// <summary>Activates or deactivates the customer account.</summary>
    public void SetStatus(bool isActive)
    {
        if (IsActive == isActive) return;
        IsActive = isActive;
        Touch();
    }

    /// <summary>Adjusts the customer's outstanding balance (positive for debits/credit sales, negative for payments).</summary>
    public void AdjustBalance(decimal amount)
    {
        OutstandingBalance += amount;
        Touch();
    }

    private void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;
}
