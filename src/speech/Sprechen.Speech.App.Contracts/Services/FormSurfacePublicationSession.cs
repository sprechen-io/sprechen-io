namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Verbindet Generation und Korrelation einer Veröffentlichungssitzung zur Zuordnung von Formularansichten.
/// </summary>
public sealed class FormSurfacePublicationSession
{
    public FormSurfacePublicationSession(long generation, Guid correlationId)
    {
        Generation = generation;
        CorrelationId = correlationId;
    }

    public long Generation { get; }

    public Guid CorrelationId { get; }
}
