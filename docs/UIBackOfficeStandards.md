# Clovent Business Operating System - Back Office UI & High-DPI Standards

## 1. High-DPI Design Rules (DeviceDpi ~240 / ~250% Scaling)
Workstations operating at 1920×1080 with 200%-250% Windows display scaling require specific WinForms layout hygiene to prevent control clipping, truncation, and layout collapse:

1. **TableLayoutPanel Over FlowLayoutPanel for Filters:**
   - Use `TableLayoutPanel` with single rows (`SizeType.AutoSize` or explicit `SizeType.Absolute` in baseline 96-DPI values).
   - Set `Anchor = AnchorStyles.Left` on labels to ensure vertical centering against adjacent edit controls.
   - Do NOT use arbitrary `Location.Y` pixel adjustments, which break as soon as DPI changes.

2. **EntityPicker Standardization:**
   - Standardize all Warehouse, Location, and Branch dropdown editors to a uniform width (min. 220-250px at 96-DPI, dynamically scaling at high-DPI) to accommodate full business names like `"Kitchen Backup Warehouse"` without truncation or ellipsis.
   - Standardize editor height to 28-30px baseline (scaling to 48-60px at 240 DPI).

3. **Toolbar Button Consistency:**
   - All action buttons on a toolbar (e.g. Generate, Preview, Print, Export PDF, Export Excel, Print Summary) must share:
     - Identical height (`28px` baseline).
     - Identical padding and margin.
     - Identical typography (`Segoe UI 9pt` or `9.5pt`).
     - Consistent width classes (Primary/standard action width vs. long-action width).
     - Baseline alignment across the single toolbar row.

4. **Visual Studio Designer Safety:**
   - `Clovent.Desktop.csproj` enforces `<ForceDesignerDPIUnaware>true</ForceDesignerDPIUnaware>` to prevent repeated designer open/save cycles from compounding pixel values on 250%-scaled displays.
   - All forms must provide parameterless constructors with guard:
     ```csharp
     if (DesignMode || System.ComponentModel.LicenseManager.UsageMode == System.ComponentModel.LicenseUsageMode.Designtime)
     {
         return;
     }
     ```
   - No runtime DI container dependencies or database connections in `InitializeComponent()`.

---

## 2. Standard Filter Bar Reference Architecture
The reference filter bar layout established in `InventoryMovementsView` and applied to `EndOfDayReportView` (Sales Summary) and `ShiftHistoryView`:
- **Order of Controls:**
  `[Location/Warehouse Label] [Location/Warehouse Picker]  [Period Label] [Period Combo]  [From Label] [From DateEdit]  [To Label] [To DateEdit]  [Action Buttons]`
- **Vertical Centering:**
  Labels are vertically aligned to the center of their corresponding editors.
- **Consistent Gaps:**
  - Label to Editor gap: `6px - 8px`.
  - Editor to next Label gap: `16px - 20px`.
  - Button to Button gap: `6px - 8px`.
