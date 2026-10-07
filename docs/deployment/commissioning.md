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
`%ProgramData%\Clovent\BusinessOperatingSystem\commissioned.json`

If the marker is absent, `Program.Main()` immediately launches the interactive **First-Run Commissioning Wizard** (`src/Clovent.Desktop/Commissioning/UI/FirstRunWizardForm.cs`).

The wizard executes a guided, 9-phase onboarding workflow:

```mermaid
flowchart LR
    P1["1. Welcome & Pre-flight"] --> P2["2. Database Setup"]
    P2 --> P3["3. Schema Migration"]
    P3 --> P4["4. Organization & Company"]
    P4 --> P5["5. Branch & Terminal"]
    P5 --> P6["6. Initial Administrator"]
    P6 --> P7["7. Master Data Seeding"]
    P7 --> P8["8. Licensing & Trial"]
    P8 --> P9["9. Completion & Persistence"]
```

---

## 2. Detailed Wizard Phases

### Phase 1: Welcome & Pre-flight Verification
- Confirms OS compatibility and 64-bit architecture.
- Verifies that `%ProgramData%\Clovent\BusinessOperatingSystem\` exists and that `ProgramDataAclManager` has established proper DACLs.

### Phase 2: Database Connection & Creation
- Technician enters SQL Server instance details: Server Name (e.g., `localhost` or `SERVER\SQLEXPRESS`), Authentication Mode (Windows Authentication or SQL Authentication), Username, and Password.
- The wizard tests connectivity. If the database `Clovent_BusinessOperatingSystem` does not exist, the wizard creates it with collation `SQL_Latin1_General_CP1_CI_AS`.
- The connection string is encrypted via Windows DPAPI and persisted into `database.config.json`.

### Phase 3: Schema Migration Execution
- Executes initial Entity Framework Core migrations across all six bounded contexts (`Authentication`, `Identity`, `MasterData`, `Catalog`, `Inventory`, `Restaurant`).
- Creates all relational tables, schema-scoped migration history tables (`[<Schema>].[__EFMigrationsHistory]`), indexes, and default schema boundaries.

### Phase 4: Organization & Legal Company Onboarding
- Enters initial enterprise structure:
  - **Organization Name:** Top-level corporate umbrella.
  - **Company Legal Name:** Registered corporate business name.
  - **Company Tax ID / NTN:** Tax registration identifier for invoice compliance.
  - **Base Currency:** Selects primary accounting currency (e.g., PKR, USD, AED, GBP).

### Phase 5: Branch & Workstation Terminal Binding
- Configures physical store location and register parameters:
  - **Branch Code & Name:** (e.g. `MAIN`, "Downtown Branch").
  - **Terminal Code & Name:** (e.g. `REG-01`, "Counter Register 1").
  - **Default Warehouse:** Creates and binds the primary inventory location (e.g. "Main Kitchen Warehouse").
  - Binds the workstation identity into `%LOCALAPPDATA%\Clovent\pos_settings.json`.

### Phase 6: Initial Administrator Onboarding
- Prompts for the master administrative credentials:
  - Administrator Username
  - Secure Administrator Password
  - Contact Email
- Assigns the `Administrator` role with all 214 permissions.
- **Single-Use Enforcement:** The commissioning endpoint permanently deactivates once this user is inserted.

### Phase 7: Initial Master Data Provisioning
- Seeds essential baseline reference data via `InitialMasterDataProvisioningService`:
  - Default payment methods: `Cash`, `Card`, `On Account`.
  - Standard units of measure: `PCS`, `KG`, `PORTION`.
  - Baseline number sequences for orders, invoices, and kitchen tickets.

### Phase 8: Licensing & Evaluation Trial Enrollment
- Offers two options:
  - **Option A: Activate Commercial License:** Technician browses and imports an authorized vendor-issued `.lic` file. The wizard validates RSA-2048 signatures and machine fingerprint.
  - **Option B: Activate 30-Day Evaluation Trial:** Initializes `trial.state` with DPAPI encryption and starts the 30-day evaluation period.

### Phase 9: Completion & Marker Persistence
- Writes `%ProgramData%\Clovent\BusinessOperatingSystem\commissioned.json` containing the commissioning timestamp, workstation fingerprint, and schema version.
- Re-launches the application into the standard operator sign-in screen (`LoginForm`).

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
