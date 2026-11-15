using System.Text.Json;
using System.Net;
using System.Net.Http;
using Sprechen.Speech.App.BrowserInfrastructure.Services;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.Runtime.Tests.TestDoubles;

namespace Sprechen.Speech.Runtime.Tests.Services;

/// <summary>
/// Prüft die Freigabe von Aufnahmeberichten und die übertragenen Diagnosefelder.
/// Ein aktiver Diagnosemodus erlaubt den Versand auch bei deaktivierter Gerätefreigabe;
/// ohne Diagnosemodus enthält die Ausgabe keine Sitzungskennung.
/// </summary>
public sealed class CaptureDiagnosticsAdmissionTests
{
    [Fact]
    public async Task WithoutAnyDeviceSetupAndOutsideAStudyRun_TheReportIsStillSent()
    {
        var handler = new CountingHttpMessageHandler();
        var reporter = CreateReporter(handler, diagnosticsAdmitted: true);

        await reporter.ReportAsync(CreateReport(), CancellationToken.None);

        Assert.Equal(1, handler.Requests);
    }

    [Fact]
    public async Task WhenTheFlagIsExplicitlyDisabled_NothingLeavesTheDevice()
    {
        // Ohne aktiven Diagnosemodus muss die deaktivierte Gerätefreigabe den Versand verhindern.
        var handler = new CountingHttpMessageHandler();
        var reporter = CreateReporter(handler, diagnosticsAdmitted: false);

        await reporter.ReportAsync(CreateReport(), CancellationToken.None);

        Assert.Equal(0, handler.Requests);
    }


    [Fact]
    public async Task TheSentPayloadCarriesTheFaultKind_BecauseTheServerLogsOnlyWhatItReceives()
    {
        // Die Fehlerart muss unter dem vereinbarten JSON-Feldnamen übertragen werden,
        // damit der getrennt geprüfte Empfänger sie auswerten kann.
        var handler = new CountingHttpMessageHandler();
        var reporter = CreateReporter(handler, diagnosticsAdmitted: true);

        await reporter.ReportAsync(
            new CaptureDiagnosticsReport(
                CaptureSeq: 1,
                Outcome: "failed",
                ArmedToOutcomeMs: 0,
                CaptureMode: "Listening",
                StartContext: "CleanListening",
                CapturedChunks: 0,
                PeakAmplitude: 0,
                FaultKind: "InvalidOperationException"),
            CancellationToken.None);

        Assert.NotNull(handler.LastBody);
        using var document = JsonDocument.Parse(handler.LastBody!);
        Assert.True(
            document.RootElement.TryGetProperty("faultKind", out var faultKind),
            "der gesendete Rumpf traegt kein faultKind: " + handler.LastBody);
        Assert.Equal("InvalidOperationException", faultKind.GetString());
    }

    [Fact]
    public async Task TheSentPayloadCarriesTheTurnId_BecauseTheRecognitionJoinReadsItFromTheReport()
    {
        // turnId verbindet den Aufnahmebericht mit dem zugehörigen Erkennungsergebnis.
        var handler = new CountingHttpMessageHandler();
        var reporter = CreateReporter(handler, diagnosticsAdmitted: true);

        await reporter.ReportAsync(
            new CaptureDiagnosticsReport(
                CaptureSeq: 1,
                Outcome: "speech",
                ArmedToOutcomeMs: 800,
                CaptureMode: "Listening",
                StartContext: "CleanListening",
                CapturedChunks: 12,
                PeakAmplitude: 700,
                TurnId: "6f2a1d4b8c3e4f5a9b0c1d2e3f405162"),
            CancellationToken.None);

        Assert.NotNull(handler.LastBody);
        using var document = JsonDocument.Parse(handler.LastBody!);
        Assert.True(
            document.RootElement.TryGetProperty("turnId", out var turnId),
            "der gesendete Rumpf traegt kein turnId: " + handler.LastBody);
        Assert.Equal("6f2a1d4b8c3e4f5a9b0c1d2e3f405162", turnId.GetString());
    }

    [Fact]
    public async Task TheSentPayloadCarriesTheWindowAndTrackProcessing_BecauseTheServerLogsOnlyWhatItReceives()
    {
        var handler = new CountingHttpMessageHandler();
        var processing = new FixedCaptureTrackProcessingReader(
            new CaptureTrackProcessing(EchoCancellation: true, NoiseSuppression: true, AutoGainControl: false));
        var reporter = CreateReporter(handler, diagnosticsAdmitted: true, trackProcessing: processing);

        await reporter.ReportAsync(
            new CaptureDiagnosticsReport(
                CaptureSeq: 1,
                Outcome: "digital_silence",
                ArmedToOutcomeMs: 3670,
                CaptureMode: "ShortConfirmation",
                StartContext: "AssistantPlaybackRecentlySettledIdle",
                CapturedChunks: 40,
                PeakAmplitude: 0,
                Window: new CaptureWindowObservation(40, true, 12000)),
            CancellationToken.None);

        Assert.NotNull(handler.LastBody);
        using var document = JsonDocument.Parse(handler.LastBody!);
        Assert.Equal(40, document.RootElement.GetProperty("observedFrames").GetInt32());
        Assert.True(document.RootElement.GetProperty("startCursorHandedOff").GetBoolean());
        Assert.Equal(12000, document.RootElement.GetProperty("lookbackBytes").GetInt32());
        Assert.True(document.RootElement.GetProperty("echoCancellation").GetBoolean());
        Assert.True(document.RootElement.GetProperty("noiseSuppression").GetBoolean());
        Assert.False(document.RootElement.GetProperty("autoGainControl").GetBoolean());
    }

