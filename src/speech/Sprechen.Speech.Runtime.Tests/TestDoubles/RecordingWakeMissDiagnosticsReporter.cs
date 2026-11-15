namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

/// <summary>
/// Sammelt übergebene Wake-Miss-Berichte, damit Tests ihren Inhalt prüfen können.
/// </summary>
internal sealed class RecordingWakeMissDiagnosticsReporter : IWakeMissDiagnosticsReporter
{
    private readonly List<WakeMissDiagnosticsReport> _reports = [];

    public IReadOnlyList<WakeMissDiagnosticsReport> Reports => _reports;

    public ValueTask ReportAsync(WakeMissDiagnosticsReport report, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(report);
        _reports.Add(report);
        return ValueTask.CompletedTask;
    }
}
