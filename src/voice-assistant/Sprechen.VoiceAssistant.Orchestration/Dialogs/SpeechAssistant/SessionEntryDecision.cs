namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Unterscheidet angenommene Sitzungssprache, Sprachwechsel, fehlende Annahme und Abbruch. Diese
/// Angaben erlauben dem Ablauf unterschiedliche Speicher- und Rückmeldeschritte, obwohl mehrere
/// Ergebnisse in derselben Sprache bleiben.
/// </summary>
internal readonly record struct SessionEntryDecision(string? TargetIsoCode, bool Answered, bool Cancelled = false)
{
    /// <summary>
    /// Kennzeichnet die angenommene Sitzungssprache ohne Sprachwechsel.
    /// </summary>
    internal static SessionEntryDecision KeptTheSessionLanguage => new(null, Answered: true);

    /// <summary>
    /// Kennzeichnet eine Fragenfolge ohne angenommene Sprache. Auch gesprochene Ablehnungen können dazu
    /// führen.
    /// </summary>
    internal static SessionEntryDecision Unanswered => new(null, Answered: false);

    /// <summary>
    /// Kennzeichnet den erkannten Abbruchwunsch getrennt vom Auftragsabbruch. Sprache der Frage und
    /// Erkennungsweg bleiben für die Diagnose erhalten, damit direkte und erneute Erkennung
    /// unterscheidbar sind.
    /// </summary>
    internal static SessionEntryDecision CancelledByTheParticipant(string rungIsoCode, string readShape)
        => new(null, Answered: true, Cancelled: true) { CancelRungIsoCode = rungIsoCode, CancelReadShape = readShape };

    internal static SessionEntryDecision Chose(string targetIsoCode) => new(targetIsoCode, Answered: true);

    /// <summary>
    /// Trägt bei einem Abbruchergebnis die Sprache der Frage, bei der er erkannt wurde.
    /// </summary>
    internal string? CancelRungIsoCode { get; init; }

    /// <summary>
    /// Trägt bei einem Abbruchergebnis den Weg, über den der Abbruch erkannt wurde.
    /// </summary>
    internal string? CancelReadShape { get; init; }
}
