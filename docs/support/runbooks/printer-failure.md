# Support Runbook: POS Thermal Receipt Printer Failure

| Attribute | Details |
| :--- | :--- |
| **Area** | Support Runbook / Hardware Peripherals |
| **Audience** | Level 1–2 Support Technicians, Cashiers, Store Managers |
| **Last Reviewed Version** | CBOS 1.2.2 |
| **Status** | **PROCEDURAL RUNBOOK** |
| **Classification** | **PUBLIC-SAFE** |

---

## 1. Problem Description
Customer receipts or kitchen tickets are not printing after completing checkout in Restaurant POS. Cashier receives a printer warning or the printer status indicator shows red on the **Operations Health Center**.

---

## 2. Step-by-Step Diagnostic & Resolution Procedure

### Step 1: Physical Printer Hardware Check
1. **Paper Roll:** Open the printer cover. Ensure thermal paper is installed in the correct orientation (thermal coating facing outward). Refill if empty.
2. **Cover Latch:** Ensure the printer lid is latched firmly down. An unlatched lid trips the hardware interlock switch.
3. **Power & Error LED:** Check the printer status LED:
   - Solid Blue/Green: Normal idle.
   - Flashing Red: Paper out, paper jam, or cutter jammed.
   - Completely Dark: Check power brick and electrical outlet.
4. **Hardware Feed Test:** Hold the hardware **FEED** button while powering the printer on to produce a printer self-test diagnostic print.

### Step 2: Windows Print Queue & Spooler Check
1. Press `Win + R`, type `control printers`, and press Enter.
2. Locate the POS printer (e.g. `POS-80` or `Epson TM-T88`).
3. Right-click the printer -> **See what's printing**.
4. If print jobs show **"Error - Printing"**:
   - Click **Printer** -> **Cancel All Documents**.
5. Restart the Windows Print Spooler service via PowerShell:
   ```powershell
   Restart-Service -Name "Spooler"
   ```

### Step 3: Verify & Reconfigure Printer in CBOS
1. Launch CBOS Back Office.
2. Navigate to **Settings** ribbon page -> **Hardware** group -> **Printers** (`PrinterSettingsForm`).
3. Select the relevant printer profile in the left-hand profile list (e.g., default Receipt printer).
4. Verify that the **Installed Windows Printer** dropdown matches the verified Windows print queue name.
5. Verify that **Document Role** is set to `Receipt` and paper width matches hardware (e.g. `80 mm Standard` or `58 mm Narrow`).
6. Click **Test Print** (or **Preview Test**) to verify direct rendering and spooling.

### Step 4: Inspect Printer Health & Replay Quarantined Jobs
1. In Back Office, navigate to **Manager Panel** ribbon page -> **Operations / Controls** group -> **Printer Health** (`PrinterHardwareHealthView`).
2. Inspect the live spooler queue depth and circuit breaker status.
3. If jobs were quarantined during an extended printer outage, review the quarantined jobs via `PrintJobQuarantineStore` and trigger replay once hardware is operational.

### Step 5: Resuming Outbox Print Queue
- Because CBOS queues secondary kitchen and receipt prints through the Transactional Outbox, print requests are durably preserved during printer downtime.
- Once the Windows print spooler and hardware respond, the outbox circuit breaker resets and drains the backlog automatically.
