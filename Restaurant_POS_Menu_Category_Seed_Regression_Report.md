# Restaurant POS Menu Category Seed Regression Report

**Date:** 2026-09-13  
**Module:** Restaurant POS (Clovent Business Operating System)  
**Solution:** `Clovent.BusinessOperatingSystem.slnx`  
**Status:** **PASSED AUTOMATED VERIFICATION — LIVE UI PENDING MANUAL ACCEPTANCE**  

---

## Executive Summary

A critical regression was identified where Pakistani cuisine dishes (`Chicken Karahi`, `Chicken Koyla Karahi`, `Salad`) in the Restaurant POS were incorrectly assigned to `Main Course` or defaulted to `Uncategorized` at runtime due to conditional startup seed execution.

The root cause was isolated to early return guards in `DevelopmentCatalogSeedStartupTask.cs` that skipped category assignment enforcement if demo organizations or warehouses were already initialized.

The issue has been resolved by decoupling category assignment enforcement from demo environment initialization, executing category assignments unconditionally on every application startup, and updating `RestaurantPosForm.cs` to group category tabs case-insensitively.

---

## Root Cause & Technical Resolution

### 1. Root Cause Analysis
- **Startup Task Guard Bypass:** In `DevelopmentCatalogSeedStartupTask.cs`, the seed method `ExecuteAsync` contained early-return checks (`if (organizations.Count == 0 || warehouses.Count == 0) return;`). When demo warehouses/organizations were already present, the entire startup task exited before `SeedPakistaniCuisineDishesAsync` could run.
- **Duplicate UI Category Tabs:** Category grouping in `RestaurantPosForm.cs` did not enforce case-insensitive string comparison when creating category filter buttons, leading to potential duplicate category tabs when database names differed in casing.

### 2. Implementation Changes
- **Unconditional Category Assignment:** Added `EnsurePakistaniCuisineCategoryAssignmentsAsync` to `DevelopmentCatalogSeedStartupTask.cs` and invoked it at the very top of `ExecuteAsync` prior to any early-return checks. This guarantees that category assignments for Pakistani cuisine dishes execute unconditionally and idempotently on every application startup.
- **Case-Insensitive Category Grouping:** Updated `BuildCategoryButtons()` in `src/Clovent.Desktop/Restaurant/Orders/RestaurantPosForm.cs` to group categories using `StringComparer.OrdinalIgnoreCase`.

---

## Canonical Category Assignment Matrix

| Product Name | SKU | Target Category | Portion Variants | Live DB State |
|---|---|---|---|---|
| **Chicken Karahi** | `CHICKEN-KARAHI` | `Karahi` | Standard | Assigned to `Karahi` |
| **Chicken Koyla Karahi** | `CKK-HALF` / `CKK-FULL` | `Karahi` | Half Plate, Full Plate | Assigned to `Karahi` |
| **White Daal Mash** | `DM-HALF` / `DM-FULL` | `Main Course` | Half Plate, Full Plate | Assigned to `Main Course` |
| **Aloo Chicken Qorma** | `ACQ-STD` | `Main Course` | Standard | Assigned to `Main Course` |
| **Chicken Biryani** | `CB-STD` | `Main Course` | Standard | Assigned to `Main Course` |
| **Chicken Haleem** | `CH-HALF` / `CH-FULL` | `Main Course` | Half Plate, Full Plate | Assigned to `Main Course` |
| **Murgh Chanay** | `MC-HALF` / `MC-FULL` | `Main Course` | Half Plate, Full Plate | Assigned to `Main Course` |
| **Aloo Gobi** | `AG-HALF` / `AG-FULL` | `Main Course` | Half Plate, Full Plate | Assigned to `Main Course` |
| **Salad** | `SALAD-STD` | `Salads` | Standard | Assigned to `Salads` |

---

## Live Database Verification Summary (`Clovent_Catalog`)

Direct query inspection of SQL Server database `Clovent_Catalog` confirms:

