namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>Unterscheidet eine gestartete Sitzung von einem Abbruch während des Einstiegs.</summary>
/// <remarks>
/// Nach einem Einstiegsabbruch muss die Oberfläche einen erneuten Start ermöglichen,
/// statt eine bereits laufende Sitzung anzuzeigen.
/// </remarks>
public enum SpeechAssistantStartOutcome
{
    SessionStarted,

    /// <summary>Die teilnehmende Person hat den Einstieg vor dem Sitzungsstart abgebrochen.</summary>
    CancelledAtEntry,
}
