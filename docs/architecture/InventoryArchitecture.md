---
title: Inventory Architecture Reference
type: Architecture
status: Awaiting Solution Architect review
created: 2026-07-28
updated: Milestone 14
applies_to: src/Clovent.Inventory, src/Clovent.Inventory.Application, src/Clovent.Inventory.Infrastructure
---

# Inventory Architecture Reference

Milestone 14 ("Product Catalog & Inventory Foundation") introduces `Clovent.Inventory`, a new bounded context tracking multi-warehouse stock balances and every movement behind them: WarehouseStock, InventoryTransaction, StockAdjustment, StockTransfer.

---

## 1. Cross-context references, and why Inventory depends on Catalog

`Clovent.Inventory` references both `Clovent.MasterData` (`WarehouseId`) and `Clovent.Catalog` (`ProductVariantId`) - always by strongly-typed id only, never loading or mutating the foreign aggregate, the same pattern every prior bounded context boundary in this solution follows. The dependency direction (Inventory → Catalog, not the reverse) follows the domain: a stock balance is meaningless without knowing which sellable variant it tracks, but a variant's catalog record is complete and useful with zero warehouses ever stocking it.

---

## 2. `WarehouseStock` mutates itself; `InventoryTransaction` is never created by it

**`WarehouseStock.Receive`/`Issue`/`Reserve`/`Release` mutate quantity on hand/reserved directly on the aggregate** - `QuantityAvailable` (`QuantityOnHand - QuantityReserved`) is a computed property, not a stored field, so it can never drift out of sync with the two numbers it derives from.

**Every one of those mutations is always paired, by the Application-layer handler that calls it, with creating an `InventoryTransaction` ledger entry** - `WarehouseStock` itself has no visibility into the transaction ledger and no method that both mutates the balance and records history in one call. This is the "cross-aggregate consistency is the handler's job" pattern (`OrganizationArchitecture.md` Section 3) applied to a ledger instead of a parent/child relationship: `ReceiveStockCommandHandler` calls `stock.Receive(quantity)` then `InventoryTransaction.Create(..., InventoryTransactionType.Receipt, quantity, ...)`, both persisted in the same handler.

**`InventoryTransaction` is a pure append-only ledger** - `Create` is its only public method; there is no update or delete. A movement that turns out to be wrong is corrected by a second, opposite entry (a `StockAdjustment` or another transfer), never by editing history - the audit-trail guarantee an inventory ledger exists to provide would be worthless if any entry could be silently rewritten.

---

## 3. `StockAdjustment`/`StockTransfer`: propose-then-commit, one-way

Both follow the identical shape `FiscalYear.Close()` established in Milestone 13: created `Pending`, then either committed (`Apply`/`Complete`) or `Cancel`led, with the committing transition explicitly one-way (no `Unapply`/`Uncomplete`). A correction that turns out to be wrong is reversed by a second, opposite adjustment or transfer, not by undoing the first - the same "no undo" reasoning, now applied twice more.

**Committing is where the actual stock mutation and ledger entries happen, not at proposal time.** `ApplyStockAdjustmentCommandHandler` finds-or-creates the target `WarehouseStock` (a genuinely new warehouse/variant pairing is a valid reason to adjust stock into existence), mutates it via `Receive`/`Issue` depending on `AdjustmentType`, and records one `InventoryTransaction`. `CompleteStockTransferCommandHandler` requires the source `WarehouseStock` to already exist (you cannot transfer stock that was never received anywhere), finds-or-creates the destination, mutates both sides, and records two `InventoryTransaction` entries (`TransferOut` at the source, `TransferIn` at the destination) - the domain's own `StockTransfer.Create` already rejects `sourceWarehouseId == destinationWarehouseId` before either aggregate is touched.

**The find-or-create pattern was refined before it shipped, not after.** An early version of `ApplyStockAdjustmentCommandHandler` detected "did I just create this stock record" via a heuristic (`stock.CreatedAtUtc == stock.UpdatedAtUtc && stock.QuantityOnHand == 0`) - fragile, since a genuinely old record could coincidentally match it. This was caught and replaced with explicit `existingStock is null` tracking (`var existing = await repo.GetByXAsync(...); var entity = existing ?? Aggregate.Create(...); if (existing is null) await repo.AddAsync(entity, ct);`) before any test or handler shipped depending on the heuristic - the same "self-corrected fragile logic before it reached anyone" discipline `AuthenticationInfrastructure.md`'s own history documents for a different bug class.

