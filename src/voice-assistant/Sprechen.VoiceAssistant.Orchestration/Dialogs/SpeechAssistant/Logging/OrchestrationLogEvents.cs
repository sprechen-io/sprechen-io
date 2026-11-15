using Microsoft.Extensions.Logging;

namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant.Logging;

/// <summary>
/// Bündelt die Ereigniskennungen der Orchestrierung im 3000er-Bereich, damit Einstiegsentscheidungen
/// und ihre technischen Ausfälle getrennt gefiltert werden können.
/// </summary>
internal static class OrchestrationLogEvents
{
    /// <summary>
    /// Kennzeichnet eine nicht verfügbare Einstiegsentscheidung oder ihre fehlgeschlagene Speicherung
    /// beziehungsweise Rückmeldung. Die einzelnen Aufrufer protokollieren den betroffenen Schritt.
    /// </summary>
    public static readonly EventId SessionEntryLanguageDecisionUnavailable =
        new(3001, nameof(SessionEntryLanguageDecisionUnavailable));

    /// <summary>
    /// Kennzeichnet die vom Einstellungsablauf zurückgegebene Sprache nach einer angenommenen Auswahl
    /// am Einstieg.
    /// </summary>
    public static readonly EventId SessionEntryLanguageChanged =
        new(3002, nameof(SessionEntryLanguageChanged));

    /// <summary>
    /// Kennzeichnet den angenommenen Abbruch am Einstieg als reguläres Ergebnis und hält ihn damit von
    /// technischen Ausfällen der Sprachentscheidung unterscheidbar.
    /// </summary>
    public static readonly EventId SessionEntryCancelled =
        new(3003, nameof(SessionEntryCancelled));

    /// <summary>
    /// Kennzeichnet eine fehlgeschlagene Vorbereitung der Aufnahme. Der Frageablauf
    /// versucht anschließend weiterhin, eine Aufnahme anzufordern.
    /// </summary>
    public static readonly EventId SessionEntryCaptureWarmUpFailed =
        new(3004, nameof(SessionEntryCaptureWarmUpFailed));
}
