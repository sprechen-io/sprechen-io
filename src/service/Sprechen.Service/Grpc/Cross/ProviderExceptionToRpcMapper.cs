using System.Diagnostics;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using SprechenProto = Sprechen.Proto;

namespace Sprechen.Service.Grpc;

/// <summary>
/// Überführt Ratenbegrenzungen und andere Anbieterfehler in gRPC-Fehler mit eigenen Fehlercodes.
/// </summary>
internal sealed class ProviderExceptionToRpcMapper(ILogger<SpeechGrpcService> logger)
{
    private readonly ILogger<SpeechGrpcService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public RpcException MapRateLimited(
        SpeechProviderRateLimitedException ex,
        string operation,
        string providerLabel,
        string? clientCorrelationId)
    {
        ArgumentNullException.ThrowIfNull(ex);
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerLabel);

        _logger.LogWarning(
            ex,
            "{Operation} rate limited (provider={Provider}, operation={ProviderOperation}, clientCorrelationId={ClientCorrelationId}, traceId={TraceId}).",
            operation,
            ex.Provider,
            ex.Operation,
            clientCorrelationId ?? "<none>",
            ResolveTraceId());

        return SprechenRpcExceptionFactory.ResourceExhausted(
            SprechenProto.SprechenErrorCode.ProviderRateLimited,
            $"{providerLabel} provider rate limited. Please try again later.");
    }

    public RpcException MapProviderFailure(
        Exception ex,
        string operation,
        string providerLabel,
        string? clientCorrelationId)
    {
        ArgumentNullException.ThrowIfNull(ex);
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerLabel);

        _logger.LogError(
            ex,
            "{Operation} failed (clientCorrelationId={ClientCorrelationId}, traceId={TraceId}).",
            operation,
            clientCorrelationId ?? "<none>",
            ResolveTraceId());

        return SprechenRpcExceptionFactory.Internal(
            SprechenProto.SprechenErrorCode.ProviderFailure,
            $"{providerLabel} provider failed.");
    }

    public static string ResolveTraceId()
        => Activity.Current?.TraceId.ToString() ?? "<none>";
}
