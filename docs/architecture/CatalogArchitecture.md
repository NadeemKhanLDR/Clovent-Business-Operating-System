---
title: Catalog Architecture Reference
type: Architecture
status: Awaiting Solution Architect review
created: 2026-07-28
updated: Milestone 14
applies_to: src/Clovent.Catalog, src/Clovent.Catalog.Application, src/Clovent.Catalog.Infrastructure
---

# Catalog Architecture Reference

Milestone 14 ("Product Catalog & Inventory Foundation") introduces `Clovent.Catalog`, a new bounded context holding the product catalog every downstream module (Restaurant POS, Retail POS, Purchasing, Sales, Manufacturing, Reporting) is expected to read from: Product, ProductCategory, ProductGroup, Brand, UnitOfMeasure, ProductVariant, Barcode, and ProductPrice.

---

## 1. The Product/Variant split, and why a bare Product is unsellable

**`Product` holds no price or stock.** It is the catalog record - name, SKU, classification (Category/Group/Brand, each optional), base unit of measure, tax treatment. **`ProductVariant`** is the actual sellable unit - the thing a `Barcode` scans to, a `ProductPrice` is quoted for, and `WarehouseStock` (see `InventoryArchitecture.md`) tracks a balance of. A product with zero variants is, by design, unsellable: there is nothing to scan, price, or stock. This mirrors a real retail catalog ("Cola 500ml" the product vs. "Cola 500ml - Standard" the orderable unit) rather than conflating "the thing in the catalog" with "the thing you can ring up."

**Cross-context references are by strongly-typed id only**, the established pattern from every prior milestone: `Product`/`ProductVariant` reference `Clovent.MasterData.Currencies.CurrencyId`(via `ProductPrice`)`Clovent.MasterData.Warehouses.WarehouseId` indirectly (via `Clovent.Inventory`, never directly from Catalog). `Clovent.Catalog` never loads or mutates a MasterData aggregate.

---

## 2. Eight aggregates, one shared status enum, two deliberate non-shares

**`CatalogStatus`** (Active/Inactive) is shared across all eight aggregates - genuine reuse, not premature abstraction, since every one of them has an identical two-value lifecycle. **Deliberately not reusing `Clovent.MasterData.Shared.MasterDataStatus`** even though the two enums are structurally identical - pulling Catalog into a dependency on MasterData for a two-value enum would be a needless coupling in the wrong direction, the same reasoning `OrganizationArchitecture.md` Section 2 already applied to Identity.

**`UnitOfMeasureCode` does not reuse `Clovent.MasterData.Shared.ValueObjects.EntityCode`**, even though both are short, uppercase, alphanumeric codes - same "avoid an unnecessary cross-project dependency" reasoning as the status-enum non-share above, applied to a value object instead of an enum.

**`Sku` is shared between `Product` and `ProductVariant`** (2-40 chars, uppercase alphanumeric + hyphens) - both need the identical shape, and each enforces its own uniqueness independently (a product's own SKU and its variants' SKUs live in different uniqueness scopes, enforced by separate unique indexes at the Infrastructure layer).

**"Multiple prices" is a consequence of `ProductPrice` being its own aggregate, not a mechanism.** A variant can have several `ProductPrice` records over time - Cost and Selling, active and inactive - the Application layer reading whichever is currently active for a given `PriceType`. This is the same "let the data model imply the feature" reasoning `MasterData.md`'s "current fiscal year" section applies in the opposite direction (there, one flag instead of two; here, one aggregate instead of one field).

**Barcode uniqueness and primary-flag consistency are Application-layer concerns, not the aggregate's.** `Barcode` has no visibility into its sibling barcodes on the same variant, so "at most one primary barcode per variant" is enforced by `CreateBarcodeCommandHandler`/`MarkBarcodeAsPrimaryCommandHandler` (each unmarks any existing primary before setting the new one) - the "cross-aggregate consistency is the handler's job" pattern `OrganizationArchitecture.md` Section 3 established for `Organization.AddCompany`. Uniqueness of the scanned value itself, across the whole catalog, is enforced by a unique index at the Infrastructure layer.

