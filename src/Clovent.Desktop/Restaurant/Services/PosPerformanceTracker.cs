using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Clovent.Desktop.Restaurant.Services;

/// <summary>Summary metrics for a specific POS interaction type.</summary>
public sealed record OperationPerformanceMetric(
    string OperationName,
    int TotalCount,
    double MinMs,
    double MaxMs,
    double AverageMs,
    double P95Ms,
    double P99Ms,
    int TargetBudgetMs,
    int OverBudgetCount);

/// <summary>
/// High-resolution performance tracker enforcing performance budgets across the POS counter.
/// Cashier interactions must remain fluid and instant during peak rushes.
/// </summary>
public sealed class PosPerformanceTracker
{
    private static readonly ConcurrentDictionary<string, ConcurrentQueue<double>> LatencyHistory = new();
    private static readonly ConcurrentDictionary<string, int> Budgets = new()
    {
        ["ProductAdd"] = 50,
        ["UniversalSearch"] = 100,
        ["CategorySwitch"] = 100,
        ["PaymentCommit"] = 150,
        ["ReceiptDispatch"] = 250,
        ["OutboxEnqueue"] = 50,
        ["CartRender"] = 50
    };

    private static ILogger? s_logger;

    /// <summary>Configures the logger used for performance telemetry.</summary>
    public static void Initialize(ILogger? logger)
    {
        s_logger = logger;
    }

    /// <summary>Executes and records the duration of a synchronous operation.</summary>
    public static T Track<T>(string operationName, Func<T> action)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            return action();
        }
        finally
        {
            sw.Stop();
            RecordDuration(operationName, sw.Elapsed.TotalMilliseconds);
        }
    }

    /// <summary>Executes and records the duration of an asynchronous operation.</summary>
    public static async Task<T> TrackAsync<T>(string operationName, Func<Task<T>> action)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            return await action().ConfigureAwait(false);
        }
        finally
        {
            sw.Stop();
            RecordDuration(operationName, sw.Elapsed.TotalMilliseconds);
        }
    }

    /// <summary>Records a measured duration for the specified operation.</summary>
    public static void RecordDuration(string operationName, double durationMs)
    {
        var queue = LatencyHistory.GetOrAdd(operationName, _ => new ConcurrentQueue<double>());
        queue.Enqueue(durationMs);

        // Keep rolling window of last 500 samples
        while (queue.Count > 500 && queue.TryDequeue(out _)) { }

        if (Budgets.TryGetValue(operationName, out var budgetMs) && durationMs > budgetMs)
        {
            s_logger?.LogWarning(
                "[POS PERFORMANCE BUDGET EXCEEDED] Operation '{Operation}' took {Elapsed:F1}ms (Budget: {Budget}ms)",
                operationName, durationMs, budgetMs);
        }
    }

    /// <summary>Retrieves performance metrics across all tracked operations.</summary>
    public static IReadOnlyList<OperationPerformanceMetric> GetMetrics()
    {
        var result = new List<OperationPerformanceMetric>();

        foreach (var (opName, queue) in LatencyHistory)
        {
            var samples = queue.ToArray();
            if (samples.Length == 0) continue;

            Array.Sort(samples);
            var min = samples[0];
            var max = samples[^1];
            var avg = samples.Average();

            var p95Index = (int)Math.Ceiling(samples.Length * 0.95) - 1;
            var p99Index = (int)Math.Ceiling(samples.Length * 0.99) - 1;
            var p95 = samples[Math.Clamp(p95Index, 0, samples.Length - 1)];
            var p99 = samples[Math.Clamp(p99Index, 0, samples.Length - 1)];

            var budget = Budgets.TryGetValue(opName, out var b) ? b : 100;
            var overBudget = samples.Count(s => s > budget);

            result.Add(new OperationPerformanceMetric(
                OperationName: opName,
                TotalCount: samples.Length,
                MinMs: min,
                MaxMs: max,
                AverageMs: avg,
                P95Ms: p95,
                P99Ms: p99,
                TargetBudgetMs: budget,
                OverBudgetCount: overBudget));
        }

        return result.OrderBy(x => x.OperationName).ToList();
    }
}
