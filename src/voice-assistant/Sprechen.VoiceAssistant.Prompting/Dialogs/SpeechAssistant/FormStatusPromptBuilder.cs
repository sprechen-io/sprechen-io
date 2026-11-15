using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Speech.App.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;

/// <summary>
/// Stellt Status- und Übersichtsansagen für Formulardialoge bereit. Spezialisierte
/// Funktionen setzen Listen und Status zusammen. Anrede und Grundlage der
/// Fortschrittsberechnung stammen aus den übergebenen Dialogeinstellungen.
/// </summary>
public static class FormStatusPromptBuilder
{
    private static readonly FormListPromptComposer FormListPromptComposer = new();
    private static readonly FormDiscoveryPromptComposer DiscoveryPromptComposer = new();
    private static readonly FormStatusPromptComposer StatusPromptComposer = new();

    /// <summary>
    /// Liefert die gesprochene Formularübersicht ohne Vertragsüberschreibung der Ansagevorlagen.
    /// </summary>
    public static string BuildFormListPrompt(ApplicationDescriptor application, string isoCode, VoiceDialogSettings voiceSettings)
        => BuildFormListPrompt(application, contract: null, isoCode, voiceSettings);

    /// <summary>
    /// Liefert die gesprochene Formularübersicht mit vorrangigen Vertragsvorlagen, sofern diese
    /// vorhanden sind.
    /// </summary>
    public static string BuildFormListPrompt(
        ApplicationDescriptor application,
        FormContract? contract,
        string isoCode,
        VoiceDialogSettings voiceSettings)
        => FormListPromptComposer.Compose(application, contract, isoCode, voiceSettings);

    /// <summary>
    /// Formuliert die Gruppenübersicht aus der aktuellen Momentaufnahme des Formulars.
    /// </summary>
    public static string BuildFieldGroupListPrompt(
        FormDiscoverySnapshot discovery,
        string isoCode,
        VoiceDialogSettings voiceSettings)
        => BuildFieldGroupListPrompt(discovery, contract: null, isoCode, voiceSettings);

    /// <summary>
    /// Formuliert die Gruppenübersicht aus der aktuellen Momentaufnahme und berücksichtigt vorhandene Vertragsvorlagen.
    /// </summary>
    public static string BuildFieldGroupListPrompt(
        FormDiscoverySnapshot discovery,
        FormContract? contract,
        string isoCode,
        VoiceDialogSettings voiceSettings)
        => DiscoveryPromptComposer.ComposeFieldGroupListPrompt(discovery, contract, isoCode, voiceSettings);

    /// <summary>
    /// Formuliert die Feldübersicht der aktuellen Gruppe aus der übergebenen Momentaufnahme.
    /// </summary>
    public static string BuildFieldListPrompt(
        FormDiscoverySnapshot discovery,
        string isoCode,
        VoiceDialogSettings voiceSettings)
        => BuildFieldListPrompt(discovery, contract: null, isoCode, voiceSettings);

    /// <summary>
    /// Formuliert die Feldübersicht der aktuellen Gruppe mit gegebenenfalls vorhandenen Vertragsvorlagen.
    /// </summary>
    public static string BuildFieldListPrompt(
        FormDiscoverySnapshot discovery,
        FormContract? contract,
        string isoCode,
        VoiceDialogSettings voiceSettings)
        => DiscoveryPromptComposer.ComposeFieldListPrompt(discovery, contract, isoCode, voiceSettings);

    /// <summary>
    /// Liefert die Formularstatusansage mit der in den Dialogeinstellungen ausgewählten
    /// Fortschrittsgrundlage.
    /// </summary>
    public static string BuildFormStatusPrompt(
        ApplicationDescriptor application,
        FormDescriptor currentForm,
        FormStatusSnapshot status,
        string isoCode,
        VoiceDialogSettings voiceSettings)
        => BuildFormStatusPrompt(application, currentForm, status, contract: null, isoCode, voiceSettings);

    /// <summary>
    /// Verbindet Formularposition und Fortschritt zur Statusansage mit den übergebenen
    /// Einstellungen und gegebenenfalls vorhandenen Vertragsvorlagen.
    /// </summary>
    public static string BuildFormStatusPrompt(
        ApplicationDescriptor application,
        FormDescriptor currentForm,
        FormStatusSnapshot status,
        FormContract? contract,
        string isoCode,
        VoiceDialogSettings voiceSettings)
    {
        if (application is null || currentForm is null || status is null || voiceSettings is null || string.IsNullOrWhiteSpace(isoCode))
        {
            return string.Empty;
        }

        var progressBasis = voiceSettings.GetProgressCalculationBasis();
        return StatusPromptComposer.ComposeFormStatusPrompt(
            application,
            currentForm,
            status,
            contract,
            FormStatusPromptResolver.NormalizeIsoCode(isoCode),
            voiceSettings.GetPolitenessLevel(),
            progressBasis);
    }

    /// <summary>
    /// Liefert den allgemeinen Status mit aktueller Position, offenen Pflichtfeldern und Validierungsfehlern.
    /// </summary>
    public static string BuildStatusPrompt(
        ApplicationDescriptor application,
        FormDescriptor currentForm,
        FormStatusSnapshot status,
        string isoCode,
        VoiceDialogSettings voiceSettings)
        => BuildStatusPrompt(application, currentForm, status, contract: null, isoCode, voiceSettings);

    /// <summary>
    /// Formuliert den allgemeinen Status mit den übergebenen Dialogeinstellungen und vorrangigen
    /// Vertragsvorlagen, sofern diese vorhanden sind.
    /// </summary>
    public static string BuildStatusPrompt(
        ApplicationDescriptor application,
        FormDescriptor currentForm,
        FormStatusSnapshot status,
        FormContract? contract,
        string isoCode,
        VoiceDialogSettings voiceSettings)
    {
        if (application is null || currentForm is null || status is null || voiceSettings is null || string.IsNullOrWhiteSpace(isoCode))
        {
            return string.Empty;
        }

        var progressBasis = voiceSettings.GetProgressCalculationBasis();
        return StatusPromptComposer.ComposeStatusPrompt(
            application,
            currentForm,
            status,
            contract,
            FormStatusPromptResolver.NormalizeIsoCode(isoCode),
            voiceSettings.GetPolitenessLevel(),
            progressBasis);
    }
}
