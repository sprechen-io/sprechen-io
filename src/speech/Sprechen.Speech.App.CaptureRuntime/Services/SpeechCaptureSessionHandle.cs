namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Hält Audioformat sowie Sitzungs- und Aufnahmeabschluss für einen Nutzer zusammen,
/// damit beide Endsignale einer laufenden Sitzung getrennt beobachtet werden können.
/// </summary>
internal readonly record struct SpeechCaptureSessionHandle(
    SpeechAudioMimeType MimeType,
    Task Completion,
    Task RecordingCompletion);
