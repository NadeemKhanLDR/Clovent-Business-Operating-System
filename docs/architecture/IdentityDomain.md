---
title: Identity Domain Reference
type: Architecture
status: Awaiting Solution Architect review
created: 2026-07-27
updated: Milestone 13
applies_to: src/Clovent.Domain, src/Clovent.Identity
---

# Identity Domain Reference

This document describes `src/Clovent.Domain` and `src/Clovent.Identity` as they exist after Milestone 4: what they are for, how they are organized, and what a future Application/Infrastructure/Authentication milestone is expected to build on top of them. Scope for this milestone was **domain only** - no persistence, no EF Core, no authentication, no UI. Both projects contain nothing but domain model: aggregates, value objects, domain events, and repository *interfaces*.

---

## 1. Why two new projects, not one

Neither `AggregateRoot<TId>`, `Entity<TId>`, `ValueObject`, nor `IDomainEvent` existed anywhere in the product solution before this milestone. `07 Domain Driven Design/07.01 Domain Model.md` documents them as the standard, but the only prior implementation was `Tools/Clovent.CLI/src/Clovent.Shared/Domain/DomainPrimitives.cs` - explicitly an internal CLI proof-of-concept, not part of the product, and coupled to `MediatR.Contracts` (`IDomainEvent : INotification`), a coupling `09 Security/09.02 Identity Package Inventory.md` flags as needing explicit architect sign-off before being baked into every future module's domain layer.

Given that, and given `Clovent.Platform` is frozen for this milestone, two new projects were created:

- **`Clovent.Domain`** - a shared kernel: `Entity<TId>`, `AggregateRoot<TId>`, `ValueObject`, `IDomainEvent`, `DomainException`. Zero external dependencies (not even `Clovent.Platform`), so any future bounded context (Identity, Restaurant POS, etc.) can depend on it without inheriting Identity-specific concepts. `IDomainEvent` is a first-party marker interface with no messaging-library dependency, sidestepping the `MediatR.Contracts` coupling question entirely rather than resolving it - if the architect later wants domain events to *be* `INotification`s for direct MediatR dispatch, that is a compatible, additive change at the Application layer, not a Domain-layer rewrite.
- **`Clovent.Identity`** - the Identity bounded context itself, referencing only `Clovent.Domain`.

This was a deliberate deviation from the milestone brief's literal instruction to "use the existing Identity project" - no such project existed in `src/`; the only candidate, `src/Clovent.Authentication`, is a different, already-substantial module (RBAC persistence, password/PIN hashing, tokens, sessions) whose contents are exactly what this milestone explicitly excludes. This was confirmed with the requester before any code was written; see Section 8.

---

## 2. Folder structure

```
src/Clovent.Domain/
  Clovent.Domain.csproj
  Entity.cs              - base type for identity-based equality
  AggregateRoot.cs        - Entity<TId> + domain event recording
  ValueObject.cs           - base type for attribute-based equality
  IDomainEvent.cs           - marker interface, no messaging dependency
  DomainException.cs         - base for invariant-violation exceptions

src/Clovent.Identity/
  Clovent.Identity.csproj
  IdentityDomainException.cs   - one exception type, one static factory per rule
  Users/
    User.cs, UserId.cs, UserStatus.cs, IUserRepository.cs
    ValueObjects/  Email.cs, UserName.cs, PersonName.cs, DisplayName.cs, PhoneNumber.cs
    Events/        UserCreated, UserActivated, UserDeactivated, UserLocked,
                    UserUnlocked, UserRoleAssigned, UserRoleRemoved, UserDisplayNameChanged
  Roles/
    Role.cs, RoleId.cs, IRoleRepository.cs
    ValueObjects/  RoleName.cs
    Events/        RoleCreated, RoleRenamed, PermissionAssignedToRole, PermissionRemovedFromRole
  Permissions/
    Permission.cs, PermissionId.cs, IPermissionRepository.cs
    ValueObjects/  PermissionCode.cs
    Events/        PermissionCreated
  Organizations/
    Organization.cs, OrganizationId.cs, IOrganizationRepository.cs
    ValueObjects/  OrganizationName.cs
    Events/        OrganizationCreated, CompanyAddedToOrganization, CompanyRemovedFromOrganization
  Companies/
    Company.cs, CompanyId.cs, ICompanyRepository.cs
    ValueObjects/  CompanyName.cs
    Events/        CompanyCreated, BranchAddedToCompany, BranchRemovedFromCompany
  Branches/
    Branch.cs, BranchId.cs, IBranchRepository.cs
    ValueObjects/  BranchName.cs
    Events/        BranchCreated
```

