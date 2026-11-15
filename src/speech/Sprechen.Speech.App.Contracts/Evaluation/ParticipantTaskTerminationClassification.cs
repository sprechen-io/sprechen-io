namespace Sprechen.Speech.App.Contracts.Evaluation;

/// <summary>
/// Gründe für das Aufgabenende, die der Teilnehmerablauf melden kann.
/// Klassifikationen aus der Beobachterperspektive gehören nicht zu diesem Vertrag.
/// </summary>
public enum ParticipantTaskTerminationClassification
{
    /// <summary>Die Aufgabe endete über den Teilnehmerablauf.</summary>
    Completed,

    /// <summary>Die teilnehmende Person hat den Abbruch bestätigt.</summary>
    ParticipantCancelled,

    /// <summary>Die Sitzung endete ohne Abschluss und ohne bestätigten Abbruch.</summary>
    SessionAbandoned,

    /// <summary>Der Teilnehmerablauf endete wegen eines technischen Fehlers.</summary>
    TechnicalFailure,
}
