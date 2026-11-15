namespace Sprechen.Service.Core.Services;

/// <summary>
/// Bündelt Spracherkennung, Sprachausgabe und Gesprächsströme unter einem Dienstvertrag, während
/// die jeweiligen Orchestratoren die Ausführung übernehmen.
/// </summary>
internal sealed class SpeechService : ISpeechService
{
    private readonly SpeechTranscriptionOrchestrator _transcription;
    private readonly SpeechSynthesisOrchestrator _synthesis;
    private readonly SpeechConversationStreamOrchestrator _conversationStream;

    public SpeechService(
        SpeechTranscriptionOrchestrator transcription,
        SpeechSynthesisOrchestrator synthesis,
        SpeechConversationStreamOrchestrator conversationStream)
    {
        _transcription = transcription ?? throw new ArgumentNullException(nameof(transcription));
        _synthesis = synthesis ?? throw new ArgumentNullException(nameof(synthesis));
        _conversationStream = conversationStream ?? throw new ArgumentNullException(nameof(conversationStream));
    }

    public IAsyncEnumerable<AudioSynthesisChunk> SynthesizeAsync(
        string text,
        AudioMimeType desired,
        float speed,
        string? languageCode,
        CancellationToken ct) =>
        SynthesizeAsync(
            text,
            desired,
            speed,
            languageCode,
            TextToSpeechPolitenessLevel.Unspecified,
            ct);

    public IAsyncEnumerable<AudioSynthesisChunk> SynthesizeAsync(
        string text,
        AudioMimeType desired,
        float speed,
        string? languageCode,
        TextToSpeechPolitenessLevel politenessLevel,
        CancellationToken ct) =>
        _synthesis.SynthesizeAsync(
            text,
            desired,
            speed,
            languageCode,
            politenessLevel,
            ct);

    public Task<SpeechTranscript> TranscribeStreamingAsync(IAsyncEnumerable<byte[]> chunks, AudioMimeType mime, CancellationToken ct, string? languageCode, RecognitionProfile recognitionProfile = RecognitionProfile.Conversational, SpeechKeyterms? keyterms = null, string? clientCorrelationId = null, bool isDiagnostic = false) =>
        _transcription.TranscribeStreamingAsync(chunks, mime, ct, languageCode, recognitionProfile, keyterms, clientCorrelationId, isDiagnostic);

    public IAsyncEnumerable<SpeechTranscript> TranscribeStreamingFeed(IAsyncEnumerable<byte[]> chunks, AudioMimeType mime, CancellationToken ct, string? languageCode, RecognitionProfile recognitionProfile = RecognitionProfile.Conversational) =>
        _transcription.TranscribeStreamingFeed(chunks, mime, ct, languageCode, recognitionProfile);

    public async IAsyncEnumerable<SpeechConversationEvent> StreamConversationAsync(
        IAsyncEnumerable<byte[]> chunks,
        AudioMimeType mime,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct,
        string? languageCode,
        RecognitionProfile recognitionProfile = RecognitionProfile.Conversational)
    {
        await foreach (var conversationEvent in _conversationStream.StreamAsync(chunks, mime, ct, languageCode, recognitionProfile))
        {
            yield return conversationEvent;
        }
    }
}
