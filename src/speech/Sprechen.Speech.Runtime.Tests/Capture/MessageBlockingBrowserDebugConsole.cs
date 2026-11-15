namespace Sprechen.Speech.Runtime.Tests.Capture;

internal sealed class MessageBlockingBrowserDebugConsole(string blockingMessage) : IBrowserDebugConsole
{
    internal TaskCompletionSource BlockingWriteStarted { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal TaskCompletionSource ContinueBlockingWrite { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal TaskCompletionSource CaptureArmed { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public ValueTask<bool> GetEnabledAsync(CancellationToken cancellationToken) => ValueTask.FromResult(false);

    public ValueTask<bool> GetDiagnosticsAdmittedAsync(CancellationToken cancellationToken) =>
        ValueTask.FromResult(false);

    public ValueTask SetEnabledAsync(bool enabled, CancellationToken cancellationToken) =>
        ValueTask.CompletedTask;

    public ValueTask SetDiagnosticSessionAsync(
        DiagnosticSessionState state,
        CancellationToken cancellationToken) => ValueTask.CompletedTask;

    public ValueTask WriteAsync(string scope, string message, CancellationToken cancellationToken)
    {
        if (message.StartsWith("capture armed ", StringComparison.Ordinal))
        {
            CaptureArmed.TrySetResult();
        }

        if (!string.Equals(message, blockingMessage, StringComparison.Ordinal))
        {
            return ValueTask.CompletedTask;
        }

        BlockingWriteStarted.TrySetResult();
        return new ValueTask(ContinueBlockingWrite.Task);
    }
}
