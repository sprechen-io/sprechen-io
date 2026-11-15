namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>Einsatz wenn kein Reporter konfiguriert ist. Diagnosen sind konstruktionsbedingt optional.</summary>
public sealed class NoOpCaptureDiagnosticsReporter : ICaptureDiagnosticsReporter
{
    public ValueTask ReportAsync(CaptureDiagnosticsReport report, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;
}
