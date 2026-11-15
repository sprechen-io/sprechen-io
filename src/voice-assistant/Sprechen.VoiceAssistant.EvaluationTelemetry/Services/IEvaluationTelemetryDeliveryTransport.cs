namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Versendet bereits gespeicherte Ereignisdaten an den Telemetrie-Endpunkt. Wiederholungen müssen dieselben Daten verwenden.
/// </summary>
public interface IEvaluationTelemetryDeliveryTransport
{
    Task<EvaluationTelemetryDeliveryResult> DeliverAsync(
        string body,
        CancellationToken cancellationToken);
}
