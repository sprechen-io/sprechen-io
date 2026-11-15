using Deepgram.Models.Speak.v2.WebSocket;
using Microsoft.Extensions.Logging;
using System.Net.WebSockets;
using System.Text.RegularExpressions;

using Sprechen.Service.Deepgram.Options;

using DeepgramSpeakWebSocketClient = global::Deepgram.Clients.Interfaces.v2.ISpeakWebSocketClient;

namespace Sprechen.Service.Deepgram.Services;

/// <summary>
/// Öffnet den Speak-WebSocket und wiederholt begrenzt Verbindungsversuche,
/// bei denen die Ausnahmemeldung den HTTP-Status 429 enthält.
/// </summary>
internal sealed class DeepgramSpeakConnectionRunner
{
    private const string ProviderName = "Deepgram";
    private const string OperationName = "TextToSpeech";

    private readonly DeepgramOptions _options;
    private readonly ILogger _logger;
    private readonly DeepgramSpeakConnectionCleanup _cleanup;

    public DeepgramSpeakConnectionRunner(DeepgramOptions options, ILogger logger, DeepgramSpeakConnectionCleanup cleanup)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cleanup = cleanup ?? throw new ArgumentNullException(nameof(cleanup));
    }

    /// <summary>Gibt nur verbundene Clients ab. Fehlgeschlagene Versuche werden hier abgebaut.</summary>
    public async Task<DeepgramSpeakWebSocketClient> ConnectAsync(
        IDeepgramSpeakWebSocketClientFactory factory,
        DeepgramSpeakAudioChannel audio,
        SpeakSchema schema,
        CancellationTokenSource clientCts,
        CancellationToken cancellationToken)
    {
        try
        {
            return await ConnectWithRetryAsync(factory, audio, schema, clientCts, cancellationToken).ConfigureAwait(false);
        }
        catch (SpeechProviderRateLimitedException)
        {
            throw;
        }
        catch (WebSocketException ex)
        {
            var wsUri = BuildSpeakWebSocketUri(schema, _options.TtsEndpoint);
            throw new InvalidOperationException(
                $"Deepgram speak streaming connection failed (wsUri={wsUri}, model={schema.Model}, encoding={schema.Encoding}, sample_rate={schema.SampleRate}).",
                ex);
        }
    }

    private async Task<DeepgramSpeakWebSocketClient> ConnectWithRetryAsync(
        IDeepgramSpeakWebSocketClientFactory factory,
        DeepgramSpeakAudioChannel audio,
        SpeakSchema schema,
        CancellationTokenSource clientCts,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(audio);
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(clientCts);

        using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(clientCts.Token, cancellationToken);

        var retriesLeft = _options.TtsRateLimitMaxRetries;
        var delay = TimeSpan.FromMilliseconds(_options.TtsRateLimitInitialDelayMs);
        var maxDelay = TimeSpan.FromMilliseconds(_options.TtsRateLimitMaxDelayMs);

        // Bei einem als 429 erkannten Verbindungsfehler wird ein neuer Client angelegt.
        // Der gemeinsame Cleanup-Token bleibt bei nicht verbundenen Clients für den nächsten Versuch aktiv.
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _logger.LogDebug(
                "Deepgram speak streaming connect: endpoint={Endpoint}, model={Model}, encoding={Encoding}, sampleRate={SampleRate}, retriesLeft={RetriesLeft}",
                _options.TtsEndpoint,
                schema.Model,
                schema.Encoding,
                schema.SampleRate,
                retriesLeft);

            var client = factory.Create();
            var connected = false;
            var transferred = false;
            try
            {
                await audio.SubscribeAsync(client).ConfigureAwait(false);
                connected = await client.Connect(
                        schema,
                        connectCts,
                        addons: DeepgramRequestAddons.CreateModelImprovementOptOut(),
                        headers: null)
                    .ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                if (!connected)
                {
                    throw new InvalidOperationException(
                        $"Deepgram text-to-speech failed to connect (endpoint={_options.TtsEndpoint}, model={schema.Model}).");
                }

                transferred = true;
                return client;
            }
            catch (WebSocketException ex) when (TryGetHttpStatusCode(ex, out var statusCode) && statusCode == 429)
            {
                if (retriesLeft <= 0)
                {
                    throw new SpeechProviderRateLimitedException(ProviderName, OperationName, ex);
                }

                retriesLeft--;
            }
            finally
            {
                if (!transferred)
                {
                    await _cleanup.CleanupAsync(client, clientCts, connected, false, cancellationToken).ConfigureAwait(false);
                }
            }

            await DelayBeforeRetryAsync(delay, cancellationToken).ConfigureAwait(false);
            delay = ExponentialBackoff.NextDelay(delay, maxDelay);
        }
    }

    private async Task DelayBeforeRetryAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        if (delay <= TimeSpan.Zero)
        {
            await Task.Yield();
            return;
        }

        _logger.LogWarning(
            "Deepgram text-to-speech rate limited (429). Retrying in {Delay}.",
            delay);

        await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
    }

    private static bool TryGetHttpStatusCode(WebSocketException exception, out int statusCode)
    {
        ArgumentNullException.ThrowIfNull(exception);

        statusCode = 0;

        var message = exception.Message;
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        var match = Regex.Match(message, @"status code ['""](\d{3})['""]", RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            return false;
        }

        return int.TryParse(match.Groups[1].ValueSpan, out statusCode);
    }

    private static string BuildSpeakWebSocketUri(SpeakSchema schema, string httpsEndpoint)
    {
        if (!Uri.TryCreate(httpsEndpoint, UriKind.Absolute, out var endpoint))
        {
            return "(invalid Deepgram endpoint)";
        }

        var queryParts = new List<string>(capacity: 4);

        if (!string.IsNullOrWhiteSpace(schema.Model))
        {
            queryParts.Add($"model={Uri.EscapeDataString(schema.Model)}");
        }

        if (!string.IsNullOrWhiteSpace(schema.Encoding))
        {
            queryParts.Add($"encoding={Uri.EscapeDataString(schema.Encoding)}");
        }

        if (schema.SampleRate is int sampleRate)
        {
            queryParts.Add($"sample_rate={sampleRate}");
        }

        if (schema.BitRate is int bitRate)
        {
            queryParts.Add($"bit_rate={bitRate}");
        }

        var builder = new UriBuilder(endpoint)
        {
            Scheme = "wss",
            Port = -1,
            Path = "/v1/speak",
            Query = string.Join("&", queryParts)
        };

        return builder.Uri.ToString();
    }
}
