# Technical Debt & Known Limitations

This document lists identified technical debt, legacy references, hardcoded layout constants, design-time constraints, and architectural simplifications within the CBOS platform, particularly focusing on the Restaurant POS and LoginForm modules.

---

## Active Technical Debt

### 1. Presentation-Only Image Store (No Database Persistence)
- **Description:** Menu item photos are stored as PNG files in the local AppData directory (`%LocalAppData%\Clovent\MenuItemImages\{productId:N}.png`) instead of as blobs in the SQL database.
- **Context:** Decided in Milestone 13 to avoid changing the shared Catalog schema for a Restaurant-specific feature.
- **Risk/Impact:** In a multi-terminal deployment, cashier stations will not share photos unless a shared network path or database synchronization is introduced.
- **Remediation Plan:** Transition to a binary stream/blob database column in `Catalog.Product` or a dedicated media service in a future milestone.

### 2. Fragile Cash/Card Reporting Split
- **Description:** `GetEndOfDayReportQueryHandler` (and related Sales Summary components) groups payment metrics into "Cash" vs. "Card" by executing case-insensitive substring checks against the payment method's name (e.g. name contains `"Cash"` or `"card"`).
- **Context:** Payment methods have no typed classification or enum tag in the domain model.
- **Risk/Impact:** Alternate payment methods like "Easy Paisa", "Mobile Wallet", or custom vouchers are grouped into "Other" and ignored by the main Cash/Card stats cards.
- **Remediation Plan:** Add a `PaymentMethodType` enum field to `PaymentMethod` in the Domain layer to categorize methods explicitly.

### 3. Per-Item Query N+1 Patterns
- **Description:** Screen loading and dashboard widgets resolve variants, prices, and photo existence by firing sequential queries/queries per line in a loop (e.g. fetching variant prices during POS rendering, or loading photos in Menu Items).
- **Context:** Reused existing single-record CQRS queries to save development time.
- **Risk/Impact:** Fine at demo scale, but causes noticeable UI latency at production volumes (hundreds of menu items or thousands of orders).
- **Remediation Plan:** Introduce bulk read queries (e.g. `ListProductPricesByVariantIdsQuery` or batch DTO joins) to retrieve all necessary data in a single SQL operation.

### 4. Wait Cursor Fallback instead of Busy Overlays
- **Description:** `RestaurantPosForm` and `EndOfDayReportView` are WinForms components that use `UseWaitCursor = true/false` to indicate background processing, rather than displaying an interactive busy overlay.
- **Context:** Building full modal overlay panels for custom user controls was out of scope.
- **Risk/Impact:** cashiers can click buttons multiple times during slow network requests if double-clicks are not explicitly guarded.
- **Remediation Plan:** Generalize `LoginForm`'s busy state overlay or build a shared loading panel helper.

### 5. Development SQLite vs. Production SQL Server
- **Description:** Automated integration tests and local development run on SQLite, whereas production uses SQL Server.
- **Risk/Impact:** SQLite does not strictly enforce column precisions (e.g., `HasPrecision(18, 4)` is ignored) and handles case-insensitivity differently, leading to potential differences in query behavior.
- **Remediation Plan:** Maintain identical EF Core database providers or run nightly integration builds against a test SQL Server database instance.

---

## Resolved Debt (Moved to History)

### 1. PaymentPanel Collapsing and Clipping
- **Resolution:** Consolidated nested `PaymentPanel` controls into `RestaurantPosForm`, reserving an absolute 180px height block for the strip and preventing AutoSize layout collapses.
- **Date:** 2026-08-09

### 2. LoginForm Username/PIN Field Clipping
- **Resolution:** Implemented `ApplyContentDrivenMinimumSize()` to measure actual row requirements and grow the login form size dynamically, preventing clipped input boxes.
- **Date:** 2026-08-09

### 3. Obsolete Control References (`RestaurantPosView` / `PaymentPanel`)
- **Resolution:** Replaced all visual design-time references to the retired custom views with references to the unified `RestaurantPosForm` in documentation.
- **Date:** 2026-08-10
