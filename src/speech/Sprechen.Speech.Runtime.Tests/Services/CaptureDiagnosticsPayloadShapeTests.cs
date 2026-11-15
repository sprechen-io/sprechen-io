using System.Text.Json;
using Sprechen.Speech.App.BrowserInfrastructure.Services;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.Runtime.Tests.TestDoubles;

namespace Sprechen.Speech.Runtime.Tests.Services;

/// <summary>
/// Prüft die vollständige Schlüsselmenge und ausgewählte Werte des übertragenen Aufnahmeberichts
/// in beiden Diagnosemodi. Die JSON-Feldreihenfolge ist unerheblich. So bleiben Änderungen
/// an der Sendeseite unabhängig vom Empfänger sichtbar.
/// </summary>
public sealed class CaptureDiagnosticsPayloadShapeTests
{
    private static readonly string[] KeysInEveryMode =
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
        "faultKind",
        "observedFrames",
        "startCursorHandedOff",
        "lookbackBytes",
        "echoCancellation",
        "noiseSuppression",
        "autoGainControl",
    ];

    private static readonly string[] KeysOnlyInDiagnosticMode =
    [
        "sessionId",
        "clientCorrelationId",
        "operatingMode",
    ];

    [Theory]
    [InlineData("speech")]
    [InlineData("no_speech")]
    [InlineData("digital_silence")]
    [InlineData("cancelled")]
    [InlineData("failed")]
    public async Task EveryOutcomeInEveryMode_SendsExactlyTheRecordedKeysAndValues(string outcome)
    {
        var normal = await SendAsync(outcome, diagnostic: false);
        AssertKeys(normal, KeysInEveryMode);
        AssertRecordedValues(normal, outcome);

        var diagnostic = await SendAsync(outcome, diagnostic: true);
        AssertKeys(diagnostic, [.. KeysInEveryMode, .. KeysOnlyInDiagnosticMode]);
        AssertRecordedValues(diagnostic, outcome);
        Assert.Equal("\"session-9\"", Raw(diagnostic, "sessionId"));
        Assert.Equal("\"cid-9\"", Raw(diagnostic, "clientCorrelationId"));
    }

    [Fact]
    public async Task WithoutAWindow_ThePayloadCarriesTheDocumentedDefaults()
    {
        var handler = new CountingHttpMessageHandler();
        var reporter = CreateReporter(handler, new FixedDiagnosticOperatingMode());

        await reporter.ReportAsync(
            new CaptureDiagnosticsReport(
                CaptureSeq: 7,
                Outcome: "no_speech",
                ArmedToOutcomeMs: 1234.56d,
                CaptureMode: "ShortConfirmation",
                StartContext: "CleanListening",
                CapturedChunks: 3,
                PeakAmplitude: 512),
            CancellationToken.None);

        Assert.NotNull(handler.LastBody);
        var payload = JsonDocument.Parse(handler.LastBody!).RootElement;
        AssertKeys(payload, KeysInEveryMode);
        Assert.Equal("0", Raw(payload, "observedFrames"));
        Assert.Equal("false", Raw(payload, "startCursorHandedOff"));
        Assert.Equal("0", Raw(payload, "lookbackBytes"));
        Assert.Equal("null", Raw(payload, "faultKind"));
        Assert.Equal("0", Raw(payload, "estimatedFloor"));
        Assert.Equal("0", Raw(payload, "appliedOnsetThreshold"));
        Assert.Equal("null", Raw(payload, "turnId"));
    }

    private static void AssertKeys(JsonElement payload, string[] expected)
    {
        var actual = payload.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal).ToArray();

        Assert.Equal(expected.Order(StringComparer.Ordinal).ToArray(), actual);
    }

    private static void AssertRecordedValues(JsonElement payload, string outcome)
    {
        Assert.Equal("\"capture_report\"", Raw(payload, "event"));
        Assert.Equal("7", Raw(payload, "captureSeq"));
        Assert.True(DateTimeOffset.TryParse(payload.GetProperty("ts").GetString(), out _));
        Assert.Equal("\"turn-7\"", Raw(payload, "turnId"));
        Assert.Equal("\"" + outcome + "\"", Raw(payload, "outcome"));
        Assert.Equal("1234.6", Raw(payload, "armedToOutcomeMs"));
        Assert.Equal("\"ShortConfirmation\"", Raw(payload, "captureMode"));
        Assert.Equal("\"CleanListening\"", Raw(payload, "startContext"));
        Assert.Equal("3", Raw(payload, "capturedChunks"));
        Assert.Equal("512", Raw(payload, "peakAmplitude"));
        Assert.Equal("42", Raw(payload, "estimatedFloor"));
        Assert.Equal("96", Raw(payload, "appliedOnsetThreshold"));
        Assert.Equal(outcome == "failed" ? "\"TimeoutException\"" : "null", Raw(payload, "faultKind"));
        Assert.Equal("9", Raw(payload, "observedFrames"));
        Assert.Equal("true", Raw(payload, "startCursorHandedOff"));
        Assert.Equal("640", Raw(payload, "lookbackBytes"));
        Assert.Equal("null", Raw(payload, "echoCancellation"));
        Assert.Equal("null", Raw(payload, "noiseSuppression"));
        Assert.Equal("null", Raw(payload, "autoGainControl"));
    }

    private static string Raw(JsonElement payload, string name)
        => payload.GetProperty(name).GetRawText();

    private static ICaptureDiagnosticsReporter CreateReporter(
        CountingHttpMessageHandler handler,
        IDiagnosticOperatingMode operatingMode)
        => new HttpCaptureDiagnosticsReporter(
            new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") },
            new InMemoryBrowserDebugConsole { DiagnosticsAdmitted = true },
            operatingMode,
            trackProcessing: null);

    private static async Task<JsonElement> SendAsync(string outcome, bool diagnostic)
    {
        var handler = new CountingHttpMessageHandler();
        var mode = new FixedDiagnosticOperatingMode
        {
            IsDiagnostic = diagnostic,
            SessionId = "session-9",
            ClientCorrelationId = "cid-9",
        };
        var reporter = CreateReporter(handler, mode);

        await reporter.ReportAsync(ReportFor(outcome), CancellationToken.None);

        Assert.NotNull(handler.LastBody);
        return JsonDocument.Parse(handler.LastBody!).RootElement.Clone();
    }

    private static CaptureDiagnosticsReport ReportFor(string outcome)
        => new(
            CaptureSeq: 7,
            Outcome: outcome,
            ArmedToOutcomeMs: 1234.56d,
            CaptureMode: "ShortConfirmation",
            StartContext: "CleanListening",
            CapturedChunks: 3,
            PeakAmplitude: 512,
            FaultKind: outcome == "failed" ? "TimeoutException" : null,
            Window: new CaptureWindowObservation(9, true, 640),
            EstimatedFloor: 42,
            AppliedOnsetThreshold: 96,
            TurnId: "turn-7");
}
