using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.VoiceAssistant.CommandParsing.Dialogs.SpeechAssistant;
using Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Baut den Erkennungswortschatz einer Mengeneingabe aus Optionsbezeichnungen,
/// Verbindungswörtern und den zugelassenen Abschluss- und Wiederholungsbefehlen auf,
/// damit die Erfassung Hinweise aus dem aktuellen Eingabekontext erhält.
/// </summary>
internal sealed class SetKeytermVocabulary
{
    // Dieselben Befehlskennungen verwenden, die der globale Zuordner des
    // Mengenablaufs unterstützt. Der Weiter-Befehl gehört nicht dazu.
    private static readonly string[] HonouredGlobalCommandIds =
    [
        GlobalCommandIds.AcceptValue,
        GlobalCommandIds.Submit,
        GlobalCommandIds.Repeat
    ];

    private readonly SpeechKeytermProjectionPolicy _projectionPolicy = new();

    public IReadOnlyList<string> GetKeyterms(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel,
        IReadOnlyList<string> optionLabels)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);
        ArgumentNullException.ThrowIfNull(optionLabels);

        return _projectionPolicy.Project(
            optionLabels
                .Concat(FormLexiconVocabularyCatalog.GetChoiceConnectives(isoCode))
                .Concat(HonouredGlobalCommandIds.SelectMany(commandId =>
                    FormGlobalCommandUtteranceCatalog.GetUtterances(contract, commandId, isoCode, politenessLevel))),
            KeytermComposition.WithoutServerBase);
    }
}
