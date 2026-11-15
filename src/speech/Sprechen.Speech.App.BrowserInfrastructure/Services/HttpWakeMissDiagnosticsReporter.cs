using System.Net.Http.Json;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.App.BrowserInfrastructure.Services;

/// <summary>
/// Sendet Berichte über ausgebliebene Aktivierungen ohne Gerätefreigabe,
/// damit Aktivierungsfehler auch ohne zuvor eingeschaltete Browserdiagnose untersuchbar sind.
/// </summary>
/// <remarks>
/// Die Nutzlast enthält Längen, Tokenanzahl, Konfidenz, Auswertungsstatus und erwartete
/// Aktivierungsformen, aber kein erkanntes Transkript. Damit lassen sich Erkennungs- und
/// Zuordnungsprobleme untersuchen, ohne die erkannte Äußerung zu übertragen.
/// Ein Versandfehler darf die Hörschleife nicht unterbrechen.
/// </remarks>
internal sealed class HttpWakeMissDiagnosticsReporter : IWakeMissDiagnosticsReporter
{
    private const string Endpoint = "/api/wake-diagnostics";

    private readonly HttpClient _httpClient;
    private readonly IDiagnosticOperatingMode _operatingMode;

    public HttpWakeMissDiagnosticsReporter(HttpClient httpClient, IDiagnosticOperatingMode operatingMode)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _operatingMode = operatingMode ?? throw new ArgumentNullException(nameof(operatingMode));
    }

    public async ValueTask ReportAsync(WakeMissDiagnosticsReport report, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(report);

        try
        {
            var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["event"] = "wake_miss_report",
                ["ts"] = DateTimeOffset.UtcNow.ToString("O"),
                ["transcriptLength"] = report.TranscriptLength,
                ["normalizedTranscriptLength"] = report.NormalizedTranscriptLength,
                ["tokenCount"] = report.TokenCount,
                ["evaluationStatus"] = report.EvaluationStatus,
                ["confidencePercent"] = report.ConfidencePercent,
                ["activationForms"] = report.ActivationForms,
                ["recognitionOnlyActivationForms"] = report.RecognitionOnlyActivationForms
            };
            if (_operatingMode.IsDiagnostic)
            {
                payload["sessionId"] = _operatingMode.SessionId;
                payload["clientCorrelationId"] = _operatingMode.ClientCorrelationId;
                payload["operatingMode"] = _operatingMode.OperatingModeMarker;
            }

            using var response = await _httpClient
                .PostAsJsonAsync(Endpoint, payload, cancellationToken)
                .ConfigureAwait(false);
        }
        catch
        {
            // Ein Fehler der optionalen Diagnose darf die Hörschleife nicht unterbrechen.
        }
    }
}
