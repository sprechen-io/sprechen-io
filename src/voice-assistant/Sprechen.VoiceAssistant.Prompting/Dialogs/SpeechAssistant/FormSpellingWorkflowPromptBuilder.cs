using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;

/// <summary>
/// Baut lokalisierte Ansagen für das Buchstabieren über mehrere Eingaben.
/// Die Bestätigungen der Aufnahme und Bearbeitung enthalten keine erfassten Zeichen.
/// Das phonetische Vorlesen einzelner Blöcke wird getrennt angefordert. Der Aufrufer
/// berücksichtigt dabei die Schutzklasse des Feldes. Angeforderte Vorleseansagen
/// erhalten die dafür freigegebenen Zeichen als Parameter.
/// </summary>
public static class FormSpellingWorkflowPromptBuilder
{
    private static readonly FormPromptComposition PromptComposition = FormPromptComposition.Default;

    public static string GetSpellingWorkflowPrompt(string isoCode, string characters)
    {
        ArgumentNullException.ThrowIfNull(characters);
        var promptId = characters.Trim().Length == 0
            ? FormDialogPromptIds.SpellingWorkflowEmpty
            : FormDialogPromptIds.SpellingWorkflowCaptured;

        return PromptComposition.ResolveFallbackPrompt(isoCode, promptId);
    }

    public static string GetSpellingNoValuePrompt(string isoCode)
        => PromptComposition.ResolveFallbackPrompt(isoCode, FormDialogPromptIds.SpellingNoValue);

    public static string GetSpellingContinueOrDonePrompt(string isoCode)
        => PromptComposition.ResolveFallbackPrompt(isoCode, FormDialogPromptIds.SpellingContinueOrDone);

    public static string GetSpellingCurrentReadbackPrompt(string isoCode, string characters)
    {
        ArgumentNullException.ThrowIfNull(characters);
        var normalized = characters.Trim();

        if (normalized.Length == 0)
        {
            return GetSpellingNoValuePrompt(isoCode);
        }

        return PromptComposition.ResolveFallbackPrompt(
            isoCode,
            FormDialogPromptIds.SpellingCurrentReadback,
            new Dictionary<string, string?>
            {
                [FormPromptTemplateParameterNames.Characters] = normalized
            });
    }

    public static string GetSpellingReadbackUnavailablePrompt(string isoCode)
        => PromptComposition.ResolveFallbackPrompt(isoCode, FormDialogPromptIds.SpellingReadbackUnavailable);

    public static string GetSpellingWorkflowPrompt(
        FormContract contract,
        string isoCode,
        string characters,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(characters);

        var promptId = characters.Trim().Length == 0
            ? FormDialogPromptIds.SpellingWorkflowEmpty
            : FormDialogPromptIds.SpellingWorkflowCaptured;

        return PromptComposition.ResolvePromptOrFallback(
            contract,
            promptId,
            isoCode,
            politenessLevel,
            GetSpellingWorkflowPrompt(isoCode, characters));
    }

    public static string GetSpellingNoValuePrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);

        return PromptComposition.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.SpellingNoValue,
            isoCode,
            politenessLevel,
            GetSpellingNoValuePrompt(isoCode));
    }

    public static string GetSpellingContinueOrDonePrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel)
        => ResolveAcknowledgementPrompt(
            contract,
            isoCode,
            FormDialogPromptIds.SpellingContinueOrDone,
            politenessLevel);

    // Die Aufforderung nach einem geschützten Block nennt das Abschlusswort,
    // ohne erfasste Zeichen zu wiederholen.
    public static string GetSpellingContinueOrDoneProtectedPrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel)
        => ResolveAcknowledgementPrompt(
            contract,
            isoCode,
            FormDialogPromptIds.SpellingContinueOrDoneProtected,
            politenessLevel);

    // Baut die Korrekturansage für eine wegen zu geringer Konfidenz abgelehnte Eingabe.
    // Das Bestätigungspräfix nennt Feld und gegebenenfalls Kandidatenwert. Die Vorlage
    // bietet nur mögliche Korrekturen an, keine Annahme der abgelehnten Eingabe.
    public static string GetSpellingLowConfidenceRepairPrompt(
        string isoCode,
        string fieldLabel,
        string confirmationValue,
        bool includesValue,
        PolitenessLevel politenessLevel)
        => PromptComposition.ResolveFallbackPrompt(
            isoCode,
            FormDialogPromptIdVariantResolver.ResolvePolitenessVariantPromptId(
                FormDialogPromptIds.SpellingLowConfidenceRepair,
                politenessLevel),
            FormConfirmationPromptBuilder.CreateConfirmationPrefixParameters(isoCode, fieldLabel, confirmationValue, includesValue));

    public static string GetSpellingLowConfidenceRepairPrompt(
        FormContract contract,
        string isoCode,
        string fieldLabel,
        string confirmationValue,
        bool includesValue,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);

        return PromptComposition.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.SpellingLowConfidenceRepair,
            isoCode,
            politenessLevel,
            GetSpellingLowConfidenceRepairPrompt(isoCode, fieldLabel, confirmationValue, includesValue, politenessLevel),
            FormConfirmationPromptBuilder.CreateConfirmationPrefixParameters(isoCode, fieldLabel, confirmationValue, includesValue));
    }

    public static string GetSpellingLastCharacterRemovedPrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel)
        => ResolveAcknowledgementPrompt(
            contract,
            isoCode,
            FormDialogPromptIds.SpellingLastCharacterRemoved,
            politenessLevel);

    public static string GetSpellingLastBlockRemovedPrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel)
        => ResolveAcknowledgementPrompt(
            contract,
            isoCode,
            FormDialogPromptIds.SpellingLastBlockRemoved,
            politenessLevel);

    public static string GetSpellingBufferClearedPrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel)
        => ResolveAcknowledgementPrompt(
            contract,
            isoCode,
            FormDialogPromptIds.SpellingBufferCleared,
            politenessLevel);

    public static string GetSpellingCurrentReadbackPrompt(
        FormContract contract,
        string isoCode,
        string characters,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(characters);

        var normalized = characters.Trim();
        if (normalized.Length == 0)
        {
            return GetSpellingNoValuePrompt(contract, isoCode, politenessLevel);
        }

        return PromptComposition.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.SpellingCurrentReadback,
            isoCode,
            politenessLevel,
            GetSpellingCurrentReadbackPrompt(isoCode, characters),
            new Dictionary<string, string?>
            {
                [FormPromptTemplateParameterNames.Characters] = normalized
            });
    }

    public static string GetSpellingReadbackUnavailablePrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel)
        => ResolveAcknowledgementPrompt(
            contract,
            isoCode,
            FormDialogPromptIds.SpellingReadbackUnavailable,
            politenessLevel);

    private static string ResolveAcknowledgementPrompt(
        FormContract contract,
        string isoCode,
        string promptId,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);

        return PromptComposition.ResolvePromptOrFallback(
            contract,
            promptId,
            isoCode,
            politenessLevel,
            PromptComposition.ResolveFallbackPrompt(isoCode, promptId));
    }
}
