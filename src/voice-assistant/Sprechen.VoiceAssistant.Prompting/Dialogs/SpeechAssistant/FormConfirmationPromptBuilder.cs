using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;

/// <summary>
/// Baut Ansagen für Bestätigung, Abbruch und die anschließende Sperrzeit aus lokalisierten
/// Vorlagen und liefert gemeinsame Präfixe für Feldbezeichnung und bestätigten Wert.
/// </summary>
public static class FormConfirmationPromptBuilder
{
    public static string GetCancelConfirmationPrompt(string isoCode, PolitenessLevel politenessLevel)
        => FormPromptResolution.ResolveFallbackPrompt(
            isoCode,
            FormDialogPromptIdVariantResolver.ResolvePolitenessVariantPromptId(
                FormDialogPromptIds.CancelConfirmation,
                politenessLevel));

    public static string GetReparableConfirmationPrompt(
        string isoCode,
        string fieldLabel,
        string confirmationValue,
        bool includesValue,
        bool offersSpell = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldLabel);
        ArgumentNullException.ThrowIfNull(confirmationValue);

        return FormPromptResolution.ResolveFallbackPrompt(
            isoCode,
            ResolveReparablePromptId(offersSpell),
            CreateConfirmationPrefixParameters(isoCode, fieldLabel, confirmationValue, includesValue));
    }

    public static string GetConfirmationReaskPrompt(string isoCode, ConfirmationUndecidedReason reason)
        => FormPromptResolution.ResolveFallbackPrompt(isoCode, ResolveReaskPromptId(reason));

    public static string GetConfirmationReaskPrompt(
        FormContract contract,
        string isoCode,
        ConfirmationUndecidedReason reason,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);

        return FormPromptResolution.ResolvePromptOrFallback(
            contract,
            ResolveReaskPromptId(reason),
            isoCode,
            politenessLevel,
            GetConfirmationReaskPrompt(isoCode, reason));
    }

    // Eine Rückfrage unterscheidet Stille von nicht zugeordneter Antwort. Die Vorlage verbindet den
    // Grund mit der kurzen Prüffrage und benötigt für diese anredefreie Form keine eigene
    // Anredevariante.
    private static string ResolveReaskPromptId(ConfirmationUndecidedReason reason)
        => reason == ConfirmationUndecidedReason.Silence
            ? FormDialogPromptIds.ConfirmationReaskSilence
            : FormDialogPromptIds.ConfirmationReaskUnrecognized;

    public static string GetShortReparableConfirmationPrompt(
        string isoCode,
        string fieldLabel,
        string confirmationValue,
        bool includesValue,
        bool offersSpell = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldLabel);
        ArgumentNullException.ThrowIfNull(confirmationValue);

        return FormPromptResolution.ResolveFallbackPrompt(
            isoCode,
            ResolveShortReparablePromptId(offersSpell),
            CreateConfirmationPrefixParameters(isoCode, fieldLabel, confirmationValue, includesValue));
    }

    // Der Aufrufer bestimmt mit offersSpell, ob die Bestätigung zusätzlich Buchstabieren anbietet.
    // Text- und Codefelder können dieses Angebot nutzen. Andere Eingabetypen verwenden
    // dieselbe Bestätigungsform ohne dieses Angebot. Die Methode bestimmt nur den Wortlaut.
    private static string ResolveReparablePromptId(bool offersSpell)
        => offersSpell ? FormDialogPromptIds.ReparableConfirmation : FormDialogPromptIds.ReparableConfirmationNoSpell;

    private static string ResolveShortReparablePromptId(bool offersSpell)
        => offersSpell ? FormDialogPromptIds.ReparableShortConfirmation : FormDialogPromptIds.ReparableShortConfirmationNoSpell;

    public static string GetReparableSpellBackConfirmationPrompt(
        string isoCode,
        string fieldLabel,
        string confirmationValue,
        string phoneticSpellOut,
        string alternativeValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldLabel);
        ArgumentException.ThrowIfNullOrWhiteSpace(confirmationValue);
        ArgumentException.ThrowIfNullOrWhiteSpace(phoneticSpellOut);
        ArgumentException.ThrowIfNullOrWhiteSpace(alternativeValue);

        return FormPromptResolution.ResolveFallbackPrompt(
            isoCode,
            FormDialogPromptIds.ReparableSpellBackConfirmation,
            CreateSpellBackParameters(isoCode, fieldLabel, confirmationValue, phoneticSpellOut, alternativeValue));
    }

    // Ein vorhandener Wert, der nicht als Sprechtext verfügbar ist, braucht eine eigene Ansage. Die
    // fehlende sprachliche Darstellung soll nicht wie ein leeres Feld erscheinen.
    public static string GetSpokenValueUnavailablePrompt(string isoCode)
        => FormPromptResolution.ResolveFallbackPrompt(isoCode, FormDialogPromptIds.SpokenValueUnavailable);

    public static string GetSpokenValueUnavailablePrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);

        return FormPromptResolution.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.SpokenValueUnavailable,
            isoCode,
            politenessLevel,
            GetSpokenValueUnavailablePrompt(isoCode));
    }

    /// <summary>
    /// Baut die Übernahmebestätigung mit Feldbezeichnung und Wert vor dem Übernahmewort.
    /// Sie dient insbesondere dem Abschluss einer Mehrfachauswahl ohne vorherige Ansage
    /// des Werts in einer Bestätigungsfrage. So kann die Person den übernommenen Wert hören.
    /// Das Präfix stammt aus demselben Katalog wie die Bestätigung mit Korrekturmöglichkeit.
    /// Eine bereits verbundene Eintragsliste wird als Wert übergeben.
    /// </summary>
    public static string GetCommitAcceptedNamingValuePrompt(
        string isoCode,
        string fieldLabel,
        string confirmationValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldLabel);
        ArgumentException.ThrowIfNullOrWhiteSpace(confirmationValue);

        return FormPromptResolution.ResolveFallbackPrompt(
            isoCode,
            FormDialogPromptIds.CommitAcceptedNamingValue,
            CreateConfirmationPrefixParameters(isoCode, fieldLabel, confirmationValue, includesValue: true));
    }

    public static string GetCancelRefractoryPrompt(string isoCode)
        => FormPromptResolution.ResolveFallbackPrompt(isoCode, FormDialogPromptIds.CancelRefractory);

    public static string GetCancelConfirmationPrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);

        return FormPromptResolution.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.CancelConfirmation,
            isoCode,
            politenessLevel,
            GetCancelConfirmationPrompt(isoCode, politenessLevel));
    }

    public static string GetReparableConfirmationPrompt(
        FormContract contract,
        string isoCode,
        string fieldLabel,
        string confirmationValue,
        bool includesValue,
        PolitenessLevel politenessLevel,
        bool offersSpell = true)
    {
        ArgumentNullException.ThrowIfNull(contract);

        return FormPromptResolution.ResolvePromptOrFallback(
            contract,
            ResolveReparablePromptId(offersSpell),
            isoCode,
            politenessLevel,
            GetReparableConfirmationPrompt(isoCode, fieldLabel, confirmationValue, includesValue, offersSpell),
            CreateConfirmationPrefixParameters(isoCode, fieldLabel, confirmationValue, includesValue));
    }

    public static string GetShortReparableConfirmationPrompt(
        FormContract contract,
        string isoCode,
        string fieldLabel,
        string confirmationValue,
        bool includesValue,
        PolitenessLevel politenessLevel,
        bool offersSpell = true)
    {
        ArgumentNullException.ThrowIfNull(contract);

        return FormPromptResolution.ResolvePromptOrFallback(
            contract,
            ResolveShortReparablePromptId(offersSpell),
            isoCode,
            politenessLevel,
            GetShortReparableConfirmationPrompt(isoCode, fieldLabel, confirmationValue, includesValue, offersSpell),
            CreateConfirmationPrefixParameters(isoCode, fieldLabel, confirmationValue, includesValue));
    }

    public static string GetCancelRefractoryPrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);

        return FormPromptResolution.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.CancelRefractory,
            isoCode,
            politenessLevel,
            GetCancelRefractoryPrompt(isoCode));
    }

    public static string GetReparableSpellBackConfirmationPrompt(
        FormContract contract,
        string isoCode,
        string fieldLabel,
        string confirmationValue,
        string phoneticSpellOut,
        string alternativeValue,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);

        return FormPromptResolution.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.ReparableSpellBackConfirmation,
            isoCode,
            politenessLevel,
            GetReparableSpellBackConfirmationPrompt(isoCode, fieldLabel, confirmationValue, phoneticSpellOut, alternativeValue),
            CreateSpellBackParameters(isoCode, fieldLabel, confirmationValue, phoneticSpellOut, alternativeValue));
    }

    private static Dictionary<string, string?> CreateSpellBackParameters(
        string isoCode,
        string fieldLabel,
        string confirmationValue,
        string phoneticSpellOut,
        string alternativeValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldLabel);
        ArgumentException.ThrowIfNullOrWhiteSpace(confirmationValue);
        ArgumentException.ThrowIfNullOrWhiteSpace(phoneticSpellOut);
        ArgumentException.ThrowIfNullOrWhiteSpace(alternativeValue);

        return new Dictionary<string, string?>
        {
            [FormPromptTemplateParameterNames.ConfirmationPrefix] =
                ResolveConfirmationPrefix(isoCode, fieldLabel, confirmationValue, includesValue: true),
            [FormPromptTemplateParameterNames.PhoneticSpellOut] = phoneticSpellOut.Trim(),
            [FormPromptTemplateParameterNames.AlternativeValue] = alternativeValue.Trim()
        };
    }

    internal static Dictionary<string, string?> CreateConfirmationPrefixParameters(
        string isoCode,
        string fieldLabel,
        string confirmationValue,
        bool includesValue)
        => new()
        {
            [FormPromptTemplateParameterNames.ConfirmationPrefix] =
                ResolveConfirmationPrefix(isoCode, fieldLabel, confirmationValue, includesValue)
        };

    // Der Wortlaut des gesprochenen Bestätigungspräfixes stammt für jede Sprache aus dem
    // Katalog. Feldbezeichnung und gegebenenfalls Wert werden in die passende Vorlage eingesetzt.
    // Die deutsche Vorlage bindet die Feldbezeichnung in eine Wortgruppe ein, um isolierte Einwortsegmente zu vermeiden.
    private static string ResolveConfirmationPrefix(
        string isoCode,
        string fieldLabel,
        string confirmationValue,
        bool includesValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldLabel);
        ArgumentNullException.ThrowIfNull(confirmationValue);

        var label = fieldLabel.Trim();
        var value = confirmationValue.Trim();

        return includesValue
            ? FormPromptResolution.ResolveFallbackPrompt(
                isoCode,
                FormDialogPromptIds.ConfirmationPrefixWithValue,
                new Dictionary<string, string?>
                {
                    [FormPromptTemplateParameterNames.FieldLabel] = label,
                    [FormPromptTemplateParameterNames.FieldValue] = value
                })
            : FormPromptResolution.ResolveFallbackPrompt(
                isoCode,
                FormDialogPromptIds.ConfirmationPrefixLabelOnly,
                new Dictionary<string, string?>
                {
                    [FormPromptTemplateParameterNames.FieldLabel] = label
                });
    }
}
