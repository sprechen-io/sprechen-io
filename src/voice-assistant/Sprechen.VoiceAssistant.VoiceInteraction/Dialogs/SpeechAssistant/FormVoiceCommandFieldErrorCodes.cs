namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Hält die Fehlerkennungen der Feldverarbeitung zusammen, damit Aufnahme, Erkennung, Eingabemodus und Sprachprojektion unterscheidbar gemeldet werden.
/// </summary>
internal static class FormVoiceCommandFieldErrorCodes
{
    public const string NoSpeechDetected = "no_speech_detected";
    public const string NoInput = "no_input";

    /// <summary>
    /// Kennzeichnet eine unterbrochene Erkennung getrennt von fehlender Eingabe.
    /// </summary>
    public const string CaptureInterrupted = "capture_interrupted";

    /// <summary>
    /// Kennzeichnet einen Erkennungsabschluss ohne Wörter getrennt von fehlender
    /// Eingabe.
    /// </summary>
    public const string RecognizedAsEmpty = "recognized_as_empty";
    public const string LowSttConfidence = "low_stt_confidence";
    public const string UnknownCommand = "unknown_command";
    public const string KeyboardModeActive = "keyboard_mode_active";
    public const string VoiceInputForbidden = "voice_input_forbidden";
    public const string UnrecognizedInput = "unrecognized_input";
    public const string ManualCorrection = "manual_correction";
    public const string SwitchToSpelling = "switch_to_spelling";

    /// <summary>
    /// Kennzeichnet die fehlgeschlagene Aufbereitung eines gespeicherten Werts
    /// für die Sprachausgabe. Der Katalogcode selbst verändert keine Feldwerte.
    /// </summary>
    public const string SpokenValueUnavailable = "spoken_value_unavailable";
}


