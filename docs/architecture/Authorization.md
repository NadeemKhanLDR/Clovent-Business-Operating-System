---
title: Authorization Reference
type: Architecture
status: Awaiting Solution Architect review
created: 2026-07-27
updated: Milestone 15
applies_to: src/Clovent.Identity.Application, src/Clovent.Identity.Infrastructure, src/Clovent.Identity
---

# Authorization Reference

Milestone 10 gives Identity's `Role`/`Permission` domain aggregates (present since Milestone 4, never persisted) their first Infrastructure and Application layers, and builds the evaluation engine every later milestone's permission-gated UI (Shell navigation in Milestone 11, module/feature gating anywhere) is expected to call. No UI changed - purely new projects/files plus two Identity domain aggregates gaining persistence.

---

## 1. New project: `Clovent.Identity.Application`

Identity had a Domain layer (Milestone 4) and, as of Milestone 9, an Infrastructure layer for `User` - but no Application layer at all. This milestone is the first Identity-layer work that is fundamentally *evaluation logic over existing aggregates* rather than commands that mutate them, so it doesn't follow Authentication's MediatR/CQRS shape - `AddApplication()` here registers plain services, not handlers, and that's a deliberate difference, not an inconsistency: there's no command being issued, no side effect being orchestrated, just a question being answered ("can this user do X").

**One service, three thin wrappers, not four evaluation engines.** `IAuthorizationService` (`HasPermissionAsync`, `HasRoleAsync`, `GetPermissionCodesAsync`, `SatisfiesPolicyAsync`) is the only place permission/role logic is evaluated. `IModuleAuthorizationPolicy`/`IMenuAuthorizationPolicy`/`IFeatureAuthorizationPolicy` - the brief's three named authorization granularities - are each a two-line wrapper that prefixes a code (`module.`, `menu.`, `feature.`) and delegates to `HasPermissionAsync`. This satisfies three distinct, named, independently-testable deliverables without three copies of the same evaluation logic (`06 Coding Standards`: "No duplicated logic").

**"Application policies"** is `AuthorizationPolicy` (a name plus a list of required permission codes, all of which a user must hold) and `IAuthorizationPolicyProvider` (an in-memory, thread-safe registry a module registers policies into at startup) - the same shape as ASP.NET Core's policy-based authorization, deliberately without that framework's requirements/handler pipeline, since there is no HTTP request context here to hang one on.

---

## 2. `Role`/`Permission` persistence, and a bug the SQLite test suite actually caught

`Clovent.Identity.Infrastructure` gained `RoleConfiguration`/`PermissionConfiguration`/`RoleRepository`/`PermissionRepository`, following the exact pattern already established for `User`. Both `Role` and `Permission` had the same "hardcoded `DateTimeOffset.UtcNow` in the constructor" issue `User`/`Session`/`RefreshSession` already had fixed - both constructors now take `createdAtUtc` explicitly.

**`User.RoleIds`/`Role.PermissionIds` map via a JSON column** (`RoleIdsConverter`/`PermissionIdsConverter`), the same reasoning already applied to `PasswordHistory` in `AuthenticationInfrastructure.md`: a small, capped-in-practice set, no independent identity, never queried apart from its owner. Reconstruction goes through the aggregate's constructor (already extended for `status`/`createdAtUtc`), not a public mutator - calling `AssignRole`/`AddPermission` once per stored id during materialization would spuriously raise domain events for state the aggregate already had, not state that just changed.

**The EF Core constructor-binding attempt first failed loudly and correctly**: the initial `roleIds`/`permissionIds` constructor parameters were typed `IEnumerable<T>`, one step too loose - EF Core's constructor binder requires the parameter type to match the mapped property's CLR type exactly (`IReadOnlyCollection<T>`), not merely be assignable from it. `dotnet ef migrations add` refused to build the model with a precise error naming the exact parameter; changing both parameters to `IReadOnlyCollection<T>` resolved it immediately.

**A second, more interesting bug only the SQLite integration tests caught**: `RemoveRole`/`RemovePermission` mutate the aggregate's private `HashSet<T>` field in place, but `RoleIds`/`PermissionIds` always return that *same reference*. EF Core's default change detection for a converted property uses reference equality unless told otherwise - "the same object, mutated" looks identical to "unchanged" without an explicit `ValueComparer`. `UserRepositoryTests.RemoveRole_ThenReload_PersistsRemoval` failed on the first attempt (`Assert.Empty()` found the removed role id still present after reload) precisely because of this. The fix, `RoleIdsComparer`/`PermissionIdsComparer` (structural `SequenceEqual` plus, critically, a snapshot function that clones via `.ToList()` rather than returning the live reference), is now the second EF Core correctness lesson this solution's SQLite-backed test strategy (`AuthenticationInfrastructure.md` Section 11) has caught before it reached anyone else - vindicating that choice over the EF Core InMemory provider a second time.

---

## 3. Permission cache

`IPermissionCache` (Application-owned interface) is implemented by `MemoryPermissionCache` (Infrastructure, wrapping `Microsoft.Extensions.Caching.Memory.IMemoryCache`, 10-minute TTL) - resolving a user's permission set requires a repository round trip per assigned role and per granted permission, worth avoiding on every single authorization check the same user triggers (a menu render, a feature gate). In-process caching is sufficient for a single desktop process; a future multi-instance host (e.g. a web API) would need a distributed cache and an invalidation broadcast, neither of which this milestone needs.

