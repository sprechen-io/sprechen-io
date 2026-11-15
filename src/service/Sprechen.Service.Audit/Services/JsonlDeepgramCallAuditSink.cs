using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sprechen.Mda.Contracts.V1.Audit;
using Sprechen.Service.Audit.Options;

namespace Sprechen.Service.Audit.Services;

/// <summary>
/// Hängt je Aufruf eine JSON-Zeile an eine nach Startdatum benannte Datei an.
/// Ein Semaphore serialisiert Schreibvorgänge dieser Instanz. Schreib- und Serialisierungsfehler
/// werden protokolliert. Abbrüche werden an den Aufrufer weitergegeben.
/// </summary>
internal sealed class JsonlDeepgramCallAuditSink : IDeepgramCallAuditSink, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly DeepgramAuditOptions _options;
    private readonly ILogger<JsonlDeepgramCallAuditSink> _logger;
    private readonly SemaphoreSlim _writeGate = new(1, 1);
    private bool _disposed;

    public JsonlDeepgramCallAuditSink(IOptions<DeepgramAuditOptions> options, ILogger<JsonlDeepgramCallAuditSink> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task LogCallAsync(DeepgramApiCallAuditEntry entry, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (_disposed)
        {
            return;
        }

        try
        {
            var line = JsonSerializer.Serialize(entry, JsonOptions);
            var filePath = ResolveFilePath(entry.StartedUtc);

            await _writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                Directory.CreateDirectory(_options.JsonlDirectory);
                await File.AppendAllTextAsync(filePath, line + Environment.NewLine, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _writeGate.Release();
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Schreib- und Serialisierungsfehler protokollieren, ohne den Sprachaufruf dadurch scheitern zu lassen.
            _logger.LogWarning(ex, "Audit sink failed to write entry kind={Kind} correlationId={CorrelationId}", entry.EntryKind, entry.CorrelationId);
        }
    }

    private string ResolveFilePath(DateTime startedUtc)
    {
        var datePart = startedUtc.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        return Path.Combine(_options.JsonlDirectory, $"deepgram-audit-{datePart}.jsonl");
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _writeGate.Dispose();
    }
}
