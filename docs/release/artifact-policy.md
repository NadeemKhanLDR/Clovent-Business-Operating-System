# Clovent Business Operating System (CBOS) — Artifact Policy

> **Standard Header**
> - **Document ID:** REL-ART-001
> - **Category:** Release Engineering & Repository Governance
> - **Target Audience:** All Engineers, Release Engineers, CI/CD Maintainers, Security Officers
> - **Status:** VALIDATED
> - **Last Updated:** 2026-10-07

---

## 1. Executive Summary & Objective

The **CBOS Artifact Policy** establishes strict repository boundaries distinguishing **Source Assets** (which belong in source control) from **Build & Distribution Artifacts** (which are generated, ephemeral, and strictly forbidden from source control).

Adherence to this policy guarantees:
1. **Repository Hygiene:** Keeps git history lean, fast, and free of binary bloat.
2. **Zero Credential / Secret Leakage:** Prevents unintentional check-in of licensing keys, passwords, or machine-specific tokens.
3. **Reproducible Builds:** Enforces that all binary distributions originate from clean, automated compilation pipelines rather than manual workstation builds.
4. **Security Verification:** Guarantees that released packages pass ReleaseGuard validation before customer delivery.

---

## 2. Git Inclusions: What Belongs in Source Control

The following categories constitute legitimate source assets and are explicitly tracked in Git:

| Category | Allowed File Types / Paths | Description |
| :--- | :--- | :--- |
| **Source Code** | `*.cs`, `*.xaml`, `*.resx`, `*.Designer.cs` | Application and domain logic across all bounded contexts. |
| **Project Definitions** | `*.sln`, `*.slnx`, `*.csproj`, `*.props`, `*.targets` | Build configuration, SDK dependencies, framework baselines. |
| **Database Migrations** | `src/Clovent.*.Infrastructure/Migrations/*.cs` | Code-based EF Core migrations and designer snapshots. |
| **Documentation** | `docs/**`, `README.md`, `CHANGELOG.md`, `AGENTS.md` | Markdown architecture, runbooks, guides, and engineering rules. |
| **Build & Tool Scripts** | `tools/**`, `scripts/**`, `*.ps1`, `*.iss` | Inno Setup scripts, ReleaseGuard scanner, CI workflow definitions. |
| **Base Configuration** | `appsettings.json`, `appsettings.Production.json` | Production templates with environment variable tokens (NO secrets). |
| **Public Cryptography** | `*.pub`, public RSA XML keys embedded in code | Public keys used for digital signature verification. |
| **Static UI Assets** | `Resources/*.png`, `Resources/*.ico`, `Resources/*.svg` | Icons and branding images used in WinForms controls. |

---

## 3. Strict Git Exclusions: What Must NEVER Be Committed

The following files and directories are **STRICTLY PROHIBITED** from Git repositories. Any pull request or commit containing these will be automatically rejected:

```
                          STRICTLY FORBIDDEN FROM GIT
 ┌──────────────────────────────────────────────────────────────────────────┐
 │ • Compiled binaries (*.dll, *.exe, *.so, *.dylib)                        │
 │ • Debug symbols (*.pdb)                                                  │
 │ • Intermediate build output (bin/, obj/, out/, artifacts/)               │
 │ • Installer packages (*.msi, *.exe installer bundles)                    │
 │ • Active or development license files (*.lic, clovent.lic)               │
 │ • Cryptographic private keys (*.key, *.pem, *.pfx, *.snk)                │
 │ • Machine configuration backups (*.bak, *.old, *.tmp)                    │
 │ • Local settings files (appsettings.Development.json, *.user)            │
 │ • Production databases (*.mdf, *.ldf, *.sqlite, *.db)                    │
 │ • Local caches & logs (logs/, *.log, *.dat, *.journal)                   │
 └──────────────────────────────────────────────────────────────────────────┘
```

### 3.1 Git Enforcement (`.gitignore`)
The root `.gitignore` enforces these exclusions at the git engine level:
```gitignore
# Build results
[Dd]ebug/
[Rr]elease/
x64/
x86/
[Bb]in/
[Oo]bj/
artifacts/

# Symbols and native outputs
*.pdb
*.ilk
*.aps

# Visual Studio / Rider / VS Code
.vs/
.idea/
.vscode/
*.user
*.suo

# Local environment configuration
appsettings.Development.json
*.local.json

# Licenses and Private Keys
*.lic
*.key
*.pem
*.pfx
*.snk

# Runtime artifacts and logs
logs/
*.log
*.journal
*.dat
```

---

## 4. Release Distribution Artifacts

Official releases are compiled and staged outside the git working tree into designated packaging directories (e.g. `artifacts/release/`).

### 4.1 Canonical Release Artifact Set

A complete CBOS release package (e.g. version `1.2.2`) consists of exactly:

1. **Self-Contained Client Bundle:**
   - Path: `artifacts/release/Clovent.BusinessOperatingSystem-win-x64/`
   - Content: `Clovent.Desktop.exe`, DevExpress runtime assemblies, .NET 10 CLR runtime (`coreclr.dll`).
   - Prerequisites: Requires zero installed .NET SDK or Visual Studio on customer terminals.
2. **Setup Installer Package:**
   - Path: `artifacts/release/CloventSetup-1.2.2.exe`
   - Content: Inno Setup executable packing the published client bundle, desktop shortcuts, and uninstaller.
3. **Database Provisioning Utility:**
   - Path: `artifacts/release/Clovent.Provisioner-win-x64/`
   - Content: CLI tool for creating physical database `Clovent_BusinessOperatingSystem`, applying initial schema migrations, and configuring `cbos_app` runtime security roles.
4. **Integrity Manifest (`SHA256SUMS.txt`):**
   - Path: `artifacts/release/SHA256SUMS.txt`
   - Content: SHA-256 cryptographic hashes for all released distribution files.

---

## 5. Automated Verification via ReleaseGuard

Before any distribution artifact directory can be published or transmitted to a customer, it must pass automated inspection using the **ReleaseGuard Scanner** (`tools/ReleaseGuard/ScanReleasePackage.ps1`).

```powershell
powershell -ExecutionPolicy Bypass -File tools\ReleaseGuard\ScanReleasePackage.ps1 -ReleaseDir artifacts\release\Clovent.BusinessOperatingSystem-win-x64
```

### 5.1 ReleaseGuard Scanning Rules
ReleaseGuard scans the release bundle recursively and enforces:
1. **Zero Source Code:** No `.cs`, `.csproj`, `.sln`, `.slnx` files.
2. **Zero Debug Files:** No `.pdb` symbols.
3. **Zero Development Configs:** No `appsettings.Development.json`.
4. **Zero Cryptographic Private Keys:** No `.pem`, `.key`, `.pfx` or sensitive credential files.
5. **Zero Shipped Licenses:** No `clovent.lic` or development license files shipped inside the client bundle (licenses are strictly provisioned per-workstation post-installation).
6. **Zero Stale Backups:** No `.bak` or `.old` files.

If any forbidden file is discovered, ReleaseGuard terminates with exit code `1`, blocking the release.

---

## 6. Retention & Archival Policy

- **Artifact Repository:** Official binaries, installer packages, and `SHA256SUMS.txt` are published to the organization's secure artifact repository (e.g., Azure Artifacts / GitHub Releases).
- **Retention Period:**
  - GA Releases: Retained indefinitely for long-term customer maintenance and patching.
  - Release Candidates: Retained for 90 days following final GA release.
  - Development / CI Builds: Retained for 14 days, then automatically purged.
