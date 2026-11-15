namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Verbindet bestätigten Sprachbeginn, fertige Äußerung und möglichen Fehler,
/// damit ein Verarbeitungsschritt seinen Fortschritt und Abschluss gemeinsam melden kann.
/// </summary>
internal readonly record struct PcmSpeechCaptureSegmenterUpdate(
    bool SpeechStarted,
    CapturedSpeechAudio? CompletedUtterance,
    Exception? CompletionException);


