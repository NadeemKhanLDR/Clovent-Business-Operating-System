# Windows Sandbox Clean-Machine Manual Acceptance Procedure

| Attribute | Details |
| :--- | :--- |
| **Area** | Release Quality Gate & Clean-Client Acceptance |
| **Audience** | QA Lead, Release Manager, Field Validation Engineers |
| **Last Reviewed Version** | CBOS 1.2.2 |
| **Status** | **PROCEDURAL ACCEPTANCE PROTOCOL** |
| **Classification** | **PUBLIC-SAFE** |

---

## 1. Acceptance Overview & Objective

To certify a candidate build for production release, CBOS requires a rigorous manual acceptance test conducted on a bare, pristine Windows operating system instance.

### Scope Distinction:
- **`CLEAN MACHINE`:** Windows Sandbox instance with zero pre-installed developer tools, zero pre-existing databases, zero .NET SDKs, and zero development configuration files.
- **`LIVE UI EXECUTED`:** The Windows application interface is interactively launched, rendered on a physical/virtual display, and operated via real mouse, keyboard, and touch interactions.
- **`AUTOMATED TEST`:** Distinct from headless unit/integration test runs.

---

## 2. Sandbox Acceptance Test Protocol

```mermaid
flowchart TD
    S1["1. Launch Clean Windows Sandbox"] --> S2["2. Install SQL Server Express 2022"]
    S2 --> S3["3. Run CBOS Inno Setup Installer"]
    S3 --> S4["4. First-Run Commissioning Wizard<br/>(DB, Company, Branch, Admin, Trial)"]
    S4 --> S5["5. Login to Back Office & POS"]
    S5 --> S6["6. Open Shift & Enter Starting Cash Float"]
    S6 --> S7["7. Ring Up Orders (Cash, Card Category, Split Tender)"]
    S7 --> S8["8. Restart App -> Verify Cart Recovery & Cache"]
    S8 --> S9["9. Simulate SQL Outage -> Verify Continuity Mode"]
    S9 --> S10["10. Restore SQL Service -> Verify Auto-Replay & Health"]
```

---

## 3. Step-by-Step Acceptance Script

### Step 1: Launch Clean Windows Sandbox
1. On the host workstation, launch **Windows Sandbox** (`WindowsSandbox.exe`).
2. Verify bare environment: `dotnet --version` returns unrecognized command.

### Step 2: Install Database Engine
1. Copy `SQL2022-SSEI-Expr.exe` into the Sandbox.
2. Run unattended installation to install instance `SQLEXPRESS`.
3. Verify `MSSQL$SQLEXPRESS` service is running.

### Step 3: Run CBOS Production Installer
1. Copy `Clovent.BusinessOperatingSystem-1.2.2-Setup.exe` into the Sandbox.
2. Run the elevated setup wizard.
3. Verify shortcuts appear on Desktop and Start Menu.
4. Confirm installation completed without error dialogs.

### Step 4: First-Run Commissioning Wizard Verification
1. Launch CBOS from Desktop shortcut.
2. Verify **First-Run Wizard** appears automatically.
3. Test database connection (`localhost\SQLEXPRESS`).
4. Apply schema migrations.
5. Enter sample company, branch ("Main Outlet"), and terminal ("REG-01").
6. Onboard initial administrator.
7. Select **Activate 30-Day Evaluation Trial**.
8. Complete wizard. Verify `commissioned.json` and `trial.state` created in `%ProgramData%\Clovent\BusinessOperatingSystem\`.

### Step 5: Operator Sign-In & Dashboard Verification
1. Enter administrator credentials in `LoginForm`.
2. Select **Back Office**. Verify DevExpress ribbon and dashboard KPIs render cleanly with no layout clipping.

### Step 6: POS Entry & Shift Opening
1. Switch to **Restaurant POS**.
2. Verify `PosEntryGateCoordinator` prompts for starting cash float.
3. Enter `1000.00` starting cash float.
4. Verify `RestaurantPosForm` opens in full-screen 3-column layout.

### Step 7: Front-of-House Order & Settlement Workflows
1. **Cash Sale:** Add 2 items, select `Cash`, tender exact amount. Verify order completes and receipt generates.
2. **Card Category Sale:** Add items, select `Card`, record external reference slip number. Verify settlement.
3. **Split Tender:** Add items, pay partial cash, remainder card. Verify balance settled within half-cent tolerance.
4. **Cash In / Cash Out:** Record 500 cash in (coin float) and 200 cash out (petty cash).

### Step 8: Crash Recovery & Cache Verification
1. Add 3 items to the cart. Do not pay.
2. Terminate `Clovent.Desktop.exe` via Task Manager.
3. Re-launch CBOS. Verify active cart session recovery restores all 3 items.

### Step 9: Emergency Continuity Mode Verification
1. Open Windows Services inside Sandbox and **Stop** `MSSQL$SQLEXPRESS`.
2. Ring up a new cash order on the POS.
3. Verify CBOS displays **Continuity Mode (Cash Only)** banner.
4. Complete the cash order. Confirm order receives collision-safe number (`EM-...`) and writes to `continuity_journal.dat`.

### Step 10: Reconnection & Exactly-Once Replay
1. Start `MSSQL$SQLEXPRESS` service in Windows Services.
2. Observe POS terminal: verify notification `"Replaying offline transactions..."`.
3. Open **Operations Health Center**: confirm journal pending count drops to 0 and replayed order appears in order history.
4. Close shift: enter counted cash, verify variance calculation formula:
   $$\text{Starting Float} + \text{Cash In} + \text{Cash Sales} - \text{Cash Out} = \text{Expected Cash}$$

---

## 4. Acceptance Certification Statement
Upon successful completion of all 10 steps, QA Lead signs off on the release candidate report with:
`WINDOWS SANDBOX CLEAN-MACHINE ACCEPTANCE: PASS (LIVE UI EXECUTED)`.
