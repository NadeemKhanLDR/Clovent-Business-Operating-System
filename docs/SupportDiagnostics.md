# Clovent Business Operating System - Support Diagnostics Architecture

This document specifies the support diagnostic subsystem in Clovent Business Operating System (CBOS), designed to provide rapid, safe, and privacy-preserving troubleshooting information to customer support engineers.

---

## 1. Principles of Support Diagnostics

1. **Zero Secret Leakage:** Diagnostic views and exports must **NEVER** expose plaintext SQL passwords, DPAPI connection strings, private encryption keys, license signing keys, employee PINs, or customer payment card information.
2. **Deterministic Version & Environment Tracking:** Displays exact assembly file versions, target database schema revisions, OS release versions, and monitor DPI scaling factors.
3. **One-Click Support Bundle Copy:** Operators can copy an anonymized, text-based diagnostic summary to the system clipboard with a single click to share with support engineers via email or ticket.

---

## 2. Diagnostic Fields Specification

| Diagnostic Field | Description / Source | Safety Classification |
|---|---|:---:|
| **Product Name** | `Clovent Business Operating System` | Public |
| **Application Version** | `AssemblyInformationalVersion` / `1.0.1` | Public |
| **Process Bitness** | `64-bit (win-x64)` | Public |
| **.NET Runtime** | `.NET 10.0.10 Windows Desktop Runtime` | Public |
| **Operating System** | `Environment.OSVersion` (e.g. Windows 11 Pro 64-bit) | Non-sensitive |
| **Display DPI Scaling** | Current DPI scale factor (e.g., `96 DPI (100%)` or `144 DPI (150%)`) | Non-sensitive |
| **Database Server** | Server hostname/instance without credentials (e.g., `SQLSERVER-PROD\SQLEXPRESS`) | Non-sensitive |
| **Database Name** | Target database catalog (e.g., `Clovent_BusinessOperatingSystem`) | Non-sensitive |
| **Authentication Type** | `Windows Authentication` or `SQL Server Authentication` | Non-sensitive |
| **Database Connectivity** | `Connected (Latency: 2ms)` or `Connection Failed` | Non-sensitive |
| **Schema Compatibility** | `Compatible` (All EF Core migrations applied) | Non-sensitive |
| **Licensed Organization** | Customer organization name from active license | Non-sensitive |
| **License Status** | `Valid`, `GracePeriod`, `Expired`, `Unlicensed` | Non-sensitive |
| **License Expiry** | Maintenance or subscription expiry date formatted in business timezone | Non-sensitive |
| **Hardware Machine ID** | Workstation hardware fingerprint hash (e.g. `A1B2-C3D4-E5F6-0789`) | Non-sensitive |
| **Assigned Terminal** | Terminal Name and Code configured for this workstation | Non-sensitive |
| **Assigned Branch** | Active Store/Branch identifier | Non-sensitive |
| **ProgramData Path** | `%ProgramData%\Clovent\BusinessOperatingSystem\` | Non-sensitive |
| **Log Directory** | `%ProgramData%\Clovent\BusinessOperatingSystem\Logs\` | Non-sensitive |

---

## 3. Sample Diagnostic Output

```text
================================================================================
CLOVENT BUSINESS OPERATING SYSTEM - SYSTEM SUPPORT DIAGNOSTICS
Timestamp: 2026-10-01 21:15:00 UTC
================================================================================
Application:
  Product Name:         Clovent Business Operating System
  Version:              1.0.1
  Target Platform:      win-x64 (.NET 10.0)
  High-DPI Mode:        PerMonitorV2 (Current: 100% / 96 DPI)
  Environment:          Production

Operating System:
  OS Version:           Microsoft Windows NT 10.0.26100.0 (Windows 11)
  Host Name:            POS-TERMINAL-01
  User Account Type:    Standard User (Interactive)

Database:
  Server:               192.168.1.10\SQLEXPRESS
  Database Name:        Clovent_BusinessOperatingSystem
  Auth Mode:            SQL Server Authentication (Credentials encrypted via DPAPI)
  Connection Status:    Connected (Pool Size: 10, Latency: 3ms)
  Schema Status:        Compatible (6 Contexts: Auth, Identity, MasterData, Catalog, Inventory, Restaurant)

Store & Workstation:
  Organization:         Grandview Hospitality LLC
  Company:              Grandview Restaurants
  Branch:               Main Street Flagship
  Terminal:             Front Counter POS 1 (ID: term-001)

Licensing:
  Status:               Valid & Active (Commercial Subscription)
  License ID:           9b8c1a74-d411-4f76-bc39-16a2d980f98e
  Expires:              01-Oct-2027 (365 days remaining)
  Hardware ID:          D7E2-90FA-B841-33C0
  Allowed Terminals:    4 allocated / 5 maximum

Storage & Log Paths:
  Config Directory:     C:\ProgramData\Clovent\BusinessOperatingSystem\Config\
  License Directory:    C:\ProgramData\Clovent\BusinessOperatingSystem\License\
  Log Directory:        C:\ProgramData\Clovent\BusinessOperatingSystem\Logs\
================================================================================
```

---

## 4. Accessing Support Diagnostics

- **From Shell / Main Menu:** Help -> System & Support Information (`Ctrl + F1`).
- **From Sign-In Screen:** Support Info link at bottom-left corner of `LoginForm`.
- **From Command Line / Script:** Launch executable with `--diagnostics` to dump diagnostics to console/log.
