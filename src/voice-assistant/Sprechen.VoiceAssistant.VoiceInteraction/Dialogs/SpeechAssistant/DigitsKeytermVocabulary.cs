using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.VoiceAssistant.CommandParsing.Dialogs.SpeechAssistant;
using Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Liefert Erkennungshinweise für die Zifferneingabe aus Zahlwörtern, lokalen
/// Ziffernbefehlen und vier direkt unterstützten globalen Befehlen. Allgemeines
/// Servervokabular wird nicht ergänzt. Andere globale Befehle können mit
/// explizitem Aktivierungswort separat verarbeitet werden.
/// </summary>
internal sealed class DigitsKeytermVocabulary
{
    private static readonly string[] HonouredGlobalCommandIds =
    [
        GlobalCommandIds.Next,
        GlobalCommandIds.Submit,
        GlobalCommandIds.AcceptValue,
        GlobalCommandIds.Repeat
    ];

    private readonly SpeechKeytermProjectionPolicy _projectionPolicy = new();

    /// <summary>
    /// Liefert das begrenzte modellierte Ziffernvokabular mit
    /// <see cref="KeytermComposition.WithoutServerBase"/>.
    /// </summary>
    public IReadOnlyList<string> GetKeyterms(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);

        return _projectionPolicy.Project(
            FormNumberWordCatalog.GetNumberWords(isoCode)
                .Concat(FormGlobalCommandUtteranceCatalog.GetUtterances(
                    DigitsWorkflowCommandCatalogResolver.ResolveCommands(),
                    isoCode,
                    politenessLevel))
                .Concat(HonouredGlobalCommandIds.SelectMany(commandId =>
                    FormGlobalCommandUtteranceCatalog.GetUtterances(contract, commandId, isoCode, politenessLevel))),
            KeytermComposition.WithoutServerBase);
    }
}
