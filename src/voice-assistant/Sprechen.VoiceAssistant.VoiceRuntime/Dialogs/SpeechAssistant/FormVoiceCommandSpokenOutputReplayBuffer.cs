using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.VoiceRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Hält das letzte Wiederholungsziel und eine einmalige Ansage für den
/// aktuellen Feldzug. Der Sprachdienst merkt die Texte vor dem Abspielen.
///
/// Der manuelle Wiederholungsbefehl darf den letzten Text über Feldwechsel
/// hinweg lesen. Das automatische Ziel wird bei einem neuen Feldzug gelöscht
/// und nach einer Wiederholung gesperrt. Der aufrufende Ablauf besitzt
/// diese Lebensdauer und die Reihenfolge der Zugriffe.
/// </summary>
internal sealed class FormVoiceCommandSpokenOutputReplayBuffer : IFormVoiceCommandSpokenOutputReplayBuffer
{
    private string? _lastSpokenOutput;
    private string? _currentFieldTurnKey;
    private string? _currentFieldTurnAnnouncement;
    private bool _currentFieldTurnAnnouncementReplayed;

    public string? LastSpokenOutput => _lastSpokenOutput;

    public string? CurrentFieldTurnAnnouncement
        => _currentFieldTurnAnnouncementReplayed ? null : _currentFieldTurnAnnouncement;

    public void Record(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var trimmed = text.Trim();
        _lastSpokenOutput = trimmed;

        // Eine neue Ansage gibt die automatische Wiederholung im Feldzug erneut
        // frei. Routinemäßige Aufnahmefragen erreichen diesen Speicher nicht.
        _currentFieldTurnAnnouncement = trimmed;
        _currentFieldTurnAnnouncementReplayed = false;
    }

    public void BeginFieldTurn(string fieldTurnKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldTurnKey);

        if (string.Equals(_currentFieldTurnKey, fieldTurnKey, StringComparison.Ordinal))
        {
            return;
        }

        // Ein neuer Feldzug verwirft das automatische Ziel des vorherigen Zuges.
        // Das letzte Ziel für den manuellen Wiederholungsbefehl bleibt erhalten.
        _currentFieldTurnKey = fieldTurnKey;
        _currentFieldTurnAnnouncement = null;
        _currentFieldTurnAnnouncementReplayed = false;
    }

    public void MarkCurrentFieldTurnAnnouncementReplayed() => _currentFieldTurnAnnouncementReplayed = true;
}
