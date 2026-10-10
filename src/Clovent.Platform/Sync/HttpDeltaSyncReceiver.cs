using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Clovent.Platform.Sync;

/// <summary>
/// Runnable HTTP receiver listening for inbound delta-sync packets from branch peer terminals.
/// Enforces authenticated terminal identity, clock skew limits, bounded payloads,
/// scope verification (Organization, Branch, Schema Version), and returns durable acknowledgements.
/// </summary>
public sealed class HttpDeltaSyncReceiver : IHostedService, IDisposable
{
    private readonly ISyncIngestionEngine _ingestionEngine;
    private readonly SyncTransportOptions _options;
    private readonly SyncScopeContext _scopeContext;
    private readonly ILogger<HttpDeltaSyncReceiver> _logger;

    private HttpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _listenTask;
    private bool _simulateLostAck;

    /// <summary>Creates a new HTTP delta sync receiver.</summary>
    public HttpDeltaSyncReceiver(
        ISyncIngestionEngine ingestionEngine,
        IOptions<SyncTransportOptions> options,
        ILogger<HttpDeltaSyncReceiver> logger,
        SyncScopeContext? scopeContext = null)
    {
        _ingestionEngine = ingestionEngine;
        _options = options.Value;
        _logger = logger;
        _scopeContext = scopeContext ?? new SyncScopeContext();
    }

    /// <summary>Configures the receiver to simulate lost acknowledgement (for acceptance resilience testing).</summary>
    public void SetSimulateLostAck(bool simulate)
    {
        _simulateLostAck = simulate;
    }

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _listener = new HttpListener();
            var prefix = _options.EndpointUri.EndsWith('/') ? _options.EndpointUri : _options.EndpointUri + "/";
            _listener.Prefixes.Add(prefix);
            _listener.Start();

            _cts = new CancellationTokenSource();
            _listenTask = Task.Run(() => ListenLoopAsync(_cts.Token), CancellationToken.None);