`src/Clovent.Domain.Tests` and `src/Clovent.Identity.Tests` mirror these one-for-one, following the pattern already established by `Clovent.Platform` / `Clovent.Platform.Tests`.

Each aggregate owns its own `ValueObjects/` and `Events/` subfolder - no folder mixes concerns, and no aggregate's model is split across folders, mirroring the discipline documented in `PlatformFoundation.md` Section 2.

---

## 3. Aggregate boundaries and relationships

Six aggregate roots, each with its own strongly-typed ID and its own repository interface:

| Aggregate | Identity | Owns (by reference) | Belongs to |
|---|---|---|---|
| `User` | `UserId` | `RoleId` set (assigned roles) | - |
| `Role` | `RoleId` | `PermissionId` set (granted permissions) | - |
| `Permission` | `PermissionId` | - | - |
| `Organization` | `OrganizationId` | `CompanyId` set | - |
| `Company` | `CompanyId` | `BranchId` set | `OrganizationId` (fixed at creation) |
| `Branch` | `BranchId` | - | `CompanyId` (fixed at creation) |

**Every cross-aggregate relationship is a reference by strongly-typed ID, never an embedded object.** This is the single most load-bearing design decision in this milestone, so it is worth stating the reasoning once:

The brief lists `Organization`, `Company`, and `Branch` as three *separate* aggregate roots, each with its own repository contract (`IOrganizationRepository`, `ICompanyRepository`, `IBranchRepository`). Standard DDD practice is that only one aggregate is loaded and saved per transaction, and aggregates reference each other by identity only - if `Organization.AddCompany` took a full `Company` object and embedded it, `Company` would no longer be independently loadable/saveable through its own repository without data living in two places at once. So:

- `Organization.AddCompany(CompanyId)` / `RemoveCompany(CompanyId)` record *membership* (an `OrganizationId ↔ CompanyId` relationship) from the Organization side.
- `Company.OrganizationId` is set once at creation and never changes - a company doesn't move between organizations in this model.
- `Company.AddBranch(BranchId)` / `RemoveBranch(BranchId)` mirror the same pattern one level down.
- `Branch.CompanyId` is set once at creation.
- `User.RoleIds` / `Role.PermissionIds` follow the identical pattern for the Users→Roles→Permissions chain.

This does mean an aggregate's membership set (e.g. `Organization.CompanyIds`) and the child's own parent pointer (`Company.OrganizationId`) are two representations of the same fact, both enforced independently. A future Application-layer command handler orchestrating "create a company under this organization" is responsible for keeping both sides consistent (create the `Company`, then call `Organization.AddCompany`) - this domain layer cannot enforce that invariant across two aggregates in one transaction, by design; only within a single aggregate's boundary.

---

## 4. Invariants enforced

| Aggregate | Rule | Enforced by |
|---|---|---|
| `User` | Cannot activate while `Locked` (must `Unlock` first) | `Activate()` |
| `User` | Cannot activate an already-`Active` user | `Activate()` |
| `User` | Can only `Deactivate`/`Lock` a currently-`Active` user | `Deactivate()`, `Lock()` |
| `User` | Can only `Unlock` a currently-`Locked` user | `Unlock()` |
| `User` | Cannot assign a role already assigned | `AssignRole()` |
| `User` | Cannot remove a role that isn't assigned | `RemoveRole()` |
| `Role` | Cannot grant a permission already granted | `AddPermission()` |
| `Role` | Cannot revoke a permission that isn't granted | `RemovePermission()` |
| `Organization` | Cannot add a company that already belongs | `AddCompany()` |
| `Organization` | Cannot remove a company that doesn't belong | `RemoveCompany()` |
| `Company` | Cannot add a branch that already belongs | `AddBranch()` |
| `Company` | Cannot remove a branch that doesn't belong | `RemoveBranch()` |

Every violation raises `IdentityDomainException` (see Section 7). Structural/input validation (empty strings, malformed formats, out-of-range lengths) is enforced by value object factory methods and raises `ArgumentException` instead - see Section 5 for why the two are kept distinct.

`ChangeDisplayName()` and `Rename()` are no-ops (no state change, no event) when the new value equals the current one, rather than throwing - re-asserting the same name isn't a rule violation.

`User` starts in `PendingActivation`; `Activate()`/`Deactivate()`/`Lock()`/`Unlock()` move it through `Active`/`Inactive`/`Locked`. There is currently no path back from `Inactive` other than `Activate()` (allowed) and no `Deactivate()` from `Locked` (must `Unlock()` first) - see Section 8 for the open question this raises.

---

## 5. Value objects