| Category Name | Active Item Count | Product List |
|---|---|---|
| **Beverages** | 1 | `Leechi` |
| **Bread** | 1 | `Garlic Nan` |
| **Karahi** | 2 | `Chicken Karahi`, `Chicken Koyla Karahi` |
| **Main Course** | 6 | `White Daal Mash`, `Aloo Chicken Qorma`, `Chicken Biryani`, `Chicken Haleem`, `Murgh Chanay`, `Aloo Gobi` |
| **Salads** | 1 | `Salad` |
| **Chicken** | 0 | *(No active items assigned)* |
| **Snacks** | 0 | *(No active items assigned)* |
| **Uncategorized** | 0 | *(No items remaining)* |
| **Total All Menu** | **11** | `1 + 1 + 2 + 6 + 1 = 11 items` |

**Mathematical Reconciliation:**
$$\text{All Menu Count (11)} = 1 (\text{Beverages}) + 1 (\text{Bread}) + 2 (\text{Karahi}) + 6 (\text{Main Course}) + 1 (\text{Salads}) + 0 (\text{Uncategorized})$$

---

## Automated Test Coverage

The test suite in `src/Clovent.Desktop.Tests/Restaurant/Orders/CategoryAssignmentTests.cs` covers 32 unit tests:

1. `CATEGORY_DATA_01` to `17`: Comprehensive validation of category singletons, individual product assignments, uncategorized count zero, total sum reconciliation, seed idempotency, and variant preservation.
2. `CATEGORY_REGRESSION_01` to `15`: Explicit regression verification of all 9 product category mappings, precise counts (`Karahi` = 2, `Main Course` = 6, `Salads` = 1, `Uncategorized` = 0, `All Menu` = 11), and startup idempotency.

---

## Manual QA Verification Matrix

| ID | Scenario | Result | Status |
|---|---|---|---|
| `CATEGORY-REGRESSION-01` | Chicken Karahi → Karahi | Automated PASS | Live UI: PENDING MANUAL ACCEPTANCE |
| `CATEGORY-REGRESSION-02` | Chicken Koyla Karahi → Karahi | Automated PASS | Live UI: PENDING MANUAL ACCEPTANCE |
| `CATEGORY-REGRESSION-03` | White Daal Mash → Main Course | Automated PASS | Live UI: PENDING MANUAL ACCEPTANCE |
| `CATEGORY-REGRESSION-04` | Aloo Chicken Qorma → Main Course | Automated PASS | Live UI: PENDING MANUAL ACCEPTANCE |
| `CATEGORY-REGRESSION-05` | Chicken Biryani → Main Course | Automated PASS | Live UI: PENDING MANUAL ACCEPTANCE |
| `CATEGORY-REGRESSION-06` | Chicken Haleem → Main Course | Automated PASS | Live UI: PENDING MANUAL ACCEPTANCE |
| `CATEGORY-REGRESSION-07` | Murgh Chanay → Main Course | Automated PASS | Live UI: PENDING MANUAL ACCEPTANCE |
| `CATEGORY-REGRESSION-08` | Aloo Gobi → Main Course | Automated PASS | Live UI: PENDING MANUAL ACCEPTANCE |
| `CATEGORY-REGRESSION-09` | Salad → Salads | Automated PASS | Live UI: PENDING MANUAL ACCEPTANCE |
| `CATEGORY-REGRESSION-10` | Karahi count = 2 | Automated PASS | Live UI: PENDING MANUAL ACCEPTANCE |
| `CATEGORY-REGRESSION-11` | Main Course count = 6 | Automated PASS | Live UI: PENDING MANUAL ACCEPTANCE |
| `CATEGORY-REGRESSION-12` | Salads count = 1 | Automated PASS | Live UI: PENDING MANUAL ACCEPTANCE |
| `CATEGORY-REGRESSION-13` | Uncategorized count = 0 | Automated PASS | Live UI: PENDING MANUAL ACCEPTANCE |
| `CATEGORY-REGRESSION-14` | All Menu total = 11 | Automated PASS | Live UI: PENDING MANUAL ACCEPTANCE |
| `CATEGORY-REGRESSION-15` | Seed idempotency across restarts | Automated PASS | Live UI: PENDING MANUAL ACCEPTANCE |

---

## Conclusion & Verification Status

- **Database State:** VERIFIED PASS
- **Automated Tests:** VERIFIED PASS
- **Live Desktop UI:** **PENDING MANUAL ACCEPTANCE**
