namespace Sprechen.VoiceAssistant.WakeRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Enthält einen Spracherkennungsfehler während der Aktivierungserkennung
/// mit der Angabe, ob ein erneuter Versuch möglich ist.
/// </summary>
public sealed record WakeRuntimeRecognitionFailedOutcome : WakeRuntimeOutcome
{
    public WakeRuntimeRecognitionFailedOutcome(string code, bool recoverable = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        Code = code.Trim();
        Recoverable = recoverable;
    }

    public string Code { get; }

    public bool Recoverable { get; }
}
