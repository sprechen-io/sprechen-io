using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;

/// <summary>
/// Baut die Ansage für einen nicht verfügbaren Rückwärtsschritt an der Formulargrenze.
/// Die Ansagen für Formular- und Feldgruppenwechsel werden getrennt aufgelöst.
/// </summary>
public static class FormBackNavigationPromptBuilder
{
    /// <summary>
    /// Liefert den Satz, der die Nichtverfügbarkeit des Rückwärtsschritts nennt. Ein
    /// Formularvertrag darf ihn überschreiben. Sonst gilt der Text des allgemeinen Ansagekatalogs
    /// in der gewählten Anrede.
    /// </summary>
    public static string GetBackUnavailablePrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);

        return FormPromptResolution.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.BackUnavailable,
            isoCode,
            politenessLevel,
            FormPromptResolution.ResolveFallbackPrompt(isoCode, FormDialogPromptIds.BackUnavailable));
    }
}