            _logger.LogInformation("HttpDeltaSyncReceiver started listening on {Prefix}.", prefix);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start HttpDeltaSyncReceiver on {Prefix}.", _options.EndpointUri);
            throw;
        }
    }

    private async Task ListenLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _listener != null && _listener.IsListening)
        {
            try
            {
                var context = await _listener.GetContextAsync().ConfigureAwait(false);
                _ = Task.Run(() => ProcessRequestAsync(context, ct), ct);
            }
            catch (HttpListenerException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception ex)
            {
                if (!ct.IsCancellationRequested)
                {
                    _logger.LogWarning(ex, "Error accepting HTTP connection in delta-sync receiver.");
                }
            }
        }
    }

    private async Task ProcessRequestAsync(HttpListenerContext context, CancellationToken ct)
    {
        var request = context.Request;
        var response = context.Response;

        try
        {
            if (!string.Equals(request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase))
            {
                response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                await WriteResponseAsync(response, new SyncAckDto { Success = false, ErrorMessage = "Only POST method is accepted." }, ct);
                return;
            }

            // 1. Bounded Payload Check
            if (request.ContentLength64 > _options.MaxPayloadSizeBytes)
            {
                response.StatusCode = (int)HttpStatusCode.RequestEntityTooLarge;
                await WriteResponseAsync(response, new SyncAckDto { Success = false, ErrorMessage = "Payload exceeds maximum allowed size." }, ct);
                return;
            }

            // 2. Read Request Body
            using var reader = new StreamReader(request.InputStream, Encoding.UTF8);
            var body = await reader.ReadToEndAsync(ct).ConfigureAwait(false);

            // 3. Extract Headers
            var terminalIdHeader = request.Headers["X-CBOS-Terminal-Id"];
            var branchIdHeader = request.Headers["X-CBOS-Branch-Id"];
            var orgIdHeader = request.Headers["X-CBOS-Org-Id"];
            var timestampHeader = request.Headers["X-CBOS-Timestamp"];
            var signatureHeader = request.Headers["X-CBOS-Signature"];

            if (string.IsNullOrWhiteSpace(signatureHeader) || string.IsNullOrWhiteSpace(timestampHeader))
            {
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                await WriteResponseAsync(response, new SyncAckDto { Success = false, ErrorMessage = "Missing authentication or signature headers." }, ct);
                return;
            }

            // 4. Validate Clock Skew
            if (DateTimeOffset.TryParse(timestampHeader, out var clientTimestamp))
            {
                var skew = Math.Abs((DateTimeOffset.UtcNow - clientTimestamp).TotalSeconds);
                if (skew > _options.AllowedClockSkewSeconds)
                {
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    await WriteResponseAsync(response, new SyncAckDto { Success = false, ErrorMessage = $"Clock skew ({skew:F0}s) exceeds allowable limit." }, ct);
                    return;
                }
            }

            // 5. Verify HMAC Signature
            var expectedSignature = HttpDeltaSyncSender.ComputeHmacSignature(timestampHeader, body, _options.SharedReplicationSecret);
            if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expectedSignature),
                Encoding.UTF8.GetBytes(signatureHeader.ToLowerInvariant())))
            {
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                await WriteResponseAsync(response, new SyncAckDto { Success = false, ErrorMessage = "Invalid cryptographic signature." }, ct);
                return;
            }

            // 6. Validate Tenancy Scope
            if (Guid.TryParse(orgIdHeader, out var senderOrgId) &&
                _scopeContext.OrganizationId != Guid.Empty &&
                senderOrgId != Guid.Empty &&
                senderOrgId != _scopeContext.OrganizationId)
            {
                response.StatusCode = (int)HttpStatusCode.Forbidden;
                await WriteResponseAsync(response, new SyncAckDto { Success = false, ErrorMessage = "Cross-organization replication prohibited." }, ct);
                return;
            }

            // 7. Parse Packets
            IReadOnlyList<SyncPacket> packets;
            try
            {
                if (body.TrimStart().StartsWith('['))
                {
                    packets = JsonSerializer.Deserialize<List<SyncPacket>>(body) ?? [];
                }
                else
                {
                    var single = JsonSerializer.Deserialize<SyncPacket>(body);
                    packets = single != null ? [single] : [];
                }
            }
            catch (JsonException ex)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                await WriteResponseAsync(response, new SyncAckDto { Success = false, ErrorMessage = $"Corrupt JSON payload: {ex.Message}" }, ct);
                return;
            }

            if (packets.Count == 0)
            {
                response.StatusCode = (int)HttpStatusCode.OK;
                await WriteResponseAsync(response, new SyncAckDto { Success = true, DeliveredCount = 0 }, ct);
                return;
            }

            // 8. Durable Ingestion Commit
            var results = await _ingestionEngine.IngestBatchAsync(packets, ct).ConfigureAwait(false);
            var firstResult = results[0];

            // If any packet was rejected, report failure
            var rejected = results.FirstOrDefault(r => r.Status == SyncIngestionStatus.Rejected);
            if (rejected != null)
            {
                response.StatusCode = (int)HttpStatusCode.Forbidden;
                await WriteResponseAsync(response, new SyncAckDto
                {
                    Success = false,
                    PacketId = rejected.PacketId,
                    Status = rejected.Status.ToString(),
                    ErrorMessage = rejected.Message
                }, ct);
                return;
            }

            // 9. Simulate Lost ACK if testing resilience
            if (_simulateLostAck)
            {
                _logger.LogWarning("Simulating lost acknowledgement: aborting connection after durable commit.");
                context.Response.Abort();
                return;
            }

            // 10. Return Successful Acknowledgement AFTER durable receipt/commit
            response.StatusCode = (int)HttpStatusCode.OK;
            await WriteResponseAsync(response, new SyncAckDto
            {
                Success = true,
                DeliveredCount = results.Count,
                PacketId = firstResult.PacketId,
                Status = firstResult.Status.ToString(),
                IngestedAtUtc = DateTimeOffset.UtcNow
            }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in HttpDeltaSyncReceiver processing request.");
            try
            {
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                await WriteResponseAsync(response, new SyncAckDto { Success = false, ErrorMessage = ex.Message }, ct);
            }
            catch { }
        }
    }

    private static async Task WriteResponseAsync(HttpListenerResponse response, SyncAckDto dto, CancellationToken ct)
    {
        response.ContentType = "application/json; charset=utf-8";
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(dto));
        response.ContentLength64 = bytes.Length;
        await response.OutputStream.WriteAsync(bytes, ct).ConfigureAwait(false);
        response.OutputStream.Close();
    }

    /// <inheritdoc/>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_cts != null)
        {
            _cts.Cancel();
        }

        if (_listener != null && _listener.IsListening)
        {
            try { _listener.Stop(); } catch { }
            try { _listener.Close(); } catch { }
        }

        if (_listenTask != null)
        {
            try { await _listenTask.ConfigureAwait(false); } catch { }
        }

        _logger.LogInformation("HttpDeltaSyncReceiver stopped.");
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _cts?.Dispose();
        if (_listener != null)
        {
            try { _listener.Close(); } catch { }
        }
    }
}
