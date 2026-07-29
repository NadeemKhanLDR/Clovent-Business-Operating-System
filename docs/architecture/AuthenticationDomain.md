---
title: Authentication Domain Reference
type: Architecture
status: Awaiting Solution Architect review
created: 2026-07-27
updated: Milestone 5.1
applies_to: src/Clovent.Authentication, src/Clovent.Authentication.Application
---

# Authentication Domain Reference

This document describes `src/Clovent.Authentication` (Domain) and `src/Clovent.Authentication.Application` (Application) as they exist after Milestone 5.1: what they are for, how they are organized, and what a future Infrastructure/UI/API milestone is expected to build on top of them. Scope for Milestones 5 and 5.1 combined was **domain and application logic only** - no persistence, no EF Core, no SQL, no UI, no API endpoints, no JWT/OAuth/OpenID Connect, no ASP.NET Identity, no external providers, no hashing/encryption implementation.

---

## 1. Starting state and what changed

`src/Clovent.Authentication` already existed before Milestone 5, but contained no real implementation: 285 `.cs` files across `RBAC/`, `Users/`, `Tokens/`, `Sessions/`, `Password/`, `Pin/`, `Policies/`, and more, every one of them empty (0 bytes), plus the default SDK-template `Class1.cs`. This was confirmed by inspection (`find ... -size +0c` matched exactly one file; a total line count across every `.cs` file in the project was 6 lines - the template file's boilerplate). `09 Security/09.02 Identity Package Inventory.md` independently corroborates this, describing the project as "empty scaffold; package references only, no implementation yet" as of Milestone 4.

**Milestone 5** established the Domain (`Clovent.Authentication`) and Application (`Clovent.Authentication.Application`) projects, their aggregates, policies, and CQRS surface, described throughout this document.

**Milestone 5.1** is a Solution Architect-directed refinement pass over that work, addressing three findings:

1. Authentication's `RecordLoginAttemptCommandHandler` called `Clovent.Identity.Users.User.Lock()` directly and depended on `Clovent.Identity.Users.IUserRepository` - a direct Application-layer dependency on another bounded context's aggregate and repository. **Removed** - see Section 9.
2. Revoking/expiring/logging out a `Session` left its `RefreshSession` still active - the two aggregates' lifecycles could drift apart. **Fixed** with a cascade rule - see Section 6.
3. Credential concepts (password/PIN hashes, security stamp, password history, failed-attempt counter, last-changed timestamp) had no home. Milestone 5 explicitly deferred them; Milestone 5.1 models them as **Authentication-owned, standalone domain types** - explicitly not a new aggregate and not an addition to `Identity.Users.User` - see Section 8.

---

## 2. Folder structure

```
src/Clovent.Authentication/                    (Domain)
  Clovent.Authentication.csproj
  AuthenticationDomainException.cs              - one exception type, one static factory per rule
  Sessions/
    Session.cs, SessionId.cs, SessionStatus.cs, ISessionRepository.cs
    Events/  SessionStarted, SessionExpired, SessionRevoked, SessionLoggedOut
  LoginAttempts/
    LoginAttempt.cs, LoginAttemptId.cs, LoginOutcome.cs, ILoginAttemptRepository.cs
    Events/  LoginAttemptRecorded
  RefreshSessions/
    RefreshSession.cs, RefreshSessionId.cs, RefreshSessionStatus.cs, IRefreshSessionRepository.cs
    Events/  RefreshSessionIssued, RefreshSessionRotated, RefreshSessionRevoked, RefreshSessionExpired
  Passwords/
    PasswordPolicy.cs, PasswordPolicyResult.cs
  Pins/
    PinPolicy.cs, PinPolicyResult.cs
  Lockouts/
    LockoutPolicy.cs
  Credentials/                                  - added in Milestone 5.1, see Section 8
    PasswordHash.cs, PinHash.cs, SecurityStamp.cs
    PasswordHistory.cs, PasswordHistoryEntry.cs
    FailedAttempts.cs
  Shared/ValueObjects/
    IpAddress.cs

src/Clovent.Authentication.Application/        (Application)
  Clovent.Authentication.Application.csproj
  NotFoundException.cs                          - Application-layer "no such aggregate" exception
  IIdentityUserService.cs                        - added in Milestone 5.1, see Section 9
  Sessions/
    Commands/  StartSessionCommand(+Handler), RevokeSessionCommand(+Handler),
               ExpireSessionCommand(+Handler), LogOutSessionCommand(+Handler)   [Expire/LogOut added in 5.1]
    Queries/   GetActiveSessionsForUserQuery(+Handler)
    Dtos/      SessionDto
    SessionTerminationCascade.cs                 - added in Milestone 5.1, see Section 6
  LoginAttempts/
    Commands/  RecordLoginAttemptCommand(+Handler)
    Queries/   GetRecentLoginAttemptsQuery(+Handler)
    Dtos/      LoginAttemptDto
  RefreshSessions/
    Commands/  IssueRefreshSessionCommand(+Handler), RotateRefreshSessionCommand(+Handler), RevokeRefreshSessionCommand(+Handler)
    Queries/   GetRefreshSessionQuery(+Handler)
    Dtos/      RefreshSessionDto
```

`src/Clovent.Authentication.Tests` and `src/Clovent.Authentication.Application.Tests` mirror these one-for-one. Command and its handler are co-located in one file per use case (a common, deliberate MediatR convention - the two are never used independently, and vertical-slice-per-file keeps a whole use case readable in one place instead of split across a request file and a handler file).

---

## 3. Aggregate and Identity boundaries

Three aggregate roots, all depending on `Clovent.Identity.Users.UserId` rather than redefining "user":

| Aggregate | Identity | Models |
|---|---|---|
| `Session` | `SessionId` | A live authenticated session, with a sliding idle timeout |
| `LoginAttempt` | `LoginAttemptId` | An immutable audit record of one login attempt, any outcome |
| `RefreshSession` | `RefreshSessionId` | A renewable credential's lifecycle, tied to a `Session` |

**No separate "Authentication" aggregate was created**, despite the brief listing it as "if required." Decision: not required. An aggregate needs its own invariants to justify existing; an umbrella "Authentication" wrapping `Session`/`LoginAttempt`/`RefreshSession` with no invariants of its own beyond what they already enforce would be a grouping, not an aggregate.

**The Authentication/Identity boundary, precisely stated (as of 5.1):**

- Authentication **may** reference Identity's `Clovent.Identity.Users.UserId` - a strongly-typed identifier is just a value, and treating Identity as authoritative for "what a UserId is" avoids primitive obsession (a raw `Guid`) without creating any behavioral coupling. `Session.UserId`, `LoginAttempt.UserId`, and every command/handler that accepts a user identifier use this type.
- Authentication **must not** reference Identity's `IUserRepository`, load a `Clovent.Identity.Users.User` instance, or call any method on one (`Lock()`, `Unlock()`, etc.). Milestone 5 violated this once (Section 9); Milestone 5.1 removed it.
- Authentication **must not** extend `Identity.Users.User` or otherwise add Authentication-specific fields to an Identity aggregate. Credential concepts belong to Authentication entirely - see Section 8.
- The one sanctioned way for Authentication to affect Identity's `User` state is through `IIdentityUserService`, an interface **Authentication itself defines and owns** (Dependency Inversion at the module boundary) - see Section 9.

**`Session`** uses a sliding idle timeout, not a fixed expiry: every `Touch(now)` pushes `ExpiresAtUtc` forward by the session's fixed `IdleTimeout` from `now`. `Touch()` deliberately does not raise a domain event - unlike every other mutating method on every aggregate in this and the Identity milestone, routine heartbeat-style activity on every request would flood the event stream with noise no consumer needs; only the state-machine transitions (start/expire/revoke/log-out) are business-significant enough to broadcast. As of 5.1, every one of those four transitions is reachable from the Application layer (`StartSessionCommand`, `RevokeSessionCommand`, `ExpireSessionCommand`, `LogOutSessionCommand`) - Milestone 5 only wired Start and Revoke; Expire and LogOut were added in 5.1 because the cascade rule (Section 6) needs to fire no matter which of the three termination paths a session takes.

**`LoginAttempt`** is immutable after creation (a `Record()` factory, no mutating methods) - it is an audit fact, not a stateful process. `AttemptedIdentifier` is a raw `string`, not a validated `Clovent.Identity.Users.ValueObjects.Email`/`UserName` - an attempt must be recordable even when the submitted identifier is malformed or matches no user at all, which is exactly the case a value object's validating factory would reject. `UserId` is nullable for the same reason: it's only populated once the identifier has been resolved to a real user.

**`RefreshSession`** holds no token secret - see Section 7.

Every mutating method on every aggregate takes an explicit `DateTimeOffset nowUtc` parameter rather than reading `DateTimeOffset.UtcNow` internally (a departure from the Identity Domain's aggregates, which read the clock directly). This is deliberate: `Session`/`RefreshSession` have invariants that depend on comparing "now" against a stored instant (`Expire()` must reject being called before `ExpiresAtUtc`), and a test needs to simulate time passing without `Thread.Sleep` or wall-clock coupling. Passing `now` in keeps the aggregate pure and the tests deterministic. The Application layer supplies it via `TimeProvider.GetUtcNow()` (the built-in .NET abstraction, injected so handlers are themselves testable with a fake `TimeProvider` - see `Clovent.Authentication.Application.Tests/TestSupport/FakeTimeProvider.cs`).

---

## 4. Invariants enforced

| Aggregate | Rule | Enforced by |
|---|---|---|
| `Session` | Cannot `Touch`/`Expire`/`Revoke`/`LogOut` unless currently `Active` | each method |
| `Session` | Cannot `Expire` before `ExpiresAtUtc` is reached | `Expire()` |
| `RefreshSession` | Cannot `Rotate`/`Revoke`/`Expire` unless currently `Active` | each method |
| `RefreshSession` | Cannot `Rotate` once past `ExpiresAtUtc` (must `Expire` instead) | `Rotate()` |
| `RefreshSession` | Cannot `Expire` before `ExpiresAtUtc` is reached | `Expire()` |
| `RefreshSession` | `Invalidate` is idempotent - silently no-ops if not currently `Active`, never throws | `Invalidate()` |

All violations raise `AuthenticationDomainException` (mirroring `Clovent.Identity.IdentityDomainException`'s shape exactly: one sealed type, one static factory method per rule). Structural validation (non-positive `TimeSpan`, empty `AttemptedIdentifier`, malformed `IpAddress`) raises `ArgumentException`/`ArgumentOutOfRangeException` instead, for the same reason documented in `IdentityDomain.md` Section 7.

`Invalidate()` (added in 5.1, see Section 6) is deliberately **not** in the "raises `AuthenticationDomainException`" bucket, and this is itself an invariant worth calling out: every other terminal-state transition on `RefreshSession` (`Revoke`, `Expire`) is strict - calling it from the wrong state is a programming/business-rule error and throws. `Invalidate()` is the one exception, because it exists specifically to be called as a side effect of a *different* aggregate's state change, where the caller cannot and should not be expected to already know this refresh session's current state.

---

## 5. Policies (Password, PIN, Lockout)

All three are immutable `ValueObject`s with a `Default` factory plus a validating `Create(...)` factory for custom configuration, and a synchronous `Evaluate(...)`/`ShouldLock(...)` method. None of them touch a stored credential:

- **`PasswordPolicy`** - shape/strength rules only (length bounds, character-class requirements). `Evaluate(candidate)` returns a `PasswordPolicyResult` listing every violated rule, not just the first. Never sees, stores, or hashes an actual password.
- **`PinPolicy`** - digit-count bounds, digits-only, rejects a single repeated digit ("1111") and strictly sequential runs ("1234", "4321"). Same non-storage boundary.
- **`LockoutPolicy`** - `ShouldLock(recentFailedAttemptCount)` is a pure function of an already-computed count against `MaxFailedAttempts`/`EvaluationWindow`. It deliberately does not query `ILoginAttemptRepository` itself, and (as of 5.1) has no dependency of any kind on Identity - counting attempts within the window is Application-layer orchestration (`RecordLoginAttemptCommandHandler`), and *acting* on the result is delegated to `IIdentityUserService` (Section 9), not a direct call into Identity's aggregate. This keeps the policy synchronous, side-effect-free, and trivially unit-testable.

None of the three has an interface (`IPasswordPolicy`, etc.) - each is a single concrete configurable value, not a swappable strategy, so an interface would be an abstraction with no second implementation to justify it.

---

## 6. Session lifecycle and the RefreshSession cascade

Added in Milestone 5.1. The rule: **when a `Session` is revoked, expires, or is logged out, its active `RefreshSession` (if any) must become invalid too** - otherwise a refresh session could keep renewing access to a session that no longer exists.

`Session` and `RefreshSession` are independent aggregate roots (Section 3), each with its own repository - by design, neither can reach into the other directly, so this cross-aggregate rule cannot live inside either aggregate's own methods. It is enforced at the Application layer instead, by `SessionTerminationCascade` (`Clovent.Authentication.Application.Sessions`):

```csharp
public async Task ApplyAsync(SessionId sessionId, DateTimeOffset nowUtc, CancellationToken cancellationToken)
{
    var refreshSession = await refreshSessionRepository.GetActiveBySessionIdAsync(sessionId, cancellationToken);
    refreshSession?.Invalidate(nowUtc);
}
```

All three session-termination command handlers - `RevokeSessionCommandHandler`, `ExpireSessionCommandHandler`, `LogOutSessionCommandHandler` - take `SessionTerminationCascade` as a constructor dependency and call it immediately after transitioning the `Session`, so the rule applies identically no matter which of the three paths ended the session. This is why `ExpireSessionCommand` and `LogOutSessionCommand` were added in 5.1: Milestone 5 only wired `RevokeSessionCommand` at the Application layer even though the `Session` aggregate already had `Expire()`/`LogOut()` methods - the cascade requirement is what made completing that CQRS surface necessary, not optional polish.

`RefreshSession.Invalidate(nowUtc)` (Section 4) is the domain-level half of this rule: it reuses `RefreshSessionStatus.Revoked` and the `RefreshSessionRevoked` event rather than introducing a fourth near-duplicate terminal state - there is no other place in this domain that distinguishes "revoked by cascade" from "revoked directly," so adding one here would be a distinction without a consumer.

**Directionality is one-way.** Nothing in this milestone makes `RefreshSession` state affect `Session` - only `Session` termination cascades to `RefreshSession`, never the reverse (a rotated or expired refresh session does not end its parent session; the user is expected to be prompted to re-authenticate through whatever flow issues a new one). This asymmetry matches how the two concepts actually relate: a `Session` is the thing being protected; a `RefreshSession` is a renewal mechanism for it, not a peer.

---

## 7. Why `RefreshSession` holds no token value

The brief calls this out explicitly ("Refresh Session model (**domain only**)"), and it's worth being precise about what that means here: `RefreshSession` models the *lifecycle rules* around a renewable credential - when it's valid, when it must be rotated (single-use: `Rotate()` marks the current one `Rotated` and returns its replacement in one call, the standard mitigation for refresh-token replay), when it expires, when it must be invalidated by cascade (Section 6) - without ever generating, storing, or comparing the actual opaque secret a client would present. Generating a cryptographically random token value and verifying a client-presented value against a stored one are security/Infrastructure concerns, explicitly out of scope. A future Infrastructure milestone is expected to generate the actual secret, associate it with a `RefreshSessionId`, and verify it - this aggregate is the seam that work plugs into.

---

## 8. Credential ownership

Added in Milestone 5.1, resolving what Milestone 5's Section 9 (then item 1) left open. The instruction was explicit and is worth restating precisely: model `PasswordHash`, `PinHash`, `SecurityStamp`, `PasswordHistory`, `FailedAttempts`, and `LastPasswordChangedUtc` as **Authentication-owned** concepts - not as an extension of `Identity.Users.User`, and not as a new "Credential" aggregate.

**"Authentication-owned" here means bounded-context ownership, not entity-instance ownership.** These six concepts belong to Authentication - Identity has no knowledge of hashes, PINs, or password history, and never will, because verifying a credential is not "who is this person," it's "did they prove it." That is the actual reason credentials don't belong on `User`: it isn't a modeling convenience, it's the same Identity/Authentication boundary already drawn in Section 3, applied to a new set of concepts.

What Milestone 5.1 deliberately does **not** do is decide which entity holds a `UserId` and these six values together, or how that entity is persisted - doing so would effectively be creating the prohibited "Credential Aggregate" in substance if not in name (extending `AggregateRoot<TId>` isn't the only way to commit to a persistence boundary; bundling all six into one cohesive class with a `UserId` foreign key does the same thing, just informally). So each concept is modeled as an independent, fully-tested domain type in `Clovent.Authentication/Credentials/`, with no owner reference and no repository:

| Type | Kind | Models |
|---|---|---|
| `PasswordHash` | Value Object | An opaque, already-computed password hash. Never computes one. |
| `PinHash` | Value Object | An opaque, already-computed PIN hash. Kept distinct from `PasswordHash` for type safety, same reasoning as Identity's per-aggregate name types. |
| `SecurityStamp` | Value Object | An opaque, unpredictable marker that changes on security-relevant events, invalidating anything that cached the old value. `Generate()` uses `Guid.NewGuid()` - this is not hashing or encryption (no secret is derived), so it stays in the domain. |
| `PasswordHistory` | Value Object | The trailing set of prior password hashes (most recent first, capped at `DefaultMaxSize = 5`), with `Contains(hash)` for reuse-prevention and `WithNewPassword(...)` to append immutably. |
| `PasswordHistoryEntry` | Value (record) | One `PasswordHistory` entry: a `PasswordHash` plus when it was set. |
| `FailedAttempts` | Value Object | A denormalized consecutive-failure counter (`Increment()`/`Reset()`/`MeetsOrExceeds(threshold)`) - a cheap complement to `LockoutPolicy`'s window-based counting over `LoginAttempt` history, not a replacement for it. |

**`LastPasswordChangedUtc` was folded into `PasswordHistory` rather than given its own type.** `PasswordHistory.LastChangedAtUtc` derives from the most recent entry's timestamp. Tracking "when did the password last change" as a value separate from "what is the password history" would mean the same fact could drift out of sync between the two the moment someone updated one without the other - deriving it removes that failure mode entirely rather than requiring discipline to avoid it. This is the only place Milestone 5.1 merged two of the brief's six named concepts; the other five map one-to-one onto their own type.

**What remains genuinely open** (see Section 10, item 1): *which* entity eventually holds a `UserId` alongside instances of these six types, and how it's persisted. That is precisely the decision this milestone's instructions say not to make. These types are the vocabulary that decision will be made in terms of, not a preview of the answer.

---

## 9. Application layer: CQRS via MediatR, and the Identity boundary

`06 Coding Standards/06.01 Coding Standards.md` documents "CQRS: Commands for write operations, Queries for read operations handled via MediatR handlers" as the standard - already approved, simply not yet used anywhere in the product solution before Milestone 5. `MediatR` is not a new architectural decision, it's the first real use of an existing one.

Every Command/Query is an `IRequest<TResponse>` record with a co-located `IRequestHandler<TRequest, TResponse>`. Handlers take repository/service interfaces and `TimeProvider` as constructor parameters (primary-constructor DI, no service locator) and are pure orchestration: load aggregate(s), call domain methods, persist. No DI container registration/`AddApplication()` wiring is included in this milestone - there is no host to register into yet, so registration is deferred to the milestone that introduces one.

### Cross-module communication: `IIdentityUserService`

Milestone 5's `RecordLoginAttemptCommandHandler` loaded `Clovent.Identity.Users.User` via `IUserRepository` and called `user.Lock()` directly when the lockout threshold was met. That is a direct Application-layer dependency on another bounded context's aggregate and repository - exactly the coupling Section 3 now prohibits, and the Solution Architect flagged it in review.

**Fix:** `IIdentityUserService` (`Clovent.Authentication.Application`) - an interface **Authentication defines and owns**, expressed in Authentication's own vocabulary:

```csharp
public interface IIdentityUserService
{
    Task<bool> IsUserActiveAsync(Guid userId, CancellationToken cancellationToken = default);
    Task LockUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
```

This is the Dependency Inversion Principle applied at the module boundary: the module that *needs* a capability defines the interface for it, rather than depending on the concrete type that happens to provide it today. `RecordLoginAttemptCommandHandler` now depends only on this interface - not on `Clovent.Identity.Users.IUserRepository`, `User`, or `UserStatus`. `Clovent.Authentication.Application` still references the `Clovent.Identity` project (for the `UserId` type reuse described in Section 3), but nothing in it touches Identity's aggregate or repository anymore.

**No implementation exists yet** - same as every repository interface in this solution, this is a seam. A future Infrastructure/Application-composition milestone is expected to implement it, most plausibly by adapting `Clovent.Identity.Users.IUserRepository` and calling `User.Lock()` from *outside* the Authentication project, where that coupling is legitimate (something has to eventually connect the two bounded contexts; the point of this refactor is that it isn't Authentication's own Application layer that does it). Tests use `FakeIdentityUserService` (`Clovent.Authentication.Application.Tests/TestSupport/`), which needs no real `Identity.Users.User` instance at all - a smaller, simpler test double than the one it replaced.

Why `IIdentityUserService` was chosen over the other explicitly-suggested option (a general "orchestration interface"): a single, narrowly-scoped interface naming exactly the two capabilities `RecordLoginAttemptCommandHandler` needs (`IsUserActiveAsync`, `LockUserAsync`) is easier to fake in tests and easier to reason about than a broader "Identity facade" that would accumulate unrelated methods as more cross-context needs arise later. If a second cross-context need appears in a future milestone, it gets its own narrowly-scoped interface rather than growing this one - Interface Segregation over a shared "kitchen sink" port.

`NotFoundException` (`Clovent.Authentication.Application`) is a single Application-layer exception raised when a command/query targets an aggregate ID that doesn't exist - distinct from `AuthenticationDomainException` (an *existing* aggregate's invariant was violated). Every handler that loads-by-ID uses it, avoiding a repeated ad-hoc `InvalidOperationException` per handler.

Session start/refresh-session issuance take an optional `TimeSpan` (idle timeout / lifetime) as a command parameter with a sane default, rather than reading from configuration - see Section 10, item 2.

---

## 10. Open questions for Solution Architect review

1. **Credential storage entity and persistence.** Section 8 models `PasswordHash`/`PinHash`/`SecurityStamp`/`PasswordHistory`/`FailedAttempts` as standalone types with no owner. **Needs a decision**: what holds a `UserId` alongside these (a new Authentication-owned entity distinct from an "aggregate" in the strict sense, a value bag returned by `IIdentityUserService`-style abstraction, something else), and does it get its own repository once persistence is in scope? This is the same question Milestone 5 raised, now narrower: the vocabulary exists, only the owning structure and persistence boundary remain undecided.
2. **Configuration for policy defaults and timeouts.** `PasswordPolicy.Default`, `PinPolicy.Default`, `LockoutPolicy.Default`, `PasswordHistory.DefaultMaxSize`, and the session idle timeout / refresh lifetime defaults in the Application handlers are all hardcoded constants. **Needs a decision**: should these become configuration-driven via `Clovent.Platform.Configuration.OptionsRegistrationExtensions.AddValidatedOptions<T>()` once a host exists to bind them?
3. **`IIdentityUserService`'s implementation.** The interface exists; nothing implements it yet, and no DI registration exists for it. **Needs a decision**: does its implementation live in a new Infrastructure project, or in an "Application composition" layer that's allowed to see both bounded contexts' Application projects? This is a slightly sharper version of the general "where does Infrastructure live" question every milestone so far has deferred.
4. **MediatR pipeline behaviors.** This milestone wires bare `IRequestHandler`s with no cross-cutting pipeline (validation, logging, transactions). **Needs a decision**: does CBOS adopt `MediatR` pipeline behaviors for these concerns platform-wide, and if so, where do they live?
5. **Event dispatch.** Same open question as `IdentityDomain.md` Section 8, item 3 - still unresolved, now relevant to two bounded contexts' worth of events instead of one.

**Resolved in 5.1** (previously open, kept here for traceability): direct Authentication→Identity aggregate coupling (now `IIdentityUserService`, Section 9); Session↔RefreshSession cascade on termination (now `SessionTerminationCascade`, Section 6).

---

## 11. What is deliberately *not* here

Per the milestone briefs (5 and 5.1 combined):

- Persistence (no EF Core, no `DbContext`, no migrations, no SQL) - repository interfaces only
- UI, DevExpress, API endpoints (no Minimal APIs, no controllers)
- JWT generation, OAuth, OpenID Connect, external providers
- ASP.NET Identity
- Actual password/PIN hashing or encryption - `PasswordHash`/`PinHash` hold already-computed values only (Section 8)
- A concrete owning entity or repository for the Section 8 credential types (Section 10, item 1)
- `IIdentityUserService`'s implementation (Section 10, item 3)
- Domain event dispatching - modeled and recorded, never published
- DI container registration (`AddApplication()`/`AddInfrastructure()`/`AddPersistence()`) - no host exists yet to register into
