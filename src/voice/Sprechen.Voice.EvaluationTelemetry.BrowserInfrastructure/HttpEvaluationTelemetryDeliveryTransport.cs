using System.Net;
using System.Text.Json;
using Microsoft.JSInterop;
using Sprechen.Mda.Contracts.V1.Telemetry;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.VoiceAssistant.EvaluationTelemetry;

namespace Sprechen.Voice.EvaluationTelemetry.BrowserInfrastructure;

/// <summary>
/// Versendet Telemetrie per Browser-POST und ordnet Fehler nach Wiederholbarkeit ein,
/// damit der Aufrufer über einen weiteren Versandversuch entscheiden kann.
/// </summary>
internal sealed class HttpEvaluationTelemetryDeliveryTransport : IEvaluationTelemetryDeliveryTransport
{
    private const string TelemetryEndpoint = "/api/telemetry";
    private const string DeliverIdentifier = "evaluationTelemetryOutbox.deliver";
    internal static readonly TimeSpan DefaultRequestTimeout = TimeSpan.FromSeconds(15);

    private readonly IJSRuntime _javascript;
    private readonly IOperationalProbeSession _operationalProbeSession;
    private readonly TimeSpan _requestTimeout;

    public HttpEvaluationTelemetryDeliveryTransport(
        IJSRuntime javascript,
        IOperationalProbeSession operationalProbeSession)
        : this(javascript, operationalProbeSession, DefaultRequestTimeout)
    {
    }

    internal HttpEvaluationTelemetryDeliveryTransport(
        IJSRuntime javascript,
        IOperationalProbeSession operationalProbeSession,
        TimeSpan requestTimeout)
    {
        _javascript = javascript ?? throw new ArgumentNullException(nameof(javascript));
        _operationalProbeSession = operationalProbeSession
            ?? throw new ArgumentNullException(nameof(operationalProbeSession));
        if (requestTimeout < TimeSpan.FromMilliseconds(1))
        {
            throw new ArgumentOutOfRangeException(nameof(requestTimeout));
        }

        _requestTimeout = requestTimeout;
    }

    public async Task<EvaluationTelemetryDeliveryResult> DeliverAsync(
        string body,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(body);

        try
        {
            using var document = JsonDocument.Parse(body);
            var envelope = document.RootElement;
            var provenance = envelope.GetProperty(TelemetryEnvelopeFieldNames.RecordProvenance).GetString();
            var isProbe = provenance == TelemetryRecordProvenances.OperationalProbe;
            if (isProbe ? !_operationalProbeSession.Matches(
                    envelope.GetProperty(TelemetryEnvelopeFieldNames.ParticipantId).GetString()!,
                    envelope.GetProperty(TelemetryEnvelopeFieldNames.Condition).GetString()!,
                    envelope.GetProperty(TelemetryEnvelopeFieldNames.ApplicationId).GetString()!,
                    envelope.GetProperty(TelemetryEnvelopeFieldNames.TaskId).GetString()!)
                : _operationalProbeSession.IsRequested || provenance is not (TelemetryRecordProvenances.Participant or TelemetryRecordProvenances.Demonstration))
            {
                return new EvaluationTelemetryDeliveryResult(EvaluationTelemetryDeliveryStatus.PermanentFailure, 403);
            }

            var route = isProbe ? TelemetryEndpoint + "/probe" : TelemetryEndpoint;
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(_requestTimeout);
            var browserResult = await _javascript.InvokeAsync<EvaluationTelemetryBrowserDeliveryResult>(
                    DeliverIdentifier,
                    timeout.Token,
                    [route, body, (int)Math.Clamp(_requestTimeout.TotalMilliseconds, 1, int.MaxValue)])
                .ConfigureAwait(false);
            if (browserResult is null)
            {
                return new EvaluationTelemetryDeliveryResult(EvaluationTelemetryDeliveryStatus.TransientFailure);
            }

            var numericStatusCode = browserResult.Status;
            var retryAfter = ReadRetryAfter(browserResult.RetryAfterMs);
            return (HttpStatusCode)numericStatusCode switch
            {
                HttpStatusCode.NoContent => new EvaluationTelemetryDeliveryResult(
                    EvaluationTelemetryDeliveryStatus.Accepted,
                    numericStatusCode),
                _ when numericStatusCode is 408 or 425 or 429 =>
                    new EvaluationTelemetryDeliveryResult(
                        EvaluationTelemetryDeliveryStatus.TransientFailure,
                        numericStatusCode,
                        retryAfter),
                >= HttpStatusCode.InternalServerError => new EvaluationTelemetryDeliveryResult(
                    EvaluationTelemetryDeliveryStatus.TransientFailure,
                    numericStatusCode,
                    retryAfter),
                _ => new EvaluationTelemetryDeliveryResult(
                    EvaluationTelemetryDeliveryStatus.PermanentFailure,
                    numericStatusCode),
            };
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            return new EvaluationTelemetryDeliveryResult(EvaluationTelemetryDeliveryStatus.PermanentFailure, 400);
        }
        catch (JSException)
        {
            return new EvaluationTelemetryDeliveryResult(EvaluationTelemetryDeliveryStatus.TransientFailure);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new EvaluationTelemetryDeliveryResult(EvaluationTelemetryDeliveryStatus.TransientFailure);
        }
    }

    private static TimeSpan? ReadRetryAfter(double? retryAfterMs)
    {
        if (retryAfterMs is not { } milliseconds || !double.IsFinite(milliseconds))
        {
            return null;
        }

        var delay = TimeSpan.FromMilliseconds(Math.Max(0, milliseconds));
        return delay;
    }
}
