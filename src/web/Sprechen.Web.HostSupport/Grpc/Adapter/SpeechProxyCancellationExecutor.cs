using System.Diagnostics;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Sprechen.Web.HostSupport.Grpc.Adapter;

/// <summary>
/// Fängt definierte Abbruchausnahmen ab, protokolliert sie und liefert bei generischen Aufrufen einen Ersatzwert.
/// </summary>
internal sealed class SpeechProxyCancellationExecutor
{
    private readonly ILogger _logger;

    public SpeechProxyCancellationExecutor(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        T fallbackResult,
        CancellationToken cancellationToken,
        string operationName,
        string stage,
        string? clientCorrelationId,
        bool swallowInvalidOperation = false)
    {
        try
        {
            return await operation(cancellationToken).ConfigureAwait(false);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
        {
            LogCancellationFallback(operationName, stage, "RpcCancelled", clientCorrelationId);
            return fallbackResult;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            LogCancellationFallback(operationName, stage, "TokenCancelled", clientCorrelationId);
            return fallbackResult;
        }
        catch (InvalidOperationException) when (swallowInvalidOperation && cancellationToken.IsCancellationRequested)
        {
            LogCancellationFallback(operationName, stage, "CancelledInvalidOperation", clientCorrelationId);
            return fallbackResult;
        }
    }

    public async Task ExecuteAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken,
        string operationName,
        string stage,
        string? clientCorrelationId,
        bool swallowInvalidOperation = false)
    {
        try
        {
            await operation(cancellationToken).ConfigureAwait(false);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
        {
            LogCancellationFallback(operationName, stage, "RpcCancelled", clientCorrelationId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            LogCancellationFallback(operationName, stage, "TokenCancelled", clientCorrelationId);
        }
        catch (InvalidOperationException) when (swallowInvalidOperation && cancellationToken.IsCancellationRequested)
        {
            LogCancellationFallback(operationName, stage, "CancelledInvalidOperation", clientCorrelationId);
        }
    }

    private void LogCancellationFallback(
        string operationName,
        string stage,
        string terminal,
        string? clientCorrelationId)
    {
        // Abbrüche auf Information protokollieren, damit sie auch ohne Debug erkennbar bleiben.
        _logger.LogInformation(
            "Proxy {Operation} canceled at stage {Stage} (terminal={Terminal}, clientCorrelationId={ClientCorrelationId}, traceId={TraceId}).",
            operationName,
            stage,
            terminal,
            clientCorrelationId ?? "<none>",
            ResolveTraceId());
    }

    private static string ResolveTraceId()
        => Activity.Current?.TraceId.ToString() ?? "<none>";
}
