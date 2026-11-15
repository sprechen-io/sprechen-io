namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Signalisiert pausierte Spracheingabe als eigenen Zustand für die Behandlung unterbrochener Eingabeabläufe.
/// </summary>
public sealed class VoiceInputPausedException : Exception
{
    public const string ErrorCode = "voice_input_paused";

    public VoiceInputPausedException()
        : base("Voice input was paused.")
    {
    }

    public string Code => ErrorCode;
}
