namespace Clovent.Platform.Printing;

/// <summary>
/// Hardware adapter boundary for dispatching print jobs to device endpoints
/// (Windows driver spooler, ESC/POS network, ESC/POS serial, virtual PDF).
/// </summary>
public interface IPrinterAdapter
{
    /// <summary>The transport connection type handled by this adapter.</summary>
    PrinterConnectionType SupportedConnection { get; }

    /// <summary>Dispatches the formatted print job to the physical printer or queue.</summary>
    Task<PrintDispatchResult> DispatchAsync(
        PrinterProfile profile,
        PrinterJob job,
        CancellationToken cancellationToken = default);

    /// <summary>Probes whether the printer endpoint or queue is currently available.</summary>
    Task<bool> IsAvailableAsync(
        PrinterProfile profile,
        CancellationToken cancellationToken = default);

    /// <summary>Polls physical hardware and spooler health status (offline, paper out, cover open, cutter error, queue depth).</summary>
    Task<PrinterHealthSnapshot> CheckHealthAsync(
        PrinterProfile profile,
        CancellationToken cancellationToken = default);
}
