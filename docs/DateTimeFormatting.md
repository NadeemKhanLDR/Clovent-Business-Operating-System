# Clovent Business Operating System - Central Date & Time Formatting

## 1. Motivation & Problem Statement
In previous builds, the Sales Summary -> Orders / Bills grid had a column titled "Time" which rendered only the date portion (e.g. `01-Oct-2026`), omitting the time entirely. This occurred because `DateTimeDisplayLoader` configured a format string with date-only tokens (`dd-MMM-yyyy`), and formatters were scattered ad-hoc across views.

---

## 2. Central Formatter Abstractions
All date and time rendering is now centralized through dedicated business abstractions located in `Clovent.Desktop.Forms.Base`:

1. **`BusinessDateFormatter`**
   - Formats `DateOnly`, `DateTime`, and `DateTimeOffset` to the configured date pattern (e.g. `01-Oct-2026`).
   - Automatically converts UTC timestamps to the company's configured local business timezone.

2. **`BusinessTimeFormatter`**
   - Formats `TimeOnly`, `DateTime`, and `DateTimeOffset` to the configured time pattern (e.g. `06:15 PM` in 12-Hour mode or `18:15` in 24-Hour mode).
   - Automatically handles local timezone conversion.

3. **`BusinessDateTimeFormatter`**
   - Combines both Date and Time into a single human-readable timestamp (e.g. `01-Oct-2026 06:15 PM`).
   - Used for audit logs, order headers, shift summaries, transaction histories, and the Sales Summary Orders / Bills timestamp column.

---

## 3. Company Display Settings
Administrators can configure the company-wide date and time preferences in **Back Office -> Settings -> Business Settings**:
- **Date Format Options:**
  - `dd-MMM-yyyy` (Default, e.g. `01-Oct-2026`)
  - `dd/MM/yyyy` (e.g. `01/10/2026`)
  - `MM/dd/yyyy` (e.g. `10/01/2026`)
  - `yyyy-MM-dd` (ISO format, e.g. `2026-10-01`)
- **Time Format Options:**
  - `12 Hour` (`hh:mm tt`, e.g. `06:15 PM`)
  - `24 Hour` (`HH:mm`, e.g. `18:15`)
- **Quantity Decimal Precision:**
  - Configurable from `0` to `4` decimals (Default: `2` decimals, e.g. `145.00` instead of `145.0000`).

---

## 4. Usage Pattern in WinForms / DevExpress Grids
When formatting grid column values in `CustomColumnDisplayText`:
```csharp
if (e.Column.FieldName == "Time" && e.Value is DateTimeOffset timestamp)
{
    e.DisplayText = BusinessDateTimeFormatter.Format(timestamp);
}
```
And for quantity columns:
```csharp
if (IsQuantityColumn(e.Column.FieldName) && e.Value is decimal qty)
{
    e.DisplayText = QuantityDisplay.Format(qty);
}
```
