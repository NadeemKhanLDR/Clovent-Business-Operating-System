using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Clovent.Platform.Sync;

/// <summary>
/// Runnable HTTP sender delivering delta sync packets to the branch replication receiver
/// over authenticated TLS/HTTP with HMAC-SHA256 signatures, bounded payloads, and cancellation support.
/// </summary>
public sealed class HttpDeltaSyncSender : IDeltaSyncTransport, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly SyncTransportOptions _options;
    private readonly ILogger<HttpDeltaSyncSender> _logger;
    private readonly bool _ownsHttpClient;

    /// <summary>Creates a new HTTP delta sync sender.</summary>
    public HttpDeltaSyncSender(
        IOptions<SyncTransportOptions> options,
        ILogger<HttpDeltaSyncSender> logger,
        HttpClient? httpClient = null)
    {
        _options = options.Value;
        _logger = logger;
        _ownsHttpClient = httpClient == null;
        _httpClient = httpClient ?? new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(_options.RequestTimeoutSeconds)
        };
    }

    /// <inheritdoc/>
    public async Task<SyncTransportResult> SendPacketAsync(SyncPacket packet, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(packet);
        return await SendBatchAsync([packet], cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<SyncTransportResult> SendBatchAsync(IReadOnlyList<SyncPacket> packets, CancellationToken cancellationToken = default)
    {
        if (packets == null || packets.Count == 0)
        {
            return SyncTransportResult.Succeeded(0);
        }

        var json = JsonSerializer.Serialize(packets);
        var byteCount = Encoding.UTF8.GetByteCount(json);

        // Bounded payload validation
        if (byteCount > _options.MaxPayloadSizeBytes)
        {
            var err = $"Payload size ({byteCount} bytes) exceeds maximum allowable limit of {_options.MaxPayloadSizeBytes} bytes.";
            _logger.LogError("{Error}", err);
            return SyncTransportResult.Failed(err);
        }

        var timestamp = DateTimeOffset.UtcNow.ToString("o");
        var signature = ComputeHmacSignature(timestamp, json, _options.SharedReplicationSecret);

        using var request = new HttpRequestMessage(HttpMethod.Post, _options.EndpointUri);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var first = packets[0];
        request.Headers.Add("X-CBOS-Terminal-Id", first.SourceTerminalId.ToString());
        request.Headers.Add("X-CBOS-Branch-Id", first.SourceBranchId.ToString());
        request.Headers.Add("X-CBOS-Org-Id", first.OrganizationId.ToString());
        request.Headers.Add("X-CBOS-Timestamp", timestamp);
        request.Headers.Add("X-CBOS-Schema-Version", first.SchemaVersion.ToString());
        request.Headers.Add("X-CBOS-Signature", signature);

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errMsg = $"Receiver returned HTTP {(int)response.StatusCode} ({response.ReasonPhrase}): {responseContent}";
                _logger.LogWarning("HTTP sync dispatch failed: {Error}", errMsg);
                return SyncTransportResult.Failed(errMsg);
            }

            var ack = JsonSerializer.Deserialize<SyncAckDto>(responseContent);
            if (ack == null || !ack.Success)
            {
                var errMsg = ack?.ErrorMessage ?? "Receiver returned unparseable or unsuccessful acknowledgement.";
                return SyncTransportResult.Failed(errMsg);
            }

            return SyncTransportResult.Succeeded(ack.DeliveredCount > 0 ? ack.DeliveredCount : packets.Count);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("HTTP sync dispatch was canceled by caller.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "HTTP sync dispatch encountered network or socket error to {Uri}.", _options.EndpointUri);
            return SyncTransportResult.Failed(ex.Message);
        }
    }

    /// <summary>Computes the HMAC-SHA256 signature over timestamp and payload.</summary>
    public static string ComputeHmacSignature(string timestamp, string payload, string secret)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var dataBytes = Encoding.UTF8.GetBytes(timestamp + "\n" + payload);
        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(dataBytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }
}
