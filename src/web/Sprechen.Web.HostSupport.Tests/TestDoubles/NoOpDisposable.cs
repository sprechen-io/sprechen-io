namespace Sprechen.Web.HostSupport.Tests.TestDoubles;

internal sealed class NoOpDisposable : IDisposable
{
    public static NoOpDisposable Instance { get; } = new();

    public void Dispose()
    {
    }
}
