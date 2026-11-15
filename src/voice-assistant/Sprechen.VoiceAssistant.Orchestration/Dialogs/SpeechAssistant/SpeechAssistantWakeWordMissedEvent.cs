namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Meldet eine nicht angenommene Aktivierungsworterkennung mit Status, optionalem
/// Konfidenzwert und Korrelationskennung. Der Reducer ändert bei diesem Ereignis
/// den Bereitschaftszustand nicht. Die Laufzeit ist für die hörbare Rückmeldung zuständig.
/// </summary>
public sealed record SpeechAssistantWakeWordMissedEvent : SpeechAssistantEvent
{
    public SpeechAssistantWakeWordMissedEvent(
        string status,
        int? confidencePercent,
        string? correlationId)
    {
        Status = NormalizeRequiredText(status, nameof(status));
        ConfidencePercent = confidencePercent;
        CorrelationId = NormalizeOptionalText(correlationId);
    }

    public string Status { get; }

    public int? ConfidencePercent { get; }

    public string CorrelationId { get; }

    private static string NormalizeRequiredText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        return value.Trim();
    }

    private static string NormalizeOptionalText(string? value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
}
