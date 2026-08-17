# ADR-002 — Payment Tender Strip Architecture

## Status

Accepted

## Context

A critical part of cash register speed is the payment flow. Cashiers need to handle diverse payment methods (Cash, Card, Credit, etc.), type in exact or custom amounts, utilize a touch-screen numeric keypad, quickly input common cash values (Quick Cash), and see instant change calculations. However, we must prevent UI-state bugs, such as when programmatic defaults (like pre-filling the balance due) get appended to user keystrokes (e.g. pressing `5` when `$500.00` is pre-filled should result in `5`, not `5005`). We also need to enforce domain rules (such as blocking Credit sales without an active customer, or checking credit limits and requiring manager permissions) before recording payment.

## Decision

We integrated the payment tender strip (`pnlPayment`) as a bottom-docked panel with a height of 180px. The layout uses a 5-column `TableLayoutPanel` (`tlpPayment` with columns sized at 20%/22%/20%/24%/14%):
1. **Payment Methods Flow (`pnlPaymentMethods` / `_methodButtonsFlow`):** Lists loaded methods (Cash, Card, Credit, etc.) as clickable buttons, dynamically colored by method type.
2. **Amount Tendered (`pnlAmountTendered`):** Shows balance due, the amount edit box (`_amountEdit`), the "Exact" balance button, and the Change output label.
3. **Numeric Keypad (`pnlKeypad`):** A touch-friendly grid (7-8-9, 4-5-6, 1-2-3, .-0-⌫, and Clear) that directly mutates `_amountEdit.Text`.
4. **Quick Cash (`pnlQuickCash`):** Standard denomination buttons (e.g. 100, 500, 1000, 5000) that set `_amountEdit.Text` instantly.
5. **Record Payment Button (`_recordButton`):** Triggers `RecordPaymentAsync` to validate permissions and submit the command.

### Keypad Input Replacement Logic
To ensure touch-typing behaves like a standard calculator:
- We track a boolean flag `_amountEntryIsPreset`.
- It is set to `true` when the textbox is populated programmatically (initial load, Exact click, Quick Cash selection, or Clear).
- When `_amountEntryIsPreset` is `true`, the first typed digit replaces the entire text and sets the flag to `false`. Subsequent digit inputs append to the text.
- Backspace on a preset value clears it. Decimal on a preset starts with `"0."`.

### Business Rules Enforced in UI Code-behind
While calculation is performed by `OrderTotalsCalculator` in the application layer, the UI code-behind gates the payment command:
- **Cash Payments:** The only method that allows tendering an amount exceeding the outstanding balance (excess is returned as change).
- **Credit Payments:** Requires an active customer associated with the order. It calls `CanUseFeatureAsync(userId, "pos.creditsale")`. If the sale exceeds the customer's credit limit, it checks `pos.exceedcreditlimit` to show a manager override confirmation dialog, logging the activity on success.

## Consequences

### Benefits
- **Keyboard/Touch Speed:** Cassette-style keypad digit replacement prevents erroneous tender inputs.
- **Robust Gating:** Core business rules are checked pre-flight before sending the MediatR command, keeping the UX clean of raw database exceptions.
- **Flexible UI Layout:** Grid layout scales cleanly at standard POS aspect ratios.

### Trade-offs
- **Name-Based Matching:** Cash/Card KPI divisions rely on string name checks (e.g. "Cash", "card"), which requires payment method names in database seeds to match expected strings.