---

## 4. Application layer: `Clovent.Inventory.Application`

Same shape as every prior Application project. What differs per entity is which operations exist:

| Entity | Create | Mutations | Notes |
|---|---|---|---|
| WarehouseStock | ✓ (zero-balance) | Receive, Issue, Reserve, Release, SetStockLevels, SetNegativeStockPolicy | No Activate/Deactivate - a balance record has no lifecycle status, only quantities and policy |
| InventoryTransaction | *(recorded only, never directly)* | *none* | No commands at all - only queries (`GetById`, `ListByWarehouse`, `ListRecent`) |
| StockAdjustment | ✓ (Pending) | Apply, Cancel | Both one-way from Pending |
| StockTransfer | ✓ (Pending) | Complete, Cancel | Both one-way from Pending |

---

## 5. Infrastructure: `InventoryDbContext`, `Inventory` schema, and a second SQLite-vs-SQL-Server discovery

Four `DbSet`s under the `Inventory` schema. All quantity/amount columns use `.HasPrecision(18, 4)`, the established convention.

**`InventoryTransaction.OccurredAtUtc` needed a `DateTimeOffset` → UTC-ticks (`long`) `ValueConverter`, discovered by the SQLite-backed test suite, not by SQL Server (the real target).** `InventoryTransactionRepository.GetRecentAsync` orders by `OccurredAtUtc` descending - correct and efficient against SQL Server, but the EF Core SQLite provider explicitly refuses to translate `ORDER BY` over a raw `DateTimeOffset` column (a deliberate provider restriction: a naive lexicographic sort would be wrong once mixed UTC offsets are possible, even though every value in this system is always offset-zero in practice). The fix - `DateTimeOffsetToUtcTicksConverter`, storing the column as its `UtcTicks` `long` - is portable, semantically identical, and orderable on both providers; the `InitialCreate` migration was regenerated once, before it had been applied anywhere, so no data migration was needed. This is a second, independently-discovered instance of "the SQLite-backed test suite catches an EF Core provider limitation before SQL Server ever would" - see `AuthenticationInfrastructure.md` Section 11 and `OrganizationArchitecture.md` Section 4 for the first two.

Repositories and `IUnitOfWork`/`UnitOfWorkBehavior` mirror every prior project's shape exactly.

---

## 6. Verified: builds clean, tests pass

- `Clovent.Inventory` (Domain): 23 tests.
- `Clovent.Inventory.Application`: 20 tests.
- `Clovent.Inventory.Infrastructure`: 17 tests (`Clovent.Inventory.Infrastructure.Tests`, SQLite-backed repository + `UnitOfWorkBehavior` tests - includes the ordering-fix regression test).
- 0 build warnings, 0 errors across all three projects.

---

## 7. Open questions for Solution Architect review

1. **`WarehouseStock` has no Activate/Deactivate lifecycle** (Section 4) - a deliberate read of the domain (a balance record is either present or absent, not "inactive") rather than an oversight. **Needs ratification**: is there a real-world scenario (discontinuing a variant at one warehouse without deleting its history) that would need a status after all?
2. **Negative-stock policy is per-(warehouse, variant) balance, not a warehouse-wide or organization-wide default** - every new `WarehouseStock` defaults to `AllowNegativeStock: false` unless explicitly set otherwise at creation. **Needs a decision**: does a future milestone need an organization- or warehouse-level default this inherits from, rather than always defaulting to the strictest policy?
3. **No cross-entity referential integrity is enforced at the database level** between `Clovent.Inventory`'s tables and `Clovent.Catalog`'s/`Clovent.MasterData`'s - the same eventual-consistency-by-convention already flagged in `MasterData.md` Section 6 and `CatalogArchitecture.md` Section 6, now applying to a third cross-context boundary.