| Value object | Aggregate | Validates |
|---|---|---|
| `Email` | User | Non-empty, ≤254 chars, matches a basic address pattern; normalized to lowercase/trimmed |
| `UserName` | User | 3-32 chars, starts with a letter, `[a-zA-Z0-9._-]`; equality is case-insensitive |
| `PersonName` | User | Non-empty `FirstName`/`LastName`, ≤100 chars each; exposes `FullName` |
| `DisplayName` | User | Non-empty, ≤100 chars |
| `PhoneNumber` | User | Loose E.164 shape after stripping spaces/dashes |
| `RoleName` | Role | 2-64 chars; equality is case-insensitive |
| `PermissionCode` | Permission | 2-5 lowercase dot-separated segments (e.g. `identity.users.manage`); normalized to lowercase |
| `OrganizationName` | Organization | 2-200 chars |
| `CompanyName` | Company | 2-200 chars |
| `BranchName` | Branch | 2-200 chars |

**`PersonName` vs. `DisplayName`:** these are deliberately separate value objects, not one collapsed into the other. `PersonName` is the structured legal name (`FirstName`/`LastName`); `DisplayName` is the independently-changeable name shown throughout the product (a nickname, a preferred name). `User.ChangeDisplayName()` only ever touches the latter - this milestone does not add a "change legal name" operation since none was requested, so `PersonName` is currently present as a value object with no corresponding `User` behavior yet. Flagged in Section 8.

Every per-aggregate `*Name` value object (`RoleName`, `OrganizationName`, `CompanyName`, `BranchName`) is intentionally its own type rather than one shared `Name` type reused across aggregates, even though their validation rules are nearly identical - this trades a small amount of duplication for compile-time protection against passing a `CompanyName` where an `OrganizationName` is expected.

Permission's `Description` is a plain validated `string`, not a value object - it carries no invariant beyond "non-empty, ≤500 chars" and isn't used for equality, lookup, or business logic anywhere, so a wrapper type would add a layer without adding meaning.

---

## 6. Domain events

One event per business-significant state transition - every public mutating method on every aggregate raises exactly one event (or zero, for the two no-op cases described in Section 4):

- **User**: `UserCreated`, `UserActivated`, `UserDeactivated`, `UserLocked`, `UserUnlocked`, `UserRoleAssigned`, `UserRoleRemoved`, `UserDisplayNameChanged`
- **Role**: `RoleCreated`, `RoleRenamed`, `PermissionAssignedToRole`, `PermissionRemovedFromRole`
- **Permission**: `PermissionCreated`
- **Organization**: `OrganizationCreated`, `CompanyAddedToOrganization`, `CompanyRemovedFromOrganization`
- **Company**: `CompanyCreated`, `BranchAddedToCompany`, `BranchRemovedFromCompany`
- **Branch**: `BranchCreated`

All events are immutable `sealed record`s implementing `IDomainEvent`, carrying only the identifiers/values relevant to that event plus `OccurredOnUtc`. None are dispatched anywhere - per the milestone brief, only the modeling is in scope. `AggregateRoot<TId>.AddDomainEvent` is `protected`, so only the aggregate itself can raise its own events; callers observe them afterward via the public `DomainEvents` collection and clear them via `ClearDomainEvents()` once handled (the same shape a future outbox/dispatcher would use).

---

## 7. Exceptions: `DomainException` vs. `ArgumentException`

Two distinct failure categories, kept deliberately separate:

