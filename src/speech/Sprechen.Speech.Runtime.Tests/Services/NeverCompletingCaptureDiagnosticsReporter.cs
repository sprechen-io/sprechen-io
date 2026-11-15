namespace Sprechen.Speech.Runtime.Tests.Services;

internal sealed class NeverCompletingCaptureDiagnosticsReporter : ICaptureDiagnosticsReporter
{
    internal TaskCompletionSource ReportStarted { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public ValueTask ReportAsync(CaptureDiagnosticsReport report, CancellationToken cancellationToken)
    {
        ReportStarted.TrySetResult();
        return new ValueTask(Task.Delay(Timeout.InfiniteTimeSpan));
    }
}
