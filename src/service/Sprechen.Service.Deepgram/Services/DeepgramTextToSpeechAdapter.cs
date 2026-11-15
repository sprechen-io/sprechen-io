using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Runtime.CompilerServices;

using Sprechen.Service.Deepgram.Options;

namespace Sprechen.Service.Deepgram.Services;

/// <summary>
/// Streamt Deepgram-Sprachausgabe und begrenzt die Zahl gleichzeitiger Synthesen pro Adapterinstanz.
/// </summary>
internal sealed class DeepgramTextToSpeechAdapter : ITextToSpeechProvider
{
    private readonly IDeepgramSpeakWebSocketClientFactory _wsClientFactory;
    private readonly DeepgramTextToSpeechRequestFactory _requestFactory;
    private readonly DeepgramSpeakConnectionRunner _connectionRunner;
    private readonly DeepgramSpeakConnectionCleanup _connectionCleanup;
    private readonly DeepgramOptions _options;
    private readonly ILogger<DeepgramTextToSpeechAdapter> _logger;
    private readonly SemaphoreSlim _ttsConcurrencyGate;

    public DeepgramTextToSpeechAdapter(
        IDeepgramSpeakWebSocketClientFactory wsClientFactory,
        DeepgramTtsModelMap ttsModelMap,
        IOptions<DeepgramOptions> options,
        ILogger<DeepgramTextToSpeechAdapter> logger)
    {
        _wsClientFactory = wsClientFactory ?? throw new ArgumentNullException(nameof(wsClientFactory));
        ArgumentNullException.ThrowIfNull(ttsModelMap);
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _requestFactory = new DeepgramTextToSpeechRequestFactory(ttsModelMap, _options, _logger);
        _connectionCleanup = new DeepgramSpeakConnectionCleanup(_logger);
        _connectionRunner = new DeepgramSpeakConnectionRunner(_options, _logger, _connectionCleanup);
        _ttsConcurrencyGate = new SemaphoreSlim(_options.TtsMaxConcurrency, _options.TtsMaxConcurrency);
    }

    public async IAsyncEnumerable<AudioSynthesisChunk> SynthesizeAsync(
        string text,
        AudioMimeType desired,
        float speed,
        string? languageCode,
        TextToSpeechPolitenessLevel politenessLevel,
        [EnumeratorCancellation] CancellationToken ct)
    {
        _ = politenessLevel;
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        var request = _requestFactory.Create(text, desired, speed, languageCode);

        await _ttsConcurrencyGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            using var clientCts = new CancellationTokenSource();
            var audio = new DeepgramSpeakAudioChannel(_options.TtsEndpoint, request.Model, _logger);
            var client = await _connectionRunner.ConnectAsync(_wsClientFactory, audio, request.Schema, clientCts, ct).ConfigureAwait(false);

            try
            {
                client.SpeakWithText(request.Text);
                client.Flush();

                await foreach (var chunk in audio.Reader.ReadAllAsync(ct).ConfigureAwait(false))
                {
                    yield return new AudioSynthesisChunk(chunk, AudioMimeType.Pcm, request.SampleRateHz);
                }
            }
            finally
            {
                await _connectionCleanup.CleanupAsync(client, clientCts, true, audio.Flushed, ct).ConfigureAwait(false);
                audio.TryComplete();
            }
        }
        finally
        {
            _ttsConcurrencyGate.Release();
        }
    }
}
