namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Legt die Anzahl der Kandidaten fest, bei der die Katalogansage ihre Form wechselt.
/// Ein einzelner Kandidat wird direkt angeboten. Zwei bis fünf Kandidaten werden
/// mit Namen und stabilen Nummern genannt. Bei mehr Kandidaten nennt die Ansage
/// nur ihre Anzahl und Möglichkeiten zum Eingrenzen, Auflisten oder Abbrechen.
/// </summary>
internal static class SelectionCandidateAnnouncementThresholds
{
    /// <summary>
    /// Ein einzelner Kandidat wird direkt angeboten.
    /// </summary>
    internal const int SingleCandidateCount = 1;

    /// <summary>Eine Mehrdeutigkeitsansage braucht mindestens zwei Kandidaten.</summary>
    internal const int MinAmbiguityCandidateCount = 2;

    /// <summary>Bis zu fünf Kandidaten werden alle genannt.</summary>
    internal const int MaxDirectlyAnnouncedCandidates = 5;
}
