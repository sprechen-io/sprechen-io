using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.Localization;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Leitet phasenbezogene Erkennungshinweise aus demselben Katalog wie der Intent-Parser ab.
/// Auswahl und Bestätigung erhalten damit Vokabular passend zu ihren unterschiedlichen
/// Antwortformen.
/// </summary>
/// <remarks>
/// Die Auswahl bietet modellierte Intents, Zahlwörter und Kandidatennamen an. Die Bestätigung
/// beginnt mit sechs Intent-Rollen und ergänzt in späteren Freigabestufen Namen und Zahlen. Die
/// Projektion begrenzt diese Mengen. Sie garantiert weder vollständige Übermittlung noch
/// Erkennung. Beide verwenden <see cref="KeytermComposition.WithoutServerBase"/>, damit
/// allgemeines Servervokabular die phasenbezogene Menge nicht erweitert.
/// </remarks>
internal sealed class FormsCatalogSelectionTurnKeyterms : ISelectionTurnKeyterms
{
    // Diese sechs Intent-Rollen werden in jeder Bestätigungsstufe behandelt. Weitere Zielangaben
    // gehören erst zu späteren Freigabestufen und werden deshalb getrennt ergänzt.
    private static readonly string[] ConfirmingIntentIds =
    [
        "cancel", "confirmNo", "help", "repeat", "list", "confirmYes"
    ];

    private readonly FormsCatalogIntentUtteranceResolver _utterances = new();
    private readonly SpeechKeytermProjectionPolicy _projectionPolicy = new();
    private readonly FormsCatalog _catalog;
    private readonly string _isoCode;
    private readonly PolitenessLevel _politenessLevel;
    private readonly IReadOnlyList<string> _candidateNames;

    public FormsCatalogSelectionTurnKeyterms(
        FormsCatalog catalog,
        string isoCode,
        PolitenessLevel politenessLevel,
        IReadOnlyList<string> candidateNames)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _isoCode = isoCode ?? throw new ArgumentNullException(nameof(isoCode));
        _politenessLevel = politenessLevel;
        _candidateNames = candidateNames ?? throw new ArgumentNullException(nameof(candidateNames));
    }

    public SpeechKeyterms? ForListening()
        => Project(AllIntentUtterances()
            .Concat(NumberWords())
            .Concat(_candidateNames));

    public SpeechKeyterms? ForConfirming(SelectionConfirmationCaptureStage stage)
    {
        var terms = ConfirmingIntentIds.SelectMany(UtterancesOf);

        // Spätere Freigabestufen akzeptieren bei ausreichender Konfidenz einen Öffnungsbefehl
        // mit exakt passendem Kandidatennamen oder dessen stabile Nummer. Die erste Bestätigung
        // akzeptiert diese Formen nicht. Die Hinweise werden deshalb erst danach ergänzt.
        if (stage != SelectionConfirmationCaptureStage.Initial)
        {
            terms = terms.Concat(_candidateNames).Concat(NumberWords());
        }

        return Project(terms);
    }

    private SpeechKeyterms? Project(IEnumerable<string> terms)
    {
        var projected = _projectionPolicy.Project(terms, KeytermComposition.WithoutServerBase);
        return new SpeechKeyterms(projected, KeytermComposition.WithoutServerBase);
    }

    private IEnumerable<string> AllIntentUtterances()
    {
        var intents = _catalog.SelectionDialog?.Intents;
        if (intents is null)
        {
            return [];
        }

        return intents
            .Where(intent => !string.IsNullOrWhiteSpace(intent.IntentId))
            .SelectMany(intent => _utterances.GetUtterances(
                _catalog, intent.IntentId!, intent.MatchMode!, _isoCode, _politenessLevel));
    }

    private IEnumerable<string> UtterancesOf(string intentId)
    {
        var intents = _catalog.SelectionDialog?.Intents;
        if (intents is null)
        {
            return [];
        }

        return intents
            .Where(intent => string.Equals(intent.IntentId, intentId, StringComparison.Ordinal))
            .SelectMany(intent => _utterances.GetUtterances(
                _catalog, intentId, intent.MatchMode!, _isoCode, _politenessLevel));
    }

    private IEnumerable<string> NumberWords()
    {
        var numberWords = _catalog.SelectionDialog?.NumberWords;
        if (numberWords is null)
        {
            return [];
        }

        var promptContext = CatalogPromptContext.CreateForCatalog(_catalog);
        return numberWords.SelectMany(numberWord => _utterances.ResolveLocalizedTexts(
            numberWord.Utterances, promptContext, _isoCode, _politenessLevel));
    }
}
