---
title: Organization Architecture Reference
type: Architecture
status: Awaiting Solution Architect review
created: 2026-07-27
updated: Milestone 13
applies_to: src/Clovent.Identity, src/Clovent.Identity.Application, src/Clovent.Identity.Infrastructure
---

# Organization Architecture Reference

Milestone 13 ("Organization & Master Data Foundation") establishes the multi-tenant business hierarchy every future ERP module builds on: Organization → Company → Branch. This document covers the architectural decision at the center of the milestone and the Application/Infrastructure work that followed it.

---

## 1. The central decision: enrich Identity's existing aggregates, don't duplicate them

The milestone brief asked for "Organization Management" alongside Department/Warehouse/Terminal/FiscalYear/Currency/etc. But `Organization`, `Company`, and `Branch` already existed as Milestone 4 domain aggregates in `Clovent.Identity` - `IdentityDomain.md` Section 9 records them as domain-only, unenriched, never persisted. Creating a second `Organization` type in a new bounded context would have meant two competing definitions of the same tenant hierarchy, one of them dead code, and would have broken `Clovent.Platform.Execution.IExecutionContext`, which already references `Clovent.Identity.Organizations.OrganizationId`/`Companies.CompanyId`/`Branches.BranchId`.

**Decision**: enrich the existing Identity aggregates in place rather than duplicate them. Everything genuinely new (Department, Warehouse, Terminal, FiscalYear, Currency, Language, TimeZone, Business Settings) lives in a new bounded context, `Clovent.MasterData` - see `MasterData.md`. This was a reversible, well-justified engineering call made under this milestone's "do not stop unless an approved architecture must change" instruction, not something requiring a pause for sign-off; it is flagged below for retroactive review.

---

## 2. What was added to each aggregate

All three gained the same shape of enrichment, applied consistently:

| Aggregate | New properties | New behavior | New events |
|---|---|---|---|
| `Organization` | `TaxId?`, `OrganizationStatus Status` | `Rename`, `SetTaxId`, `Activate`, `Deactivate` | `OrganizationRenamed`, `OrganizationActivated`, `OrganizationDeactivated` |
| `Company` | `TaxId?`, `CompanyStatus Status` | `Rename`, `SetTaxId`, `Activate`, `Deactivate` | `CompanyRenamed`, `CompanyActivated`, `CompanyDeactivated` |
| `Branch` | `Address?`, `BranchStatus Status` | `Rename`, `SetAddress`, `Activate`, `Deactivate` | `BranchRenamed`, `BranchActivated`, `BranchDeactivated`, `BranchAddressChanged` |

`TaxId` (opaque, loosely-validated string, max 50 chars) is shared between `Organization` and `Company` since both are legal entities that may register one, and the shape is identical - a genuine value object reuse, not premature abstraction. `Address` (Street/City/State/PostalCode/Country, all required once supplied) is Branch-only; Organization and Company have no physical location of their own in this model.

Each status enum (`OrganizationStatus`, `CompanyStatus`, `BranchStatus`) is its own two-value type per aggregate rather than one shared enum - deliberately not reusing `Clovent.MasterData.Shared.MasterDataStatus` (see `MasterData.md` Section 2), since pulling Identity into a dependency on the new MasterData project for a two-value enum would be a needless coupling in the wrong direction (MasterData already depends on Identity for `OrganizationId`/`BranchId`, not the other way around).

**Constructor convention preserved.** Every enriched aggregate's private constructor still takes every persisted field explicitly (`id, name, taxId, status, createdAtUtc, companyIds`, etc.) - the same EF Core constructor-binding requirement documented since Milestone 6, now with the new fields folded in rather than defaulted internally.

---

## 3. Application layer: CQRS added to `Clovent.Identity.Application`

Before this milestone, `Clovent.Identity.Application` held only Milestone 10's Authorization evaluation services - no MediatR, no commands, no queries (see `Authorization.md` Section 1: "doesn't follow Authentication's MediatR/CQRS shape"). Organization/Company/Branch are the first genuinely command-driven work in this project, so it now also registers MediatR (`AddApplication` does both: the existing plain-service registrations, plus `AddMediatR` scanning this assembly) - Authorization's evaluation-only nature and this milestone's CQRS surface coexist in the same project without conflict, since they're organized into separate folders (`Authorization/` vs. `Organizations/`, `Companies/`, `Branches/`).

Per aggregate: a DTO record, five commands (`Create`, `Rename`, `SetTaxId`/`SetAddress`, `Activate`, `Deactivate`), and two queries (`GetById`, a list scoped to the parent - `ListOrganizationsQuery` has no parent so lists everything; `ListCompaniesByOrganizationQuery`/`ListBranchesByCompanyQuery` filter). `IUnitOfWork` (new to this project) and `NotFoundException` mirror `Clovent.Authentication.Application`'s identical types exactly.

