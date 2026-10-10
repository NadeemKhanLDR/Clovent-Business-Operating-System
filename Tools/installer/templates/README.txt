Clovent Business Operating System (CBOS) - Configuration Templates
========================================================================

These template files provide administrative references for CBOS configuration.

1. database.config.template.json
   - Demonstrates the schema used by DatabaseSecretStore.
   - When configured through Setup or Commissioning, the file is encrypted using Windows DPAPI (LocalMachine scope)
     and saved to %ProgramData%\Clovent\BusinessOperatingSystem\Config\database.config.json.
   - Folder ACLs enforce: Administrators & SYSTEM (Full Control), Users (Read-Only).

2. pos_settings.template.json
   - Demonstrates the default POS operational settings (items per row, active order panel state, default payment method).
   - Stored in %ProgramData%\Clovent\BusinessOperatingSystem\pos_settings.json.
