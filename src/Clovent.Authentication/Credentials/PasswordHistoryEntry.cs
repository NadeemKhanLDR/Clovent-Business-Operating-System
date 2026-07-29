namespace Clovent.Authentication.Credentials;

/// <summary>One previously-set password hash and when it was set.</summary>
public sealed record PasswordHistoryEntry(PasswordHash Hash, DateTimeOffset ChangedAtUtc);
