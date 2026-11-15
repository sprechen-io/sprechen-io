using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Liest Formularübersicht und Status, baut daraus den Orientierungstext und
/// fordert ihn als einzelne Ansage an. Dieser Sprecher führt selbst keine
/// Einmaligkeitsprüfung für den Formulareinstieg aus.
/// </summary>
internal sealed class FormVoiceCommandFormOrientationSpeaker
{
    private readonly IFormVoiceCommandSpeechService _speechService;

    public FormVoiceCommandFormOrientationSpeaker(IFormVoiceCommandSpeechService speechService)
    {
        _speechService = speechService ?? throw new ArgumentNullException(nameof(speechService));
    }

    public async Task SpeakAsync(
        ApplicationDescriptor application,
        FormDescriptor currentForm,
        FormContract contract,
        IFormDialogRuntime interpreter,
        string isoCode,
        VoiceDialogSettings voiceSettings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(application);
        ArgumentNullException.ThrowIfNull(currentForm);
        ArgumentNullException.ThrowIfNull(interpreter);
        ArgumentNullException.ThrowIfNull(voiceSettings);
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);

        var discovery = await interpreter.GetDiscoverySnapshotAsync().ConfigureAwait(false);
        var status = FormVoiceCommandStatusSnapshotFactory.Enrich(
            application,
            currentForm,
            await interpreter.GetStatusSnapshotAsync().ConfigureAwait(false));

        // Den gemeinsam erstellten Orientierungstext als eine Ansage anfordern.
        var prompt = FormOrientationPromptBuilder.BuildOrientationPrompt(
            application,
            currentForm,
            contract,
            discovery,
            status,
            isoCode,
            voiceSettings);

        if (!string.IsNullOrWhiteSpace(prompt))
        {
            await _speechService.SpeakSafeAsync(prompt, isoCode, voiceSettings, cancellationToken).ConfigureAwait(false);
        }
    }
}
