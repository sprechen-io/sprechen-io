using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.App.Contracts.PromptTemplates;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;

/// <summary>
/// Baut Fragen und begründete Rückfragen für einzelne Datumsteile sowie den Hinweis auf diesen
/// Eingabeweg. Die Fragen nennen Tag, Monat oder Jahr und ergänzen den verfügbaren Abbruchhinweis,
/// damit die Person auch innerhalb der schrittweisen Eingabe eine Ausstiegsmöglichkeit erfährt. Die
/// Häufigkeit des Einführungshinweises steuert der Aufrufer.
/// </summary>
public static class FormDateComponentPromptBuilder
{
    private static readonly FormPromptComposition PromptComposition = FormPromptComposition.Default;

    /// <summary>
    /// Fragt nach dem benannten Datumsteil und ergänzt den aufgelösten Abbruchhinweis, sofern er
    /// nicht leer ist.
    /// </summary>
    public static string GetSlotPrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel,
        DateComponentSlot slot)
    {
        ArgumentNullException.ThrowIfNull(contract);
        return WithExitCue(contract, isoCode, politenessLevel, Resolve(contract, isoCode, politenessLevel, SlotPromptId(slot)));
    }

    /// <summary>
    /// Verbindet den Grund der Rückfrage mit der Frage nach dem betroffenen Datumsteil und ihrem
    /// Abbruchhinweis. Wertebereich, vierstellige Jahresangabe und unmögliches Datum erhalten
    /// unterschiedliche Vorlagen.
    /// </summary>
    public static string GetReaskPrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel,
        DateComponentSlot slot,
        DateComponentReaskReason reason)
    {
        ArgumentNullException.ThrowIfNull(contract);

        var reasonText = Resolve(contract, isoCode, politenessLevel, ReasonPromptId(slot, reason));
        var slotPrompt = GetSlotPrompt(contract, isoCode, politenessLevel, slot);
        return $"{reasonText} {slotPrompt}";
    }

    /// <summary>
    /// Liefert den lokalisierten Hinweis auf die schrittweise Datumseingabe samt benanntem
    /// Wechselwort. Der Aufrufer kann ihn mit der Einführung am ersten Datumsfeld verbinden. Hier
    /// wird kein Sitzungsmerker geführt.
    /// </summary>
    public static string GetDateComponentEntryAvailablePrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);
        return Resolve(contract, isoCode, politenessLevel, FormDialogPromptIds.DateComponentEntryAvailable);
    }

    private static string WithExitCue(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel,
        string prompt)
    {
        var exitCue = Resolve(contract, isoCode, politenessLevel, FormDialogPromptIds.DateComponentExitCue);
        return string.IsNullOrWhiteSpace(exitCue) ? prompt : $"{prompt} {exitCue}";
    }

    private static string SlotPromptId(DateComponentSlot slot)
        => slot switch
        {
            DateComponentSlot.Day => FormDialogPromptIds.DateComponentDayPrompt,
            DateComponentSlot.Month => FormDialogPromptIds.DateComponentMonthPrompt,
            DateComponentSlot.Year => FormDialogPromptIds.DateComponentYearPrompt,
            _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, "Unmapped date component slot.")
        };

    private static string ReasonPromptId(DateComponentSlot slot, DateComponentReaskReason reason)
        => reason switch
        {
            DateComponentReaskReason.NotRecognized => slot switch
            {
                DateComponentSlot.Day => FormDialogPromptIds.DateComponentDayNotRecognizedReask,
                DateComponentSlot.Month => FormDialogPromptIds.DateComponentMonthNotRecognizedReask,
                DateComponentSlot.Year => FormDialogPromptIds.DateComponentYearNotRecognizedReask,
                _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, "Unmapped date component slot.")
            },
            DateComponentReaskReason.OutOfRange => slot switch
            {
                DateComponentSlot.Day => FormDialogPromptIds.DateComponentDayReask,
                DateComponentSlot.Month => FormDialogPromptIds.DateComponentMonthReask,
                // Der Datumsteilparser liefert für das Jahr keinen Bereichsfehler. Der reguläre Parser
                // unterscheidet unvollständige, unsichere und nicht erkannte Jahresantworten.
                DateComponentSlot.Year => FormDialogPromptIds.DateComponentYearFourDigitsReask,
                _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, "Unmapped date component slot.")
            },
            DateComponentReaskReason.YearNotFourDigits => FormDialogPromptIds.DateComponentYearFourDigitsReask,
            DateComponentReaskReason.YearUncertain => FormDialogPromptIds.DateComponentYearUncertainReask,
            DateComponentReaskReason.ImpossibleDate => FormDialogPromptIds.DateComponentImpossibleDateReask,
            _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, "Unmapped date component reask reason.")
        };

    private static string Resolve(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel,
        string promptId)
        => PromptComposition.ResolvePromptOrFallback(
            contract,
            promptId,
            isoCode,
            politenessLevel,
            PromptComposition.ResolveFallbackPrompt(isoCode, promptId));
}
