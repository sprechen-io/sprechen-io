using Deepgram.Models.Exceptions.v1;
using Deepgram.Models.Listen.v1.REST;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Text.Json;
using DeepgramListenRestClient = Deepgram.Clients.Interfaces.v1.IListenRESTClient;

namespace Sprechen.Service.Deepgram.Services;

/// <summary>
/// Transkribiert gepufferte Audiodaten über Deepgram REST und protokolliert Laufzeit- und Anfragemetadaten.
/// Erkannte Ratenbegrenzungen erhalten einen eigenen Ausnahmetyp.
/// </summary>
internal sealed class DeepgramPrerecordedTranscriptionClient
{
    private const int MinimumPcmDurationMs = 250;

    private readonly DeepgramListenRestClient _restClient;
    private readonly DeepgramOptions _options;
    private readonly DeepgramPrerecordedTranscriptionDiagnostics _diagnostics;
    private readonly DeepgramPrerecordedSchemaFactory _schemaFactory;
    private readonly DeepgramPrerecordedTranscriptMapper _transcriptMapper;
    private readonly RecognizedAudioDebugWriter _recognizedAudioDebugWriter;

    internal DeepgramPrerecordedTranscriptionClient(
        DeepgramListenRestClient restClient,
        IOptions<DeepgramOptions> options,
        ILogger<DeepgramPrerecordedTranscriptionClient> logger,
        string? recognizedAudioDumpDirectory = null)
    {
        _restClient = restClient ?? throw new ArgumentNullException(nameof(restClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        ArgumentNullException.ThrowIfNull(logger);
        _diagnostics = new DeepgramPrerecordedTranscriptionDiagnostics(logger, _options.SttModel);
        _schemaFactory = new DeepgramPrerecordedSchemaFactory(_options);
        _transcriptMapper = new DeepgramPrerecordedTranscriptMapper();
        _recognizedAudioDebugWriter = new RecognizedAudioDebugWriter(logger, recognizedAudioDumpDirectory);
    }

    internal async Task<SpeechTranscript> TranscribeAsync(
        IAsyncEnumerable<byte[]> chunks,
        AudioMimeType mime,
        CancellationToken ct,
        string? languageCode,
        RecognitionProfile recognitionProfile = RecognitionProfile.Conversational,
        SpeechKeyterms? keyterms = null,
        string? clientCorrelationId = null,
        bool isDiagnostic = false)
    {
        ArgumentNullException.ThrowIfNull(chunks);
        ct.ThrowIfCancellationRequested();

        var totalStart = Stopwatch.GetTimestamp();
        var readStart = Stopwatch.GetTimestamp();
        var audio = await DeepgramAudioPayload.ReadAllAsync(chunks, ct).ConfigureAwait(false);
        var readMs = GetElapsedMilliseconds(readStart);
        if (audio.Length == 0)
        {
            _diagnostics.LogSkippedEmptyAudio(mime, languageCode, readMs, GetElapsedMilliseconds(totalStart), clientCorrelationId);
            return new SpeechTranscript(string.Empty, null);
        }

        var audioDurationMs = mime == AudioMimeType.Pcm
            ? DeepgramAudioPayload.GetPcmDurationMs(audio.Length)
            : (int?)null;
        if (mime == AudioMimeType.Pcm && audioDurationMs < MinimumPcmDurationMs)
        {
            _diagnostics.LogSkippedShortPcm(
                audio.Length,
                audioDurationMs.Value,
                MinimumPcmDurationMs,
                languageCode,
                readMs,
                GetElapsedMilliseconds(totalStart),
                clientCorrelationId);
            return new SpeechTranscript(string.Empty, null);
        }

        var prepareStart = Stopwatch.GetTimestamp();
        var payload = DeepgramAudioPayload.Prepare(audio, mime);
        var prepareMs = GetElapsedMilliseconds(prepareStart);
        var schemaComposition = _schemaFactory.Compose(languageCode, recognitionProfile, keyterms);
        var schema = schemaComposition.Schema;
        var outgoingKeytermListJson = SerializeKeyterms(schema.Keyterm);

        using var requestCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        requestCts.CancelAfter(TimeSpan.FromMilliseconds(global::Deepgram.Abstractions.v1.Constants.DefaultRESTTimeout));

        SyncResponse response;
        var providerStart = Stopwatch.GetTimestamp();
        try
        {
            response = await _restClient.TranscribeFile(
                    payload,
                    schema,
                    requestCts,
                    addons: DeepgramRequestAddons.CreateModelImprovementOptOut(),
                    headers: null)
                .ConfigureAwait(false);
        }
        catch (DeepgramException ex) when (DeepgramRateLimitSignal.Matches(ex.ErrCode, ex.ErrMsg, ex.Message))
        {
            _diagnostics.LogFailed(
                mime,
                languageCode,
                audio.Length,
                payload.Length,
                recognitionProfile,
                schemaComposition,
                outgoingKeytermListJson,
                GetElapsedMilliseconds(providerStart),
                GetElapsedMilliseconds(totalStart),
                "rate-limited",
                ex.RequestId,
                clientCorrelationId);
            // Die Ratenbegrenzung wird anhand von Fehlercode, Fehlerbeschreibung und Ausnahmemeldung erkannt.
            // Fehlt darin ein erkennbarer Hinweis, greift die allgemeine Anbieterfehlerbehandlung.
            throw new SpeechProviderRateLimitedException("Deepgram", "SpeechToText", ex);
        }
        catch (DeepgramException ex)
        {
            _diagnostics.LogFailed(
                mime,
                languageCode,
                audio.Length,
                payload.Length,
                recognitionProfile,
                schemaComposition,
                outgoingKeytermListJson,
                GetElapsedMilliseconds(providerStart),
                GetElapsedMilliseconds(totalStart),
                "provider-error",
                ex.RequestId,
                clientCorrelationId);
            var requestId = string.IsNullOrWhiteSpace(ex.RequestId) ? "n/a" : ex.RequestId;
            var code = string.IsNullOrWhiteSpace(ex.ErrCode) ? "n/a" : ex.ErrCode;
            var msg = string.IsNullOrWhiteSpace(ex.ErrMsg) ? ex.Message : ex.ErrMsg;
            throw new InvalidOperationException(
                $"Deepgram transcription failed (endpoint={_options.Endpoint}, model={_options.SttModel}, code={code}, requestId={requestId}): {msg}",
                ex);
        }
        catch (HttpRequestException ex) when (ex.InnerException is HttpIOException { HttpRequestError: HttpRequestError.ResponseEnded })
        {
            _diagnostics.LogFailed(
                mime,
                languageCode,
                audio.Length,
                payload.Length,
                recognitionProfile,
                schemaComposition,
                outgoingKeytermListJson,
                GetElapsedMilliseconds(providerStart),
                GetElapsedMilliseconds(totalStart),
                "response-ended",
                deepgramRequestId: null,
                clientCorrelationId);
            throw new InvalidOperationException("Deepgram transcription failed: response ended prematurely.", ex);
        }
        catch (OperationCanceledException)
        {
            _diagnostics.LogFailed(
                mime,
                languageCode,
                audio.Length,
                payload.Length,
                recognitionProfile,
                schemaComposition,
                outgoingKeytermListJson,
                GetElapsedMilliseconds(providerStart),
                GetElapsedMilliseconds(totalStart),
                ct.IsCancellationRequested ? "cancelled" : "timeout",
                deepgramRequestId: null,
                clientCorrelationId);
            throw;
        }
        catch (Exception ex)
        {
            _diagnostics.LogFailed(
                mime,
                languageCode,
                audio.Length,
                payload.Length,
                recognitionProfile,
                schemaComposition,
                outgoingKeytermListJson,
                GetElapsedMilliseconds(providerStart),
                GetElapsedMilliseconds(totalStart),
                ex.GetType().Name,
                deepgramRequestId: null,
                clientCorrelationId);
            throw;
        }

        var providerMs = GetElapsedMilliseconds(providerStart);

        var transcript = _transcriptMapper.Map(response);
        // Audiobytes nur bei ausdrücklich gesetzter Diagnosekennzeichnung an den Dateischreiber geben.
        // Dieser schreibt nur, wenn zusätzlich ein Zielverzeichnis konfiguriert ist.
        if (isDiagnostic)
        {
            _recognizedAudioDebugWriter.TryWrite(payload, mime, clientCorrelationId);
        }
        _diagnostics.LogCompleted(
            mime,
            languageCode,
            audio.Length,
            payload.Length,
            audioDurationMs,
            readMs,
            prepareMs,
            providerMs,
            GetElapsedMilliseconds(totalStart),
            recognitionProfile,
            schemaComposition,
            outgoingKeytermListJson,
            response.Metadata?.RequestId,
            transcript,
            clientCorrelationId);
        return transcript;
    }

    private static double GetElapsedMilliseconds(long startTimestamp)
        => Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;

    private static string SerializeKeyterms(IReadOnlyList<string>? keyterms)
        => JsonSerializer.Serialize(keyterms ?? []);
}
