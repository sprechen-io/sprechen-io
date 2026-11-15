using System.Text.Json;
using Sprechen.Speech.Runtime.Tests.TestDoubles;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class HttpDialogDecisionDiagnosticsReporterTests
{
    [Fact]
    public async Task WithoutDiagnosticMode_NothingLeavesTheDevice()
    {
        var handler = new CountingHttpMessageHandler();
        var reporter = new HttpDialogDecisionDiagnosticsReporter(
            new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") },
            new FixedDiagnosticOperatingMode { IsDiagnostic = false });

        await reporter.RecordAsync(DialogDecisions.SpellingBuffer(3, 1), CancellationToken.None);

        Assert.Equal(0, handler.Requests);
    }

    [Fact]
    public async Task WithDiagnosticMode_TheDecisionReachesTheServerWithSessionModeAndRun()
    {
        var handler = new CountingHttpMessageHandler();
        var mode = new FixedDiagnosticOperatingMode
        {
            IsDiagnostic = true,
            SessionId = "session-abc",
            ClientCorrelationId = "cid-abc",
        };
        var reporter = new HttpDialogDecisionDiagnosticsReporter(
            new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") },
            mode);

        await reporter.RecordAsync(DialogDecisions.SpellingBuffer(3, 1), CancellationToken.None);

        Assert.Equal(1, handler.Requests);
        Assert.NotNull(handler.LastBody);
        using var document = JsonDocument.Parse(handler.LastBody!);
        var root = document.RootElement;
        Assert.Equal("dialog_decision", root.GetProperty("event").GetString());
        Assert.Equal(DialogDecisionNames.SpellingBuffer, root.GetProperty("name").GetString());
        Assert.Equal("session-abc", root.GetProperty("sessionId").GetString());
        Assert.Equal("cid-abc", root.GetProperty("clientCorrelationId").GetString());
        Assert.Equal(DiagnosticOperatingModeNames.Diagnostic, root.GetProperty("operatingMode").GetString());
        Assert.Equal("3", root.GetProperty("fields")[0].GetProperty("value").GetString());
    }
}
