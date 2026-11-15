namespace Sprechen.Service.Core.Services;

/// <summary>
/// Leitet Transkriptionsanfragen mit geprüftem und normalisiertem Sprachcode an den Anbieter weiter.
/// </summary>
internal sealed class SpeechTranscriptionOrchestrator
{
    private readonly ISpeechToTextProvider _provider;
    private readonly ISupportedLanguagesService _supportedLanguages;

    public SpeechTranscriptionOrchestrator(
        ISpeechToTextProvider provider,
        ISupportedLanguagesService supportedLanguages)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _supportedLanguages = supportedLanguages ?? throw new ArgumentNullException(nameof(supportedLanguages));
    }

    public Task<SpeechTranscript> TranscribeStreamingAsync(
        IAsyncEnumerable<byte[]> chunks,
        AudioMimeType mime,
        CancellationToken cancellationToken,
        string? languageCode,
        RecognitionProfile recognitionProfile = RecognitionProfile.Conversational,
        SpeechKeyterms? keyterms = null,
        string? clientCorrelationId = null,
        bool isDiagnostic = false)
    {
        return _provider.TranscribeStreamingAsync(
            chunks,
            mime,
            cancellationToken,
            _supportedLanguages.GetEffectiveIsoCode(languageCode),
            recognitionProfile,
            keyterms,
            clientCorrelationId,
            isDiagnostic);
    }

    public IAsyncEnumerable<SpeechTranscript> TranscribeStreamingFeed(
        IAsyncEnumerable<byte[]> chunks,
        AudioMimeType mime,
        CancellationToken cancellationToken,
        string? languageCode,
        RecognitionProfile recognitionProfile = RecognitionProfile.Conversational)
    {
        return _provider.TranscribeStreamingFeed(
            chunks,
            mime,
            cancellationToken,
            _supportedLanguages.GetEffectiveIsoCode(languageCode),
            recognitionProfile);
    }
}
