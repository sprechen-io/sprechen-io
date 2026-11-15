using Deepgram.Models.Speak.v2.WebSocket;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;
using DeepgramSpeakWebSocketClient = global::Deepgram.Clients.Interfaces.v2.ISpeakWebSocketClient;

namespace Sprechen.Service.Deepgram.Services;

/// <summary>
/// Nimmt Speak-Audio entgegen und beendet den Audiostrom bei Flushed.
/// </summary>
internal sealed class DeepgramSpeakAudioChannel
{
    private readonly Channel<byte[]> _audio = Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false
    });
    private readonly string _endpoint;
    private readonly string _model;
    private readonly ILogger _logger;
    private volatile bool _flushed;

    public DeepgramSpeakAudioChannel(string endpoint, string model, ILogger logger)
    {
        _endpoint = string.IsNullOrWhiteSpace(endpoint) ? "(unknown endpoint)" : endpoint;
        _model = string.IsNullOrWhiteSpace(model) ? "(unknown model)" : model;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public ChannelReader<byte[]> Reader => _audio.Reader;

    /// <summary>
    /// Wahr, sobald der Server eine FlushedResponse gesendet hat.
    /// Der Verbindungsabbau erfolgt getrennt vom Abschluss des Audiokanals.
    /// </summary>
    public bool Flushed => _flushed;

    public async Task SubscribeAsync(DeepgramSpeakWebSocketClient client)
    {
        ArgumentNullException.ThrowIfNull(client);

        await client.Subscribe(new EventHandler<AudioResponse>((_, response) => OnAudio(response))).ConfigureAwait(false);
        await client.Subscribe(new EventHandler<WarningResponse>((_, warning) => OnWarning(warning))).ConfigureAwait(false);
        await client.Subscribe(new EventHandler<ErrorResponse>((_, error) => OnError(error))).ConfigureAwait(false);
        await client.Subscribe(new EventHandler<FlushedResponse>((_, _) => OnFlushed())).ConfigureAwait(false);
    }

    public void TryComplete()
        => _audio.Writer.TryComplete();

    private void OnFlushed()
    {
        _flushed = true;
        TryComplete();
    }

    private void OnAudio(AudioResponse? response)
    {
        try
        {
            if (response?.Stream is null || response.Stream.Length == 0)
            {
                return;
            }

            var bytes = response.Stream.ToArray();
            if (bytes.Length == 0)
            {
                return;
            }

            _ = _audio.Writer.TryWrite(bytes);
        }
        catch (Exception ex)
        {
            _audio.Writer.TryComplete(ex);
        }
    }

    private void OnWarning(WarningResponse? warning)
    {
        if (warning is null || string.IsNullOrWhiteSpace(warning.WarnMsg))
        {
            return;
        }

        _logger.LogWarning(
            "Deepgram text-to-speech warning (code={Code}, variant={Variant}): {Message}",
            warning.WarnCode,
            warning.Variant,
            warning.WarnMsg);
    }

    private void OnError(ErrorResponse? error)
    {
        var msg = error?.Message;
        if (string.IsNullOrWhiteSpace(msg))
        {
            msg = error?.Description;
        }

        _audio.Writer.TryComplete(new InvalidOperationException(
            $"Deepgram text-to-speech failed (endpoint={_endpoint}, model={_model}): {msg ?? "Unknown error"}"));
    }
}
