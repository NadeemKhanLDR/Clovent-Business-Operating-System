# CBOS Hardware Printer Configuration & Recovery Guide

| Attribute | Details |
| :--- | :--- |
| **Area** | Peripherals / Hardware Integrations & Field Support |
| **Audience** | Installer Technicians, Field Support Engineers, System Administrators |
| **Target Baseline** | CBOS 1.2.2 Baseline / CBOS 1.3.0 Candidate |
| **Status** | **SOURCE-VERIFIED STANDARD** |
| **Classification** | **PUBLIC-SAFE** |

---

## 1. Overview & Printing Architecture

Clovent Business Operating System (CBOS) implements a resilient printing architecture rooted in Windows Print Spooler integration and Transactional Outbox durability.

Rather than relying on unmanaged direct COM/LPT port access, CBOS binds logical printing profiles to installed Windows printer queues. This ensures compatibility with USB, Ethernet/LAN, Wi-Fi, and virtual printers from all major point-of-sale manufacturers (Epson, Star Micronics, Bixolon, Citizen, Xprinter, etc.).

Key source components include:
- Configuration Interface: `src/Clovent.Desktop/Forms/Settings/PrinterSettingsForm.cs`
- Spooler Health Monitor: `src/Clovent.Desktop/Printing/PrinterHardwareHealthView.cs`
- Document Rendering: `src/Clovent.Desktop/Printing/EnhancedReceiptPrintDocument.cs`
- Quarantine Store: `src/Clovent.Platform/Printing/PrintJobQuarantineStore.cs`

---

## 2. Navigating to Printer Configuration

The printer management form is verified in source code at `src/Clovent.Desktop/Forms/Settings/PrinterSettingsForm.cs`.

### UI Navigation Route:
1. Log in to CBOS Back Office with Administrator privileges.
2. In the top ribbon bar, click the **Settings** ribbon page.
3. In the **Hardware** ribbon group, click the **Printers** button.
4. The **Printer Configuration** dialog opens.

```
Ribbon: [ Settings Tab ]
             │
             └── Group: [ Hardware ]
                           │
                           └── Action: [ Printers ]  ──> Opens PrinterSettingsForm
```

---

## 3. Creating & Configuring a Logical Printer Profile

In the `PrinterSettingsForm`, technicians configure logical printer profiles that dictate where different types of POS documents are printed.

### Step 1: Add a New Profile
Click the **Add Profile** button (`btnAddProfile`) at the bottom of the profile list.

### Step 2: Select the Installed Windows Printer
In the **Installed Windows Printer** dropdown (`cmbInstalledPrinters`):
- Click **Refresh Queues** (`btnRefreshQueues`) if your printer was recently installed.
- Select the exact Windows print queue installed for this workstation (e.g. `EPSON TM-T88VI Receipt` or `POS-80`).

### Step 3: Enter Profile Details
- **Profile Name:** Enter an intuitive identifier (e.g. `Main Cashier Receipt Printer` or `Kitchen Hot Line Printer`).
- **Set as Default:** Check **Set as Default Printer** (`chkIsDefault`) if this profile should handle untargeted print jobs on this terminal.

### Step 4: Assign Document Role (`cmbRole`)
Assign the logical role defining which documents route to this printer:

| Printer Role | Target Documents |
| :--- | :--- |
| **`Receipt`** | Customer sales receipts, checkout bills, reprint checks, and card payment vouchers. |
| **`Invoice`** | Formal legal/corporate tax invoices and full-page customer bills. |
| **`Kitchen`** | Food preparation tickets, course orders, and kitchen order modifier slips. |
| **`Bar`** | Beverage preparation tickets, drink orders, and barista chits. |
| **`Label`** | Sticky barcode labels, cup labels, and shelf-edge pricing tags. |
| **`Report`** | Shift Close Z-Reports, cashier X-Reports, and End-of-Day financial summaries. |

