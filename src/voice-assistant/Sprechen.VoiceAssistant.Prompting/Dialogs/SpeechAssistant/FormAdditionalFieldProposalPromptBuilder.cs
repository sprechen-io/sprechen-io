using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Speech.App.Contracts.PromptTemplates;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;

/// <summary>
/// Formuliert das freiwillige Übernahmeangebot für einen zusätzlich erkannten Feldwert und die
/// vier Ausgänge danach. Feldname und Wert werden in die modellierten Vorlagen eingesetzt. Die
/// Entscheidung über die Übernahme bleibt im Dialogablauf.
/// </summary>
public static class FormAdditionalFieldProposalPromptBuilder
{
    public static string GetProposalPrompt(
        FormContract contract,
        string isoCode,
        string fieldLabel,
        string fieldValue,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldLabel);
        ArgumentNullException.ThrowIfNull(fieldValue);

        var parameters = BuildParameters(fieldLabel, fieldValue);

        var fallback = FormPromptResolution.ResolveFallbackPrompt(
            isoCode,
            FormDialogPromptIdVariantResolver.ResolvePolitenessVariantPromptId(
                FormDialogPromptIds.AdditionalFieldProposalOffer,
                politenessLevel),
            parameters);

        return FormPromptResolution.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.AdditionalFieldProposalOffer,
            isoCode,
            politenessLevel,
            fallback,
            parameters);
    }

    /// <summary>
    /// Verbindet den Grund der einen Rückfrage mit dem wiederholten Angebot, damit eine nicht
    /// zugeordnete Antwort nicht als neue, unerklärte Frage klingt.
    /// </summary>
    public static string GetProposalReaskPrompt(
        FormContract contract,
        string isoCode,
        string fieldLabel,
        string fieldValue,
        ConfirmationUndecidedReason reason,
        PolitenessLevel politenessLevel)
        => FormStatusPromptResolver.JoinPromptItems(
        [
            FormConfirmationPromptBuilder.GetConfirmationReaskPrompt(contract, isoCode, reason, politenessLevel),
            GetProposalPrompt(contract, isoCode, fieldLabel, fieldValue, politenessLevel)
        ]);

    /// <summary>
    /// Sagt für ein an einer Bedingung gescheitertes Angebot den Zusammenhang und die modellierte
    /// Meldung der Bedingung, gefolgt von der Zusicherung, dass nichts geschrieben wurde. Die
    /// Reparaturaufforderung der Meldung bleibt weg: nach diesem Zug folgt keine Aufnahme für dieses
    /// Feld, und eine Antwort darauf ginge an das fokussierte Feld.
    /// </summary>
    public static string GetBlockedByValidationPrompt(
        FormContract contract,
        string isoCode,
        string fieldLabel,
        string fieldValue,
        FormConstraintViolation violation,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldLabel);
        ArgumentNullException.ThrowIfNull(fieldValue);
        ArgumentNullException.ThrowIfNull(violation);

        return FormStatusPromptResolver.JoinPromptItems(
        [
            FormConstraintViolationPromptFormatter.FormatViolationPrompt(
                contract,
                isoCode,
                politenessLevel,
                violation,
                fieldLabel,
                fieldValue,
                includesValue: true,
                offersRepairTurn: false),
            GetUnchangedAssurance(contract, isoCode, politenessLevel)
        ]);
    }

    /// <summary>
    /// Die alleinstehende Zusicherung, dass der Formularzustand unberührt blieb. Die übrigen
    /// Ergebnisansagen tragen sie im eigenen Wortlaut. Nur der aus der Bedingung zusammengesetzte
    /// Ausgang braucht sie getrennt.
    /// </summary>
    public static string GetUnchangedAssurance(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);

        var fallback = FormPromptResolution.ResolveFallbackPrompt(
            isoCode,
            FormDialogPromptIds.AdditionalFieldProposalUnchanged);

        return FormPromptResolution.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.AdditionalFieldProposalUnchanged,
            isoCode,
            politenessLevel,
            fallback);
    }

    public static string GetAppliedPrompt(
        FormContract contract,
        string isoCode,
        string fieldLabel,
        string fieldValue,
        PolitenessLevel politenessLevel)
        => GetOutcomePrompt(
            contract,
            isoCode,
            FormDialogPromptIds.AdditionalFieldProposalApplied,
            fieldLabel,
            fieldValue,
            politenessLevel);

    public static string GetUnavailablePrompt(
        FormContract contract,
        string isoCode,
        string fieldLabel,
        string fieldValue,
        PolitenessLevel politenessLevel)
        => GetOutcomePrompt(
            contract,
            isoCode,
            FormDialogPromptIds.AdditionalFieldProposalUnavailable,
            fieldLabel,
            fieldValue,
            politenessLevel);

    public static string GetInvalidPrompt(
        FormContract contract,
        string isoCode,
        string fieldLabel,
        string fieldValue,
        PolitenessLevel politenessLevel)
        => GetOutcomePrompt(
            contract,
            isoCode,
            FormDialogPromptIds.AdditionalFieldProposalInvalid,
            fieldLabel,
            fieldValue,
            politenessLevel);

    public static string GetSkippedPrompt(
        FormContract contract,
        string isoCode,
        string fieldLabel,
        string fieldValue,
        PolitenessLevel politenessLevel)
        => GetOutcomePrompt(
            contract,
            isoCode,
            FormDialogPromptIds.AdditionalFieldProposalSkipped,
            fieldLabel,
            fieldValue,
            politenessLevel);

    // Die vier Ergebnisansagen sind anredefrei modelliert und teilen deshalb denselben
    // Auflösungsweg, und nur die Vorlagenkennung unterscheidet sie.
    private static string GetOutcomePrompt(
        FormContract contract,
        string isoCode,
        string promptId,
        string fieldLabel,
        string fieldValue,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldLabel);
        ArgumentNullException.ThrowIfNull(fieldValue);

        var parameters = BuildParameters(fieldLabel, fieldValue);
        var fallback = FormPromptResolution.ResolveFallbackPrompt(isoCode, promptId, parameters);

        return FormPromptResolution.ResolvePromptOrFallback(
            contract,
            promptId,
            isoCode,
            politenessLevel,
            fallback,
            parameters);
    }

    private static Dictionary<string, string?> BuildParameters(string fieldLabel, string fieldValue)
        => new()
        {
            [FormPromptTemplateParameterNames.FieldLabel] = fieldLabel.Trim(),
            [FormPromptTemplateParameterNames.FieldValue] = fieldValue.Trim()
        };
}
