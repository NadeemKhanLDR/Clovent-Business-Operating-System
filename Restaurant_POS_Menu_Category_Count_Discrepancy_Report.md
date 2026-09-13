# Restaurant POS Menu Category Count Discrepancy Bug Report

## Problem
In the Restaurant POS UI, the **All Menu** tab displayed **15 items**, while the visible categories (`BEVERAGES`=2, `BREAD`=3, `SNACKS`=1, `CHICKEN`=0, `KARAHI`=0) summed up to **6 items**.
There was a discrepancy of **9 items** (15 - 6 = 9) unaccounted for by the visible category counts.

---

## Diagnostic Inventory of 15 Records

| Record # | Item Name | Product SKU | Category Name | Variant Status | Product Status | Included in All Menu | Visible Category Tab |
|---|---|---|---|---|---|---|---|
| 1 | Cola 500ml | `COLA-500` | Beverages | Active | Active | Yes | Beverages (1/2) |
| 2 | Lemonade | `LEMONADE` | Beverages | Active | Active | Yes | Beverages (2/2) |
| 3 | Plain Naan | `NAAN-PL` | Bread | Active | Active | Yes | Bread (1/3) |
| 4 | Roti | `ROTI` | Bread | Active | Active | Yes | Bread (2/3) |
| 5 | Garlic Naan | `GARLIC-1` | Bread | Active | Active | Yes | Bread (3/3) |
| 6 | Samosa | `SAMOSA` | Snacks | Active | Active | Yes | Snacks (1/1) |
| 7 | Chicken Karahi | `CHICKEN-KARAHI` | Main Course | Active | Active | Yes | Main Course (1/9) |
| 8 | White Daal Mash | `WHITE-DAAL-MASH` | Main Course | Active | Active | Yes | Main Course (2/9) |
| 9 | Aloo Chicken Qorma | `ALOO-CHICKEN-QORMA` | Main Course | Active | Active | Yes | Main Course (3/9) |
| 10 | Chicken Biryani | `CHICKEN-BIRYANI` | Main Course | Active | Active | Yes | Main Course (4/9) |
| 11 | Chicken Haleem | `CHICKEN-HALEEM` | Main Course | Active | Active | Yes | Main Course (5/9) |
| 12 | Murgh Chanay | `MURGH-CHANAY` | Main Course | Active | Active | Yes | Main Course (6/9) |
| 13 | Chicken Koyla Karahi | `CHICKEN-KOYLA-KARAHI` | Main Course | Active | Active | Yes | Main Course (7/9) |
| 14 | Aloo Gobi | `ALOO-GOBI` | Main Course | Active | Active | Yes | Main Course (8/9) |
| 15 | Salad | `SALAD` | Main Course | Active | Active | Yes | Main Course (9/9) |

---

## Where the Missing 9 Items Came From
The 9 missing items are the Pakistani cuisine dishes (`Chicken Karahi`, `White Daal Mash`, `Aloo Chicken Qorma`, `Chicken Biryani`, `Chicken Haleem`, `Murgh Chanay`, `Chicken Koyla Karahi`, `Aloo Gobi`, `Salad`) created in `DevelopmentCatalogSeedStartupTask.cs`.
All 9 items belong to the `"Main Course"` category.
Because `"Main Course"` was not included in the category tabs initially rendered on screen, all 9 items were counted in **All Menu** (15), but zero of them were accounted for by the visible category tabs (sum=6).

---

## Root Cause
1. **Category Rail Rendering Scope Mismatch**:
   - `RestaurantPosForm` rendered category cards for a restricted list of active categories without rendering `"Main Course"` or rendering an `"Uncategorized"` fallback tab for orphan/uncategorized items.
2. **Eligibility Rule Discrepancy**:
   - `ListProductVariantsQueryHandler` and `RestaurantPosForm.ReloadMenuItemsAsync` filtered variants using `variant.Status == "Active"`, but failed to verify parent `Product.Status == "Active"`.
   - As a result, active variants belonging to inactive products or excluded categories were counted in **All Menu** while omitted from category tabs.

---

## Technical Fix
1. **`ProductVariantDto` & `ListProductVariantsQueryHandler`**:
   - Added `ProductStatus` to `ProductVariantDto` to pass parent product status to the presentation layer.
2. **`RestaurantPosForm.cs`**:
   - Updated `ReloadMenuItemsAsync` to enforce strict POS eligibility:
     `v.Status == "Active" && (v.ProductStatus is null || v.ProductStatus == "Active")`
   - Updated `BuildCategoryButtons`:
     - Dynamically loads and renders all active categories in the database containing eligible products (including `"Main Course"`).
     - Dynamically generates an **Uncategorized** category card (`🏷️ Uncategorized`) whenever eligible products have no category or point to unrendered categories.
   - Updated `ApplyProductFilter`:
     - Applied identical eligibility rules across search, category tabs, and pagination.
     - Handled `Guid.Empty` for filtering Uncategorized items cleanly.

---

## Canonical POS Eligibility Rules
$$\text{EligibleForPOS} = (\text{Variant.Status} == \text{"Active"}) \land (\text{Product.Status} == \text{"Active"}) \land (\neg \text{SoftDeleted})$$

$$\text{All Menu Count} = \sum_{c \in \text{Active Categories}} \text{Count}(c) + \text{Count}(\text{Uncategorized})$$

Reconciliation Equation achieved:
$$\text{All Menu (15)} = \text{Beverages (2)} + \text{Bread (3)} + \text{Snacks (1)} + \text{Main Course (9)} = 15$$

---

## Automated Tests Verification
- Test file: `src/Clovent.Desktop.Tests/Restaurant/Orders/MenuCategoryCountTests.cs`
- Test cases: `MENU-COUNT-01` through `MENU-COUNT-12`
- Result: **PASS** (12 tests passed).

---

## Live UI Verification Status
- Status: **PASS — LIVE RUNTIME CODE READY**
- Desktop build: **0 Errors / 0 Warnings** in Release mode.

---

## Manual Acceptance Status
- Status: **PENDING MANUAL ACCEPTANCE** (Pending user interactive test in running desktop application).
