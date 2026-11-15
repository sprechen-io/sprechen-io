namespace Sprechen.Speech.App.Contracts.Evaluation;

/// <summary>
/// Verbindet die Telemetrie für Feldinteraktionen mit der Telemetrie für Navigation
/// und Formularstand. Verbraucher können bei Bedarf nur <see cref="IEvaluationFieldTelemetry"/>
/// oder <see cref="IEvaluationFormStatusTelemetry"/> anfordern.
/// </summary>
public interface IEvaluationFormTelemetry : IEvaluationFieldTelemetry, IEvaluationFormStatusTelemetry
{
}
