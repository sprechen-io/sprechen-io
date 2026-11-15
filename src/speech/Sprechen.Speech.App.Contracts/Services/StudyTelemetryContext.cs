namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Sitzungsweiter Speicher des Teilnehmer-Tokens. Leer, solange kein Studienlink gebunden ist.
/// </summary>
public sealed class StudyTelemetryContext : IStudyTelemetryContext
{
    public string? ParticipantId { get; private set; }

    public void SetStudyContext(string? participantId)
    {
        ParticipantId = Normalize(participantId);
    }

    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}
