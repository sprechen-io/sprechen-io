namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Beschreibt, ob die schrittweise Eingabe abgeschlossen, fortgesetzt oder wegen
/// eines Befehls verlassen wird. Dialogergebnis sowie Fokus- oder Moduswechsel
/// werden gesondert weitergegeben. Die nächste Aufnahme und die Behandlung
/// fehlender Sprache bleiben beim gemeinsamen Ablauf.
/// <see cref="FinalizedByExplicitAccept"/> kennzeichnet eine ausdrückliche
/// Übernahme, damit die nachfolgende Bestätigungsprüfung sie berücksichtigen kann.
/// </summary>
internal readonly record struct CharacterAccumulationTurnOutcome(
    bool Finalized,
    bool FinalizedByExplicitAccept = false,
    VoiceCommandDialogResult? DialogResult = null,
    bool ExitedOnFocusOrModeChange = false)
{
    public static CharacterAccumulationTurnOutcome Finalize() => new(true);

    public static CharacterAccumulationTurnOutcome FinalizeByExplicitAccept() => new(true, FinalizedByExplicitAccept: true);

    public static CharacterAccumulationTurnOutcome AwaitNextTurn() => new(false);

    // Ein Befehl hat ein Dialogergebnis geliefert. Die Eingabe endet.
    // Der Feldablauf reicht das Ergebnis unverändert weiter.
    public static CharacterAccumulationTurnOutcome ReturnDialogResult(VoiceCommandDialogResult dialogResult)
    {
        ArgumentNullException.ThrowIfNull(dialogResult);
        return new CharacterAccumulationTurnOutcome(false, DialogResult: dialogResult);
    }

    // Nach einem Fokus- oder Moduswechsel endet die Eingabe ohne Wert.
    // Der Feldablauf beginnt die Eingabe im neuen Kontext.
    public static CharacterAccumulationTurnOutcome ExitOnFocusOrModeChange() => new(false, ExitedOnFocusOrModeChange: true);
}
