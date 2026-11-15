using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Speech.App.Contracts.PromptTemplates;

namespace Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;

/// <summary>
/// Baut die lokalisierten Ansagen für schreibgeschützte Anzeigefelder, sowohl beim Gruppeneinstieg
/// als auch beim angeforderten Vorlesen. Die Inhalte stammen aus der bereits nach Datenschutzregeln
/// aufgelösten Vorleseprojektion. Der Builder wählt keine Rohwerte aus und maskiert nicht selbst.
/// </summary>
public static class FormReadOnlyFieldPromptBuilder
{
    /// <summary>
    /// Formuliert die Einstiegsansage eines schreibgeschützten Feldes aus seiner Bezeichnung
    /// und dem freigegebenen Text zum Vorlesen. Die Häufigkeit steuert der Aufrufer.
    /// </summary>
    public static string GetAnnouncementPrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel,
        string fieldLabel,
        string spokenValue)
        => Resolve(contract, FormDialogPromptIds.ReadOnlyFieldAnnouncement, isoCode, politenessLevel, fieldLabel, spokenValue);

    /// <summary>
    /// Nennt nur die Feldbezeichnung, wenn die Vorleseregel eine automatische Ansage
    /// des vorhandenen Werts ausschließt.
    /// </summary>
    public static string GetAnnouncementLabelOnlyPrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel,
        string fieldLabel)
        => Resolve(contract, FormDialogPromptIds.ReadOnlyFieldAnnouncementLabelOnly, isoCode, politenessLevel, fieldLabel, spokenValue: null);

    /// <summary>
    /// Formatiert das angeforderte Vorlesen aus Feldbezeichnung und dem bereits
    /// nach Datenschutzregeln aufbereiteten Text des Werts.
    /// </summary>
    public static string GetReadbackPrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel,
        string fieldLabel,
        string spokenValue)
        => Resolve(contract, FormDialogPromptIds.ReadOnlyFieldReadback, isoCode, politenessLevel, fieldLabel, spokenValue);

    /// <summary>
    /// Liefert die lokalisierte Ansage für ein schreibgeschütztes Feld ohne Inhalt.
    /// </summary>
    public static string GetEmptyValuePrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel,
        string fieldLabel)
        => Resolve(contract, FormDialogPromptIds.ReadOnlyFieldEmptyValue, isoCode, politenessLevel, fieldLabel, spokenValue: null);

    /// <summary>
    /// Nennt das Feld zusammen mit der modellierten Meldung für einen vorhandenen, aber nicht in
    /// Sprechtext überführbaren Wert. So erhält dieser Zustand eine eigene Textantwort, ohne ihn
    /// als leeres Feld zu beschreiben.
    /// </summary>
    public static string GetUnavailablePrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel,
        string fieldLabel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldLabel);
        var unavailable = FormConfirmationPromptBuilder.GetSpokenValueUnavailablePrompt(
            contract,
            isoCode,
            politenessLevel);
        return Resolve(
            contract,
            FormDialogPromptIds.ReadOnlyFieldReadback,
            isoCode,
            politenessLevel,
            fieldLabel,
            unavailable);
    }

    private static string Resolve(
        FormContract contract,
        string promptId,
        string isoCode,
        PolitenessLevel politenessLevel,
        string fieldLabel,
        string? spokenValue)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldLabel);

        var parameters = new Dictionary<string, string?>
        {
            [FormPromptTemplateParameterNames.FieldLabel] = fieldLabel,
            [FormPromptTemplateParameterNames.FieldValue] = spokenValue
        };

        var fallback = FormPromptResolution.ResolveFallbackPrompt(isoCode, promptId, parameters);

        return FormPromptResolution.ResolvePromptOrFallback(
            contract,
            promptId,
            isoCode,
            politenessLevel,
            fallback,
            parameters);
    }
}
