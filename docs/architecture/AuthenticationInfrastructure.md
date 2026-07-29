---
title: Authentication Infrastructure Reference
type: Architecture
status: Awaiting Solution Architect review
created: 2026-07-27
updated: Milestone 6
applies_to: src/Clovent.Authentication.Infrastructure, src/Clovent.Authentication (Credentials additions), src/Clovent.Authentication.Application (IUnitOfWork)
---

# Authentication Infrastructure Reference

This document describes `src/Clovent.Authentication.Infrastructure` as it exists after Milestone 6: EF Core persistence (SQL Server) for the four repository contracts named in the milestone brief - `ISessionRepository`, `ILoginAttemptRepository`, `IRefreshSessionRepository`, and `IUserCredentialsRepository`. Scope was **Infrastructure only** - no UI, no login page, no JWT, no API, no DevExpress, no WinForms. Building `IUserCredentialsRepository` required resolving one open question `AuthenticationDomain.md` (Milestone 5.1) deliberately left unresolved; that resolution is Section 3 below and is the one place this milestone made a judgment call bigger than "how do these four things get persisted."

---

## 1. Folder structure

```
src/Clovent.Authentication.Infrastructure/
  Clovent.Authentication.Infrastructure.csproj
  Persistence/
    AuthenticationDbContext.cs          - DbSets for all four aggregates
    AuthenticationDbContextFactory.cs   - IDesignTimeDbContextFactory, for `dotnet ef` tooling only
    AuthenticationPersistenceInitializer.cs - IPersistenceInitializer (Clovent.Platform), applies migrations
    UnitOfWork.cs                       - IUnitOfWork (Clovent.Authentication.Application) implementation
    ValueConverters.cs                  - every EF Core ValueConverter shared across configurations
    Configurations/
      SessionConfiguration.cs, LoginAttemptConfiguration.cs,
      RefreshSessionConfiguration.cs, UserCredentialsConfiguration.cs
    Migrations/
      InitialCreate (+ Designer, + model snapshot)
  Repositories/
    SessionRepository.cs, LoginAttemptRepository.cs,
    RefreshSessionRepository.cs, UserCredentialsRepository.cs
  DependencyInjection/
    PersistenceServiceCollectionExtensions.cs   - AddPersistence(services, configuration)
    InfrastructureServiceCollectionExtensions.cs - AddInfrastructure(services, configuration)

src/Clovent.Authentication.Infrastructure.Tests/
  TestSupport/  SqliteTestBase.cs, InMemoryTestBase.cs
  Repositories/ one test class per repository (+ a filtering-only class, see Section 10)
  Persistence/  UnitOfWorkTests.cs
```

Two small additions elsewhere, both additive/non-breaking - see Sections 3 and 4:

```
src/Clovent.Authentication/Credentials/
  UserCredentialsId.cs, UserCredentials.cs, IUserCredentialsRepository.cs
  Events/ UserCredentialsCreated.cs, PasswordChanged.cs, PinChanged.cs

src/Clovent.Authentication.Application/
  IUnitOfWork.cs
```

Plus `src/Clovent.Authentication.Tests/Credentials/UserCredentialsTests.cs`.

---

## 2. Naming and registration convention

Follows `Clovent.Platform`'s already-documented convention exactly: every module exposes its own `AddApplication()`/`AddInfrastructure()`/`AddPersistence()` extension methods, never registering itself directly from a host's `Program.cs`. Authentication's Application layer still has no `AddApplication()` (MediatR handler registration) - that remains out of scope here, same as `AuthenticationDomain.md` Section 11 left it after Milestone 5.1, because there is still no host/API project to register a `Program.cs`-level composition into. This milestone adds the other two:

