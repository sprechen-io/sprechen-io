using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.VoiceRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Übergibt Ansage, Aufnahmeeinstellungen und Versuchskontext gemeinsam an
/// den Aufnahmeablauf. Der Modus nach der Ansage gilt auch bei ausschließlichem
/// Hinweiston. Bestätigungen können den Kurzantwortmodus wählen. Bei
/// unterbrechbarer Ansage verwendet der Ablauf den Unterbrechungsmodus.
/// </summary>
internal sealed record FormVoiceCommandCaptureRequest(
    string Prompt,
    string IsoCode,
    FormVoiceCommandCaptureSettings CaptureSettings,
    Action OnStartListening,
    SpeechCaptureTelemetryContext? TelemetryContext,
    FormVoiceCommandAttempt? Attempt = null,
    SpeechCaptureMode PostPromptCaptureMode = SpeechCaptureMode.Listening);
