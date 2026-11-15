using Deepgram.Models.Speak.v2.WebSocket;
using Microsoft.Extensions.Logging;

using Sprechen.Service.Deepgram.Options;

namespace Sprechen.Service.Deepgram.Services;

/// <summary>
/// Erstellt Syntheseanfragen für PCM und prüft Sprachcode, Modellzuordnung und Abtastrate.
/// </summary>
internal sealed class DeepgramTextToSpeechRequestFactory
{
    private static readonly int[] SupportedPcmSampleRatesHz = { 8_000, 16_000, 24_000, 32_000, 48_000 };

    private const string PcmEncoding = "linear16";

    private readonly DeepgramTtsModelMap _ttsModelMap;
    private readonly DeepgramOptions _options;
    private readonly ILogger _logger;

    public DeepgramTextToSpeechRequestFactory(
        DeepgramTtsModelMap ttsModelMap,
        DeepgramOptions options,
        ILogger logger)
    {
        _ttsModelMap = ttsModelMap ?? throw new ArgumentNullException(nameof(ttsModelMap));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public DeepgramTextToSpeechRequest Create(
        string text,
        AudioMimeType desired,
        float speed,
        string? languageCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        if (desired != AudioMimeType.Pcm)
        {
            throw new InvalidOperationException(
                $"Deepgram text-to-speech supports streaming PCM only. Requested '{desired}'.");
        }

        if (string.IsNullOrWhiteSpace(languageCode))
        {
            throw new InvalidOperationException("Deepgram text-to-speech requires a languageCode (ISO 639-1, e.g. 'de').");
        }

        if (Math.Abs(speed - 1.0f) > 0.01f)
        {
            _logger.LogDebug("Deepgram text-to-speech ignores speed (requested {Speed}).", speed);
        }

        var model = _ttsModelMap.GetModel(languageCode);
        var sampleRate = ResolveSampleRateHz();
        var schema = new SpeakSchema
        {
            Model = model,
            Encoding = PcmEncoding,
            SampleRate = sampleRate
        };

        return new DeepgramTextToSpeechRequest(text, model, sampleRate, schema);
    }

    private int ResolveSampleRateHz()
    {
        var sampleRate = _options.TtsSampleRateHz;
        if (SupportedPcmSampleRatesHz.Contains(sampleRate))
        {
            return sampleRate;
        }

        throw new InvalidOperationException(
            $"{DeepgramOptions.SectionName}:TtsSampleRateHz must be one of {string.Join(", ", SupportedPcmSampleRatesHz)} for {PcmEncoding}.");
    }
}
