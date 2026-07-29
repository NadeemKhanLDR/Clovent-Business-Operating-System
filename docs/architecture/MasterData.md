---
title: MasterData Reference
type: Architecture
status: Awaiting Solution Architect review
created: 2026-07-27
updated: Milestone 14
applies_to: src/Clovent.MasterData, src/Clovent.MasterData.Application, src/Clovent.MasterData.Infrastructure
---

# MasterData Reference

Milestone 13 ("Organization & Master Data Foundation") introduces `Clovent.MasterData`, a new bounded context holding everything genuinely new the milestone asked for that isn't Organization/Company/Branch (those enrich existing `Clovent.Identity` aggregates - see `OrganizationArchitecture.md`): Department, Warehouse, Terminal, FiscalYear, Currency, Language, TimeZone, and Business Settings.

---

## 1. Why a new project, and why this name

The milestone's own scope list asked for a document named `MasterData.md`, and the milestone's entities don't belong in Identity (Identity owns *who* and *which tenant*; these entities are operational reference data and organizational subdivisions a tenant configures). A project first named `Clovent.Organization` was started, then deleted and recreated as `Clovent.MasterData` once it became clear "Organization" already means something specific and different in `Clovent.Identity` - keeping the name would have invited exactly the kind of namespace confusion Section 3 describes for a different reason.

**Cross-context references are by strongly-typed id only**, the established pattern from `Clovent.Authentication` depending on `Clovent.Identity.Users.UserId`: `Department`/`Warehouse`/`Terminal` reference `Clovent.Identity.Branches.BranchId`; `FiscalYear`/`BusinessSettings` reference `Clovent.Identity.Organizations.OrganizationId`. `Clovent.MasterData` never loads or mutates an Identity aggregate - it only holds the fact that one exists and is related.

---

## 2. Domain: eight aggregates, three shared value types, one deliberate non-share

**Shared across five aggregates**: `MasterDataStatus` (Active/Inactive) - Department, Warehouse, Terminal, Currency, Language, and TimeZoneEntry all use it. **Deliberately not reused for Organization/Company/Branch** (Identity keeps its own per-aggregate status enums - see `OrganizationArchitecture.md` Section 2) - a genuine judgment call to avoid coupling Identity to MasterData for a two-value enum, since the dependency direction only goes one way (MasterData → Identity).

**`EntityCode`** (2-20 chars, uppercase alphanumeric + hyphens, e.g. `"WH-01"`) is shared by Warehouse and Terminal codes - deliberately kept separate from `CurrencyCode` (ISO 4217, exactly 3 uppercase letters) and `LanguageCode` (ISO 639-1, exactly 2 lowercase letters), since those two have standards-defined formats `EntityCode`'s free-form convention doesn't match.

**`FiscalYearStatus` (Open/Closed) is its own enum, not `MasterDataStatus`.** Closing a fiscal year is a one-way transition (`FiscalYear.Close()`, no `Reopen()`) - conceptually different from a reversible Active/Inactive toggle even though both happen to be two-valued today. Reusing `MasterDataStatus` would have made "Activate" a nonsensical, silently-unsupported operation on a closed fiscal year rather than a type that simply doesn't offer it.

**"Current fiscal year" is not a flag on `FiscalYear` itself.** There is deliberately no `IsCurrent` boolean - it is derived from `BusinessSettings.DefaultFiscalYearId`, a single source of truth. Two places recording "which fiscal year is current" (a flag on the year plus a pointer from settings) could drift apart if a caller updated one and not the other; one place cannot.

**`TimeZoneEntry`, not `TimeZone`.** The BCL has a legacy `System.TimeZone` type reachable via implicit usings - reusing that name would repeat the exact collision class Milestone 9 discovered with `Clovent.Desktop.Session`/`Clovent.Authentication.Sessions.Session` (documented in `AuthenticationIntegration.md`). Naming the aggregate `TimeZoneEntry` sidesteps it entirely rather than requiring every caller to fully-qualify the type forever.

**A second, near-identical collision was caught before it shipped.** `BusinessSettings` (the class) was initially placed in a `Clovent.MasterData.BusinessSettings` namespace - itself nested directly under the project's own root namespace, where `MasterDataDomainException.cs` and other root-level files live. Any file in the `Clovent.MasterData` root importing `using Clovent.MasterData.BusinessSettings;` and then referencing the bare type `BusinessSettings` would hit `CS0118 "X is a namespace but is used like a type"` - the identical bug class, recognized proactively this time from the `TimeZone` lesson, and fixed by renaming the folder/namespace to `Clovent.MasterData.Settings` before it ever reached a build.

**`Currency`/`Language`/`TimeZoneEntry` are catalog-wide, not organization-scoped.** They have no owning `OrganizationId` - USD is the same currency for every tenant, so these three are shared reference data, queried via `GetAllAsync`/`GetByCodeAsync` rather than filtered by parent.

---

## 3. Application layer: `Clovent.MasterData.Application`

Same shape as `Clovent.Identity.Application`'s new Organization/Company/Branch surface (`OrganizationArchitecture.md` Section 3): a DTO, commands, queries, `IUnitOfWork`, `NotFoundException`, `AddApplication` registering MediatR. What differs per entity is which operations exist at all, driven directly by what the domain actually supports:

