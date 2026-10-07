# CBOS Logging & Diagnostics Architecture

| Attribute | Details |
| :--- | :--- |
| **Area** | Diagnostics, Logging & Field Telemetry |
| **Audience** | Support Engineers, DevOps, Systems Administrators |
| **Last Reviewed Version** | CBOS 1.2.2 |
| **Status** | **FACTUAL BASELINE** |
| **Classification** | **PUBLIC-SAFE** |

---

## 1. Diagnostics Overview

CBOS implements a persistent, zero-crash diagnostics subsystem designed to capture operational events and runtime exceptions without interrupting point-of-sale checkout.

Diagnostics are rooted in `Microsoft.Extensions.Logging` and extended via a rolling file provider (`FileLoggerProvider`).

---

## 2. File Logging Architecture (`FileLoggerProvider`)

### 2.1 Physical Storage & File Rotation
- **Directory Path:** `%LOCALAPPDATA%\Clovent\Logs\` (with certain components logging under `%ProgramData%\Clovent\BusinessOperatingSystem\Logs\`).
- **File Naming Pattern:** `cbos-YYYY-MM-DD.log`
- **Rotation Policy:** Automatically rolls over daily with retained file count limits.
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
- **Connection Strings:** SQL Server connection strings logged by secret stores have passwords masked (`Password=******`).

---

## 4. Current State vs. Known Limitations vs. Planned Architecture

### 4.1 Current Capability (CBOS 1.2.2)
- Text-based rolling daily log files managed by `FileLoggerProvider`.
- Automatic exception catch-and-log in `GlobalExceptionHandler` and background outbox loops.
- Secret redaction for connection passwords.

### 4.2 Verified Known Limitations (CBOS 1.2.2)
- **Omitted Exception Stack Traces:** `FileLoggerProvider.FormatEntry()` logs exception type and `Message` for inner exceptions, but does **not** append `Exception.StackTrace`. Deep diagnostic stack traces are therefore lost in log files.
- **Directory Fragmentation:** Log files are written to `%LOCALAPPDATA%\Clovent\Logs\`, while machine-wide services reference `%ProgramData%\Clovent\BusinessOperatingSystem\Logs\`.
- **No Automated Support Package Exporter:** Field support requires manual navigation to log folders; no one-click bundle exporter exists in the UI.
- **No SQL Telemetry Interceptor:** EF Core database command interception (`DbCommandInterceptor`) is not implemented; query latency and SQL error telemetry are not logged.
- **Incomplete Correlation Propagation:** Correlation IDs are not systematically propagated across MediatR handlers and background outbox worker tasks.

### 4.3 Planned Architecture (CBOS 1.3.1+)
- **Structured JSON Logging:** Migration to Serilog / OpenTelemetry with full stack trace capture.
- **Support Package Bundler:** One-click UI export bundling sanitized logs, hardware fingerprints, health metrics, and outbox state.
- **Full EF Core SQL Interceptor:** Execution time, parameter inspection (sanitized), and slow query logging.

---

## 5. Key Classes & Source Traceability

- **Logger Extension:** `src/Clovent.Platform/Logging/FileLoggingServiceCollectionExtensions.cs`
- **File Logger Provider:** `src/Clovent.Platform/Logging/FileLoggerProvider.cs`
- **Per-Category Logger:** `src/Clovent.Platform/Logging/FileLogger.cs`
- **Options Class:** `src/Clovent.Platform/Logging/FileLoggerOptions.cs`
- **Global Exception Handler:** `src/Clovent.Desktop/Forms/Base/GlobalExceptionHandler.cs`

---

## 6. Cross References
- [Operations Health Monitoring](operations-health.md)
- [Master Troubleshooting Guide](../support/troubleshooting.md)
- [Security Architecture](../security/security-architecture.md)
- [Known Limitations](../known-limitations.md)
