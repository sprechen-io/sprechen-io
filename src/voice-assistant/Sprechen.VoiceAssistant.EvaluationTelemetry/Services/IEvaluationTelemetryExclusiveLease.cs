namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Versucht eine exklusive Sperre für den gemeinsamen Schlüssel einer
/// Auswertungsidentität oder eines einzelnen Ereignisses zu erwerben.
/// </summary>
public interface IEvaluationTelemetryExclusiveLease
{
    /// <summary>
    /// Liefert die Sperre bei freiem Schlüssel, sonst <see langword="null"/>.
    /// Browseradapter koordinieren darüber die beteiligten Kontexte.
    /// </summary>
    ValueTask<IEvaluationTelemetryExclusiveLeaseHandle?> TryAcquireAsync(
        string leaseKey,
        CancellationToken cancellationToken);
}