| Entity | Create | Rename | Status ops | Notes |
|---|---|---|---|---|
| Department | ✓ | ✓ | Activate/Deactivate | |
| Warehouse | ✓ (Name + Code) | ✓ (Name only) | Activate/Deactivate | `Code` is immutable after creation - no `SetCode` exists on the domain aggregate, so no command was written for it |
| Terminal | ✓ (Name + Code) | ✓ (Name only) | Activate/Deactivate | Same as Warehouse |
| FiscalYear | ✓ (Name + dates) | ✓ (Name only) | Close only | Dates are immutable after creation; no Activate exists, matching the domain's one-way `Close()` |
| Currency | ✓ | *none* | Activate/Deactivate | `Currency` has no update method beyond Activate/Deactivate - no rename/edit command exists because the domain doesn't support one |
| Language | ✓ | *none* | Activate/Deactivate | Same reasoning as Currency |
| TimeZoneEntry | ✓ | *none* | Activate/Deactivate | Same reasoning as Currency |
| BusinessSettings | ✓ (one per org) | `UpdateDefaults` (all fields together) | *none* | Not a lifecycle-managed entity - see Section 2 |

**A command surface was never written for an operation the domain doesn't expose.** This is a direct consequence of "the Application layer calls into the domain, never around it" - if `Currency.Rename` doesn't exist, `RenameCurrencyCommand` doesn't either, and the Desktop screen (see `DesktopAdministration.md`) simply has no "Edit" button for Currency rather than a button that would silently no-op.

---

## 4. Infrastructure: a new `MasterDataDbContext`, `MasterData` schema

Eight `DbSet`s, one configuration class per aggregate, tables under the `MasterData` schema (mirroring `Identity`/`Authentication`'s own schema-per-context convention). Value converters follow the same pattern as every prior EF Core project in this solution: every conversion goes through the value object's own public `Create` factory, never a Domain-layer change. `BusinessSettings.DefaultFiscalYearId` (a nullable `FiscalYearId`) uses a nullable-aware converter pair (`NullableFiscalYearIdConverter`) rather than the non-nullable pattern used everywhere else, since it is the only optional foreign-key-shaped id in this project.

Repositories mirror `Clovent.Identity.Infrastructure`'s shape exactly. `IUnitOfWork`/`UnitOfWorkBehavior` are new to this project (first MasterData work needing them) and are field-for-field identical to Identity's and Authentication's own.

**Development seed data** (`Clovent.Desktop.Seed.DevelopmentMasterDataSeedStartupTask`, gated by `DesktopOptions.SeedDevelopmentMasterData`) creates, on first run only (guarded by "does any Organization already exist"): one demo Organization → Company → Branch, one Department/Warehouse/Terminal under that Branch, two Currencies (USD, EUR), two Languages (en, es), two TimeZoneEntries (UTC, America/New_York), one FiscalYear for the current calendar year, and one BusinessSettings record tying the organization to USD/en/UTC/that fiscal year. This is what makes every Milestone 13 Desktop screen and the Dashboard's Current Organization/Company/Branch/Fiscal Year tiles demonstrable without a separate manual provisioning step - see `DesktopAdministration.md` Section 4.

---

## 5. Verified: builds clean, tests pass

- `Clovent.MasterData` (Domain): 46 tests.
- `Clovent.MasterData.Application`: 38 tests.
- `Clovent.MasterData.Infrastructure`: 31 tests.
- 0 build warnings, 0 errors across all three projects.

---

## 6. Open questions for Solution Architect review

1. **Currency/Language/TimeZoneEntry have no rename/edit command** (Section 3) because the domain itself never grew one. **Needs a decision**: is this permanent (these are truly immutable-once-cataloged reference data), or should a future milestone add `Currency.Rename`/`UpdateSymbol`/etc. if real-world currency catalog corrections turn out to be needed?
2. **`BusinessSettings` is one-per-organization, enforced only by a unique index at the Infrastructure layer** (`HasIndex(s => s.OrganizationId).IsUnique()`), not by a domain invariant that could reject a second `Create` call before hitting the database. **Needs a decision**: should `CreateBusinessSettingsCommandHandler` check for an existing record first (an extra repository round trip) rather than relying on the database to reject a duplicate?
3. **No cross-entity referential integrity is enforced at the database level** between `Clovent.MasterData`'s tables and `Clovent.Identity`'s (`Department.BranchId` has no FK constraint into `Identity.Branches`, since they're different `DbContext`s/schemas that could even live in different databases). **Needs ratification**: is eventual-consistency-by-convention (nothing currently deletes an Organization/Branch, so orphaning isn't reachable yet) acceptable long-term, or does a future milestone need a cross-context integrity check?

---

## 7. Milestone 14 addendum: `IWarehouseRepository.GetAllAsync`

`Clovent.Inventory`'s Desktop screens (`WarehouseManagement.md` Section 3) needed a flat "every warehouse" list to populate a single-level `EntityPicker`, scoping by warehouse directly rather than drilling through Organization → Company → Branch first (every Milestone 13 Warehouse consumer had a branch already selected by that point). `IWarehouseRepository` gained `GetAllAsync` (and its EF Core implementation), backing a new `ListAllWarehousesQuery` alongside the existing branch-scoped `ListWarehousesByBranchQuery` - an additive change; nothing about the existing branch-scoped query path changed.
