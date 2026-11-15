namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Entscheidet, ob die untere Vertrauensgrenze für einen erfassten Freitextwert
/// abgeschwächt werden darf, damit die nachfolgende Vertrauensbewertung denselben
/// eingegrenzten Freigabeentscheid verwendet.
/// </summary>
/// <remarks>
/// Erforderlich sind ein anderer Eingabemodus als Buchstabieren, die gesetzte
/// Konfiguration zum Vorlesen des Werts und ein als Freitext eingeordnetes Feld.
/// Diese Prüfung führt keine Wiedergabe aus und prüft weder deren tatsächlichen
/// Erfolg noch die Richtigkeit des erkannten Werts.
/// </remarks>
internal static class FreeTextConfidenceRelaxationPolicy
{
    /// <summary>
    /// Prüft die drei Freigabebedingungen für eine abgeschwächte Vertrauensgrenze.
    /// Die gesetzte Vorlesekonfiguration ersetzt dabei keinen Wiedergabenachweis.
    /// </summary>
    public static bool AllowsAcceptWithReadback(
        InputMode expectedInputMode,
        bool codeStyle,
        string? valueTypeKind,
        bool speakValueInConfirmation)
    {
        if (expectedInputMode == InputMode.Spelling)
        {
            return false;
        }

        if (!speakValueInConfirmation)
        {
            return false;
        }

        return FieldInteractionSemantics.IsTextField(codeStyle, valueTypeKind);
    }
}