    [Fact]
    public async Task TheSentPayloadCarriesFloorAndAppliedThreshold_BecauseThePeakIsMeaninglessWithoutThem()
    {
        var handler = new CountingHttpMessageHandler();
        var reporter = CreateReporter(handler, diagnosticsAdmitted: true);

        await reporter.ReportAsync(
            new CaptureDiagnosticsReport(
                CaptureSeq: 1,
                Outcome: "no_speech",
                ArmedToOutcomeMs: 1200,
                CaptureMode: "Listening",
                StartContext: "CleanListening",
                CapturedChunks: 183,
                PeakAmplitude: 288,
                EstimatedFloor: 40,
                AppliedOnsetThreshold: 350),
            CancellationToken.None);

        Assert.NotNull(handler.LastBody);
        using var document = JsonDocument.Parse(handler.LastBody!);
        Assert.Equal(40, document.RootElement.GetProperty("estimatedFloor").GetInt32());
        Assert.Equal(350, document.RootElement.GetProperty("appliedOnsetThreshold").GetInt32());
    }

    [Fact]
    public async Task WhenTheFlagIsDisabledButTheDiagnosticModeIsOn_TheReportIsSent()
    {
        var handler = new CountingHttpMessageHandler();
        var mode = new FixedDiagnosticOperatingMode { IsDiagnostic = true };
        var reporter = CreateReporter(handler, diagnosticsAdmitted: false, mode);

        await reporter.ReportAsync(CreateReport(), CancellationToken.None);

        Assert.Equal(1, handler.Requests);
    }

    [Fact]
    public async Task WithoutDiagnosticMode_ThePayloadDoesNotCarrySessionIdentity()
    {
        var handler = new CountingHttpMessageHandler();
        var reporter = CreateReporter(handler, diagnosticsAdmitted: true);

        await reporter.ReportAsync(CreateReport(), CancellationToken.None);

        Assert.NotNull(handler.LastBody);
        using var document = JsonDocument.Parse(handler.LastBody!);
        Assert.False(document.RootElement.TryGetProperty("sessionId", out _));
        Assert.False(document.RootElement.TryGetProperty("operatingMode", out _));
    }

    [Fact]
    public async Task WithDiagnosticMode_ThePayloadCarriesSessionAndMode()
    {
        var handler = new CountingHttpMessageHandler();
        var mode = new FixedDiagnosticOperatingMode
        {
            IsDiagnostic = true,
            SessionId = "session-abc",
            ClientCorrelationId = "cid-abc",
        };
        var reporter = CreateReporter(handler, diagnosticsAdmitted: false, mode);

        await reporter.ReportAsync(CreateReport(), CancellationToken.None);

        Assert.NotNull(handler.LastBody);
        using var document = JsonDocument.Parse(handler.LastBody!);
        Assert.Equal("session-abc", document.RootElement.GetProperty("sessionId").GetString());
        Assert.Equal("cid-abc", document.RootElement.GetProperty("clientCorrelationId").GetString());
        Assert.Equal(DiagnosticOperatingModeNames.Diagnostic, document.RootElement.GetProperty("operatingMode").GetString());
    }

    private static ICaptureDiagnosticsReporter CreateReporter(
        CountingHttpMessageHandler handler,
        bool diagnosticsAdmitted,
        IDiagnosticOperatingMode? operatingMode = null,
        ICaptureTrackProcessingReader? trackProcessing = null)
    {
        var console = new InMemoryBrowserDebugConsole { DiagnosticsAdmitted = diagnosticsAdmitted };

        return new HttpCaptureDiagnosticsReporter(
            new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") },
            console,
            operatingMode ?? new FixedDiagnosticOperatingMode(),
            trackProcessing);
    }

    private static CaptureDiagnosticsReport CreateReport()
        => new(
            CaptureSeq: 1,
            Outcome: "no_speech",
            ArmedToOutcomeMs: 1200.0d,
            CaptureMode: "Listening",
            StartContext: "armed",
            CapturedChunks: 0,
            PeakAmplitude: 3);
}
