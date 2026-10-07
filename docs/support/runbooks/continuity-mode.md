# Support Runbook: Continuity Mode Troubleshooting

| Attribute | Details |
| :--- | :--- |
| **Area** | Support Runbook / Offline Resilience |
| **Audience** | Level 1–2 Support Technicians, Cashiers, Store Managers |
| **Last Reviewed Version** | CBOS 1.2.2 |
| **Status** | **PROCEDURAL RUNBOOK** |
| **Classification** | **PUBLIC-SAFE** |

---

## 1. Problem Description
The POS terminal displays an orange/yellow visual banner:
`"EMERGENCY CONTINUITY MODE ACTIVE — CASH ONLY"`
Orders are being assigned local emergency receipt numbers (`EM-...`), and credit sales are disabled.

---

## 2. Operating Safely in Continuity Mode

> [!IMPORTANT]
> **DO NOT PANIC — CONTINUITY MODE IS DESIGNED TO OPERATE:**
> While in Continuity Mode, cashiers can safely continue serving customers, adding menu items, accepting cash payments, and printing thermal receipts. The system is operating normally within its emergency boundaries.

### Golden Rules for Store Managers:
1. **Cash Only:** Accept physical cash only. Instruct customers paying by card to wait or use alternative registers if connected.
2. **DO NOT Restart the PC Repeatedly:** Let the cashier continue operating until service slows down or network connectivity returns.
3. **NEVER Delete `continuity_journal.dat`:**
   Located at `C:\ProgramData\Clovent\BusinessOperatingSystem\continuity_journal.dat`. This file stores un-replayed cash sales. Deleting it destroys customer sales records!

---

## 3. Resolving the Outage & Triggering Journal Replay

### Step 1: Diagnose Network Connectivity
- Check physical Ethernet cables at the POS terminal and network switch.
- Verify that the database server is online (see [database-unavailable.md](database-unavailable.md)).

### Step 2: Automatic Replay Verification
Once database connectivity is restored:
1. `ContinuityCoordinator` automatically detects the restored SQL Server connection.
2. The POS banner updates to: `"Replaying Offline Transactions..."`.
3. The replayer verifies HMAC signatures and atomically commits offline sales into `[Restaurant].[Orders]`.
4. Open the **Operations Health Center** and confirm that **Continuity Journal Pending Entries** drops to **0**.
5. The POS banner disappears, returning the workstation to normal online status.

### Step 3: Handling Replay Failures
If the replayer reports: `"HMAC Signature Verification Failed on Journal Entry"`:
- The journal file was modified or corrupted.
- Immediately contact Level 3 Support to run the offline journal recovery utility. Do not overwrite the file.
