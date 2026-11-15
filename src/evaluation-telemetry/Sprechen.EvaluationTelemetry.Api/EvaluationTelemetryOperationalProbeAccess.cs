using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Sprechen.EvaluationTelemetry.Ingestion;

namespace Sprechen.EvaluationTelemetry.Api;

/// <summary>
/// Gibt einem neuen Testlauf eine Kennung und die Bedingung des Hosts.
/// Die Kennung dient der Zuordnung des Laufs.
/// </summary>
internal sealed class EvaluationTelemetryOperationalProbeAccess(
    IOptions<EvaluationTelemetryOperationalProbeAdmissionOptions> options,
    EvaluationTelemetryIngressCondition ingressCondition)
{
    private readonly EvaluationTelemetryOperationalProbeAdmissionOptions _options =
        options?.Value ?? throw new ArgumentNullException(nameof(options));
    private readonly EvaluationTelemetryIngressCondition _ingressCondition =
        ingressCondition ?? throw new ArgumentNullException(nameof(ingressCondition));

    public bool IsEnabled => _options.IsEnabled;

    public IResult CreateContext(HttpRequest request, HttpResponse response)
    {
        response.Headers.CacheControl = "no-store";
        if (!IsEnabled)
        {
            return Results.NotFound();
        }

        if (request.Query.Count != 0)
        {
            return Results.BadRequest();
        }

        return Results.Json(new
        {
            probe_id = $"probe-{Guid.NewGuid():N}",
            condition = _ingressCondition.CanonicalValue,
        });
    }
}