- **`Clovent.Authentication.Infrastructure.DependencyInjection.InfrastructureServiceCollectionExtensions.AddInfrastructure`** - registers `TimeProvider.System` via `TryAddSingleton` (the milestone's "TimeProvider registration" item; every Application-layer handler already takes `TimeProvider` as a constructor dependency per `AuthenticationDomain.md` Section 3, but nothing registered a real one until now). `TryAdd` so a host or test fixture that already registered a fake `TimeProvider` for deterministic tests is not overridden.
- **`Clovent.Authentication.Infrastructure.DependencyInjection.PersistenceServiceCollectionExtensions.AddPersistence`** - registers `AuthenticationDbContext` (SQL Server), the four repository implementations, `IUnitOfWork`, and `IPersistenceInitializer`.

---

## 3. Resolving the `IUserCredentialsRepository` open question

`AuthenticationDomain.md` Section 8/10 modeled `PasswordHash`, `PinHash`, `SecurityStamp`, `PasswordHistory`, and `FailedAttempts` as standalone value types with **no owner**, explicitly deferring "what holds a `UserId` alongside these six values, and does it get its own repository" to a future milestone - warning that deciding this informally (bundling all six into one class with a `UserId` foreign key) would be creating the "prohibited Credential Aggregate" in substance if not in name.

This milestone's brief names `IUserCredentialsRepository` directly as a deliverable, which is not buildable without such an entity existing. Resolving that open question was therefore unavoidable, not optional scope creep. The resolution, kept as small as the six-concept vocabulary allows:

**`UserCredentials`** (`Clovent.Authentication.Credentials`) - a new `AggregateRoot<UserCredentialsId>` holding exactly `UserId` plus the six values, and nothing else:

| Member | Purpose |
|---|---|
| `Create(UserId, DateTimeOffset)` | New, empty credential record - no password/PIN set yet. Raises `UserCredentialsCreated`. |
| `SetPassword(PasswordHash, DateTimeOffset, maxHistorySize)` | Updates the hash, appends to `PasswordHistory`, and rotates `SecurityStamp` (a password change is security-relevant). Raises `PasswordChanged`. |
| `SetPin(PinHash, DateTimeOffset)` | Updates the hash and rotates `SecurityStamp`. Raises `PinChanged`. |
| `RecordFailedAttempt()` / `ResetFailedAttempts()` | Routine bookkeeping - like `Session.Touch`, deliberately does not raise an event. |

It does **not** hash, validate policy, or evaluate lockouts - `PasswordPolicy`/`PinPolicy`/`LockoutPolicy` remain untouched and are still the only place those rules live. `UserCredentials` is a thin owner, not a second place business rules could drift into.

This is a judgment call, not a unilateral override of the prior milestone's caution - the brief naming `IUserCredentialsRepository` explicitly is read here as the Solution Architect-level decision that this seam should exist now. It should still be confirmed in review (Section 12, item 1), the same way Milestone 5.1's own judgment calls were confirmed after the fact rather than blocked on beforehand.

---

## 4. EF Core materialization: why `Session` and `RefreshSession` changed

EF Core needs exactly one constructor it can bind unambiguously to every mapped property when materializing an aggregate from a query result. `Session`'s and `RefreshSession`'s private constructors (as Milestone 5/5.1 left them) only accepted "just started" parameters and derived `LastActivityAtUtc`/`ExpiresAtUtc`/`Status` (`Session`) or `Status` (`RefreshSession`) internally, always to their brand-new-aggregate values. Materializing an existing, possibly-revoked-or-touched row through that constructor would have silently reset those fields back to "just started" on every load - a correctness bug, not a style issue.

Fix: both private constructors now take every persisted field explicitly (`Session` gained `lastActivityAtUtc`, `expiresAtUtc`, `status`; `RefreshSession` gained `status`), and `Start()`/`Issue()` pass their already-known values through. This is:

- **Not a public API change** - both constructors are `private`, called only by their own static factory within the same file.
- **Not a behavior change** - `Start()`/`Issue()` produce identical aggregates before and after.
- The **only** correctness-motivated Domain-layer edit this milestone made. `LoginAttempt`'s constructor already took every mapped field (it has no derived state beyond `IsFailure`, which is computed from `Outcome` and is `Ignore()`d in its configuration) and needed no change.

No other Domain-layer method or invariant changed.

---

## 5. Value conversion strategy - no further Domain changes required

Every strongly-typed ID, value object, and enum in the Authentication Domain is opaque to a relational column until converted. `Persistence/ValueConverters.cs` centralizes every `ValueConverter` used across the four `IEntityTypeConfiguration`s:

| Model type | Provider column | Notes |
|---|---|---|
| `SessionId`/`UserId`/`LoginAttemptId`/`RefreshSessionId`/`UserCredentialsId` | `Guid` | Direct unwrap/rewrap through each type's existing constructor. |
| Nullable `UserId` (`LoginAttempt.UserId`) | Nullable `Guid` | Separate converter so a null identifier round-trips without the aggregate's own non-null constructor rejecting `Guid.Empty`. |
| `IpAddress?` | Nullable `string` | Via existing `IpAddress.Create`. |
| `TimeSpan` (`Session.IdleTimeout`) | `long` (ticks) | Not SQL Server's native `time` type, which cannot represent a duration &ge; 24 hours - nothing guarantees an idle timeout stays under that. |
| `PasswordHash?`/`PinHash?` | Nullable `string` | Via existing `.Create`. |
| `SecurityStamp` | `string` | Via existing `.Create`. |
| `FailedAttempts` | `int` | See below. |
| `PasswordHistory` | `string` (JSON array of entries) | See below. |
| Every `*Status`/`LoginOutcome` enum | `string` (`HasConversion<string>()`) | Readable directly in the database; not relied on as a stability contract beyond what enum renames already risk everywhere else. |

**`FailedAttempts` and `PasswordHistory` needed reconstruction from storage, and neither required a Domain-layer change**, deliberately - modifying `Credentials/FailedAttempts.cs` or `Credentials/PasswordHistory.cs` to add an Infrastructure-only "restore" factory would be exactly the kind of persistence-driven Domain change this project has otherwise avoided:

- `FailedAttempts` exposes only `Zero` and `Increment()` (one at a time), by design (`AuthenticationDomain.md` Section 8). The converter replays `Increment()` the stored count's number of times from `Zero`. Counts here are bounded by a lockout threshold (small), so this is cheap.
- `PasswordHistory` exposes only `Empty` and `WithNewPassword(hash, changedAt, maxSize)` (always prepending, always capped). The converter serializes `Entries` to a small JSON array and, on read, replays `WithNewPassword` from oldest to newest against `Empty` - the same construction sequence the aggregate itself would have produced originally - reproducing the original most-recent-first order and cap.
- **Why JSON instead of an owned collection table:** `PasswordHistory.Entries` is reached only through the `PasswordHistory` value object, which owns a private, capped (`DefaultMaxSize = 5`) list with no independent identity of its own and is never queried apart from its owning `UserCredentials`. An EF owned collection would require restructuring `PasswordHistory` to expose a mutable, EF-shaped collection navigation - a Domain-layer change - to solve a problem a single JSON column already solves without one.

The one Infrastructure-owned convention this implies: any future consumer that needs to query password history independently of its owner (e.g. "find every account that reused hash X across the whole system") is not supported by this shape and would need a deliberate schema change at that point - flagged in Section 12, item 3.

---

## 6. Schema

All four tables live under the `Authentication` schema (`Sessions`, `LoginAttempts`, `RefreshSessions`, `UserCredentials`), so this bounded context can share a database with others (Identity, future modules) without name collisions. Indexes: `Sessions(UserId)`, `Sessions(UserId, Status)`, `LoginAttempts(AttemptedIdentifier)`, `LoginAttempts(UserId)`, `RefreshSessions(SessionId)`, `RefreshSessions(SessionId, Status)`, and a **unique** `UserCredentials(UserId)` (one credential record per user - enforced at the database, not just assumed). `AuthenticationDbContext` uses `ApplyConfigurationsFromAssembly`, so a fifth aggregate's configuration in a later milestone needs no `OnModelCreating` edit.

---

## 7. Repository implementations

Each of the four (`SessionRepository`, `LoginAttemptRepository`, `RefreshSessionRepository`, `UserCredentialsRepository`) is a thin adapter over `AuthenticationDbContext`: `GetByIdAsync`/natural-key lookups via `FirstOrDefaultAsync`, collection queries via `Where(...).ToListAsync()`, `AddAsync` via `DbSet<T>.AddAsync`. None calls `SaveChangesAsync` itself - see Section 8. `ISessionRepository.GetActiveByUserIdAsync` and `IRefreshSessionRepository.GetActiveBySessionIdAsync` filter on `Status == Active` in the query, not by loading everything and filtering in memory.

---

## 8. Unit of Work - a seam, not (yet) a wired pipeline

`Clovent.Authentication.Application.IUnitOfWork` (new, this milestone) is a single `SaveChangesAsync(CancellationToken)`, following the exact Dependency Inversion pattern already established for `IIdentityUserService` in Milestone 5.1: the layer that would need the capability defines the interface; Infrastructure implements it (`Persistence/UnitOfWork.cs`, wrapping `AuthenticationDbContext.SaveChangesAsync`).

**No Application-layer command handler calls it yet, and none was changed to.** Every existing handler (`RevokeSessionCommandHandler`, `StartSessionCommandHandler`, etc.) loads/mutates/adds aggregates but never explicitly commits - this was already true before this milestone and is not something Infrastructure work can silently fix by injecting a call into someone else's constructor. `AuthenticationDomain.md` Section 10, item 4 already flagged "does CBOS adopt MediatR pipeline behaviors" as an open, unresolved question; the natural way to wire `IUnitOfWork` in without touching every handler's signature is exactly such a behavior (call `next()`, then `SaveChangesAsync()`), and deciding that platform-wide is explicitly bigger than this milestone's scope. **Until that decision is made, calling `IUnitOfWork.SaveChangesAsync()` is the caller's responsibility** (a future API/composition layer, or an explicit per-handler edit) - `UnitOfWorkTests.cs` demonstrates the mechanism works; it does not claim the wiring is complete end-to-end.

**Resolved in Milestone 9**: `UnitOfWorkBehavior<TRequest,TResponse>`, a MediatR open-generic `IPipelineBehavior<,>`, now calls `next()` then `SaveChangesAsync()` around every Authentication Application request - see `AuthenticationIntegration.md` Section 3. No handler signature changed.

---

## 9. Persistence initializer, migrations, and the design-time factory

`AuthenticationPersistenceInitializer` implements `Clovent.Platform.Bootstrap.IPersistenceInitializer.InitializeAsync` by calling `AuthenticationDbContext.Database.MigrateAsync()`, matching the convention `PersistenceServiceCollectionExtensions.cs` (Platform Foundation) already documented: "future modules register their own IPersistenceInitializer/DbContext from within their own AddPersistence()." It is registered **Scoped**, not Singleton - `AddDbContext` registers `AuthenticationDbContext` as Scoped by default, and a Singleton initializer would capture one `DbContext` instance for the lifetime of the host (a captive-dependency bug the DI container would reject at construction time anyway). `ApplicationBootstrapper.BuildAndInitializeAsync` already resolves every `IPersistenceInitializer` from a freshly-created scope, so this fits its existing call pattern without any change there.

The `InitialCreate` migration was generated via `dotnet ef migrations add`, which needs a way to construct `AuthenticationDbContext` without a running host or a real connection string. `AuthenticationDbContextFactory` (`IDesignTimeDbContextFactory<AuthenticationDbContext>`) supplies a placeholder SQL Server connection string used only to build the model for migration generation - it is never used to actually connect, and is entirely separate from `AddPersistence`'s runtime connection string resolution (Section 10).

---

## 10. Connection string convention

`AddPersistence` reads `configuration.GetConnectionString("Authentication")` - the standard ASP.NET Core `ConnectionStrings:Authentication` configuration key - and throws `InvalidOperationException` immediately if it is missing, matching `PlatformOptions`'s "no hardcoded fallback values" philosophy exactly (`PlatformOptions.cs`: "there are deliberately no hardcoded fallback values here"). No new custom Options type was introduced for this; the built-in `ConnectionStrings` section is already the idiomatic .NET convention and needed no reinvention.

---

## 11. Testing strategy: SQLite vs. InMemory, and why both appear

Structural/mapping tests (`Clovent.Authentication.Infrastructure.Tests`) run against a real relational engine - SQLite, in-memory, one shared connection per test class - rather than EF Core's InMemory provider, because the InMemory provider **ignores** most of what this milestone is actually about (value converters are applied, but column types, unique indexes, and provider-level query translation are not meaningfully exercised). SQLite catches real mapping mistakes the InMemory provider would silently paper over - this is exactly how the `nvarchar(max)` column-type mistake (Section 5's `PasswordHistory` column) was caught during this milestone: a hardcoded SQL-Server-only `HasColumnType("nvarchar(max)")` made SQLite's DDL parser fail outright, at which point it was removed in favor of leaving the column unbounded (`string` with no `HasMaxLength`) - which SQL Server already defaults to `nvarchar(max)` for regardless.

