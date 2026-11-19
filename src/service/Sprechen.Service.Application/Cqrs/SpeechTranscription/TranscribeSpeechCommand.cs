using Monstertacle.SharpQrs;

namespace Sprechen.Service.Application.Cqrs.SpeechTranscription;

/// <summary>
/// Auftrag zur Transkription eines Audiostroms mit einem abschließenden Ergebnis.
/// </summary>
public sealed record TranscribeSpeechCommand(
    IAsyncEnumerable<byte[]> Chunks,
    AudioMimeType MimeType,
    string LanguageCode,
    RecognitionProfile RecognitionProfile = RecognitionProfile.Conversational,
    SpeechKeyterms? Keyterms = null,
    string? ClientCorrelationId = null,
    bool IsDiagnostic = false) : ICommand<TranscribeSpeechResult>;
