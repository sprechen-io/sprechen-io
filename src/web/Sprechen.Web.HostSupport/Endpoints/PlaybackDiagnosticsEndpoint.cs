using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Sprechen.Web.HostSupport.Endpoints;

// Clientberichte ergänzen die Dienstprotokolle um Wiedergabezähler, AudioContext-Zustand und Sichtbarkeitswechsel.
// Der Endpunkt protokolliert die ausgewählten Felder. Die Aktivierung des Sendens entscheidet der Client.
/// <summary>
/// Nimmt Diagnoseberichte des Players entgegen, um Wiedergabeprobleme serverseitig untersuchen zu können.
/// </summary>
internal static class PlaybackDiagnosticsEndpoint
{
    internal static void MapPlaybackDiagnosticsEndpoint(this WebApplication app)
    {
        app.MapPost("/api/playback-diagnostics", (
            HttpRequest request,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
            HandleAsync(request, loggerFactory, cancellationToken))
            .AllowAnonymous().DisableAntiforgery();
    }


    private static readonly string[] LoggableFields =
    [
        "event",
        "utteranceSeq",
        "ts",
        "writtenSeconds",
        "playedOutSeconds",
        "underruns",
        "underrunOutputFrames",
        "silentOutputWindows",
        "maxContiguousSilentMs",
        "outputSampleRate",
        "keepAlive",
        "playbackPath",
        "streamingSinkStatus",
        "firstContentLeadMs",
        "firstContentCtxState",
        "droppedFrames",
        "starvedQuanta",
        "onsetPeakDbfs",
        "onsetFrames",
        "drainOutcome",
        "drainMs",
        "ctxState",
        "ctxStateEvents",
        "visibilityEvents",
        "sessionId",
        "clientCorrelationId",
        "operatingMode",
    ];
    private static async Task<IResult> HandleAsync(
        HttpRequest request,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        JsonDocument doc;
        try
        {
            doc = await JsonDocument.ParseAsync(request.Body, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (JsonException)
        {
            return Results.BadRequest(new { error = "E_PLAYBACK_DIAGNOSTICS_PAYLOAD_INVALID" });
        }

        using (doc)
        {
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return Results.BadRequest(new { error = "E_PLAYBACK_DIAGNOSTICS_PAYLOAD_INVALID" });
            }

            loggerFactory
                .CreateLogger("Sprechen.Voice.PlaybackDiagnostics")
                .LogInformation("playback_diagnostic {report}", DiagnosticsReportProjection.Project(doc.RootElement, LoggableFields));

            return Results.NoContent();
        }
    }
}