**Invalidation is the caller's responsibility** - `IPermissionCache.InvalidateAsync` exists, but nothing in this milestone calls it, because nothing in this milestone *changes* a user's roles or a role's permissions yet (no "assign role to user" command exists anywhere in the solution). Whichever future milestone adds that mutation is expected to call `InvalidateAsync` immediately after.

---

## 4. Open questions for Solution Architect review

1. **No role/permission assignment commands exist yet.** This milestone can evaluate authorization but nothing yet lets an administrator grant a role to a user or a permission to a role outside of directly calling `User.AssignRole`/`Role.AddPermission` in code (e.g. the development seed task). **Needs a decision**: does a future milestone add these as MediatR commands (matching Authentication's CQRS shape), and where do they live - a new `Clovent.Identity.Application` command surface, alongside the authorization services already here?
2. **Cache invalidation has no trigger yet** (Section 3) - flagged so it isn't mistaken for an oversight once assignment commands exist.
3. ~~**No seed data for permissions/roles/policies exists.**~~ **Resolved in Milestone 13** by `Clovent.Desktop.Seed.DevelopmentAuthorizationSeedStartupTask` - see `DesktopAdministration.md` Section 3. It seeds every `menu.*`/`feature.*.*` permission code Milestone 13's screens check, one "Administrator" role holding all of them, and assigns it to the seed admin user; gated by the existing `DesktopOptions.SeedDevelopmentUser` flag and guarded against re-running by checking the admin user already has a role.
4. **`PermissionCode` conventions for module/menu/feature codes** (`module.{name}`, `menu.{code}`, `feature.{code}`) are this milestone's own invention, not specified anywhere. **Needs ratification** before real modules start depending on the exact prefix strings.

---

## 5. Milestone 13 addendum: `feature.{code}` conventions in practice, and item 4's ratification still pending

Milestone 13's nine Desktop management screens are the first real consumers of the `feature.{code}` convention item 4 above flagged as needing ratification - each screen checks `feature.{entity}.{operation}` (e.g. `feature.organizations.create`, `feature.fiscalyears.deactivate`), `entity` matching the plural noun in that screen's navigation key and `operation` one of `create`/`edit`/`activate`/`deactivate` (only the subset the entity's domain actually supports - see `MasterData.md` Section 3 and `DesktopAdministration.md` Section 2). No new authorization primitive was introduced; this is more data points for item 4's still-open ratification question, not a resolution of it.

---

## 6. Milestone 14 addendum: `feature.{code}` extended to non-CRUD operations, item 4 still unratified

Milestone 14's eleven Desktop screens (`WarehouseManagement.md`) extend the `feature.{entity}.{operation}` convention beyond the `create`/`edit`/`activate`/`deactivate` vocabulary item 4 originally described - `operation` now also takes values like `receive`/`issue`/`reserve`/`release` (Warehouse Stock), `apply`/`complete`/`cancel` (Stock Adjustment/Transfer), `markprimary` (Barcode), and `view` (the read-only Inventory Transactions screen, which has no mutation to gate at all). `DevelopmentAuthorizationSeedStartupTask`'s `FeatureOperations` table was extended with these new entity/operation pairs, gated through the exact same `IFeatureAuthorizationPolicy.CanUseFeatureAsync` mechanism - no new authorization primitive was introduced here either, confirming the convention generalizes cleanly to workflow actions beyond simple CRUD. This is a third data point for item 4's still-open ratification question, not a resolution of it.

---

## 7. Milestone 15 addendum: `feature.pos.{operation}` covers a whole workflow's actions under one feature name

Milestone 15's ten Restaurant POS screens/dialogs (`RestaurantPOSArchitecture.md`) add `menu.{key}` entries for `diningareas`/`tables`/`pos`/`runningorders`/`holdorders`/`kitchentickets`, and extend `FeatureOperations` with four new entity/operation groups: `diningareas`/`tables` follow the established CRUD-plus-workflow shape exactly (`tables` adding `occupy`/`vacate`/`reserve`/`outofservice`/`returntoservice`, the same "operation vocabulary beyond CRUD" pattern Section 6 already documents for Warehouse Stock); `kitchentickets` adds `start`/`markready`/`serve`/`cancel` (`KitchenWorkflow.md` Section 2's transition table).

**`pos` is a single feature name covering seventeen distinct operations** (`create`, `hold`, `resume`, `void`, `cancel`, `reopen`, `sendtokitchen`, `complete`, `pay`, `transfertable`, `mergetables`, `splitbill`, `notes`, `discount`, `servicecharge`, `additem`, `editline`) rather than one feature per screen the way every prior milestone's screens were named - `RestaurantPosView`, `RunningOrdersView`, and `HoldOrdersView` all check `feature.pos.{operation}` even though they're three different screens, since every one of those operations is fundamentally "an action on an order," regardless of which screen currently has it in view. This is a deliberate scope call (`RestaurantPOSArchitecture.md` open question #3), not an oversight, and is the fourth data point for item 4's still-open ratification question.
