# CBOS Logging & Diagnostics Architecture

| Attribute | Details |
| :--- | :--- |
| **Area** | Diagnostics, Logging & Field Telemetry |
| **Audience** | Support Engineers, DevOps, Systems Administrators |
| **Last Reviewed Version** | CBOS 1.2.2 |
| **Status** | **IMPLEMENTED** |
| **Classification** | **PUBLIC-SAFE** |

---

## 1. Diagnostics Overview

CBOS implements a persistent, zero-crash diagnostics subsystem designed to capture operational anomalies, unhandled exceptions, and circuit breaker events without interrupting point-of-sale checkout.

Diagnostics are rooted in `Microsoft.Extensions.Logging` and extended via a dedicated rolling file provider (`FileLoggerProvider`).

---

## 2. File Logging Architecture (`FileLoggerProvider`)

### 2.1 Physical Storage & File Rotation
- **Directory Path:** `%LOCALAPPDATA%\Clovent\Logs\`
- **File Naming Pattern:** `cbos-YYYY-MM-DD.log`
- **Rotation Policy:** Automatically rolls over at midnight UTC into a new daily log file.
- **Log Format:** Structured textual entries containing timestamp, log level, event category/logger name, and message:
  ```text
  [2026-10-07 14:15:22.104] [INF] [Clovent.Desktop.Program] STARTUP: Persistence and display loaders configured successfully.
  [2026-10-07 14:15:24.891] [WRN] [TerminalResolutionService] CBOS_TERMINAL_ID was not set; falling back to saved workstation settings.
  ```

### 2.2 Standard Log Levels
- **`Debug`:** Verbose internal state transitions (disabled in production; enabled during troubleshooting).
- **`Information`:** Lifecycle milestones (startup phases, shift openings/closings, completed settlements).
- **`Warning`:** Non-fatal faults (circuit breaker trips, retried outbox tasks, fallback terminal bindings).
- **`Error`:** Caught operational errors (failed database queries, printer communication timeouts).
- **`Critical`:** Fatal system halts (DPAPI decryption failures, database schema incompatibility).

---

## 3. Data Sanitization & Secret Redaction

To maintain privacy and compliance:
- **Cardholder Data:** No card numbers (PANs) or CVVs are logged.
- **Passwords & Keys:** Operator passwords, cashier PINs, and RSA private keys are strictly prohibited from appearing in log statements.
- **Connection Strings:** SQL Server connection strings logged by `DatabaseSecretStore` have passwords redacted (`Password=******`).

---

## 4. Current Limitations & Planned Evolution

| Capability | Current State (CBOS 1.2.2) | Planned Future Evolution |
| :--- | :--- | :--- |
| **Log Format** | Rolling plaintext log files. | Structured Serilog JSON logs with OpenTelemetry tracing headers. |
| **Support Package** | Manual folder navigation to `%LOCALAPPDATA%\Clovent\Logs\`. | **Automated Support Package Bundler:** A one-click UI button generating an encrypted `.zip` archive containing redacted logs, machine info, and health metrics. |
| **Central Telemetry** | Local workstation logging only. | Cloud log streaming to Azure Monitor / Seq for multi-store chains. |

---

## 5. Key Classes & Source Traceability

- **Logger Extension:** `src/Clovent.Platform/Logging/FileLoggerExtensions.cs`
- **Diagnostics Form:** `src/Clovent.Desktop/Diagnostics/SupportDiagnosticsForm.cs`
- **Global Exception Handler:** `src/Clovent.Desktop/Forms/Base/GlobalExceptionHandler.cs`
- **Error Dialog Service:** `src/Clovent.Desktop/Services/ErrorDialogService.cs`

---

## 6. Cross References
- [Operations Health Monitoring](operations-health.md)
- [Troubleshooting Guide](../support/troubleshooting.md)
- [Security Architecture](../security/security-architecture.md)
