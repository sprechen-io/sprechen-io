using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.Runtime.Tests.Services;

internal sealed class CapturingReporter : ICaptureDiagnosticsReporter
{
    public List<CaptureDiagnosticsReport> Reports { get; } = [];

    public ValueTask ReportAsync(CaptureDiagnosticsReport report, CancellationToken cancellationToken)
    {
        Reports.Add(report);
        return ValueTask.CompletedTask;
    }
}
