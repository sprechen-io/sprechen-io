using Grpc.Core;
using Sprechen.Proto;
using AppRecognitionProfile = Sprechen.Speech.App.Contracts.Services.RecognitionProfile;
using AppSpeechTranscript = Sprechen.Speech.App.Contracts.Services.SpeechTranscript;
using SpeechGrpcClient = Sprechen.Proto.Speech.SpeechClient;
using Sprechen.Proto.Transport;

using Sprechen.Speech.App.Contracts.Services;
namespace Sprechen.Speech.App.GrpcInfrastructure.Services;

/// <summary>
/// Überträgt abgeschlossene Audioaufnahmen mit Erkennungsprofil und Schlüsselbegriffen über gRPC
/// in einzelne normalisierte Transkripte, damit die Auftragsverarbeitung im Anwendungsvertrag bleibt.
/// </summary>
internal sealed class SpeechRecognitionClient : ISpeechRecognitionClient
{
    private readonly SpeechGrpcClient _languageClient;
    private readonly SpeechRecognitionClientTrace _trace;
    private readonly SpeechRecognitionUnaryResponseMapper _responseMapper;
    private readonly ISpeechSessionIdentity? _sessionIdentity;
    private readonly IDiagnosticOperatingMode? _diagnosticOperatingMode;

    public SpeechRecognitionClient(
        SpeechGrpcClient languageClient,
        ISpeechClientTrace trace,
        SpeechRecognitionUnaryResponseMapper? responseMapper = null,
        ISpeechSessionIdentity? sessionIdentity = null,
        IDiagnosticOperatingMode? diagnosticOperatingMode = null)
    {
        _languageClient = languageClient ?? throw new ArgumentNullException(nameof(languageClient));
        _trace = new SpeechRecognitionClientTrace(trace);
        _responseMapper = responseMapper ?? new SpeechRecognitionUnaryResponseMapper();
        // Die Identität bleibt für direkte Konstruktionen optional. Ohne sie wird keine
        // Sitzungskennung in die Anfrage geschrieben. Eine Zuordnung wird nicht erfunden.
        _sessionIdentity = sessionIdentity;
        // Auch der Diagnosemodus ist optional. Ohne ihn erhält keine Aufnahme
        // ein Diagnosekennzeichen.
        _diagnosticOperatingMode = diagnosticOperatingMode;
    }

    /// <summary>
    /// Erzeugt die Korrelationsmetadaten dieser Erkennung und ergänzt die
    /// vorhandene Sitzungskennung zur Zuordnung zu den Sprachausgaben derselben Sitzung.
    /// </summary>
    private Metadata BuildHeaders(string correlationId)
    {
        var headers = SprechenGrpcCorrelationMetadata.CreateHeaders(correlationId);
        if (_sessionIdentity is not null)
        {
            SprechenGrpcSessionMetadata.AddOrReplaceSessionId(headers, _sessionIdentity.SessionId);
        }

        return headers;
    }

    public async Task<AppSpeechTranscript> SpeechToTextAsync(
        CapturedSpeechAudio utterance,
        string? languageCode,
        AppRecognitionProfile recognitionProfile,
        CancellationToken cancellationToken,
        SpeechKeyterms? keyterms = null)
    {
        ArgumentNullException.ThrowIfNull(utterance);
        // Die Kennung aus der Aufnahme hat Vorrang, damit Aufnahmebericht, Anfrage und
        // Erkennung dieselbe Korrelation nutzen. Ohne sie wird eine neue Kennung erzeugt.
        var correlationId = utterance.TurnId ?? SprechenGrpcCorrelationMetadata.CreateCorrelationId();

        try
        {
            await _trace.SpeechToTextStartedAsync(correlationId, utterance.Chunks.Count, languageCode).ConfigureAwait(false);
            using var call = _languageClient.SpeechToText(
                headers: BuildHeaders(correlationId),
                cancellationToken: cancellationToken);
            await WriteChunksAsync(call.RequestStream, utterance.Chunks, utterance.MimeType, languageCode, recognitionProfile, keyterms, _diagnosticOperatingMode?.IsDiagnostic == true, cancellationToken).ConfigureAwait(false);
            var response = await call.ResponseAsync.WaitAsync(cancellationToken).ConfigureAwait(false);
            var transcript = _responseMapper.MapSpeechTranscript(response);
            await _trace.SpeechToTextResultAsync(correlationId, transcript).ConfigureAwait(false);
            return transcript;
        }
        catch (RpcException ex)
        {
            var mappedException = SpeechErrorExceptionFactory.Create(ex, cancellationToken);
            if (mappedException is SpeechErrorException speechError)
            {
                await _trace.SpeechToTextFailedAsync(correlationId, speechError.ErrorCode, ex.StatusCode).ConfigureAwait(false);
            }

            throw mappedException;
        }
    }

    private static async Task WriteChunksAsync(
        IClientStreamWriter<AudioChunk> requestStream,
        IReadOnlyList<byte[]> chunks,
        SpeechAudioMimeType mimeType,
        string? languageCode,
        AppRecognitionProfile recognitionProfile,
        SpeechKeyterms? keyterms,
        bool isDiagnostic,
        CancellationToken cancellationToken)
    {
        await new GrpcAudioChunkSendLoop()
            .WriteAsync(chunks, requestStream, mimeType, cancellationToken, languageCode, recognitionProfile, keyterms, isDiagnostic)
            .ConfigureAwait(false);
    }

}
