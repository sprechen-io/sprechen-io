namespace Sprechen.Speech.TestDoubles;

public sealed class NoOpEarconPlayer : IEarconPlayer
{
    public ValueTask WarmupAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;

    public ValueTask PlayAsync(EarconId earcon, CancellationToken cancellationToken) => ValueTask.CompletedTask;

    public ValueTask PreviewAsync(EarconId earcon, CancellationToken cancellationToken) => ValueTask.CompletedTask;

    public ValueTask StopAllAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;

    public ValueTask StopLoopingAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;
}
