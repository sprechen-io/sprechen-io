using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Sprechen.Service.Deepgram.Services;

/// <summary>
/// Bietet Deepgram-Spracherkennung für gepufferte Audiodaten und laufende Audioströme an.
/// </summary>
internal sealed class DeepgramSpeechToTextAdapter : ISpeechToTextProvider, IConversationalSpeechToTextProvider
{
    private readonly DeepgramPrerecordedTranscriptionClient _prerecordedTranscriptionClient;
    private readonly IDeepgramListenWebSocketClientFactory _wsClientFactory;
    private readonly DeepgramLiveSchemaFactory _liveSchemaFactory;
    private readonly DeepgramOptions _options;
    private readonly ILogger<DeepgramSpeechToTextAdapter> _logger;

    public DeepgramSpeechToTextAdapter(
        IDeepgramListenWebSocketClientFactory wsClientFactory,
        DeepgramPrerecordedTranscriptionClient prerecordedTranscriptionClient,
        IOptions<DeepgramOptions> options,
        ILogger<DeepgramSpeechToTextAdapter> logger)
    {
        _wsClientFactory = wsClientFactory ?? throw new ArgumentNullException(nameof(wsClientFactory));
        _prerecordedTranscriptionClient = prerecordedTranscriptionClient ?? throw new ArgumentNullException(nameof(prerecordedTranscriptionClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _liveSchemaFactory = new DeepgramLiveSchemaFactory(_options);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<SpeechTranscript> TranscribeStreamingAsync(IAsyncEnumerable<byte[]> chunks, AudioMimeType mime, CancellationToken ct, string? languageCode, RecognitionProfile recognitionProfile = RecognitionProfile.Conversational, SpeechKeyterms? keyterms = null, string? clientCorrelationId = null, bool isDiagnostic = false)
    {
        ArgumentNullException.ThrowIfNull(chunks);

        return _prerecordedTranscriptionClient.TranscribeAsync(chunks, mime, ct, languageCode, recognitionProfile, keyterms, clientCorrelationId, isDiagnostic);
    }

    public async IAsyncEnumerable<SpeechTranscript> TranscribeStreamingFeed(
        IAsyncEnumerable<byte[]> chunks,
        AudioMimeType mime,
        [EnumeratorCancellation] CancellationToken ct,
        string? languageCode,
        RecognitionProfile recognitionProfile = RecognitionProfile.Conversational,
        SpeechKeyterms? keyterms = null)
    {
        ArgumentNullException.ThrowIfNull(chunks);

        _liveSchemaFactory.ValidateMimeType(mime);

        var channel = System.Threading.Channels.Channel.CreateUnbounded<SpeechTranscript>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        using var clientCts = new CancellationTokenSource();
        using var runCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var client = _wsClientFactory.Create();

        var session = new DeepgramLiveTranscriptionSession(
            client,
            _liveSchemaFactory,
            _options.LiveFinalizeWaitTimeout,
            _options.LiveStopTimeout,
            _logger);
        var sessionTask = session.RunAsync(chunks, mime, languageCode, recognitionProfile, channel.Writer, clientCts, runCts, keyterms);

        try
        {
            await foreach (var transcript in channel.Reader.ReadAllAsync(ct).ConfigureAwait(false))
            {
                if (!string.IsNullOrWhiteSpace(transcript.Text))
                {
                    yield return transcript;
                }
            }
        }
        finally
        {
            runCts.Cancel();
            channel.Writer.TryComplete();

            try
            {
                await sessionTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested || runCts.IsCancellationRequested)
            {
                // Abbruch des Aufrufers oder der eigenen Sitzung beim Beenden des Ausgabestroms.
            }
        }
    }

    /// <summary>
    /// Gibt jedes nicht leere Transkript als Aktualisierung mit Zugindex 0 aus.
    /// Nach regulärem Stromende wird das letzte Transkript finalisiert und der Zug beendet,
    /// sofern mindestens ein Transkript vorliegt.
    /// </summary>
    public async IAsyncEnumerable<SpeechConversationEvent> StreamConversationAsync(
        IAsyncEnumerable<byte[]> chunks,
        AudioMimeType mime,
        [EnumeratorCancellation] CancellationToken ct,
        string? languageCode,
        RecognitionProfile recognitionProfile = RecognitionProfile.Conversational)
    {
        SpeechTranscript? lastTranscript = null;
        var turnIndex = 0;

        await foreach (var transcript in TranscribeStreamingFeed(chunks, mime, ct, languageCode, recognitionProfile).ConfigureAwait(false))
        {
            lastTranscript = transcript;
            yield return new SpeechConversationTranscriptUpdateEvent(
                transcript.Text,
                transcript.ConfidencePercent,
                turnIndex);
        }

        if (lastTranscript is not null)
        {
            yield return new SpeechConversationTranscriptFinalizedEvent(
                lastTranscript.Text,
                lastTranscript.ConfidencePercent,
                turnIndex);
            yield return new SpeechConversationEndOfTurnEvent(turnIndex, lastTranscript.Text);
        }
    }
}
