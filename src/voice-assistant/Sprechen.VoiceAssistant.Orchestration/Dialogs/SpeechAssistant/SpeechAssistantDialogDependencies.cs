using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Gemeinsame Dienste für Dialoge, stille Eingaben und Formularoberflächen der Assistentensteuerung.
/// </summary>
internal sealed class SpeechAssistantDialogDependencies
{
    public SpeechAssistantDialogDependencies(
        IFormsCatalogDialog applicationCatalogDialog,
        IFormSelectionDialog formSelectionDialog,
        IVoiceCommandDialog voiceCommandDialog,
        IFormSilentInputTransaction silentInputTransaction,
        ISpeechAssistantFormSurfaceController formSurfaceController,
        ISpeechAssistantFormSurfaceState formSurfaceState)
    {
        FormsCatalogDialog = applicationCatalogDialog ?? throw new ArgumentNullException(nameof(applicationCatalogDialog));
        FormSelectionDialog = formSelectionDialog ?? throw new ArgumentNullException(nameof(formSelectionDialog));
        VoiceCommandDialog = voiceCommandDialog ?? throw new ArgumentNullException(nameof(voiceCommandDialog));
        SilentInputTransaction = silentInputTransaction ?? throw new ArgumentNullException(nameof(silentInputTransaction));
        FormSurfaceController = formSurfaceController ?? throw new ArgumentNullException(nameof(formSurfaceController));
        FormSurfaceState = formSurfaceState ?? throw new ArgumentNullException(nameof(formSurfaceState));
    }

    public IFormsCatalogDialog FormsCatalogDialog { get; }

    public IFormSelectionDialog FormSelectionDialog { get; }

    public IVoiceCommandDialog VoiceCommandDialog { get; }

    public IFormSilentInputTransaction SilentInputTransaction { get; }

    public ISpeechAssistantFormSurfaceController FormSurfaceController { get; }

    public ISpeechAssistantFormSurfaceState FormSurfaceState { get; }
}
