using System.Net.Http.Json;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.App.BrowserInfrastructure.Services;

/// <summary>
/// Sendet technische Aufnahmeberichte zur Fehlerdiagnose ohne erkanntes Sprachtranskript.
/// Der Diagnosemodus gibt sie frei. Sonst entscheidet die Diagnosefreigabe des Geräts.
/// </summary>
/// <remarks>
/// Die Nutzlast enthält technische Messwerte und Fehlermerkmale statt des Sprachinhalts.
/// Im normalen Betrieb genügt die Gerätefreigabe, die nur bei ausdrücklicher Deaktivierung sperrt.
/// Fehler beim Versand werden abgefangen, damit der Aufnahmeablauf weitergehen kann.
/// </remarks>
internal sealed class HttpCaptureDiagnosticsReporter : ICaptureDiagnosticsReporter
{
    private const string Endpoint = "/api/capture-diagnostics";

    private readonly HttpClient _httpClient;
    private readonly IBrowserDebugConsole _browserDebugConsole;
    private readonly IDiagnosticOperatingMode _operatingMode;
    private readonly ICaptureTrackProcessingReader _trackProcessing;

    public HttpCaptureDiagnosticsReporter(
        HttpClient httpClient,
        IBrowserDebugConsole browserDebugConsole,
        IDiagnosticOperatingMode operatingMode,
        ICaptureTrackProcessingReader? trackProcessing = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _browserDebugConsole = browserDebugConsole ?? throw new ArgumentNullException(nameof(browserDebugConsole));
        _operatingMode = operatingMode ?? throw new ArgumentNullException(nameof(operatingMode));
        _trackProcessing = trackProcessing ?? UnknownCaptureTrackProcessingReader.Instance;
    }

    public async ValueTask ReportAsync(CaptureDiagnosticsReport report, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(report);

        try
        {
            if (!_operatingMode.IsDiagnostic
                && !await _browserDebugConsole.GetDiagnosticsAdmittedAsync(cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            var window = report.Window ?? new CaptureWindowObservation(0, false, 0);
            var processing = await _trackProcessing.ReadAsync(cancellationToken).ConfigureAwait(false);
            var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["event"] = "capture_report",
                ["captureSeq"] = report.CaptureSeq,
                ["ts"] = DateTimeOffset.UtcNow.ToString("O"),
                // Die zufällige Zugkennung verbindet Bericht und Erkennungsanfrage
                // derselben Aufnahme, ohne Teilnehmende zu identifizieren.
                ["turnId"] = report.TurnId,
                ["outcome"] = report.Outcome,
                ["armedToOutcomeMs"] = Math.Round(report.ArmedToOutcomeMs, 1),
                ["captureMode"] = report.CaptureMode,
                ["startContext"] = report.StartContext,
                ["capturedChunks"] = report.CapturedChunks,
                ["peakAmplitude"] = report.PeakAmplitude,
                ["estimatedFloor"] = report.EstimatedFloor,
                ["appliedOnsetThreshold"] = report.AppliedOnsetThreshold,
                ["faultKind"] = report.FaultKind,
                ["observedFrames"] = window.ObservedFrames,
                ["startCursorHandedOff"] = window.StartCursorHandedOff,
                ["lookbackBytes"] = window.LookbackBytes,
                ["echoCancellation"] = processing.EchoCancellation,
                ["noiseSuppression"] = processing.NoiseSuppression,
                ["autoGainControl"] = processing.AutoGainControl
            };
            StampDiagnosticIdentity(payload);

            using var response = await _httpClient
                .PostAsJsonAsync(Endpoint, payload, cancellationToken)
                .ConfigureAwait(false);
        }
        catch
        {
            // Ein Fehler der optionalen Diagnose darf den aufrufenden Ablauf nicht unterbrechen.
        }
    }

    private void StampDiagnosticIdentity(Dictionary<string, object?> payload)
    {
        if (!_operatingMode.IsDiagnostic)
        {
            return;
        }

        payload["sessionId"] = _operatingMode.SessionId;
        payload["clientCorrelationId"] = _operatingMode.ClientCorrelationId;
        payload["operatingMode"] = _operatingMode.OperatingModeMarker;
    }
}
