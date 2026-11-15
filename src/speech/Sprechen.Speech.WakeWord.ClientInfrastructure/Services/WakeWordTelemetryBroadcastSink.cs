using Microsoft.Extensions.Logging;
using Sprechen.Speech.WakeWord.Contracts;

namespace Sprechen.Speech.WakeWord.ClientInfrastructure.Services;

/// <summary>
/// Leitet Wake-Word-Ereignisse an die Diagnose und <see cref="WakeWordMissEvent"/>
/// zusätzlich an die Evaluation, damit beide Projektionen denselben Ereigniszugang nutzen.
/// </summary>
/// <remarks>
/// Die Diagnose wird zuerst ausgegeben. Fehler der anschließenden Evaluationsprojektion
/// werden abgefangen, sofern sie keinen Abbruch melden.
/// </remarks>
internal sealed class WakeWordTelemetryBroadcastSink(
    WakeWordTelemetryForwardingSink diagnosticSink,
    WakeWordEvaluationTelemetryProjection evaluationProjection,
    ILogger<WakeWordTelemetryBroadcastSink> logger) : IWakeWordTelemetrySink
{
    private readonly WakeWordTelemetryForwardingSink _diagnosticSink =
        diagnosticSink ?? throw new ArgumentNullException(nameof(diagnosticSink));
    private readonly WakeWordEvaluationTelemetryProjection _evaluationProjection =
        evaluationProjection ?? throw new ArgumentNullException(nameof(evaluationProjection));
    private readonly ILogger<WakeWordTelemetryBroadcastSink> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    public ValueTask RecordListenerSetupAsync(WakeWordListenerSetupEvent telemetryEvent, CancellationToken cancellationToken)
        => _diagnosticSink.RecordListenerSetupAsync(telemetryEvent, cancellationToken);

    public ValueTask RecordAcousticDetectionAsync(WakeWordAcousticDetectionEvent telemetryEvent, CancellationToken cancellationToken)
        => _diagnosticSink.RecordAcousticDetectionAsync(telemetryEvent, cancellationToken);

    public ValueTask RecordCaptureLoopAsync(WakeWordCaptureLoopEvent telemetryEvent, CancellationToken cancellationToken)
        => _diagnosticSink.RecordCaptureLoopAsync(telemetryEvent, cancellationToken);

    public ValueTask RecordTranscriptionRejectionAsync(WakeWordTranscriptionRejectionEvent telemetryEvent, CancellationToken cancellationToken)
        => _diagnosticSink.RecordTranscriptionRejectionAsync(telemetryEvent, cancellationToken);

    public ValueTask RecordStitchAsync(WakeWordStitchEvent telemetryEvent, CancellationToken cancellationToken)
        => _diagnosticSink.RecordStitchAsync(telemetryEvent, cancellationToken);

    public async ValueTask RecordMissAsync(WakeWordMissEvent telemetryEvent, CancellationToken cancellationToken)
    {
        await _diagnosticSink.RecordMissAsync(telemetryEvent, cancellationToken).ConfigureAwait(false);

        try
        {
            _evaluationProjection.ProjectMiss(telemetryEvent);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(exception, "Wake-word miss evaluation telemetry failed and was dropped.");
        }
    }

    public ValueTask RecordActivationAsync(WakeWordActivationEvent telemetryEvent, CancellationToken cancellationToken)
        => _diagnosticSink.RecordActivationAsync(telemetryEvent, cancellationToken);

    public ValueTask RecordFallbackAsync(WakeWordFallbackEvent telemetryEvent, CancellationToken cancellationToken)
        => _diagnosticSink.RecordFallbackAsync(telemetryEvent, cancellationToken);
}
