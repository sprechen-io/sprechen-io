using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;

/// <summary>
/// Verbindet die Rückmeldung zu einer Regelverletzung mit optionalem Feldkontext und
/// Reparaturhinweis. Der Aufrufer entscheidet über die Wertnennung. Fehlt die modellierte Meldung,
/// wird eine allgemeine lokalisierte Vorlage aufgelöst, nicht die technische Regelkennung
/// ausgegeben. Ohne eigenen Reparaturtext folgt der allgemeine Hinweis, außer die Meldung stammt
/// aus der eingebetteten Ersatzvorlage, die ihn bereits enthält.
/// </summary>
public static class FormConstraintViolationPromptFormatter
{
    /// <param name="offersRepairTurn">
    /// Ob unmittelbar eine Aufnahme folgt, die einen neuen Wert für dieses Feld entgegennimmt. Nur
    /// dann darf die Ansage einen dazu auffordern. Endet der Zug stattdessen, ginge die Aufforderung
    /// ins Leere und die nächste Antwort landete am falschen Feld.
    /// </param>
    public static string FormatViolationPrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel,
        FormConstraintViolation violation,
        string? fieldLabel,
        string? understoodValue,
        bool includesValue,
        bool offersRepairTurn = true)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);
        ArgumentNullException.ThrowIfNull(violation);

        var context = ResolveContext(contract, isoCode, politenessLevel, fieldLabel, understoodValue, includesValue);
        var message = ResolveMessage(contract, isoCode, politenessLevel, violation.Message, out var messageIsEmbeddedCatalogFallback);
        var remediation = offersRepairTurn
            ? ResolveRemediation(contract, isoCode, politenessLevel, violation.Remediation, messageIsEmbeddedCatalogFallback)
            : null;

        return JoinSegments(context, message, remediation);
    }

    private static string ResolveMessage(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel,
        string message,
        out bool isEmbeddedCatalogFallback)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            isEmbeddedCatalogFallback = false;
            return message.Trim();
        }

        // Eine Vertragsvorlage ersetzt auch den Reparaturteil der eingebetteten allgemeinen
        // Meldung. Die gemeinsame Auflösung liefert deshalb neben dem Text dessen Herkunft. Nur die
        // eingebettete Ersatzvorlage ist von der zusätzlichen Reparaturansage ausgenommen.
        var resolved = FormPromptResolution.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.ValidationInvalidValue,
            isoCode,
            politenessLevel,
            FormPromptResolution.ResolveFallbackPrompt(isoCode, FormDialogPromptIds.ValidationInvalidValue),
            out var resolvedFromContractOverride);

        isEmbeddedCatalogFallback = !resolvedFromContractOverride;
        return resolved;
    }

    private static string? ResolveRemediation(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel,
        string? remediation,
        bool messageIsEmbeddedCatalogFallback)
    {
        if (!string.IsNullOrWhiteSpace(remediation))
        {
            return remediation;
        }

        // Die eingebettete Vorlage für einen ungültigen Wert enthält den Reparaturhinweis bereits.
        // Ein weiterer allgemeiner Hinweis würde ihn unmittelbar wiederholen.
        if (messageIsEmbeddedCatalogFallback)
        {
            return null;
        }

        return FormPromptResolution.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.ValidationRepairPrompt,
            isoCode,
            politenessLevel,
            FormPromptResolution.ResolveFallbackPrompt(isoCode, FormDialogPromptIds.ValidationRepairPrompt));
    }

    private static string ResolveContext(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel,
        string? fieldLabel,
        string? understoodValue,
        bool includesValue)
    {
        if (string.IsNullOrWhiteSpace(fieldLabel))
        {
            return string.Empty;
        }

        var label = fieldLabel.Trim();
        if (includesValue && !string.IsNullOrWhiteSpace(understoodValue))
        {
            var parameters = new Dictionary<string, string?>
            {
                [FormPromptTemplateParameterNames.FieldLabel] = label,
                [FormPromptTemplateParameterNames.UnderstoodValue] = understoodValue.Trim()
            };

            return FormPromptResolution.ResolvePromptOrFallback(
                contract,
                FormDialogPromptIds.ValidationContext,
                isoCode,
                politenessLevel,
                FormPromptResolution.ResolveFallbackPrompt(isoCode, FormDialogPromptIds.ValidationContext, parameters),
                parameters);
        }

        var labelOnlyParameters = new Dictionary<string, string?>
        {
            [FormPromptTemplateParameterNames.FieldLabel] = label
        };

        return FormPromptResolution.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.ValidationContextWithoutValue,
            isoCode,
            politenessLevel,
            FormPromptResolution.ResolveFallbackPrompt(isoCode, FormDialogPromptIds.ValidationContextWithoutValue, labelOnlyParameters),
            labelOnlyParameters);
    }

    private static string JoinSegments(string context, string message, string? remediation)
        => string.Join(
            " ",
            new[] { context, message, remediation }
                .Where(segment => !string.IsNullOrWhiteSpace(segment))
                .Select(segment => segment!.Trim()));
}
