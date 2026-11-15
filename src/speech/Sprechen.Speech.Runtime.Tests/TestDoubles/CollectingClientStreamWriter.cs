using Grpc.Core;

namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

internal sealed class CollectingClientStreamWriter<T> : IClientStreamWriter<T> where T : class
{
    private readonly List<T> _messages = new();

    public IReadOnlyList<T> Messages => _messages;

    public bool Completed { get; private set; }

    public WriteOptions? WriteOptions { get; set; }

    public Task WriteAsync(T message)
    {
        ArgumentNullException.ThrowIfNull(message);

        _messages.Add(message);
        return Task.CompletedTask;
    }

    // Der Testversand unterstützt auch den Token-Aufruf und prüft den Abbruch vor dem Sammeln.
    public Task WriteAsync(T message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return WriteAsync(message);
    }

    public Task CompleteAsync()
    {
        Completed = true;
        return Task.CompletedTask;
    }
}


