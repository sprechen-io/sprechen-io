using System.Threading.Channels;
using Deepgram.Models.Listen.v2.WebSocket;
using Microsoft.Extensions.Logging;
using DeepgramListenWebSocketClient = Deepgram.Clients.Interfaces.v2.IListenWebSocketClient;

namespace Sprechen.Service.Deepgram.Services;

/// <summary>
/// Überträgt Audio in einer Live-Erkennungssitzung und verarbeitet Transkript- und Fehlerereignisse.
/// Nach Finalize wird nur bis zur konfigurierten Zeitgrenze auf Close gewartet.
/// </summary>
internal sealed class DeepgramLiveTranscriptionSession
{
    private readonly DeepgramListenWebSocketClient _client;
    private readonly DeepgramLiveSchemaFactory _schemaFactory;
    private readonly TimeSpan _finalizeWaitTimeout;
    private readonly TimeSpan _stopTimeout;
    private readonly ILogger _logger;

    internal DeepgramLiveTranscriptionSession(
        DeepgramListenWebSocketClient client,
        DeepgramLiveSchemaFactory schemaFactory,
        TimeSpan finalizeWaitTimeout,
        TimeSpan stopTimeout,
        ILogger logger)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _schemaFactory = schemaFactory ?? throw new ArgumentNullException(nameof(schemaFactory));
        _finalizeWaitTimeout = finalizeWaitTimeout;
        _stopTimeout = stopTimeout;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    internal async Task RunAsync(
        IAsyncEnumerable<byte[]> chunks,
        AudioMimeType mime,
        string? languageCode,
        RecognitionProfile recognitionProfile,
        ChannelWriter<SpeechTranscript> output,
        CancellationTokenSource clientCts,
        CancellationTokenSource runCts,
        SpeechKeyterms? keyterms = null)
    {
        ArgumentNullException.ThrowIfNull(chunks);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(clientCts);
        ArgumentNullException.ThrowIfNull(runCts);

        var linkedToken = runCts.Token;
        var closeTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var transcriptAccumulator = new DeepgramLiveTranscriptAccumulator();
        var transcriptGate = new object();

        void Complete(Exception? exception)
        {
            output.TryComplete(exception);

            try
            {
                runCts.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // Die Abbruchquelle ist bereits freigegeben.
            }
        }

        await SubscribeTranscriptsAsync(output, transcriptAccumulator, transcriptGate, Complete).ConfigureAwait(false);
        await SubscribeErrorsAsync(Complete).ConfigureAwait(false);
        await _client.Subscribe(new EventHandler<CloseResponse>((_, _) => closeTcs.TrySetResult())).ConfigureAwait(false);

        try
        {
            var schema = _schemaFactory.CreateLowLatency(mime, languageCode, recognitionProfile, keyterms);
            bool connected;
            using (var connectCts = CancellationTokenSource.CreateLinkedTokenSource(clientCts.Token, linkedToken))
            {
                linkedToken.ThrowIfCancellationRequested();
                connected = await _client.Connect(
                        schema,
                        connectCts,
                        addons: DeepgramRequestAddons.CreateModelImprovementOptOut(),
                        headers: null)
                    .ConfigureAwait(false);
                linkedToken.ThrowIfCancellationRequested();
            }
            if (!connected)
            {
                throw new InvalidOperationException("Deepgram live transcription failed: could not connect to WebSocket.");
            }

            await SendChunksAsync(chunks, linkedToken).ConfigureAwait(false);

            await _client.SendFinalize().ConfigureAwait(false);
            await Task.WhenAny(closeTcs.Task, Task.Delay(_finalizeWaitTimeout, linkedToken)).ConfigureAwait(false);

            if (!closeTcs.Task.IsCompleted)
            {
                // Ohne bestätigtes Close wird die Ausgabe nach Ablauf der Wartezeit beendet.
                // Ein noch ausstehendes Endtranskript kann dabei fehlen. Deshalb wird eine Warnung protokolliert.
                _logger.LogWarning(
                    "Deepgram live transcription finalize wait ({FinalizeWaitTimeout}) elapsed before the provider close arrived; completing the transcript stream without a confirmed close (possible truncation).",
                    _finalizeWaitTimeout);
            }

            output.TryComplete();
        }
        catch (OperationCanceledException) when (linkedToken.IsCancellationRequested)
        {
            output.TryComplete();
        }
        catch (Exception ex)
        {
            Complete(ex);
        }
        finally
        {
            await StopAsync(clientCts).ConfigureAwait(false);
        }
    }

    private async Task SubscribeTranscriptsAsync(
        ChannelWriter<SpeechTranscript> output,
        DeepgramLiveTranscriptAccumulator transcriptAccumulator,
        object transcriptGate,
        Action<Exception?> complete)
    {
        await _client.Subscribe(new EventHandler<ResultResponse>((_, e) =>
        {
            try
            {
                var alternative = e.Channel?.Alternatives?.FirstOrDefault();
                var transcript = alternative?.Transcript?.Trim();
                if (string.IsNullOrWhiteSpace(transcript))
                {
                    return;
                }

                var confidencePercent = SttConfidencePercent.FromNormalized(alternative?.Confidence);

                lock (transcriptGate)
                {
                    var isFinal = e.IsFinal == true || e.SpeechFinal == true;
                    var speechTranscript = transcriptAccumulator.Apply(
                        transcript,
                        confidencePercent,
                        isFinal);
                    if (speechTranscript is not null)
                    {
                        output.TryWrite(speechTranscript);
                    }
                }
            }
            catch (Exception ex)
            {
                complete(ex);
            }
        })).ConfigureAwait(false);
    }

    private async Task SubscribeErrorsAsync(Action<Exception?> complete)
    {
        await _client.Subscribe(new EventHandler<ErrorResponse>((_, e) =>
        {
            if (DeepgramRateLimitSignal.Matches(errCode: null, errMsg: null, message: e.Message))
            {
                // Ratenbegrenzungen als eigenen Ausnahmetyp an den Aufrufer weitergeben.
                // Dieser Handler verarbeitet Anbieterereignisse nach dem Abonnieren, nicht Fehler aus Connect selbst.
                complete(new SpeechProviderRateLimitedException("Deepgram", "SpeechToText"));
                return;
            }

            var msg = string.IsNullOrWhiteSpace(e.Message) ? "Deepgram live transcription failed." : e.Message;
            complete(new InvalidOperationException(msg));
        })).ConfigureAwait(false);
    }

    private async Task SendChunksAsync(
        IAsyncEnumerable<byte[]> chunks,
        CancellationToken cancellationToken)
    {
        await foreach (var chunk in chunks.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            if (chunk.Length == 0)
            {
                continue;
            }

            _client.Send(chunk, chunk.Length);
        }
    }

    private async Task StopAsync(CancellationTokenSource clientCts)
    {
        try
        {
            var stopTask = _client.Stop(clientCts, nullByte: false);
            await stopTask.WaitAsync(_stopTimeout).ConfigureAwait(false);
        }
        catch
        {
            try
            {
                await _client.SendClose(nullByte: false, _cancellationToken: clientCts).WaitAsync(_stopTimeout).ConfigureAwait(false);
            }
            catch
            {
            }
        }
    }
}
