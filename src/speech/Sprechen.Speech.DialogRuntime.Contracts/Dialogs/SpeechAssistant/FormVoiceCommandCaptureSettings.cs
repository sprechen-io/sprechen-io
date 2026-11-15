using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Fasst Dialogvorgaben und Rückmeldungsarten zusammen, um die Sprachaufnahme auf den aktuellen Formularablauf abzustimmen.
/// </summary>
public sealed record FormVoiceCommandCaptureSettings
{
    public FormVoiceCommandCaptureSettings(
        VoiceDialogSettings voiceSettings,
        int pauseAfterPromptMs,
        SpeechCapturePromptPresentation promptPresentation,
        SpeechInputAcceptanceFeedback inputAcceptanceFeedback)
    {
        VoiceSettings = voiceSettings ?? throw new ArgumentNullException(nameof(voiceSettings));

        if (pauseAfterPromptMs < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pauseAfterPromptMs), pauseAfterPromptMs, "pauseAfterPromptMs must be >= 0.");
        }

        if (!Enum.IsDefined(promptPresentation))
        {
            throw new ArgumentOutOfRangeException(nameof(promptPresentation), promptPresentation, "Unknown capture prompt presentation.");
        }

        if (!Enum.IsDefined(inputAcceptanceFeedback))
        {
            throw new ArgumentOutOfRangeException(nameof(inputAcceptanceFeedback), inputAcceptanceFeedback, "Unknown input acceptance feedback.");
        }

        PauseAfterPromptMs = pauseAfterPromptMs;
        PromptPresentation = promptPresentation;
        InputAcceptanceFeedback = inputAcceptanceFeedback;
    }

    public VoiceDialogSettings VoiceSettings { get; }

    public int PauseAfterPromptMs { get; }

    public SpeechCapturePromptPresentation PromptPresentation { get; }

    public SpeechInputAcceptanceFeedback InputAcceptanceFeedback { get; }

    public FormVoiceCommandCaptureSettings WithInputAcceptanceFeedback(
        SpeechInputAcceptanceFeedback inputAcceptanceFeedback)
        => new(VoiceSettings, PauseAfterPromptMs, PromptPresentation, inputAcceptanceFeedback);

    public FormVoiceCommandCaptureSettings WithPromptPresentation(
        SpeechCapturePromptPresentation promptPresentation)
        => new(VoiceSettings, PauseAfterPromptMs, promptPresentation, InputAcceptanceFeedback);

    public static FormVoiceCommandCaptureSettings From(
        VoiceDialogSettings voiceSettings,
        FormContract contract)
    {
        ArgumentNullException.ThrowIfNull(contract);

        return new FormVoiceCommandCaptureSettings(
            voiceSettings,
            contract.Projections?.Voice?.FormVoiceProfile?.PauseAfterPromptMs ?? 0,
            SpeechCapturePromptPresentation.Spoken,
            SpeechInputAcceptanceFeedback.Standard);
    }
}
