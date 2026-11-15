namespace Sprechen.Voice.EvaluationTelemetry.BrowserInfrastructure;

/// <summary>
/// Nimmt den Browser-Versandstatus und eine optionale Wiederholwartezeit
/// aus dem JavaScript-Aufruf entgegen.
/// </summary>
internal sealed class EvaluationTelemetryBrowserDeliveryResult
{
    public int Status { get; init; }

    public double? RetryAfterMs { get; init; }
}
