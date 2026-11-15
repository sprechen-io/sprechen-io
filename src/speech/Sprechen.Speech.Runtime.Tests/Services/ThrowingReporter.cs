using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.Runtime.Tests.Services;

internal sealed class ThrowingReporter : ICaptureDiagnosticsReporter
{
    public ValueTask ReportAsync(CaptureDiagnosticsReport report, CancellationToken cancellationToken)
        => throw new InvalidOperationException("diagnostics ingress unreachable");
}
