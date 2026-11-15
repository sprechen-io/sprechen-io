namespace Sprechen.Speech.App.Runtime.Services;

/// <summary>
/// Bindet Audiodaten, Erkennungsoptionen und Abbruchsignal an das Auftragsergebnis, damit die Warteschlange jeden Aufrufer gezielt abschließen kann.
/// </summary>
internal sealed record SpeechRecognitionWorkItem(
    CapturedSpeechAudio Audio,
    string? LanguageCode,
    RecognitionProfile RecognitionProfile,
    SpeechKeyterms? Keyterms,
    CancellationToken RequestToken,
    TaskCompletionSource<SpeechTranscript> Tcs);
