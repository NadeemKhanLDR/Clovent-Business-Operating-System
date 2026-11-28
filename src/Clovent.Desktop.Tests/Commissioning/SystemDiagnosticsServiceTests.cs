using System.Text;
using Clovent.Desktop.Commissioning.Database;
using Clovent.Desktop.Commissioning.UI;
using Xunit;

namespace Clovent.Desktop.Tests.Commissioning;

public sealed class SystemDiagnosticsServiceTests
{
    [Fact]
    public async Task BuildDiagnosticsReportAsync_IncludesDiskLogAndBackupHealth_AndExposesNoSecrets()
    {
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var tempDir = Path.Combine(Path.GetTempPath(), $"cbos-diag-test-{suffix}");
        var logsDir = Path.Combine(tempDir, "Logs");
        Directory.CreateDirectory(logsDir);
        File.WriteAllText(Path.Combine(logsDir, "cbos-20261128.log"), "Sample log entry");

        var historyPath = Path.Combine(tempDir, "backup_history.json");
        var cfgPath = Path.Combine(tempDir, "backup_settings.json");

        try
        {
            var historyStore = new BackupHistoryStore(historyPath);
            historyStore.Record(new BackupHistoryEntry(
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                "Backup",
                "Clovent_Test",
                @"D:\Backups\Clovent_Test.bak",
                2048,
                Verified: true,
                Success: true,
                "OK",
                "tester"));

            var cfgStore = new BackupConfigurationStore(cfgPath);
            cfgStore.Save(new BackupConfiguration
            {
                BackupDirectory = tempDir,
                ScheduledBackupEnabled = true,
                ScheduledBackupIntervalHours = 12
            });

            var service = new SystemDiagnosticsService(historyStore, cfgStore, logsDir);
            var report = await service.BuildDiagnosticsReportAsync(deviceDpi: 96);

            Assert.Contains("CLOVENT BUSINESS OPERATING SYSTEM - SYSTEM SUPPORT DIAGNOSTICS", report);
            Assert.Contains("Backup & Recovery:", report);
            Assert.Contains("Verified: True", report);
            Assert.Contains("Scheduled Backups:    Enabled (Every 12h)", report);
            Assert.Contains("Disk Space Health:", report);
            Assert.Contains("Log File Health:", report);
            Assert.Contains("1 file(s)", report);

            // Ensure no plaintext secrets or encrypted credential fields leak
            Assert.DoesNotContain("EncryptedPassword", report, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("PlainTextPassword", report, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Password=", report, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public void AppendLogHealth_HandlesMissingDirectoryGracefully()
    {
        var sb = new StringBuilder();
        SystemDiagnosticsService.AppendLogHealth(sb, "Test Logs", Path.Combine(Path.GetTempPath(), $"nonexistent-{Guid.NewGuid():N}"));
        Assert.Contains("Directory not created yet", sb.ToString());
    }
}
