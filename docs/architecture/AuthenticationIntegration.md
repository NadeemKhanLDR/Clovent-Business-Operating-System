---
title: Authentication Integration Reference
type: Architecture
status: Awaiting Solution Architect review
created: 2026-07-27
updated: Milestone 9
applies_to: src/Clovent.Desktop, src/Clovent.Identity.Infrastructure, src/Clovent.Authentication.Application, src/Clovent.Authentication.Infrastructure
---

# Authentication Integration Reference

Milestone 9 connects the Milestone 8 Login form to real credential checking, session creation, and account lockout - closing several seams every prior milestone's doc had deliberately left open ("no implementation exists yet"). This is the biggest single milestone so far in scope, because none of those seams could be closed independently.

---

## 1. New: `Clovent.Identity.Infrastructure`

Login cannot resolve a submitted username/email to a user without Identity persistence, which never existed before this milestone (`09.02 Identity Package Inventory.md` even describes `Clovent.Authentication`'s *pre-Milestone-5* scaffold, and Identity itself has only ever had a Domain layer). Mirrors `Clovent.Authentication.Infrastructure`'s shape exactly: `IdentityDbContext` (schema `Identity`), `UserConfiguration`, `UserRepository`, a design-time factory, an `InitialCreate` migration, `IPersistenceInitializer`.

**Deliberately maps only `User`** - `RoleIds` is `Ignore()`d. Nothing in this milestone assigns or reads a role (that's Milestone 10, "Authorization"), and mapping a private `HashSet<RoleId>` field correctly is a real design decision (owned collection table vs. primitive collection vs. replaying `AssignRole`, which would spuriously raise domain events during materialization) that deserves Milestone 10's full attention, not a guess made in passing here.

**`User`'s constructor gained `status`/`createdAtUtc` parameters** - identical reasoning and identical bug class to the Session/RefreshSession fix in `AuthenticationInfrastructure.md` Section 4: the original constructor hardcoded `PendingActivation`/`DateTimeOffset.UtcNow`, which would have silently reset both fields on every load. `UserRepositoryTests.Activate_ThenReload_PersistsNewStatus` asserts `CreatedAtUtc` specifically survives a round trip, since that's exactly the bug this fix prevents.

---

## 2. Two previously-open cross-context questions, resolved

**`IIdentityUserService`'s implementation** (`AuthenticationDomain.md` Section 10, item 3; `IdentityDomain.md` Section 8, item 2) - both docs speculated it would be implemented "from outside the Authentication project, where that coupling is legitimate." `Clovent.Desktop.Composition.IdentityUserServiceAdapter` is exactly that: it lives in the composition root (`Clovent.Desktop`), adapts `Clovent.Identity.Users.IUserRepository`, and calls `User.Lock()` - the one place in the whole solution allowed to reference both bounded contexts' concrete types.

It also had to solve a persistence problem neither doc anticipated: `LockUserAsync` mutates a `User` tracked by `IdentityDbContext`, but Authentication's `UnitOfWorkBehavior` (Section 4 below) only commits `AuthenticationDbContext` - a completely different DbContext. `IdentityUserServiceAdapter` therefore takes `IdentityDbContext` directly and commits it itself, rather than introducing a parallel `IIdentityUnitOfWork` abstraction for what is currently exactly one call site.

**Authentication's own `AddApplication()`** (`AuthenticationInfrastructure.md` Section 12, item 4) - deferred because "there is still no host/API project." `Clovent.Desktop` is that host; `Clovent.Authentication.Application.DependencyInjection.ApplicationServiceCollectionExtensions.AddApplication` now registers MediatR, scanning the assembly for every handler already written across Milestones 5/5.1/9.

---

## 3. Unit of Work, finally wired: `UnitOfWorkBehavior`

`AuthenticationInfrastructure.md` Section 8 flagged that nothing called `IUnitOfWork.SaveChangesAsync` - true until this milestone actually needed mutations (a locked account, an incremented failure counter, a new `Session`) to persist. `UnitOfWorkBehavior<TRequest,TResponse>` (`Clovent.Authentication.Infrastructure.Persistence`) is a MediatR `IPipelineBehavior<,>` registered as an **open generic**, so it wraps every Authentication Application command/query without this project referencing their concrete types. It calls `next()`, then `unitOfWork.SaveChangesAsync()` - a failed handler never triggers a save (`UnitOfWorkBehaviorTests.Handle_WhenNextThrows_DoesNotSaveChanges` asserts this explicitly).

**New command**: `RecordCredentialCheckCommand` (`Clovent.Authentication.Application.Credentials`) - increments/resets `UserCredentials.FailedAttempts`. Separate from `RecordLoginAttemptCommand` deliberately: one is an audit fact (an immutable `LoginAttempt`), the other mutates the credential record's own state. A login flow issues both.

---

## 4. Password/PIN hashing: `IPasswordHasher`/`IPinHasher`

New Application-owned interfaces (same Dependency Inversion pattern as `IIdentityUserService`), implemented in Infrastructure by `Pbkdf2PasswordHasher`/`Pbkdf2PinHasher`, both delegating to one shared `Pbkdf2Hash` helper (PBKDF2-SHA256, 210,000 iterations, random 16-byte salt, `CryptographicOperations.FixedTimeEquals` for verification) so the actual crypto exists in exactly one place. Deliberately **not** `Microsoft.Extensions.Identity.Core`'s hasher - `09.02 Identity Package Inventory.md` flagged that package as needing an explicit architect decision before adoption (it would couple `User` toward `IdentityUser<TKey>`, conflicting with the plain-`AggregateRoot<TId>` design Milestone 4 already committed to), and nothing in this milestone's scope required revisiting that.

---

## 5. `LoginService`: one DI scope per login attempt

`LoginService` (`Clovent.Desktop.Login`) replaces Milestone 8's placeholder registration only - `LoginForm` is unchanged, exactly as that milestone's seam was designed to allow. Creates one `IServiceScope` per `LoginAsync` call and resolves every scoped dependency (`IUserRepository`, `IUserCredentialsRepository`, `IMediator`, the hashers) from it - necessary because a WinForms app has no ambient "current scope" the way an ASP.NET Core request does, and `UnitOfWorkBehavior` only commits the `AuthenticationDbContext` instance that was resolved into the *same* scope as the command it wraps.

Flow: resolve user (tries `UserName.Create` then `Email.Create` - a submitted identifier that matches neither shape falls through to "not found" rather than throwing) → check `UserStatus` → verify the submitted password/PIN against `UserCredentials` via the hashers → `RecordCredentialCheckCommand` + `RecordLoginAttemptCommand` → on success, `StartSessionCommand`, optionally `IssueRefreshSessionCommand` (only if `RememberMe`), then `ICurrentSession.SignIn`. Every failure path returns the same generic `"Invalid username or password."` message regardless of *why* it failed (user not found vs. wrong password) - standard practice against username enumeration - except account-status failures (`locked`/`not active yet`), which are informative on purpose since they don't reveal whether the credential itself was right.

**Five consecutive failures lock the account** end-to-end through real components, not mocks: `LoginServiceTests.LoginAsync_FifthConsecutiveFailure_LocksTheUser` drives real MediatR through a real `RecordLoginAttemptCommandHandler`, which evaluates the real `LockoutPolicy.Default` (5 failures / 15 minutes) and calls the real `IIdentityUserService` contract - only the repositories and the DbContext-backed pieces are faked, and even the password hashing is the real PBKDF2 implementation.

---

## 6. `ICurrentSession` vs. `IExecutionContext` - not redundant

`Clovent.Platform.Execution.IExecutionContext`/`IExecutionContextAccessor` is `AsyncLocal`-backed, scoped ambient state for **one unit of work** (a command, a query), pushed via `ExecutionContextScope` and restored on dispose - the same shape as ASP.NET Core's `IHttpContextAccessor`. It is the wrong tool for "who is signed into this desktop app right now": that's session-lifetime state, not per-operation state, and forcing it into that role would mean either never disposing the scope (defeating its restore-on-dispose design) or re-opening it per operation from something else that remembers who's signed in - which is exactly `ICurrentSession`'s job.

`Clovent.Desktop.Sessions.ICurrentSession`/`CurrentSession` (namespace **`Sessions`**, plural - a first attempt at `Session`, singular, collided with `Clovent.Authentication.Sessions.Session`: C# namespace member lookup also searches enclosing namespaces, so any file under `Clovent.Desktop.*` trying to use the unqualified domain type `Session` would have silently resolved to the sibling namespace instead, a `CS0118` "namespace used like a type" error at every call site) is a simple, long-lived, explicitly-set/cleared singleton: `SignIn(userId, sessionId)` / `SignOut()` / `IsAuthenticated`. `LoginService` calls `SignIn` on success. **The two are complementary, not redundant**: once a per-operation command-dispatch pipeline exists (nothing in this solution needs one yet beyond MediatR's own pipeline), it is expected to open an `ExecutionContextScope` derived *from* `ICurrentSession.UserId` around each operation - flagged as an open question below.

---

## 7. Development-only seed data

No milestone across 7-12 includes a user-registration flow, so nothing can create the *first* user to log in as. `Clovent.Desktop.Seed.DevelopmentUserSeedStartupTask` (an `IStartupTask`, runs after migrations per the existing Platform Foundation pipeline) creates one demo user (`admin` / `Admin123!`) only if none exists, gated by an explicit, required `Desktop:SeedDevelopmentUser` configuration boolean - not an implicit "Development environment" check, so a production `appsettings.Production.json` must deliberately opt in rather than silently inherit a development default. Not a substitute for real user provisioning.

---

## 8. Open questions for Solution Architect review

1. **`ExecutionContextScope` wiring** (Section 6). Needs a decision on when/where a per-operation `ExecutionContextScope` gets opened from `ICurrentSession` - most plausibly a MediatR pipeline behavior once one exists for this purpose specifically (distinct from `UnitOfWorkBehavior`).
2. **Development seed data** (Section 7). Confirm `DevelopmentUserSeedStartupTask` is acceptable to ship gated behind configuration, versus requiring a real (even if minimal) registration command before any milestone reaches production.
3. **`IdentityUserServiceAdapter` taking `IdentityDbContext` directly** (Section 2). Ratify or override: introduce a proper `IIdentityUnitOfWork` abstraction once a second caller needs one, or keep the direct dependency since this adapter is already a deliberate, singular cross-boundary exception.
4. **Generic failure-message policy** (Section 5). Confirm hiding *why* a login failed (except account-status reasons) is the desired security posture, versus a product requirement to be more specific.
