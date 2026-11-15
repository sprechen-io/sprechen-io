using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Liefert die modellierten Abbruchäußerungen für den Sitzungseinstieg. Der Port löst sie erst mit der
/// beim Start bekannten Sprache und Anrede auf, damit der Einstieg keinen eigenen Wortbestand halten
/// muss.
/// </summary>
public interface ISessionEntryCancelVocabulary
{
    /// <summary>
    /// Liefert unterschiedliche Abbruchäußerungen für Sprache und Anrede in Katalogreihenfolge. Die
    /// Katalogauflösung bestimmt auch den Umgang mit unbekannten Sprachen. Der Einstieg ergänzt keine
    /// eigene Rückfallregel.
    /// </summary>
    IReadOnlyList<string> GetCancelUtterances(string isoCode, PolitenessLevel politenessLevel);
}
