# Bounded prerequisite diagnostics

Use `sandbox_diagnostic_supervisor.py launch --folder UNIQUE_PREPARED_FOLDER --seconds N --mode watchdog|prerequisite [--media SQL_MEDIA_DIRECTORY]`.

The prepared folder must contain `guest.cmd`. Never reuse an attempt folder. The launcher exits immediately; a detached supervisor preassigns and owns exactly one Sandbox ID, records UTC deadline and PID, bounds CLI calls, and cleans up in finally. Networking is disabled; there are no security-policy writes. Cleanup retries stop up to three times, captures every native exit/stdout/stderr, and parses a fresh Sandbox list to verify the owned ID is absent without affecting other IDs. Deadline bounds work; cleanup has a separate bounded retry allowance (at most three 20-second stops and 10-second lists). It reports failure if absence cannot be established. A crashed/killed supervisor or host cannot guarantee cleanup; persist ownership for operator recovery. The stalled-task test verifies independence from launcher/controller lifetime, not survival of supervisor or host termination.

For prerequisite mode, use `prepare_prerequisite_diagnostic.ps1 -AttemptFolder UNIQUE_NEW_FOLDER`. It preserves readable source and creates a command entry point without changing execution policy. It verifies the SQL source signature, extracts media with a 150-second bound, runs only the previously failing OLE DB MSI with a 120-second bound and full flushed verbose log, then ends at the first failure or that prerequisite's success. It never invokes SQL engine installation, restores a database, or launches CBOS. Event XML is captured continuously; EVTX export is bounded. Temporary Windows Installer binaries are copied and hashed with signature metadata while they still exist.

Run the watchdog test first and require `supervisor_result.json` passed=true before one prerequisite attempt. Inspect `guest_result.json` separately: supervisor success means bounded cleanup succeeded, not prerequisite success. Keep CBOS visual gates blocked until real UI inspection.

Validation: `python qa/test_sandbox_diagnostic_supervisor.py`; then the actual unique-folder stalled Sandbox test. Do not rebuild CBOS for these script changes.