**Cross-aggregate consistency is the handler's job, not the domain's.** `Organization.AddCompany`/`Company.AddBranch` exist on the parent aggregate but nothing calls them automatically - `CreateCompanyCommandHandler` loads the parent Organization, creates the child Company, calls `organization.AddCompany(company.Id)`, and persists both via their own repositories in the same handler. This keeps each aggregate's own invariants (a Company always knows which companies belong to its Organization) enforced without introducing a bigger transaction script or saga.

---

## 4. Infrastructure: extending `IdentityDbContext`, not a new DbContext

`IdentityDbContext` (Milestone 9-10: Users, Roles, Permissions) gained three more `DbSet`s and configurations under the same `Identity` schema - `Organizations`, `Companies`, `Branches` tables, migration `OrganizationCompanyBranch`.

**`Organization.CompanyIds`/`Company.BranchIds`** map via a JSON column + `ValueConverter`/`ValueComparer` pair, the identical pattern `User.RoleIds`/`Role.PermissionIds` already established (Milestone 10) - a capped-in-practice set with no independent identity, never queried apart from its owning aggregate, and the `ValueComparer` snapshotting via `ToList()` is required for the same reason: without it, EF Core's default reference-equality change tracking would never notice `AddCompany`/`AddBranch` mutating the same backing `HashSet` in place.

**`Branch.Address` maps via a JSON-column `ValueConverter`, not an EF Core owned type - and this was a real design correction, not a starting choice.** The first attempt used `builder.OwnsOne(b => b.Address, ...)`, which failed at `dotnet ef migrations add` time with: *"No suitable constructor was found for the type 'Branch'... Cannot bind 'address'... Note that only mapped properties can be bound to constructor parameters. Navigations to related entities, including references to owned types, cannot be bound."* EF Core's constructor-binding (required by this solution's "every field explicit in the private constructor" convention) only handles scalar/converted properties, not owned-type navigations. Switching `Address` to a converter (serializing its five fields to one JSON column, mirroring `Address.GetEqualityComponents`) resolved it while keeping the constructor-binding convention intact. This is a second, independently-discovered instance of "EF Core has a specific limitation this solution's conventions run into" - see Section 5's cross-reference to the earlier `RoleIds` change-tracking discovery.

**Repositories** (`OrganizationRepository`, `CompanyRepository`, `BranchRepository`) follow `UserRepository`'s exact shape - thin EF Core wrappers, no query logic beyond what the interface's method names imply. **`IUnitOfWork`/`UnitOfWorkBehavior`** are new to `Clovent.Identity.Infrastructure` (this is the first Identity work needing them), mirroring `Clovent.Authentication.Infrastructure`'s identical types field-for-field.

---

## 5. Verified: builds clean, tests pass, no regressions

- `Clovent.Identity` (Domain): 132 tests passing (up from 106 pre-Milestone-13 - the delta is the new Rename/Activate/Deactivate/TaxId/Address tests for the three enriched aggregates).
- `Clovent.Identity.Application`: 41 tests (new project surface).
- `Clovent.Identity.Infrastructure`: 32 tests (up from ~17 - the delta is `OrganizationRepositoryTests`/`CompanyRepositoryTests`/`BranchRepositoryTests`/`UnitOfWorkBehaviorTests`).
- 0 build warnings, 0 errors across all three projects.

---

## 6. Open questions for Solution Architect review

1. **Enrichment-in-place vs. duplication** (Section 1) was decided autonomously under this milestone's execution-mode instructions. **Needs ratification**: confirm this is the correct long-term direction before a future milestone builds further on top of it (e.g. before Restaurant POS references `BranchId` for till/session scoping).
2. **`Branch.Address` as a JSON-column converter, not an EF Core owned type** (Section 4) is a direct consequence of this solution's constructor-binding convention. **Needs a decision**: is that convention worth keeping given this exact friction, or should a future milestone reconsider owned types for genuinely multi-field value objects, accepting a less uniform materialization story for those specific cases?
3. **No API/UI yet existed for creating an Organization's *first* Company/Branch before this milestone** - `Program.cs`'s composition root has no seed for the hierarchy itself; `Clovent.Desktop`'s `DevelopmentMasterDataSeedStartupTask` (see `MasterData.md`/`DesktopAdministration.md`) now seeds one demo Organization → Company → Branch chain for development purposes only. **Needs a decision**: does production provisioning need a dedicated "create tenant" workflow distinct from the per-screen "New Organization"/"New Company"/"New Branch" CRUD dialogs this milestone built?
