# ADR-0007: Windows Forms & DevExpress 26.1 Desktop Presentation

| Attribute | Details |
| :--- | :--- |
| **Status** | **ACCEPTED** |
| **Date** | 2026-09-01 |
| **Deciders** | Desktop Architecture Working Group |
| **Area** | Desktop Presentation & UI Standards |

---

## 1. Context & Problem Statement

POS and back-office workstations in hospitality and retail environments demand:
- Instant UI startup and zero latency during high-speed cashier entry.
- Direct, unmediated hardware interaction with receipt printers, cash drawers, customer displays, and barcode scanners.
- High-performance, GPU-accelerated grid controls capable of filtering and grouping tens of thousands of stock records with zero lag.
- Flawless visual scaling across modern high-DPI displays (1080p, 1440p, 4K at 150%–250% Windows scaling).
- Visual Studio Designer compatibility for rapid visual form development.

---

## 2. Decision

CBOS adopts **Windows Forms (.NET 10-windows)** paired with **DevExpress 26.1** UI components:
1. **Presentation Framework:** Windows Forms with `PerMonitorV2` High-DPI mode.
2. **Component Suite:** DevExpress 26.1 (`DevExpress.Win`, `DevExpress.Reporting.Core`, `DevExpress.Images`).
3. **High-DPI Standard:** Native DevExpress vector skin rendering is used. WinForms `AutoScaleMode` is disabled to prevent permanent coordinate corruption during Designer editing.
4. **Designer Safety:** All forms implement parameterless constructors marked `[EditorBrowsable(EditorBrowsableState.Never)]` and guard design-time execution using `DesignModeHelper.IsInDesignMode`.
5. **Centralized Typography:** Global typographic scales and dimensional constants are defined in `DesktopStyle.cs` (e.g. 30px standard toolbar height, standardized button widths).

---

## 3. Consequences

### Positive
- Exceptional UI responsiveness with zero web/browser runtime overhead.
- Native hardware peripheral integration with standard Windows print queues and serial ports.
- Enterprise-grade grid controls with instant grouping, summary footers, and export capabilities.

### Negative / Trade-offs
- Desktop client is restricted to the Microsoft Windows operating system (`net10.0-windows`).
- Developers must strictly adhere to Designer-safety coding rules to prevent breaking Visual Studio CodeDom serializers.
