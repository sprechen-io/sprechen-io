namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormNavigation;

/// <summary>
/// Unterscheidet das Ergebnis eines Sprungversuchs, bevor der Laufzeitadapter vorzulesende Werte und lokalisierte Titel ergänzt.
/// </summary>
internal enum FormJumpExecutionKind
{
    Focused,

    /// <summary>
    /// Das Ziel wurde aufgelöst, konnte aber nicht fokussiert werden. Seine Auflösung bleibt
    /// für die reine Rückleseausgabe verfügbar. Diese Kennzeichnung spielt selbst keine Ansage ab.
    /// </summary>
    AudibleOnlyTarget,

    NotFound
}
