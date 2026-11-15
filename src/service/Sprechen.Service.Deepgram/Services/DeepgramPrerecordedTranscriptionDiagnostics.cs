using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Sprechen.Service.Deepgram.Services;

/// <summary>
/// Protokolliert übersprungene, abgeschlossene und fehlgeschlagene REST-Transkriptionen.
/// Anfrageschema und Begriffszähler werden vom aufrufenden Client übernommen.
/// </summary>
internal sealed class DeepgramPrerecordedTranscriptionDiagnostics
{
    private readonly ILogger<DeepgramPrerecordedTranscriptionClient> _logger;
    private readonly string _model;

    internal DeepgramPrerecordedTranscriptionDiagnostics(
        ILogger<DeepgramPrerecordedTranscriptionClient> logger,
        string model)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _model = string.IsNullOrWhiteSpace(model)
            ? throw new ArgumentException("Deepgram model is required.", nameof(model))
            : model;
    }

    internal void LogSkippedEmptyAudio(
        AudioMimeType mime,
        string? languageCode,
        double readMs,
        double totalMs,
        string? clientCorrelationId)
    {
        _logger.LogDebug(
            "Deepgram prerecorded transcription skipped empty audio segment (mimeType={MimeType}, languageCode={LanguageCode}, model={Model}, readMs={ReadMs:F1}, totalMs={TotalMs:F1}, clientCorrelationId={ClientCorrelationId}).",
            mime,
            FormatLanguageCode(languageCode),
            _model,
            readMs,
            totalMs,
            FormatClientCorrelationId(clientCorrelationId));
    }

    internal void LogSkippedShortPcm(
        int audioBytes,
        int audioDurationMs,
        int minimumDurationMs,
        string? languageCode,
        double readMs,
        double totalMs,
        string? clientCorrelationId)
    {
        _logger.LogDebug(
            "Deepgram prerecorded transcription skipped short PCM segment (audioBytes={AudioBytes}, audioDurationMs={AudioDurationMs}, minimumDurationMs={MinimumDurationMs}, languageCode={LanguageCode}, model={Model}, readMs={ReadMs:F1}, totalMs={TotalMs:F1}, clientCorrelationId={ClientCorrelationId}).",
            audioBytes,
            audioDurationMs,
            minimumDurationMs,
            FormatLanguageCode(languageCode),
            _model,
            readMs,
            totalMs,
            FormatClientCorrelationId(clientCorrelationId));
    }

    internal void LogCompleted(
        AudioMimeType mime,
        string? languageCode,
        int audioBytes,
        int payloadBytes,
        int? audioDurationMs,
        double readMs,
        double prepareMs,
        double providerMs,
        double totalMs,
        RecognitionProfile recognitionProfile,
        DeepgramPrerecordedSchemaComposition schemaComposition,
        string outgoingKeytermListJson,
        string? deepgramRequestId,
        SpeechTranscript transcript,
        string? clientCorrelationId)
    {
        _logger.LogInformation(
            "Deepgram prerecorded transcription completed (mimeType={MimeType}, languageCode={LanguageCode}, model={Model}, recognitionProfile={RecognitionProfile}, audioBytes={AudioBytes}, payloadBytes={PayloadBytes}, audioDurationMs={AudioDurationMs}, readMs={ReadMs:F1}, prepareMs={PrepareMs:F1}, providerMs={ProviderMs:F1}, totalMs={TotalMs:F1}, transcriptLength={TranscriptLength}, confidencePercent={ConfidencePercent}, keytermParameterPresent={KeytermParameterPresent}, keytermCount={KeytermCount}, keytermList={KeytermList}, confirmationKeytermsApplied={ConfirmationKeytermsApplied}, confirmationKeytermCount={ConfirmationKeytermCount}, contextKeytermsRequested={ContextKeytermsRequested}, contextKeytermRequestedCount={ContextKeytermRequestedCount}, contextKeytermsApplied={ContextKeytermsApplied}, contextKeytermAppliedCount={ContextKeytermAppliedCount}, deepgramRequestId={DeepgramRequestId}, clientCorrelationId={ClientCorrelationId}, traceId={TraceId}).",
            mime,
            FormatLanguageCode(languageCode),
            _model,
            recognitionProfile,
            audioBytes,
            payloadBytes,
            audioDurationMs,
            readMs,
            prepareMs,
            providerMs,
            totalMs,
            transcript.Text.Length,
            transcript.ConfidencePercent,
            schemaComposition.KeytermParameterPresent,
            schemaComposition.KeytermCount,
            outgoingKeytermListJson,
            schemaComposition.ConfirmationKeytermsApplied,
            schemaComposition.ConfirmationKeytermCount,
            schemaComposition.ContextKeytermsRequested,
            schemaComposition.ContextKeytermRequestedCount,
            schemaComposition.ContextKeytermsApplied,
            schemaComposition.ContextKeytermAppliedCount,
            FormatDeepgramRequestId(deepgramRequestId),
            FormatClientCorrelationId(clientCorrelationId),
            ResolveTraceId());
    }

    internal void LogFailed(
        AudioMimeType mime,
        string? languageCode,
        int audioBytes,
        int payloadBytes,
        RecognitionProfile recognitionProfile,
        DeepgramPrerecordedSchemaComposition schemaComposition,
        string outgoingKeytermListJson,
        double providerMs,
        double totalMs,
        string failureKind,
        string? deepgramRequestId,
        string? clientCorrelationId)
    {
        _logger.LogInformation(
            "Deepgram prerecorded transcription failed (mimeType={MimeType}, languageCode={LanguageCode}, model={Model}, recognitionProfile={RecognitionProfile}, audioBytes={AudioBytes}, payloadBytes={PayloadBytes}, providerMs={ProviderMs:F1}, totalMs={TotalMs:F1}, failureKind={FailureKind}, keytermParameterPresent={KeytermParameterPresent}, keytermCount={KeytermCount}, keytermList={KeytermList}, confirmationKeytermsApplied={ConfirmationKeytermsApplied}, confirmationKeytermCount={ConfirmationKeytermCount}, contextKeytermsRequested={ContextKeytermsRequested}, contextKeytermRequestedCount={ContextKeytermRequestedCount}, contextKeytermsApplied={ContextKeytermsApplied}, contextKeytermAppliedCount={ContextKeytermAppliedCount}, deepgramRequestId={DeepgramRequestId}, clientCorrelationId={ClientCorrelationId}, traceId={TraceId}).",
            mime,
            FormatLanguageCode(languageCode),
            _model,
            recognitionProfile,
            audioBytes,
            payloadBytes,
            providerMs,
            totalMs,
            failureKind,
            schemaComposition.KeytermParameterPresent,
            schemaComposition.KeytermCount,
            outgoingKeytermListJson,
            schemaComposition.ConfirmationKeytermsApplied,
            schemaComposition.ConfirmationKeytermCount,
            schemaComposition.ContextKeytermsRequested,
            schemaComposition.ContextKeytermRequestedCount,
            schemaComposition.ContextKeytermsApplied,
            schemaComposition.ContextKeytermAppliedCount,
            FormatDeepgramRequestId(deepgramRequestId),
            FormatClientCorrelationId(clientCorrelationId),
            ResolveTraceId());
    }

    private static string FormatLanguageCode(string? languageCode)
        => string.IsNullOrWhiteSpace(languageCode) ? "auto" : languageCode.Trim();

    private static string FormatDeepgramRequestId(string? requestId)
        => string.IsNullOrWhiteSpace(requestId) ? "<none>" : requestId.Trim();

    private static string FormatClientCorrelationId(string? clientCorrelationId)
        => string.IsNullOrWhiteSpace(clientCorrelationId) ? "<none>" : clientCorrelationId.Trim();

    private static string ResolveTraceId()
        => Activity.Current?.TraceId.ToString() ?? "<none>";
}
