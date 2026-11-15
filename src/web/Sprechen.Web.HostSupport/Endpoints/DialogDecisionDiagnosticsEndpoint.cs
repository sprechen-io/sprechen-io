using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Sprechen.Web.HostSupport.Endpoints;

/// <summary>
/// Protokolliert Dialogentscheidungen über einen eigenen Endpunkt mit einer getrennten Liste erlaubter Felder.
/// </summary>
internal static class DialogDecisionDiagnosticsEndpoint
{
    internal static void MapDialogDecisionDiagnosticsEndpoint(this WebApplication app)
    {
        app.MapPost("/api/dialog-decision-diagnostics", (
            HttpRequest request,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
            HandleAsync(request, loggerFactory, cancellationToken))
            .AllowAnonymous().DisableAntiforgery();
    }


    private static readonly string[] LoggableFields =
    [
        "event",
        "name",
        "fields",
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
            return Results.BadRequest(new { error = "E_DIALOG_DECISION_DIAGNOSTICS_PAYLOAD_INVALID" });
        }

        using (doc)
        {
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return Results.BadRequest(new { error = "E_DIALOG_DECISION_DIAGNOSTICS_PAYLOAD_INVALID" });
            }

            loggerFactory
                .CreateLogger("Sprechen.Voice.DialogDecisionDiagnostics")
                .LogInformation("dialog_decision {report}", DiagnosticsReportProjection.Project(doc.RootElement, LoggableFields));

            return Results.NoContent();
        }
    }
}
