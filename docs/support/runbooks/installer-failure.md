# Support Runbook: Installer & Deployment Failures

| Attribute | Details |
| :--- | :--- |
| **Area** | Support Runbook / Packaging & Inno Setup |
| **Audience** | Level 1–2 Support Technicians, Field Engineers |
| **Last Reviewed Version** | CBOS 1.2.2 |
| **Status** | **PROCEDURAL RUNBOOK** |
| **Classification** | **PUBLIC-SAFE** |

---

## 1. Problem Description
Running `Clovent.BusinessOperatingSystem-1.2.2-Setup.exe` encounters an error dialog:
- `"Setup was unable to create directory 'C:\Program Files\Clovent\...'"`
- `"Access denied writing to C:\ProgramData\Clovent\..."`
- `"An error occurred while trying to replace the existing file: MoveFile failed; code 5"`

---

## 2. Step-by-Step Diagnostic & Resolution Procedure

### Step 1: Terminate Hanging Application Processes
The most frequent cause of installer file replacement errors (`code 5: Access Denied`) is an active instance of `Clovent.Desktop.exe` or `Clovent.Installer.Provisioner.exe` running in the background.

Open PowerShell as Administrator:
```powershell
Get-Process -Name "Clovent*" | Stop-Process -Force
```

### Step 2: Run Installer with Explicit UAC Administrator Elevation
Do not double-click from restricted network shares. Instead:
1. Copy the setup installer to local drive `C:\Temp\`.
2. Right-click `Clovent.BusinessOperatingSystem-1.2.2-Setup.exe` -> **Run as administrator**.

### Step 3: Antivirus & Endpoint Detection & Response (EDR) Conflicts
Certain third-party endpoint security tools (e.g. SentinelOne, CrowdStrike, Windows Defender Application Control) block newly published unsigned binaries from executing.
- Generate an installer verbose log to diagnose the blocking filter:
  ```powershell
  .\Clovent.BusinessOperatingSystem-1.2.2-Setup.exe /LOG="C:\Temp\cbos_install.log"
  ```
- Inspect `C:\Temp\cbos_install.log` for the exact Windows API failure code.
- Add temporary installation exclusions for `C:\Program Files\Clovent\` if corporate policy permits.
