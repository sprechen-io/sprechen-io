namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

internal sealed class FirstWriteBlockingBrowserDebugConsole : IBrowserDebugConsole
{
    private int _writeCount;

    internal TaskCompletionSource FirstWriteStarted { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal TaskCompletionSource ContinueFirstWrite { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public ValueTask<bool> GetEnabledAsync(CancellationToken cancellationToken) => ValueTask.FromResult(false);

    public ValueTask<bool> GetDiagnosticsAdmittedAsync(CancellationToken cancellationToken) => ValueTask.FromResult(false);

    public ValueTask SetEnabledAsync(bool enabled, CancellationToken cancellationToken) => ValueTask.CompletedTask;

    public ValueTask SetDiagnosticSessionAsync(DiagnosticSessionState state, CancellationToken cancellationToken) =>
        ValueTask.CompletedTask;

    public ValueTask WriteAsync(string scope, string message, CancellationToken cancellationToken)
    {
        if (Interlocked.Increment(ref _writeCount) != 1)
        {
            return ValueTask.CompletedTask;
        }

        FirstWriteStarted.TrySetResult();
        return new ValueTask(ContinueFirstWrite.Task);
    }
}
