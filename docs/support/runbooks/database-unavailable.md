# Support Runbook: Database Unavailable

| Attribute | Details |
| :--- | :--- |
| **Area** | Support Runbook / Persistence Connectivity |
| **Audience** | Level 1–2 Support Technicians, Field Engineers |
| **Last Reviewed Version** | CBOS 1.2.2 |
| **Status** | **PROCEDURAL RUNBOOK** |
| **Classification** | **PUBLIC-SAFE** |

---

## 1. Problem Description
The application fails to start or prompts with:
`"Clovent Business Operating System was unable to connect to the database..."`
or POS terminals unexpectedly transition to **Continuity Mode**.

---

## 2. Step-by-Step Diagnostic & Resolution Procedure

### Step 1: Verify SQL Server Service State
On the database server (or local workstation in Standalone mode), open PowerShell as Administrator:

```powershell
Get-Service -Name "*MSSQL*" | Select-Object Name, Status, StartType
```

- If `MSSQL$SQLEXPRESS` (or `MSSQLSERVER`) is **Stopped**, start it:
  ```powershell
  Start-Service -Name "MSSQL`$SQLEXPRESS"
  ```
- If the service fails to start, inspect Windows Event Viewer -> Application log for SQL Server error events (e.g. disk space full or corrupted system database).

### Step 2: Test TCP/IP Port 1433 Connectivity
From the affected POS workstation, run:

```powershell
Test-NetConnection -ComputerName "<DatabaseServerIPOrHost>" -Port 1433
```

- **`TcpTestSucceeded : True`** -> Network connectivity is healthy; proceed to Step 4.
- **`TcpTestSucceeded : False`** -> Network packet blocked; proceed to Step 3.

### Step 3: Enable TCP/IP & Windows Firewall Rules
On the database server:
1. Open **SQL Server Configuration Manager** -> **SQL Server Network Configuration** -> **Protocols for SQLEXPRESS**.
2. Verify that **TCP/IP** is **Enabled**.
3. Confirm Windows Firewall permits inbound TCP 1433:
   ```powershell
   Get-NetFirewallRule -DisplayName "*1433*" | Select-Object Name, Enabled, Action
   ```

### Step 4: Re-Save DPAPI Connection Settings
If server name or SQL authentication credentials changed:
1. When prompted by CBOS with the connection error dialog, click **Yes** to open the **Database Connection Settings** dialog.
2. Enter the updated server hostname, database name (`Clovent_BusinessOperatingSystem`), and credentials.
3. Click **Test Connection**. Once successful, click **Save**. CBOS will restart with updated DPAPI encrypted settings.

---

## 3. Verification & Escalation
- Launch CBOS Desktop. Verify the **Operations Health Center** displays a green indicator for Database Connectivity with latency < 50ms.
- If connectivity fails after following all steps, escalate to Level 3 Infrastructure Support.
