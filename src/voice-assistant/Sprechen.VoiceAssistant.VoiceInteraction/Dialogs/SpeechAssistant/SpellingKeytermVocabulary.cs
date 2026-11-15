using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.VoiceAssistant.CommandParsing.Dialogs.SpeechAssistant;
using Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Erstellt Erkennungshinweise aus dem modellierten Buchstabierwortschatz
/// der Sitzungssprache, lokalen Buchstabierbefehlen und ausgewählten globalen Befehlen.
/// Erwartete Feldwerte werden nicht aufgenommen. Die anschließende Zuordnung
/// und Validierung bleiben unabhängig von diesen Erkennungshinweisen.
/// </summary>
internal sealed class SpellingKeytermVocabulary
{
    // Diese globalen Befehle zusätzlich als Erkennungshinweise übergeben.
    // Die Liste ist nicht vollständig mit allen unterstützten Befehlen des Mappers identisch.
    private static readonly string[] HonouredGlobalCommandIds =
    [
        GlobalCommandIds.InputModeVoice,
        GlobalCommandIds.Submit,
        GlobalCommandIds.AcceptValue,
        GlobalCommandIds.Repeat
    ];

    private readonly SpeechKeytermProjectionPolicy _projectionPolicy = new();

    public IReadOnlyList<string> GetKeyterms(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);

        return _projectionPolicy.Project(
            FormLexiconVocabularyCatalog.GetSpellingRecognitionTerms(isoCode)
                .Concat(FormGlobalCommandUtteranceCatalog.GetUtterances(
                    SpellingWorkflowCommandCatalogResolver.ResolveCommands(),
                    isoCode,
                    politenessLevel))
                .Concat(HonouredGlobalCommandIds.SelectMany(commandId =>
                    FormGlobalCommandUtteranceCatalog.GetUtterances(contract, commandId, isoCode, politenessLevel))),
            KeytermComposition.WithoutServerBase);
    }
}
