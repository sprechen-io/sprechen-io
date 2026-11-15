namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Beschreibt den Abschluss einer schrittweisen Werteingabe. Ein erfasster Wert
/// trägt das Minimum der gemeldeten Vertrauenswerte seiner verbleibenden Beiträge
/// oder <c>null</c>, wenn keiner gemeldet wurde. Bei nicht verfügbarer Erkennung,
/// Fokus- oder Moduswechsel und einem zurückgegebenen Dialogergebnis wird der
/// bisherige Puffer nicht als erfasster Wert ausgegeben.
/// <see cref="FinalizedByExplicitAccept"/> unterscheidet eine ausdrückliche
/// Übernahme von anderen Abschlusswegen.
/// </summary>
public readonly record struct CharacterAccumulationResult(
    bool HasCapturedTranscript,
    string CapturedTranscript,
    int? AggregatedConfidencePercent,
    bool FinalizedByExplicitAccept = false,
    VoiceCommandDialogResult? DialogResult = null,
    bool ExitedOnFocusOrModeChange = false)
{
    public static CharacterAccumulationResult Captured(string transcript, int? aggregatedConfidencePercent)
        => new(true, transcript, aggregatedConfidencePercent);

    // Nach ausgeschöpften Wiederholungen bei nicht verfügbarer Erkennung wird
    // kein Wert übernommen. Der aufrufende Feldablauf kann die Eingabe neu beginnen.
    public static CharacterAccumulationResult NotCaptured()
        => new(false, string.Empty, null);

    // Eine ausdrückliche Übernahme wird getrennt markiert, damit die folgende
    // Bestätigungsprüfung die bereits erteilte Zustimmung berücksichtigen kann.
    public static CharacterAccumulationResult CapturedByExplicitAccept(string transcript, int? aggregatedConfidencePercent)
        => new(true, transcript, aggregatedConfidencePercent, FinalizedByExplicitAccept: true);

    // Ein Befehl mit Aktivierungswort hat ein Dialogergebnis geliefert.
    // Der aufrufende Feldablauf erhält dieses Ergebnis unverändert.
    public static CharacterAccumulationResult CommandExecuted(VoiceCommandDialogResult dialogResult)
    {
        ArgumentNullException.ThrowIfNull(dialogResult);
        return new CharacterAccumulationResult(false, string.Empty, null, DialogResult: dialogResult);
    }

    // Nach Fokus- oder Moduswechsel beginnt der Feldablauf die Eingabe im neuen
    // Kontext. Der bisher gesammelte Wert wird dabei nicht übernommen.
    public static CharacterAccumulationResult FocusOrModeChanged()
        => new(false, string.Empty, null, ExitedOnFocusOrModeChange: true);
}
