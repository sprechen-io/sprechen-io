namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Hält die Sperre und Abschlusssignale eines übernommenen Durchlaufs zusammen.
/// Ein wiederhergestellter Abschluss darf nur einmal an die Anwendung übergeben werden.
/// </summary>
internal sealed class EvaluationTelemetryTaskOwner(
    string leaseKey,
    string storageKey,
    string sessionId,
    IEvaluationTelemetryExclusiveLeaseHandle lease,
    bool restoredTerminalAdoptionAvailable)
{
    private bool _restoredTerminalAdoptionAvailable = restoredTerminalAdoptionAvailable;

    internal string LeaseKey { get; } = leaseKey;

    internal string StorageKey { get; } = storageKey;

    internal string SessionId { get; } = sessionId;

    internal IEvaluationTelemetryExclusiveLeaseHandle Lease { get; } = lease;

    internal TaskCompletionSource<EvaluationSessionAcceptance> TerminalAcceptance { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal TaskCompletionSource LocalCommit { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal bool TryConsumeRestoredTerminalAdoption()
    {
        if (!_restoredTerminalAdoptionAvailable)
        {
            return false;
        }

        _restoredTerminalAdoptionAvailable = false;
        return true;
    }
}
