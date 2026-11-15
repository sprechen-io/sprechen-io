using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sprechen.EvaluationTelemetry.Ingestion;

namespace Sprechen.EvaluationTelemetry.Api;

/// <summary>
/// Registriert die Telemetrie-Endpunkte und verhindert, dass Fehlerseiten
/// Antworten für deaktivierte Testrouten oder unberechtigte Archivzugriffe ersetzen.
/// </summary>
public static class EvaluationTelemetryApiApplicationExtensions
{
    public static WebApplication UseEvaluationTelemetryApi(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseRateLimiter();

        app.MapPost(EvaluationTelemetryApiContract.IngestionRoute, AcceptEventAsync)
            .AllowAnonymous()
            .DisableAntiforgery()
            .RequireRateLimiting(EvaluationTelemetryApiServiceCollectionExtensions.IngestionRateLimitPolicy);

        // Deaktivierte Testrouten und unberechtigte Archivzugriffe liefern 404 ohne Antwortinhalt.
        // Die StatusCodePages-Middleware darf diese Antwort nicht durch eine Fehlerseite ersetzen.
        app.MapPost(EvaluationTelemetryApiContract.OperationalProbeIngestionRoute, AcceptProbeEventAsync)
            .AllowAnonymous()
            .DisableAntiforgery()
            .ExcludeFromDescription()
            .WithMetadata(new SkipStatusCodePagesAttribute())
            .RequireRateLimiting(EvaluationTelemetryApiServiceCollectionExtensions.ProbeIngestionRateLimitPolicy);

        app.MapGet(EvaluationTelemetryApiContract.OperationalProbeContextRoute,
                (HttpRequest request, HttpResponse response, EvaluationTelemetryOperationalProbeAccess access) =>
                    access.CreateContext(request, response))
            .AllowAnonymous()
            .ExcludeFromDescription()
            .WithMetadata(new SkipStatusCodePagesAttribute())
            .RequireRateLimiting(EvaluationTelemetryApiServiceCollectionExtensions.ProbeIngestionRateLimitPolicy);

        app.MapGet(EvaluationTelemetryApiContract.ArchiveRoute, CreateArchiveAsync)
            .AllowAnonymous()
            .DisableAntiforgery()
            .ExcludeFromDescription()
            .WithMetadata(new SkipStatusCodePagesAttribute())
            .RequireRateLimiting(EvaluationTelemetryApiServiceCollectionExtensions.ArchiveRateLimitPolicy);

        return app;
    }

    private static async Task<IResult> AcceptEventAsync(
        HttpRequest request,
        IEvaluationTelemetryEventAcceptanceService acceptanceService,
        CancellationToken cancellationToken)
    {
        if (request.ContentLength > EvaluationTelemetryIngestionLimits.MaximumEnvelopeBytes)
        {
            return PayloadTooLarge();
        }

        var result = await acceptanceService.AcceptPublicAsync(request.Body, cancellationToken)
            .ConfigureAwait(false);
        return ToHttpResult(result);
    }

    private static async Task<IResult> AcceptProbeEventAsync(
        HttpRequest request,
        IEvaluationTelemetryEventAcceptanceService acceptanceService,
        EvaluationTelemetryOperationalProbeAccess access,
        CancellationToken cancellationToken)
    {
        if (!access.IsEnabled)
        {
            return Results.NotFound();
        }

        if (request.ContentLength > EvaluationTelemetryIngestionLimits.MaximumEnvelopeBytes)
        {
            return PayloadTooLarge();
        }

        var result = await acceptanceService.AcceptOperationalProbeAsync(
                request.Body,
                cancellationToken)
            .ConfigureAwait(false);
        return ToHttpResult(result);
    }

    private static async Task<IResult> CreateArchiveAsync(
        HttpRequest request,
        HttpResponse response,
        IEvaluationTelemetryArchiveReader archiveReader,
        IOptions<EvaluationTelemetryArchiveOptions> options,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var archiveOptions = options.Value;
        if (!archiveOptions.IsEnabled
            || !EvaluationTelemetrySecretKeyValidator.HasValidKey(
                request,
                EvaluationTelemetryApiContract.ArchiveReadKeyHeaderName,
                archiveOptions.ReadKey))
        {
            return Results.NotFound();
        }

        try
        {
            var snapshot = await archiveReader.CreateSnapshotAsync(cancellationToken).ConfigureAwait(false);
            response.RegisterForDisposeAsync(snapshot);
            response.Headers.CacheControl = "no-store";
            response.Headers.ETag = $"\"sha256:{snapshot.Sha256}\"";
            var fileName = $"evaluation-telemetry-{snapshot.GeneratedAtUtc:yyyyMMddTHHmmssZ}.zip";
            return Results.File(snapshot.Content, "application/zip", fileName);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            loggerFactory.CreateLogger("EvaluationTelemetryArchive")
                .LogError(exception, "Failed to create the evaluation telemetry archive snapshot.");
            return Results.Problem(
                title: "Evaluation telemetry archive unavailable.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }

    private static IResult PayloadTooLarge()
    {
        return Results.Json(
            new { error = TelemetryIngestionErrorCodes.PayloadTooLarge },
            statusCode: StatusCodes.Status413PayloadTooLarge);
    }

    private static IResult ToHttpResult(EvaluationTelemetryEventAcceptanceResult result)
    {
        return result.Status switch
        {
            EvaluationTelemetryEventAcceptanceStatus.Accepted
                or EvaluationTelemetryEventAcceptanceStatus.AlreadyAccepted => Results.NoContent(),
            EvaluationTelemetryEventAcceptanceStatus.Invalid =>
                Results.BadRequest(new { error = result.ErrorCode }),
            EvaluationTelemetryEventAcceptanceStatus.PayloadTooLarge => PayloadTooLarge(),
            EvaluationTelemetryEventAcceptanceStatus.Conflicting =>
                Results.Conflict(new { error = result.ErrorCode }),
            EvaluationTelemetryEventAcceptanceStatus.Unavailable =>
                Results.Problem(
                    title: "Evaluation telemetry store unavailable.",
                    detail: result.ErrorCode,
                    statusCode: StatusCodes.Status503ServiceUnavailable),
            _ => throw new InvalidOperationException(
                $"Unsupported telemetry acceptance status '{result.Status}'."),
        };
    }
}
