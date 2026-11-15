using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Sprechen.Web.HostSupport.Endpoints;

// Erfassungsberichte ergänzen die Dienstprotokolle um clientseitige Aufnahmeversuche, die keinen Sprachaufruf ausgelöst haben.
// Der Endpunkt protokolliert nur die unten aufgeführten Felder. Die Aktivierung des Sendens entscheidet der Client.
/// <summary>
/// Nimmt Diagnoseberichte zu clientseitigen Aufnahmeversuchen entgegen und protokolliert ausgewählte Felder.
/// </summary>
internal static class CaptureDiagnosticsEndpoint
{
    internal static void MapCaptureDiagnosticsEndpoint(this WebApplication app)
    {
        app.MapPost("/api/capture-diagnostics", (
            HttpRequest request,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
            HandleAsync(request, loggerFactory, cancellationToken))
            .AllowAnonymous().DisableAntiforgery();
    }

    /// <summary>
    /// Erlaubte Feldnamen für das Betriebsprotokoll. Zusätzliche Felder werden verworfen.
    /// Die Liste filtert Feldnamen, nicht Feldwerte.
    /// </summary>
    private static readonly string[] LoggableFields =
    [
        "event",
        "captureSeq",
        "ts",
        "turnId",
        "outcome",
        "armedToOutcomeMs",
        "captureMode",
        "startContext",
        "capturedChunks",
        "peakAmplitude",
        "estimatedFloor",
        "appliedOnsetThreshold",
        // Für den Ausnahmetyp vorgesehen. Der Endpunkt prüft den Inhalt dieses Feldes nicht.
        "faultKind",
        "observedFrames",
        "startCursorHandedOff",
        "lookbackBytes",
        "echoCancellation",
        "noiseSuppression",
        "autoGainControl",
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
            return Results.BadRequest(new { error = "E_CAPTURE_DIAGNOSTICS_PAYLOAD_INVALID" });
        }

        using (doc)
        {
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return Results.BadRequest(new { error = "E_CAPTURE_DIAGNOSTICS_PAYLOAD_INVALID" });
            }

            loggerFactory
                .CreateLogger("Sprechen.Voice.CaptureDiagnostics")
                .LogInformation("capture_diagnostic {report}", DiagnosticsReportProjection.Project(doc.RootElement, LoggableFields));

            return Results.NoContent();
        }
    }
}
