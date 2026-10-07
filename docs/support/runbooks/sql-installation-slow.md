# Support Runbook: Slow or Hanging SQL Server Express Installation

| Attribute | Details |
| :--- | :--- |
| **Area** | Support Runbook / SQL Server Setup |
| **Audience** | Level 1–2 Support Technicians, Field Deployment Teams |
| **Last Reviewed Version** | CBOS 1.2.2 |
| **Status** | **PROCEDURAL RUNBOOK** |
| **Classification** | **PUBLIC-SAFE** |

---

## 1. Problem Description
During workstation setup, running the unattended or interactive Microsoft SQL Server 2022 Express installer appears to freeze, taking over 30 minutes on steps such as `"SqlEngineConfigAction_install_confignonrc_Cpu64"` or consuming 100% CPU without progress.

---

## 2. Root Causes & Diagnosing the Hang

1. **Pending Windows Reboot:** If Windows Updates installed recently, pending file rename operations in the Windows registry cause SQL Server prerequisite checks to stall.
2. **Real-Time Antivirus Scanning:** Windows Defender or third-party antivirus inspects and locks every temporary cabinet file (`.cab`) extracted into `C:\Users\<User>\AppData\Local\Temp\`.
3. **Missing Visual C++ Redistributable 2015-2022:** The unattended installer attempts to silently install missing redistributable runtimes, waiting on background mutexes.

---

## 3. Step-by-Step Resolution Procedure

### Step 1: Check and Clear Pending Reboots
1. Check registry key:
   ```powershell
   Test-Path "HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\PendingFileRenameOperations"
   ```
2. If `True`, perform a clean Windows restart before proceeding with SQL installation.

### Step 2: Pre-Install Visual C++ Runtimes
Manually download and install the official Microsoft Visual C++ 2015–2022 Redistributable (x64) prior to launching SQL Server setup:
```powershell
Start-Process -FilePath "vc_redist.x64.exe" -ArgumentList "/install /passive /norestart" -Wait
```

### Step 3: Run SQL Server Express Setup from Local Directory with Antivirus Bypass
1. Extract `SQL2022-SSEI-Expr.exe` to a permanent folder: `C:\SQLSetup\`.
2. Temporarily exclude `C:\SQLSetup\` from Windows Defender real-time scanning:
   ```powershell
   Add-MpPreference -ExclusionPath "C:\SQLSetup"
   ```
3. Execute unattended installation using the verified CBOS configuration parameters:
   ```powershell
   .\SETUP.EXE /Q /ACTION=Install /IACCEPTSQLSERVERLICENSETERMS `
       /FEATURES=SQLEngine /INSTANCENAME=SQLEXPRESS `
       /SQLSVCACCOUNT="NT AUTHORITY\Network Service" `
       /SQLSYSADMINACCOUNTS="BUILTIN\Administrators" `
       /TCP=1 /NP=1
   ```
4. Installation will complete within 3–5 minutes on modern SSD workstations. Remove the temporary Defender exclusion upon completion.
