namespace Sprechen.Service.Application;

/// <summary>
/// Transkriptionsergebnis mit Kennzeichen für fehlenden oder leeren Text.
/// </summary>
public sealed record TranscribeSpeechResult(SpeechTranscript Transcript, bool IsEmpty);
