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

### Step 3: Verify Default Printer Assignment in CBOS
1. Launch CBOS Back Office.
2. Navigate to **Administration** -> **Printers & Terminals**.
3. Confirm that the default receipt printer matches the installed Windows print queue name.
4. Click **Print Test Page** to verify direct communication.

### Step 4: Resuming Outbox Print Queue
- Because CBOS queues secondary receipts through the Transactional Outbox, no receipts were lost during the outage.
- Once the Windows printer prints successfully, the outbox circuit breaker resets and clears the backlog automatically.
