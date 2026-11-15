using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Bündelt Speicher, Versand, Sperren, Wiederholungssteuerung und Konfiguration für die Telemetrie-Outbox.
/// </summary>
internal sealed class EvaluationTelemetryOutboxInfrastructure
{
    internal EvaluationTelemetryOutboxInfrastructure(
        IEvaluationTelemetryOutboxStore store,
        IEvaluationTelemetryDeliveryTransport transport,
        IEvaluationTelemetryExclusiveLease exclusiveLease,
        IEvaluationTelemetryRetryDelay retryDelay,
        IOptions<EvaluationTelemetryOptions> options,
        ILogger<EvaluationTelemetry> logger)
    {
        Store = store ?? throw new ArgumentNullException(nameof(store));
        Transport = transport ?? throw new ArgumentNullException(nameof(transport));
        ExclusiveLease = exclusiveLease ?? throw new ArgumentNullException(nameof(exclusiveLease));
        RetryDelay = retryDelay ?? throw new ArgumentNullException(nameof(retryDelay));
        Options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    internal IEvaluationTelemetryOutboxStore Store { get; }

    internal IEvaluationTelemetryDeliveryTransport Transport { get; }

    internal IEvaluationTelemetryExclusiveLease ExclusiveLease { get; }

    internal IEvaluationTelemetryRetryDelay RetryDelay { get; }

    internal EvaluationTelemetryOptions Options { get; }

    internal ILogger<EvaluationTelemetry> Logger { get; }
}
