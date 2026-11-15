using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.ModelRuntime.Services;
using Sprechen.VoiceAssistant.CommandParsing.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Liefert Erkennungshinweise für Feldbestätigungen: Ja- und Nein-Ausdrücke
/// sowie vier dafür ausgewählte Befehle ohne vorangestelltes Aktivierungswort.
/// Die begrenzte Menge enthält keine Aktivierungswörter und ergänzt
/// kein allgemeines Servervokabular. Sie garantiert keine korrekte Erkennung.
/// </summary>
internal sealed class ConfirmationKeytermVocabulary
{
    // Diese vier Befehle ergänzen das Bestätigungsvokabular ohne Aktivierungswort.
    // Die Liste ist kein vollständiges Verzeichnis aller ohne Aktivierungswort zulässigen Befehle.
    // Weitere Modusausnahmen bestimmt die gemeinsame Befehlsprüfung.
    private static readonly string[] AnchorFreeCommandIds =
    [
        GlobalCommandIds.AcceptValue,
        GlobalCommandIds.EditValue,
        GlobalCommandIds.InputModeSpelling,
        GlobalCommandIds.Submit
    ];

    private readonly SpeechKeytermProjectionPolicy _projectionPolicy = new();

    /// <summary>
    /// Liefert das modellierte Bestätigungsvokabular mit
    /// <see cref="KeytermComposition.WithoutServerBase"/>.
    /// </summary>
    public IReadOnlyList<string> GetKeyterms(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);

        var yesNo = VoiceInterfaceModelCatalog.Lexicon.YesNo;
        return _projectionPolicy.Project(
            yesNo.GetYesTokens(isoCode)
                .Concat(yesNo.GetNoTokens(isoCode))
                .Concat(AnchorFreeCommandIds.SelectMany(commandId =>
                    FormGlobalCommandUtteranceCatalog.GetUtterances(contract, commandId, isoCode, politenessLevel))),
            KeytermComposition.WithoutServerBase);
    }
}
