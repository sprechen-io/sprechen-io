using System.Net.Http.Json;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.App.BrowserInfrastructure.Services;

/// <summary>
/// Sendet strukturierte Dialogentscheidungen nur im Diagnosemodus an den Host,
/// damit die entfernte Entscheidungsspur an diese Sitzungsfreigabe gebunden bleibt.
/// </summary>
internal sealed class HttpDialogDecisionDiagnosticsReporter : IDialogDecisionSink
{
    private const string Endpoint = "/api/dialog-decision-diagnostics";

    private readonly HttpClient _httpClient;
    private readonly IDiagnosticOperatingMode _operatingMode;

    public HttpDialogDecisionDiagnosticsReporter(
        HttpClient httpClient,
        IDiagnosticOperatingMode operatingMode)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _operatingMode = operatingMode ?? throw new ArgumentNullException(nameof(operatingMode));
    }

    public async ValueTask RecordAsync(DialogDecision decision, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(decision);

        if (!_operatingMode.IsDiagnostic)
        {
            return;
        }

        try
        {
            var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["event"] = "dialog_decision",
                ["name"] = decision.Name,
                ["fields"] = decision.Fields
                    .Select(field => new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["name"] = field.Name,
                        ["value"] = field.Value
                    })
                    .ToArray(),
                ["sessionId"] = _operatingMode.SessionId,
                ["clientCorrelationId"] = _operatingMode.ClientCorrelationId,
                ["operatingMode"] = _operatingMode.OperatingModeMarker
            };

            using var response = await _httpClient
                .PostAsJsonAsync(Endpoint, payload, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            // Ein Fehler der optionalen Diagnose darf den aufrufenden Ablauf nicht unterbrechen.
        }
    }
}
