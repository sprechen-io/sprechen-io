using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.WakeRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Enthält eine erfolgreiche Aktivierungserkennung des akustischen Detektors.
/// </summary>
public sealed record WakeRuntimeAcousticDetectedOutcome : WakeRuntimeDetectedOutcome
{
    public WakeRuntimeAcousticDetectedOutcome(
        AcousticWakeWordEvidence evidence,
        string correlationId)
        : base(correlationId)
    {
        Evidence = evidence ?? throw new ArgumentNullException(nameof(evidence));
    }

    public AcousticWakeWordEvidence Evidence { get; }
}