---

## 3. Application layer: `Clovent.Catalog.Application`

Same shape as every prior milestone's Application project: a DTO, commands, queries, `IUnitOfWork`, `NotFoundException`, `AddApplication` registering MediatR. What differs per entity is which operations exist, driven directly by the domain:

| Entity | Create | Rename/Update | Status ops | Notes |
|---|---|---|---|---|
| ProductCategory | ✓ | Rename, SetParent | Activate/Deactivate | Self-referencing hierarchy via `ParentCategoryId?` |
| ProductGroup | ✓ | Rename | Activate/Deactivate | Flat, no hierarchy |
| Brand | ✓ | Rename | Activate/Deactivate | |
| UnitOfMeasure | ✓ (Code + Name) | Rename (Name only) | Activate/Deactivate | `Code` immutable after creation |
| Product | ✓ | Rename, SetCategory/SetGroup/SetBrand, SetTaxConfiguration | Activate/Deactivate | `Sku`/`BaseUnitOfMeasureId` immutable after creation |
| ProductVariant | ✓ | Rename, SetUnitOfMeasure | Activate/Deactivate | `Sku` immutable after creation |
| Barcode | ✓ | MarkAsPrimary/UnmarkAsPrimary | Activate/Deactivate | `Value` immutable after creation - no rename, since a scanned code doesn't change |
| ProductPrice | ✓ | UpdateAmount | Activate/Deactivate | `PriceType`/`CurrencyId`/`ProductVariantId` immutable after creation |

**A command surface was never written for an operation the domain doesn't expose** - the same discipline `MasterData.md` Section 3 established: Barcode has no `RenameBarcodeCommand` because `Barcode.Value` has no setter.

---

## 4. Infrastructure: `CatalogDbContext`, `Catalog` schema

Eight `DbSet`s under the `Catalog` schema. Value converters follow the established pattern - every conversion goes through the value object's own public `Create` factory.

**`Product.TaxConfiguration` maps via a JSON-column `ValueConverter`, applied proactively from the start** - a private `TaxConfigurationJson` record (`RatePercentage` decimal + `IsInclusive` bool) serialized via `JsonSerializer`. This is a direct, deliberately-applied lesson from `OrganizationArchitecture.md` Section 4's `Branch.Address` failure (EF Core's constructor-binding cannot bind an owned-type navigation): the migration succeeded on the first attempt because the converter was written before any owned-type attempt was made, not after one failed.

Repositories mirror every prior project's shape - thin EF Core wrappers, no query logic beyond what the interface's method names imply. `IUnitOfWork`/`UnitOfWorkBehavior` are field-for-field identical to every other bounded context's own.

---

## 5. Verified: builds clean, tests pass

- `Clovent.Catalog` (Domain): 45 tests.
- `Clovent.Catalog.Application`: 36 tests.
- `Clovent.Catalog.Infrastructure`: 31 tests (`Clovent.Catalog.Infrastructure.Tests`, SQLite-backed repository + `UnitOfWorkBehavior` tests).
- 0 build warnings, 0 errors across all three projects.

---

## 6. Open questions for Solution Architect review

1. **`ProductGroup` has no dedicated Desktop screen** (see `WarehouseManagement.md` Section 1), mirroring the Milestone 13 precedent where Language/TimeZone had full Domain/Application/Infrastructure layers but no screen. **Needs a decision**: does Group management belong in a future milestone's Desktop scope, or is it meant to be seeded/administered another way?
2. **No cross-entity referential integrity is enforced at the database level** between `Clovent.Catalog`'s tables and `Clovent.MasterData`'s (`ProductPrice.CurrencyId` has no FK constraint into `MasterData.Currencies`) - the same eventual-consistency-by-convention already flagged in `MasterData.md` Section 6, now applying to a second cross-context boundary.
3. **Barcode/Price uniqueness enforcement lives entirely in the Application layer and a database unique index**, with no domain-level check - flagged for the same reason `MasterData.md`'s open question #2 flags `BusinessSettings`' one-per-organization enforcement: is an extra repository round trip in the handler worth adding before relying on the database to reject a duplicate?
