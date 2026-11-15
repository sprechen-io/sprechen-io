using System.Net.Http.Headers;
using System.Text.Json;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Voice.EvaluationTelemetry.BrowserInfrastructure;

/// <summary>
/// Lädt Kennung und Bedingung eines Testlaufs vom Host,
/// damit die folgenden Telemetrieereignisse diesem Lauf zugeordnet werden können.
/// </summary>
internal sealed class HttpOperationalProbeBootstrapClient(HttpClient httpClient) : IOperationalProbeBootstrapClient
{
    public async Task<OperationalProbeContext> BootstrapAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/telemetry/probe/context");
        request.Headers.CacheControl = new CacheControlHeaderValue { NoStore = true };
        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        if (response.Headers.CacheControl?.NoStore != true)
        {
            throw new InvalidDataException("The operational bootstrap must not be cached.");
        }

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 2
            || !root.TryGetProperty("probe_id", out var probeId) || probeId.ValueKind != JsonValueKind.String
            || !root.TryGetProperty("condition", out var condition) || condition.ValueKind != JsonValueKind.String)
        {
            throw new InvalidDataException("The operational bootstrap response is invalid.");
        }

        return new OperationalProbeContext(probeId.GetString()!, condition.GetString()!);
    }
}
