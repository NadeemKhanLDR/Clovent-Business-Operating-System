# Troubleshooting Runbook: Windows Application Control, Code Integrity & Installer Identity

## 1. Executive Summary & Observed Incident

### Observed Symptoms
During manual acceptance testing in a Windows Sandbox or Windows 11 hardened environment, launching the CBOS production installer (`Clovent.BusinessOperatingSystem-1.3.0-Production-Setup.exe`) was blocked by the operating system with the shell dialog:
> **"An Application Control policy has blocked this file."**

Additionally, the compiled setup executable displayed the generic Inno Setup installer icon rather than the official multi-resolution Clovent Business Operating System (CBOS) brand icon.

---

## 2. Root Cause Analysis

### Cause A: Application Control Policy Enforcement (WDAC / Smart App Control)
1. **Mechanism**:
   - Windows 11 Smart App Control (SAC) and Windows Defender Application Control (WDAC) enforce kernel-level Code Integrity checks (`ci.dll`) during process creation (`NtCreateUserProcess`).
   - When an executable is unsigned, does not possess a trusted Authenticode signature chain, and lacks recognized Microsoft cloud reputation, the kernel denies execution (`STATUS_INVALID_IMAGE_HASH` or `STATUS_ACCESS_DENIED`), raising Event ID `3077` in `Microsoft-Windows-CodeIntegrity/Operational` or Event ID `8028` in `Microsoft-Windows-AppLocker/EXE and DLL`.
2. **Layer Distinction**:
   - **Outer Setup Executable** (`*-Setup.exe`): The Inno Setup 6 bootstrapper executable was blocked immediately upon execution before creating any child processes or extracting payload files.
   - **Extracted Payloads** (`is-*.tmp`, `Clovent.Desktop.exe`, `Clovent.Installer.Provisioner.exe`): Were not reached because the outer container was denied execution at the entry point.
   - **Third-Party Microsoft Prerequisites** (`SQLEXPR_x64_ENU.exe`, `SSMS-Setup-ENU.exe`, `VC_redist.*.exe`, `msoledbsql.msi`): Are officially signed by Microsoft Corporation and carry valid digital signatures, but were never unpacked.

### Cause B: Missing `SetupIconFile` in Inno Setup Script
- In `Tools/installer/cbos-setup.iss`, `[Setup]` defined `UninstallDisplayIcon` but omitted the `SetupIconFile` directive.
- Without `SetupIconFile`, Inno Setup's compiler (`ISCC.exe`) defaults to embedding the standard Inno Setup icon (2 resolutions, low color depth) into the PE resource table (`RT_GROUP_ICON`), failing to display the official 7-resolution 32bpp `cbos.ico`.

---

## 3. Implemented Corrections

### 1. Inno Setup Script Icon Integration (`cbos-setup.iss`)
- Defined `#define SetupIconPath "..\..\src\Clovent.Desktop\Resources\cbos.ico"`.
- Set `SetupIconFile={#SetupIconPath}` in `[Setup]`.
- Set `UninstallDisplayIcon={app}\Clovent.Desktop.exe,0`.
- Deployed `{app}\Resources\cbos.ico` explicitly in `[Files]` for shortcut fidelity.

### 2. Automated PE Icon Resource Verification (`Tools/verify_installer_icon.ps1`)
- Inspects the Win32 Portable Executable resource directory (`RT_GROUP_ICON` and `RT_ICON`) directly via Win32 APIs.
- Compares embedded icon dimensions and color depths against `src/Clovent.Desktop/Resources/cbos.ico` (16x16, 24x24, 32x32, 48x48, 64x64, 128x128, 256x256, all 32 bpp).
- Fails the packaging pipeline immediately if the embedded icon is missing, corrupted, or mismatched.

### 3. Canonical Authenticode Code Signing Pipeline (`Tools/sign_release_payloads.ps1`)
- Implements two-stage code signing using `signtool.exe`:
  1. **Pre-Packaging**: Signs owned application binaries (`Clovent.Desktop.exe`, `Clovent.Installer.Provisioner.exe`, and first-party `Clovent.*.dll`s).
  2. **Post-Packaging**: Signs the final generated installer (`Clovent.BusinessOperatingSystem-1.3.0-Production-Setup.exe`).
- Strictly preserves third-party Microsoft signatures on prerequisites without tampering.
- Employs SHA-256 digests (`/fd sha256`) and RFC 3161 trusted timestamping (`/tr http://timestamp.digicert.com /td sha256`).
- Verifies all signatures post-signing using `signtool verify /pa /v` and `Get-AuthenticodeSignature`.

### 4. Read-Only Diagnostic Evidence Collector (`Tools/diagnostics/Collect-AppControlEvidence.ps1`)
- Standalone diagnostic tool for capturing guest/host Application Control state without altering security settings.
- Collects:
  - Complete XML event dumps from `Microsoft-Windows-CodeIntegrity/Operational` (IDs 3076, 3077, 3089, 3099) and `Microsoft-Windows-AppLocker`.
  - Smart App Control registry state (`VerifiedAndReputablePolicyState`).
  - Active WDAC policy IDs and friendly names.
  - File hashes (SHA-256, SHA-1) and Authenticode certificate details.

---

## 4. Diagnostic Runbook for Windows Sandbox Testing

When testing within an existing Windows Sandbox session:

1. **Copy the Diagnostic Collector**:
   - Copy `Collect-AppControlEvidence.ps1` into the Windows Sandbox session.
2. **Execute Read-Only Evidence Collection**:
   ```powershell
   powershell.exe -ExecutionPolicy Bypass -File .\Collect-AppControlEvidence.ps1 -TargetFilePath .\Clovent.BusinessOperatingSystem-1.3.0-Production-Setup.exe
   ```
3. **Inspect Output Files**:
   - `AppControlEvidence_<timestamp>.txt`: Human-readable summary.
   - `AppControlEvidence_<timestamp>.json`: Machine-readable provenance and policy state.
   - `AppControlEvents_<timestamp>.xml`: Complete raw Windows Event Log XML.
4. **Identify Blocked Entity**:
   - Check whether the blocked file was the outer installer or an extracted binary.
   - Verify whether the event lists a specific WDAC PolicyID or Smart App Control rule.

---

## 5. Unresolved Policy Requirements & Commercial Release Caveats

1. **Commercial Authenticode Signing Credential**:
   - The current build environment does not possess an enterprise EV/OV code-signing certificate or HSM access token.
   - **Release Candidate Status**: Local release candidates remain **unsigned** until signed in the secure release enclave using:
     ```powershell
     .\Tools\sign_release_payloads.ps1 -FilesToSign @("installer\Clovent.BusinessOperatingSystem-1.3.0-Production-Setup.exe") -CertificateThumbprint "<Thumbprint>"
     ```
2. **Policy Scope Limitation**:
   - Code signing satisfies standard SmartScreen and Smart App Control requirements once reputation is established.
   - However, custom enterprise WDAC policies that enforce explicit allow-lists by Hash or Publisher must have an approved rule for the Clovent certificate authority or root certificate.
3. **Strict Non-Permissible Actions**:
   - Never use self-signed certificates as a substitute for production trust.
   - Never disable OS security controls, add antivirus exclusions, or modify system security policies to bypass Application Control checks.