- **Structural/input validation** (a value object's raw input doesn't have a valid shape - empty, too long, wrong pattern) raises `ArgumentException`/`ArgumentNullException` directly from the value object's `Create()` factory or an aggregate method's guard clause. This is standard .NET practice for "this argument's value is unusable," not a business rule.
- **Business invariant violations** (an operation is not valid given the aggregate's *current state* - activating an already-active user, granting a permission a role already has) raise `IdentityDomainException`, a single `sealed` type deriving from `Clovent.Domain.DomainException` with one static factory method per rule (`IdentityDomainException.UserAlreadyActive(userId)`, etc.). One type rather than one subclass per rule keeps `catch (IdentityDomainException)` meaningful as "an Identity Domain rule was broken" while the factory method name and resulting message still identify exactly which rule and which aggregate instance.

---

## 8. Open questions for Solution Architect review

This milestone made the following judgment calls where the brief was silent or where the repository's actual state didn't match the brief's assumptions. All are reversible without touching Platform Foundation, but should be ratified (or overridden) before an Application/Infrastructure milestone builds on top of this domain model:

1. **Project topology.** Confirmed with the requester mid-milestone (no `Clovent.Identity` project existed; `Clovent.Authentication` was rejected as the target since its existing contents overlap with this milestone's explicit exclusions): created `Clovent.Domain` (shared kernel) and `Clovent.Identity` (bounded context) as two new projects. **Needs sign-off**, particularly on whether `Clovent.Domain` is the right long-term home for shared DDD primitives across *all* future modules, or whether Platform Foundation should eventually absorb it.
2. **Relationship to `Clovent.Authentication` - RESOLVED in Milestone 5.** At the time this was written, `src/Clovent.Authentication` was a 285-file scaffold of empty placeholder classes (confirmed via inspection: `wc -l` across every `.cs` file in the project totaled 6 lines, all from the default SDK-template `Class1.cs` - zero actual implementation). Milestone 5 replaced that scaffold with the real Authentication Domain, which references `Clovent.Identity` rather than duplicating it: `Clovent.Authentication`'s `Session`/`LoginAttempt` aggregates hold a `Clovent.Identity.Users.UserId`, and `Clovent.Authentication.Application`'s lockout orchestration calls `Clovent.Identity.Users.User.Lock()` directly. See [AuthenticationDomain.md](AuthenticationDomain.md). There are no longer two parallel notions of "User" - Identity owns the concept, Authentication depends on it.
3. **`IDomainEvent` and MediatR.** `Clovent.Domain.IDomainEvent` deliberately does *not* extend `MediatR.INotification` (unlike the CLI tool's proof-of-concept), avoiding the coupling `09.02 Identity Package Inventory.md` flagged as needing sign-off. **Needs a decision** on how domain events actually get dispatched once Application/Infrastructure exists - MediatR, a first-party in-process bus, or an outbox pattern - since that determines whether this interface needs to grow a base interface or an adapter later.
4. **User ↔ Organization/Company/Branch scoping.** Multi-tenancy fields exist on `Clovent.Platform.Execution.IExecutionContext` (`OrganizationId`, `CompanyId`, `BranchId`, alongside `UserId`), implying a `User` operates within some tenant scope - but no such relationship was requested in this milestone's explicit `User` business rules, so none was added. **Needs a decision**: should `User` hold a direct reference (e.g. `CompanyId`) once Application-layer work begins, or is that relationship modeled elsewhere (e.g. a separate membership/assignment concept)?
5. **`PersonName` without a corresponding `User` behavior.** `PersonName` exists as a value object (per the brief's example list) but no `User` method currently sets or changes it - only `Email`, `UserName`, and `DisplayName` are populated at `Create()`. **Needs a decision**: add `PersonName` to `User.Create()`'s signature (most likely correct, but changes the aggregate's public API) or remove it until a concrete requirement exists.
6. **Locked → Deactivated path.** A locked user cannot currently be deactivated directly (`Deactivate()` requires `Active`) - it must be unlocked first. This mirrors the same "unlock before activate" rule already in place for `Activate()`, but **was not explicitly specified** and may not match the intended offboarding flow for a user who is locked out and being terminated.

---

## 9. What is deliberately *not* here

Per the milestone brief, this domain model contains no:

- Persistence (no EF Core, no `DbContext`, no migrations, no SQL)
- Authentication (no login, no JWT, no password/PIN hashing, no refresh tokens, no `ClaimsPrincipal`, no ASP.NET Identity)
- Application layer (no command/query handlers, no `AddApplication()`/`AddInfrastructure()`/`AddPersistence()`, no `IModule` implementation)
- UI, API, or DevExpress references
- Domain event *dispatching* - events are modeled and recorded, never published
- Specifications - no Specification pattern exists elsewhere in the repository to be consistent with, so none was introduced

Repository interfaces (`IUserRepository`, `IRoleRepository`, `IPermissionRepository`, `IOrganizationRepository`, `ICompanyRepository`, `IBranchRepository`) are contracts only, each with the minimum methods the aggregate's own behavior implies a caller would need (`GetByIdAsync`, a natural-key lookup where one exists, `AddAsync`) - no implementation, no Unit of Work, no generic repository base.

---

## 10. Milestone 13 addendum: Organization/Company/Branch are enriched, and this is no longer domain-only

Milestone 13 ("Organization & Master Data Foundation") enriches `Organization`/`Company`/`Branch` (`TaxId`/`Address`, a `Status` per aggregate, `Rename`/`Activate`/`Deactivate`/`SetTaxId`/`SetAddress`, new domain events) and gives them their first Application and Infrastructure layers - see `OrganizationArchitecture.md` for the full detail. Section 9's "no persistence, no Application layer" description above is now historical for these three aggregates specifically; `User`/`Role`/`Permission` had already gained their own Application/Infrastructure layers in Milestones 9-10, so this milestone completes the pattern for the last three aggregates in this project that still lacked it.

Everything else in this document (the two-project split, `AggregateRoot<TId>`/`ValueObject`/`IDomainEvent` primitives, strongly-typed ids, the constructor-binding convention) is unchanged.
