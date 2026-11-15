using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.Behavior.Tests.TestDoubles;

/// <summary>
/// Stellt die für diese Verhaltenstests benötigten deutschen und englischen Abbruchwörter bereit.
/// Das Testdouble ersetzt nicht den vollständigen produktiven Katalogresolver.
/// </summary>
internal sealed class CatalogWordSessionEntryCancelVocabulary : ISessionEntryCancelVocabulary
{
    public IReadOnlyList<string> GetCancelUtterances(string isoCode, PolitenessLevel politenessLevel)
        => isoCode switch
        {
            "de" => ["abbrechen", "beenden"],
            "en" => ["cancel", "quit"],
            _ => [],
        };
}
