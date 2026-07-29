using Clovent.Domain;

namespace Clovent.Identity.Users.ValueObjects;

/// <summary>A person's structured legal name, as opposed to their chosen <see cref="DisplayName"/>.</summary>
public sealed class PersonName : ValueObject
{
    private const int MaxPartLength = 100;

    /// <summary>The given name.</summary>
    public string FirstName { get; }

    /// <summary>The family name.</summary>
    public string LastName { get; }

    /// <summary>First and last name joined with a single space.</summary>
    public string FullName => $"{FirstName} {LastName}";

    private PersonName(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
    }

    /// <summary>Validates and creates a <see cref="PersonName"/> from its parts.</summary>
    /// <exception cref="ArgumentException">Either part is empty or exceeds <c>100</c> characters.</exception>
    public static PersonName Create(string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name is required.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name is required.", nameof(lastName));

        firstName = firstName.Trim();
        lastName = lastName.Trim();

        if (firstName.Length > MaxPartLength)
            throw new ArgumentException($"First name cannot exceed {MaxPartLength} characters.", nameof(firstName));
        if (lastName.Length > MaxPartLength)
            throw new ArgumentException($"Last name cannot exceed {MaxPartLength} characters.", nameof(lastName));

        return new PersonName(firstName, lastName);
    }

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return FirstName;
        yield return LastName;
    }

    /// <inheritdoc/>
    public override string ToString() => FullName;
}