### Step 5: Configure Paper Roll Width (`cmbPaperWidth`)
Select the paper roll specification matching the physical printer:
- **`80 mm Standard`:** Standard 3⅛ inch thermal paper rolls (48 columns). Recommended for customer receipts and kitchen tickets.
- **`58 mm Narrow`:** Compact 2¼ inch paper rolls (32 columns). Used on mobile POS terminals or compact counter registers.
- **`A4 Document`:** Full-sheet page layout. Used for standard laser/inkjet corporate invoices and accounting reports.

### Step 6: Hardware Options
- **Auto Cut Paper (`chkCutPaper`):** Enables partial or full knife cut command after document completion.
- **Open Cash Drawer (`chkOpenDrawer`):** Sends cash drawer pulse hook upon printing receipt.

### Step 7: Save Profile
Click **Save Profile** (`btnSaveProfile`). Configuration persists to `%ProgramData%\Clovent\BusinessOperatingSystem\Config\printers.json`.

---

## 4. Test Printing & Layout Verification

Technicians must verify physical printing prior to declaring a register operational:

1. In `PrinterSettingsForm`, highlight the saved profile in the left-hand list.
2. **Preview Test (`btnPreviewTest`):**
   - Renders the receipt layout in a print preview modal without feeding paper.
   - Use this to verify logo placement, column alignment, tax breakdowns, and typography.
3. **Physical Test Print (`btnTestPrint`):**
   - Dispatches a physical diagnostic receipt directly to the Windows print queue.
   - Verifies spooler communication, paper feeding, font readability, and knife cut action.

---

## 5. Cash Drawer Setup & Hardware Triggering

> [!IMPORTANT]
> **HARDWARE WIRING & DRIVER HOOKS:**
> Direct software ESC/POS pulse command generation (`ESC p`) varies across hardware models and is not relied upon in software.
> Cash drawers operate via standard thermal printer kick mechanisms:

### Physical Hardware Setup:
1. Connect the cash drawer's **RJ11 / RJ12** modular cable to the **DK (Drawer Kick)** port on the back of the thermal receipt printer.
2. Ensure the cable is plugged into the printer, **not** a telephone or Ethernet network jack.

### Windows Printer Driver Configuration:
1. Open Windows **Control Panel** -> **Hardware and Sound** -> **Devices and Printers**.
2. Right-click the receipt printer -> select **Printer Properties** (not Printing Preferences).
3. Open the **Device Settings** tab (or **Peripherals** tab, depending on manufacturer).
4. Locate **Cash Drawer** settings:
   - Set **Cash Drawer #1** to: `Open Before Printing` (or `Open After Printing`).
   - Set Pulse duration to standard `50 ms`.
5. Click **Apply** and **OK**.
6. When CBOS completes a cash settlement, the printer driver automatically triggers the RJ11 solenoid to kick the drawer open.

---

## 6. Peripheral Health Monitor & Quarantine Recovery

When a receipt or kitchen printer runs out of paper or goes offline mid-shift, CBOS protects customer transactions from being dropped. Secondary print jobs enter the `PrintJobQuarantineStore`.

### Accessing the Peripheral Health Monitor:
1. In Back Office, select the **Manager Panel** ribbon tab.
2. In the **Operations / Controls** group, click the **Printer Health** button.
3. The **Peripheral Health & Spooler Monitor** (`PrinterHardwareHealthView.cs`) opens.

### Diagnostic Capabilities:
- **Queue Depth:** Displays active and stalled Windows spooler documents.
- **Circuit Breaker Status:** Indicates whether the printing circuit breaker is `Closed` (normal), `Open` (tripped due to repeated errors), or `HalfOpen` (probing for hardware recovery).
- **Quarantined Print Jobs:** Lists failed print tasks with order IDs, timestamps, and destination roles.

### Recovering Quarantined Jobs:
1. Resolve the underlying physical issue (refill paper roll, latch lid, reconnect USB cable).
2. Clear any stuck documents in the Windows print queue (`control printers`).
3. In `PrinterHardwareHealthView`, click **Replay Quarantined Jobs**.
4. The system safely flushes pending tickets to the printer in chronological order.
