using Sprechen.Mda.Contracts.V1.Audit;

namespace Sprechen.Service.Audit.Services;

/// <summary>
/// Verarbeitet Protokollierungsaufrufe ohne Ausgabe, wenn die Aufzeichnung deaktiviert ist.
/// </summary>
internal sealed class NoOpDeepgramCallAuditSink : IDeepgramCallAuditSink
{
    public Task LogCallAsync(DeepgramApiCallAuditEntry entry, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
