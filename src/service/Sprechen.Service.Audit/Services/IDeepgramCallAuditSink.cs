using Sprechen.Mda.Contracts.V1.Audit;

namespace Sprechen.Service.Audit.Services;

/// <summary>
/// Schnittstelle zum Protokollieren von Deepgram-Aufrufen.
/// </summary>
public interface IDeepgramCallAuditSink
{
    /// <summary>
    /// Protokolliert einen Aufruf. Implementierungen müssen parallele Aufrufe unterstützen
    /// und Schreibfehler protokollieren, ohne sie an den Sprachdienst weiterzugeben.
    /// </summary>
    Task LogCallAsync(DeepgramApiCallAuditEntry entry, CancellationToken cancellationToken);
}
