using Clovent.Desktop.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Clovent.Desktop.Commissioning.Database;

/// <summary>
/// Periodically executes non-blocking COPY_ONLY database backups when scheduled backups
/// are enabled in <see cref="BackupConfigurationStore"/>.
/// </summary>
public sealed class ScheduledBackupHostedService(
    IDatabaseBackupService backupService,
    IConfiguration configuration,
    ILogger<ScheduledBackupHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval);

        // Initial check shortly after startup
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken).ConfigureAwait(false);
        await TryRunScheduledBackupAsync(stoppingToken).ConfigureAwait(false);

        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            await TryRunScheduledBackupAsync(stoppingToken).ConfigureAwait(false);
        }
    }

    internal async Task TryRunScheduledBackupAsync(CancellationToken ct)
    {
        try
        {
            var configStore = new BackupConfigurationStore();
            var config = configStore.Load();
            if (!config.ScheduledBackupEnabled)
            {
                return;
            }

            var interval = TimeSpan.FromHours(Math.Max(1, config.ScheduledBackupIntervalHours));
            var now = DateTimeOffset.UtcNow;
            if (config.LastScheduledBackupUtc.HasValue && (now - config.LastScheduledBackupUtc.Value) < interval)
            {
                return;
            }

            var connectionString = DatabaseSecretStore.ResolveConnectionString(configuration);
            var (success, backupPath, message) = await backupService
                .CreateBackupAsync(connectionString, config.BackupDirectory, ct)
                .ConfigureAwait(false);

            if (success)
            {
                config.LastScheduledBackupUtc = now;
                configStore.Save(config);
                logger.LogInformation("Scheduled backup succeeded: {BackupPath}", backupPath);
            }
            else
            {
                logger.LogWarning("Scheduled backup failed: {Message}", message);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unhandled error in scheduled backup check.");
        }
    }
}