One exception: `LoginAttemptRepository.GetRecentByIdentifierAsync`/`GetRecentByUserIdAsync` filter on a `DateTimeOffset` range (`OccurredAtUtc >= sinceUtc`). This is a **documented SQLite provider limitation** (range comparisons/ordering on `DateTimeOffset` columns are not translatable server-side by EF Core's SQLite provider) that does not apply to SQL Server, the actual target. Rather than distort the repository implementation to accommodate a test-tooling gap, `LoginAttemptRepositoryFilteringTests` verifies that specific filtering/ordering logic against EF Core's InMemory provider instead (full client-side LINQ evaluation, so the translation gap never applies) - see `InMemoryTestBase`'s doc comment for the reasoning. The simple by-id round trip (including the nullable `UserId` conversion) stays on SQLite in `LoginAttemptRepositoryTests`, since that query is a plain equality lookup and hits no SQLite limitation.

---

## 12. Open questions for Solution Architect review

1. **`UserCredentials` as the credential-storage entity (Section 3).** This milestone resolved `AuthenticationDomain.md` Section 10, item 1 by creating `UserCredentials` because `IUserCredentialsRepository` cannot exist otherwise. **Needs ratification**: is a thin `AggregateRoot<UserCredentialsId>` holding exactly the six existing value types (no new behavior beyond `SetPassword`/`SetPin`/failed-attempt bookkeeping) the right shape, or should hashing/policy orchestration (currently Application-layer, per `PasswordPolicy`/`PinPolicy`) move onto it?
3. **`PasswordHistory` as JSON (Section 5).** Chosen because nothing today queries password history independently of its owner. **Needs a decision** if a future requirement ever needs to query across users' password history directly (e.g. breach-detection tooling) - at that point the JSON column stops being sufficient and an owned collection table (requiring a `PasswordHistory` Domain-layer change) would be the correct fix.
5. **Event dispatch.** Still unresolved, same as `AuthenticationDomain.md` Section 10 item 5 and `IdentityDomain.md` Section 8 item 3 - now relevant to three bounded contexts' worth of events. This milestone's EF configurations `Ignore()` every aggregate's `DomainEvents` property; nothing persists or publishes them.

**Resolved in Milestone 9** (previously items 2 and 4 above, kept here for traceability): Unit of Work wiring (`UnitOfWorkBehavior`) and `Clovent.Authentication.Application.AddApplication()` - see `AuthenticationIntegration.md` Sections 2 and 3.

---

## 13. What is deliberately *not* here

Per the milestone brief:

- UI, login page, DevExpress, WinForms
- JWT generation, OAuth, OpenID Connect, API endpoints (no Minimal APIs, no controllers)
- Actual password/PIN hashing - `UserCredentials.SetPassword`/`SetPin` still take an already-computed `PasswordHash`/`PinHash`, exactly as `AuthenticationDomain.md` Section 8 specified; hashing remains a caller's responsibility
- `Clovent.Authentication.Application`'s own DI registration (`AddApplication()`/MediatR) - see Section 12, item 4
- The Unit of Work actually being invoked anywhere in the request/command lifecycle - see Section 8 and Section 12, item 2
- Domain event dispatching - modeled, recorded, explicitly ignored by every EF configuration, never published
