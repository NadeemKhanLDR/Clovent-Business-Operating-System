# CBOS First-Run Commissioning Wizard

| Attribute | Details |
| :--- | :--- |
| **Area** | Workstation Provisioning & Onboarding |
| **Audience** | Field Service Engineers, System Administrators |
| **Last Reviewed Version** | CBOS 1.2.2 |
| **Status** | **SOURCE-VERIFIED** |
| **Classification** | **PUBLIC-SAFE** |

---

## 1. Commissioning Overview

When CBOS launches on a freshly deployed workstation, it detects whether the machine has completed initial setup by checking for the commissioning marker:
`%ProgramData%\Clovent\BusinessOperatingSystem\Config\commissioning.json`

If the marker is absent (or if any commissioning signal is missing), `Program.Main()` immediately launches the interactive **First-Run Commissioning Wizard** (`src/Clovent.Desktop/Commissioning/UI/FirstRunWizardForm.cs`).

The wizard executes an 8-step onboarding workflow:

```mermaid
flowchart LR
    S1["1. Welcome & Pre-flight"] --> S2["2. Database Setup"]
    S2 --> S3["3. Schema Migration"]
    S3 --> S4["4. Organization & Branch"]
    S4 --> S5["5. Master Administrator"]
    S5 --> S6["6. Regional & Terminal"]
    S6 --> S7["7. License Activation"]
    S7 --> S8["8. Complete Setup"]
```

---

## 2. Detailed Wizard Steps

### Step 1: Welcome & Pre-flight Verification
- Confirms OS compatibility and 64-bit architecture.
- Verifies that `%ProgramData%\Clovent\BusinessOperatingSystem\` exists and that `ProgramDataAclManager` has established proper DACLs.

### Step 2: Database Connection & Credentials
- Technician enters SQL Server instance details: Server Name (e.g., `localhost` or `SERVER\SQLEXPRESS`), Database Name (`Clovent_BusinessOperatingSystem`), Authentication Mode (Windows Authentication or SQL Server Authentication), Username, and Password.
- Technician clicks **Test Connection**. If the database does not exist, the wizard creates it with collation `SQL_Latin1_General_CP1_CI_AS`.
- The connection settings are encrypted via Windows DPAPI and persisted into `%ProgramData%\Clovent\BusinessOperatingSystem\Config\database.config.json`.

### Step 3: Schema Migration Execution
- Technician clicks **Apply Migrations**.
- Executes Entity Framework Core migrations across all six bounded contexts (`Authentication`, `Identity`, `MasterData`, `Catalog`, `Inventory`, `Restaurant`).
- Seeds baseline master reference data (default payment methods: `Cash`, `Card`, `On Account`; units of measure; number sequences).
- Migration progress and success confirmation are displayed.

### Step 4: Organization, Company & Branch Hierarchy
- Enters initial enterprise structure:
  - **Organization Name:** Top-level corporate umbrella.
  - **Company Legal Name:** Registered corporate business name.
  - **Branch Name:** Initial physical store location (e.g., "Downtown Branch").

### Step 5: Master Administrator Account Setup
- Provisions the master administrative user credentials:
  - Administrator Username
  - Contact Email
  - Secure Administrator Password & Confirmation (minimum 8 characters)
- Assigns the `Administrator` role with full system privileges.
- **Single-Use Enforcement:** This provisioning step is strictly for the initial administrator. Cashier accounts are created subsequently in Back Office.

### Step 6: Regional Settings & Terminal Configuration
- Configures regional parameters and local register identification:
  - **Time Zone:** (e.g. `Pakistan Standard Time`, `UTC`).
  - **Date & Time Formats:** Display date format (`yyyy-MM-dd`) and time format.
  - **Base Currency:** Selects primary currency code (`PKR`, `USD`, `AED`, `GBP`).
  - **Terminal Name / Code:** Identifies this register (e.g., `REG-01`, "Counter 1").
  - Workstation settings persist to `%LOCALAPPDATA%\Clovent\pos_settings.json`.

### Step 7: Software License Activation & Evaluation Trial
- Provides two activation options:
  - **Option A: Activate Commercial License:** Technician views Hardware ID, clicks **Import License File**, and imports a signed vendor `.lic` file. The wizard validates RSA signatures and machine fingerprint.
  - **Option B: Activate 30-Day Evaluation Trial:** Check **Enable Evaluation Trial** to initialize a 30-day evaluation trial secured with DPAPI.

### Step 8: Review & Complete Setup
- Displays a final summary of configured settings.
- Technician clicks **Finish Setup**.
- Writes the completion marker containing timestamp and machine fingerprint to:
  `%ProgramData%\Clovent\BusinessOperatingSystem\Config\commissioning.json`
- Launches the standard operator sign-in screen (`LoginForm`).

---

## 3. Post-Commissioning Step: Cashier Account Creation

> [!IMPORTANT]
> **COMMISSIONING PROVISIONS ADMINISTRATOR ONLY:**
> The First-Run Commissioning Wizard provisions only the master Administrator account. Frontline cashiers must **never** operate POS registers using Administrator credentials.
>
> Immediately following first login as Administrator, navigate to **Users** ribbon page -> **Security** group -> **Users** to create individual cashier accounts, assign the `Cashier` role, and configure 4-digit POS sign-in PINs. See the dedicated guide: [Cashier Account Setup & Permissions](cashier-account-setup.md).

---

## 3. Key Classes & Source Traceability

- **Wizard UI Form:** `src/Clovent.Desktop/Commissioning/UI/FirstRunWizardForm.cs`
- **Commissioning Service:** `src/Clovent.Desktop/Commissioning/Services/CommissioningStateService.cs`
- **Provisioning Coordinator:** `src/Clovent.Desktop/Commissioning/Services/CommissioningProvisioningCoordinator.cs`
- **Master Data Seeder:** `src/Clovent.Desktop/Commissioning/Services/InitialMasterDataProvisioningService.cs`
- **ACL Manager:** `src/Clovent.Desktop/Commissioning/Security/ProgramDataAclManager.cs`

---

## 4. Cross References
- [Installation Guide](installation.md)
- [SQL Server Deployment](sql-server.md)
- [Software Licensing Architecture](../security/licensing.md)
- [Terminal Identity](../configuration/terminal-identity.md)
