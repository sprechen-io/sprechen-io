namespace Sprechen.Speech.TestDoubles;

public sealed class NoOpDisposable : IDisposable
{
    public static NoOpDisposable Instance { get; } = new();

    public void Dispose()
    {
    }
}
