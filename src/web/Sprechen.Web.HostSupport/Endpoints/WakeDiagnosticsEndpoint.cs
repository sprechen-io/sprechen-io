using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Sprechen.Web.HostSupport.Endpoints;

// Clientberichte zu fehlgeschlagenen Sprachaktivierungen ergänzen die Dienstprotokolle um Erkennungs- und Regeldiagnosen.
// Der Endpunkt übernimmt nur die unten aufgeführten Feldnamen. Die Auswahl prüft nicht den Inhalt ihrer Werte.
/// <summary>
/// Nimmt Diagnoseberichte zu fehlgeschlagenen Sprachaktivierungen entgegen und protokolliert ausgewählte Felder.
/// </summary>
internal static class WakeDiagnosticsEndpoint
{
    internal static void MapWakeDiagnosticsEndpoint(this WebApplication app)
    {
        app.MapPost("/api/wake-diagnostics", (
            HttpRequest request,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
            HandleAsync(request, loggerFactory, cancellationToken))
            .AllowAnonymous().DisableAntiforgery();
    }

    /// <summary>
    /// Erlaubte Feldnamen für Aktivierungsberichte.
    /// Vorgesehen sind Messwerte, Kategorien, Aktivierungsformen und Korrelationskennungen, keine Transkripte.
    /// Fehlende Felder werden ausgelassen. Die Werte freigegebener Felder werden hier nicht inhaltlich geprüft.
    /// </summary>
    private static readonly string[] LoggableFields =
    [
        "event",
        "ts",
        "transcriptLength",
        "normalizedTranscriptLength",
        "tokenCount",
        "evaluationStatus",
        "confidencePercent",
        "activationForms",
        "recognitionOnlyActivationForms",
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
            return Results.BadRequest(new { error = "E_WAKE_DIAGNOSTICS_PAYLOAD_INVALID" });
        }

        using (doc)
        {
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return Results.BadRequest(new { error = "E_WAKE_DIAGNOSTICS_PAYLOAD_INVALID" });
            }

            loggerFactory
                .CreateLogger("Sprechen.Voice.WakeDiagnostics")
                .LogInformation("wake_miss_report {report}", DiagnosticsReportProjection.Project(doc.RootElement, LoggableFields));

            return Results.NoContent();
        }
    }
}
