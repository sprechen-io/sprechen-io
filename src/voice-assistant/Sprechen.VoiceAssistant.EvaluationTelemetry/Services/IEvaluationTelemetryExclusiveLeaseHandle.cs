namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Hält eine erworbene Auswertungssperre bis zur asynchronen Freigabe.
/// </summary>
public interface IEvaluationTelemetryExclusiveLeaseHandle : IAsyncDisposable;
