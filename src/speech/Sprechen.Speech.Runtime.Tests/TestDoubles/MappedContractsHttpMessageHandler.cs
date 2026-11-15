using System.Net;

namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

internal sealed class MappedContractsHttpMessageHandler : HttpMessageHandler
{
    private readonly Dictionary<string, Func<Task<Stream>>> _responses = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _callCounts = new(StringComparer.Ordinal);

    public void Add(string path, Func<Stream> response)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(response);

        _responses[path] = () => Task.FromResult(response());
    }

    public int GetCallCount(string path)
    {
        return _callCounts.TryGetValue(path, out var count) ? count : 0;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var path = request.RequestUri?.AbsolutePath;
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException("Contracts API test request did not contain a path.");
        }

        if (!_responses.TryGetValue(path, out var responseFactory))
        {
            throw new InvalidOperationException($"Unsupported contracts API test path '{path}'.");
        }

        _callCounts[path] = GetCallCount(path) + 1;
        var stream = await responseFactory().ConfigureAwait(false);
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(stream),
            RequestMessage = request
        };
    }
}
